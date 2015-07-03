﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.Joints;
using Voxalia.ServerGame.WorldSystem;

namespace Voxalia.ServerGame.JointSystem
{
    public class JointBallSocket : BaseJoint
    {
        public JointBallSocket(PhysicsEntity e1, PhysicsEntity e2, Location pos)
        {
            One = e1;
            Two = e2;
            Position = pos;
        }

        public override TwoEntityConstraint GetBaseJoint()
        {
            return new BallSocketJoint(Ent1.Body, Ent2.Body, Position.ToBVector());
        }

        public override bool ApplyVar(World tworld, string var, string value)
        {
            switch (var)
            {
                case "position":
                    Position = Location.FromString(value);
                    return true;
                default:
                    return base.ApplyVar(tworld, var, value);
            }
        }

        public Location Position;
    }
}