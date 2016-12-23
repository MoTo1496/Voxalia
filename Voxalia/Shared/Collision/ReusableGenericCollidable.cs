//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Entities;

namespace Voxalia.Shared.Collision
{
    public class ReusableGenericCollidable<T> : ConvexCollidable<T> where T: ConvexShape
    {
        public ReusableGenericCollidable(T tshape)
        {
            shape = tshape;
        }

        protected override void UpdateBoundingBoxInternal(double dt)
        {
            Shape.GetBoundingBox(ref worldTransform, out boundingBox);
            ExpandBoundingBox(ref boundingBox, dt);
        }

        public void SetEntity(Entity e)
        {
            Entity = e;
        }
    }
}
