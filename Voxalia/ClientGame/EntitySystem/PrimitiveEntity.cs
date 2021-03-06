//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.Shared;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.Shared.Collision;
using OpenTK;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ClientGame.EntitySystem
{
    public abstract class PrimitiveEntity : Entity
    {
        public PrimitiveEntity(Region tregion, bool cast_shadows)
            : base(tregion, true, cast_shadows)
        {
        }

        public Location Gravity = Location.Zero;

        public List<long> NoCollide = new List<long>(); // TODO: Populate me via packets

        public bool FilterHandle(BEPUphysics.BroadPhaseEntries.BroadPhaseEntry entry)
        {
            if (entry is BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable)
            {
                long eid = ((PhysicsEntity)((BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable)entry).Entity.Tag).EID;
                if (NoCollide.Contains(eid))
                {
                    return false;
                }
            }
            if (entry.CollisionRules.Group == CollisionUtil.NonSolid)
            {
                return false;
            }
            return true;
        }

        public override void Tick()
        {
            SetVelocity(Velocity * 0.99f + Gravity * TheClient.Delta);
            if (Velocity.LengthSquared() > 0)
            {
                CollisionResult cr = TheClient.TheRegion.Collision.CuboidLineTrace(Scale, GetPosition(), GetPosition() + Velocity * TheClient.Delta, FilterHandle);
                SetPosition(cr.Position);
            }
        }

        public abstract void Spawn();

        public abstract void Destroy();

        public Location Position;

        public Location Velocity;

        public BEPUutilities.Quaternion Angles;

        public Matrix4d GetOrientationMatrix()
        {
            return Matrix4d.CreateFromQuaternion(new Quaterniond(Angles.X, Angles.Y, Angles.Z, Angles.W));
        }

        public override BEPUutilities.Quaternion GetOrientation()
        {
            return Angles;
        }

        public override void SetOrientation(BEPUutilities.Quaternion quat)
        {
            Angles = quat;
        }

        public Location Scale;

        public override Location GetPosition()
        {
            return Position;
        }

        public override void SetPosition(Location pos)
        {
            Position = pos;
        }

        public virtual void SetVelocity(Location vel)
        {
            Velocity = vel;
        }

        public const int PrimitiveNetDataLength = 24 + 24 + 16 + 24 + 24 + 4;
    }
}
