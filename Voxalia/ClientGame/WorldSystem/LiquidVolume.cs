//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.UpdateableSystems.ForceFields;
using BEPUutilities;
using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.UpdateableSystems;
using Voxalia.Shared;
using BEPUphysics.BroadPhaseEntries;
using Voxalia.Shared.Collision;
using BEPUutilities.DataStructures;
using FreneticGameCore;

namespace Voxalia.ClientGame.WorldSystem
{
    public class LiquidVolume : Updateable, IDuringForcesUpdateable
    {
        public Region TheRegion;

        public LiquidVolume(Region tregion)
        {
            TheRegion = tregion;
        }

        public void Update(double dt)
        {
            ReadOnlyList<Entity> ents = TheRegion.PhysicsWorld.Entities; // TODO: Direct/raw read?
            TheRegion.PhysicsWorld.ParallelLooper.ForLoop(0, ents.Count, (i) =>
            {
                ApplyLiquidForcesTo(ents[i], dt);
            });
        }

        void ApplyLiquidForcesTo(Entity e, double dt)
        {
            if (e.Mass <= 0)
            {
                return;
            }
            Location min = new Location(e.CollisionInformation.BoundingBox.Min);
            Location max = new Location(e.CollisionInformation.BoundingBox.Max);
            min = min.GetBlockLocation();
            max = max.GetUpperBlockBorder();
            for (int x = (int)min.X; x < max.X; x++)
            {
                for (int y = (int)min.Y; y < max.Y; y++)
                {
                    for (int z = (int)min.Z; z < max.Z; z++)
                    {
                        Location c = new Location(x, y, z);
                        Material mat = (Material)TheRegion.GetBlockInternal(c).BlockMaterial;
                        if (mat.GetSolidity() != MaterialSolidity.LIQUID)
                        {
                            continue;
                        }
                        // TODO: Account for block shape?
                        double vol = e.CollisionInformation.Shape.Volume;
                        double dens = (e.Mass / vol);
                        double WaterDens = 5; // TODO: Read from material. // TODO: Sanity of values.
                        float modifier = (float)(WaterDens / dens);
                        float submod = 0.125f;
                        // TODO: Tracing accuracy!
                        Vector3 impulse = -(TheRegion.PhysicsWorld.ForceUpdater.Gravity + TheRegion.GravityNormal.ToBVector() * 0.4f) * e.Mass * dt * modifier * submod;
                        // TODO: Don't apply small-scale logic (the loops below) if the entity scale is big enough to irrelevantize it!
                        for (double x2 = 0.25; x2 < 1.0; x2 += 0.5)
                        {
                            for (double y2 = 0.25; y2 < 1.0; y2 += 0.5)
                            {
                                for (double z2 = 0.25; z2 < 1.0; z2 += 0.5)
                                {
                                    Location lc = c + new Location(x2, y2, z2);
                                    Vector3 center = lc.ToBVector();
                                    if (e.CollisionInformation.RayCast(new Ray(center, new Vector3(0, 0, 1)), 0.01f, out RayHit rh)) // TODO: Efficiency!
                                    {
                                        e.ApplyImpulse(ref center, ref impulse);
                                        e.ModifyLinearDamping(mat.GetSpeedMod());
                                        e.ModifyAngularDamping(mat.GetSpeedMod());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
