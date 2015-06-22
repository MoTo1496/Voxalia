﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.Shared;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace Voxalia.ClientGame.WorldSystem
{
    public partial class Chunk
    {
        public VBO _VBO = null;

        public void CreateVBO()
        {
            if (_VBO != null)
            {
                _VBO.Destroy();
            }
            List<Vector3> Vertices = new List<Vector3>(CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE * 6); // TODO: Make me an array?
            List<Vector3> TCoords = new List<Vector3>(CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE * 6);
            List<Vector3> Norms = new List<Vector3>(CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE * 6);
            Vector3 ppos = WorldPosition.ToOVector();
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < CHUNK_SIZE; z++)
                    {
                        ushort c = GetBlockAt(x, y, z);
                        ushort zp = z + 1 < CHUNK_SIZE ? GetBlockAt(x, y, z + 1) : (ushort)0;
                        ushort zm = z - 1 > 0 ? GetBlockAt(x, y, z - 1) : (ushort)0;
                        ushort yp = y + 1 < CHUNK_SIZE ? GetBlockAt(x, y + 1, z) : (ushort)0;
                        ushort ym = y - 1 > 0 ? GetBlockAt(x, y - 1, z) : (ushort)0;
                        ushort xp = x + 1 < CHUNK_SIZE ? GetBlockAt(x + 1, y, z) : (ushort)0;
                        ushort xm = x - 1 > 0 ? GetBlockAt(x - 1, y, z) : (ushort)0;
                        ushort cm = MaterialHelpers.GetMaterialHardMat(c);
                        ushort zpm = MaterialHelpers.GetMaterialHardMat(zp);
                        ushort zmm = MaterialHelpers.GetMaterialHardMat(zm);
                        ushort ypm = MaterialHelpers.GetMaterialHardMat(yp);
                        ushort ymm = MaterialHelpers.GetMaterialHardMat(ym);
                        ushort xpm = MaterialHelpers.GetMaterialHardMat(xp);
                        ushort xmm = MaterialHelpers.GetMaterialHardMat(xm);
                        if (((Material)cm).IsOpaque() || ((Material)cm).IsSolid()) // TODO: Better check. OccupiesFullBlock()?
                        {
                            Vector3 pos = new Vector3(ppos.X + x, ppos.Y + y, ppos.Z + z);
                            if (!((Material)zpm).IsOpaque())
                            {
                                int tID_TOP = 0;// ((Material)cm).TextureID(MaterialSide.TOP);
                                for (int i = 0; i < 6; i++)
                                {
                                    Norms.Add(new Vector3(0, 0, 1));
                                }
                                TCoords.Add(new Vector3(0, 0, tID_TOP));
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z + 1));
                                TCoords.Add(new Vector3(1, 0, tID_TOP));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z + 1));
                                TCoords.Add(new Vector3(1, 1, tID_TOP));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                                TCoords.Add(new Vector3(0, 0, tID_TOP));
                                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z + 1));
                                TCoords.Add(new Vector3(1, 1, tID_TOP));
                                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                                TCoords.Add(new Vector3(0, 1, tID_TOP));
                                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z + 1));
                            }
                            // TODO: zm, yp, ym, xp, xm
                            // TODO: Else, handle special case direction data
                        }
                    }
                }
            }
            if (Vertices.Count == 0)
            {
                _VBO = null;
                return;
            }
            List<uint> inds = new List<uint>(Vertices.Count); // TODO: VBO Array input instead of a list
            List<Vector4> bWeight = new List<Vector4>(Vertices.Count);
            List<Vector4> bID = new List<Vector4>(Vertices.Count);
            List<Vector4> bWeight2 = new List<Vector4>(Vertices.Count);
            List<Vector4> bID2 = new List<Vector4>(Vertices.Count);
            List<Vector4> Colors = new List<Vector4>(Vertices.Count);
            for (uint i = 0; i < inds.Count; i++)
            {
                inds.Add(i);
                bWeight.Add(Vector4.Zero);
                bID.Add(Vector4.Zero);
                bWeight2.Add(Vector4.Zero);
                bID2.Add(Vector4.Zero);
                Colors.Add(Vector4.One);
            }
            _VBO = new VBO();
            _VBO.Indices = inds;
            _VBO.Vertices = Vertices;
            _VBO.TexCoords = TCoords;
            _VBO.BoneWeights = bWeight;
            _VBO.BoneIDs = bID;
            _VBO.BoneWeights2 = bWeight2;
            _VBO.BoneIDs2 = bID2;
            _VBO.Colors = Colors;
            _VBO.Normals = Norms;
            Vector3[] vecs = Vertices.ToArray();
            _VBO.GenerateVBO();
        }

        public void Destroy()
        {
            if (_VBO != null)
            {
                _VBO.Destroy();
            }
        }

        public void Render()
        {
            if (_VBO != null)
            {
                Matrix4 mat = Matrix4.Identity;
                GL.UniformMatrix4(2, false, ref mat);
                _VBO.Render(true);
            }
        }
    }
}
