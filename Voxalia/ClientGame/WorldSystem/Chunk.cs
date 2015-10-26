﻿using System;
using System.Collections.Generic;
using Voxalia.Shared;
using BEPUutilities;
using BEPUphysics.BroadPhaseEntries;

namespace Voxalia.ClientGame.WorldSystem
{
    public partial class Chunk
    {
        public const int CHUNK_SIZE = 30;

        public Region OwningRegion = null;

        public Location WorldPosition;

        public int CSize = CHUNK_SIZE;

        public int PosMultiplier;

        public Chunk(int posMult)
        {
            PosMultiplier = posMult;
            CSize = CHUNK_SIZE / posMult;
            BlocksInternal = new BlockInternal[CSize * CSize * CSize];
        }

        public BlockInternal[] BlocksInternal;
        
        public int BlockIndex(int x, int y, int z)
        {
            return z * CSize * CSize + y * CSize + x;
        }

        public void SetBlockAt(int x, int y, int z, BlockInternal mat)
        {
            BlocksInternal[BlockIndex(x, y, z)] = mat;
        }

        public BlockInternal GetBlockAt(int x, int y, int z)
        {
            return BlocksInternal[BlockIndex(x, y, z)];
        }

        static Location[] slocs = new Location[] { new Location(1, 0, 0), new Location(-1, 0, 0), new Location(0, 1, 0),
            new Location(0, -1, 0), new Location(0, 0, 1), new Location(0, 0, -1) };

        public void UpdateSurroundingsFully()
        {
            foreach (Location loc in slocs)
            {
                Chunk ch = OwningRegion.GetChunk(WorldPosition + loc);
                if (ch != null)
                {
                    ch.AddToWorld();
                    ch.CreateVBO();
                }
            }
        }

        public void CalculateLighting()
        {
            if (OwningRegion.TheClient.CVars.r_fallbacklighting.ValueB)
            {
                for (int x = 0; x < CSize; x++)
                {
                    for (int y = 0; y < CSize; y++)
                    {
                        byte light = 255;
                        for (int z = CSize - 1; z >= 0; z--)
                        {
                            /*Material mat = (Material)GetBlockAt(x, y, z).BlockMaterial;
                            if (mat.IsOpaque())
                            {
                                light = 0;
                            }
                            if (mat.RendersAtAll())
                            {
                                light /= 2;
                            }*/
                            BlocksInternal[BlockIndex(x, y, z)].BlockLocalData = light;
                        }
                    }
                }
            }
        }

        public StaticMesh CalculateChunkShape()
        {
            List<Vector3> Vertices = new List<Vector3>(CSize * CSize * CSize * 6); // TODO: Make this an array?
            Vector3 ppos = WorldPosition.ToBVector() * 30;
            for (int x = 0; x < CSize; x++)
            {
                for (int y = 0; y < CSize; y++)
                {
                    for (int z = 0; z < CSize; z++)
                    {
                        BlockInternal c = GetBlockAt(x, y, z);
                        if (((Material)c.BlockMaterial).IsSolid())
                        {
                            // TODO: Handle ALL blocks against the surface when low-LOD?
                            BlockInternal zp = z + 1 < CSize ? GetBlockAt(x, y, z + 1) : OwningRegion.GetBlockInternal(new Location(ppos) + new Location(x, y, 30));
                            BlockInternal zm = z > 0 ? GetBlockAt(x, y, z - 1) : OwningRegion.GetBlockInternal(new Location(ppos) + new Location(x, y, -1));
                            BlockInternal yp = y + 1 < CSize ? GetBlockAt(x, y + 1, z) : OwningRegion.GetBlockInternal(new Location(ppos) + new Location(x, 30, z));
                            BlockInternal ym = y > 0 ? GetBlockAt(x, y - 1, z) : OwningRegion.GetBlockInternal(new Location(ppos) + new Location(x, -1, z));
                            BlockInternal xp = x + 1 < CSize ? GetBlockAt(x + 1, y, z) : OwningRegion.GetBlockInternal(new Location(ppos) + new Location(30, y, z));
                            BlockInternal xm = x > 0 ? GetBlockAt(x - 1, y, z) : OwningRegion.GetBlockInternal(new Location(ppos) + new Location(-1, y, z));
                            bool zps = ((Material)zp.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[zp.BlockData].OccupiesTOP();
                            bool zms = ((Material)zm.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[zm.BlockData].OccupiesBOTTOM();
                            bool xps = ((Material)xp.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[xp.BlockData].OccupiesXP();
                            bool xms = ((Material)xm.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[xm.BlockData].OccupiesXM();
                            bool yps = ((Material)yp.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[yp.BlockData].OccupiesYP();
                            bool yms = ((Material)ym.BlockMaterial).IsSolid() && BlockShapeRegistry.BSD[ym.BlockData].OccupiesYM();
                            Vector3 pos = new Vector3(x, y, z);
                            List<Vector3> vecsi = BlockShapeRegistry.BSD[c.BlockData].GetVertices(pos, xps, xms, yps, yms, zps, zms);
                            foreach (Vector3 vec in vecsi)
                            {
                                Vertices.Add(vec * PosMultiplier + ppos);
                            }
                        }
                    }
                }
            }
            if (Vertices.Count == 0)
            {
                return null;
            }
            int[] inds = new int[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                inds[i] = i;
            }
            Vector3[] vecs = Vertices.ToArray();
            StaticMesh sm = new StaticMesh(vecs, inds);
            return sm;
        }

        public StaticMesh worldObject = null;

        public ASyncScheduleItem adding = null;

        public void AddToWorld(Action callback = null)
        {
            if (adding != null)
            {
                ASyncScheduleItem item = OwningRegion.TheClient.Schedule.AddASyncTask(() => AddInternal(callback));
                adding = adding.ReplaceOrFollowWith(item);
            }
            else
            {
                adding = OwningRegion.TheClient.Schedule.StartASyncTask(() => AddInternal(callback));
            }
        }

        public void Destroy()
        {
            if (worldObject != null)
            {
                OwningRegion.PhysicsWorld.Remove(worldObject);
            }
            if (_VBO != null)
            {
                _VBO.Destroy();
            }
        }

        public bool LOADING = false;
        public bool PROCESSED = false;
        public bool PRED = false;

        void AddInternal(Action callback)
        {
            StaticMesh tregionObject = CalculateChunkShape();
            OwningRegion.TheClient.Schedule.ScheduleSyncTask(() =>
            {
                if (worldObject != null)
                {
                    OwningRegion.RemoveChunkQuiet(worldObject);
                }
                worldObject = tregionObject;
                if (worldObject != null)
                {
                    OwningRegion.AddChunk(worldObject);
                }
                if (callback != null)
                {
                    callback.Invoke();
                }
            });
        }
    }
}
