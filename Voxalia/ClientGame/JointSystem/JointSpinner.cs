﻿using Voxalia.ClientGame.EntitySystem;
using Voxalia.Shared;
using BEPUphysics.Constraints.TwoEntity;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints;

namespace Voxalia.ClientGame.JointSystem
{
    public class JointSpinner : BaseJoint
    {
        public JointSpinner(PhysicsEntity e1, PhysicsEntity e2, Location dir)
        {
            Ent1 = e1;
            Ent2 = e2;
            Direction = dir;
        }

        public override SolverUpdateable GetBaseJoint()
        {
            return new RevoluteAngularJoint(Ent1.Body, Ent2.Body, Direction.ToBVector());
        }

        public Location Direction;
    }
}
