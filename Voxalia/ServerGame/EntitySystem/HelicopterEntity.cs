﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.Shared;
using Voxalia.ServerGame.WorldSystem;
using BEPUutilities;
using BEPUphysics.Constraints;
using BEPUphysics.Constraints.SingleEntity;

namespace Voxalia.ServerGame.EntitySystem
{
    public class HelicopterEntity : VehicleEntity
    {
        public HelicopterEntity(string heli, Region tregion)
            : base(heli, tregion)
        {
            SetMass(1000);
        }

        public override EntityType GetEntityType()
        {
            return EntityType.HELICOPTER;
        }

        public override byte[] GetSaveBytes()
        {
            // TODO: Save properly!
            return null;
        }

        public bool ILeft = false;
        public bool IRight = false;

        public bool Sprint = false;
        public bool Walk = false;

        public float ForwBack = 0;
        public float RightLeft = 0;

        public override void SpawnBody()
        {
            base.SpawnBody();
            Motion = new HelicopterMotionConstraint(this);
            TheRegion.PhysicsWorld.Add(Motion);
        }
        
        // TODO: Customizable, networked!
        public float LiftStrength
        {
            get
            {
                return GetMass() * 20f;
            }
        }

        public float FallStrength
        {
            get
            {
                return GetMass() * 9f;
            }
        }

        public HelicopterMotionConstraint Motion;

        public override void Tick()
        {
            base.Tick();
        }

        public class HelicopterMotionConstraint : SingleEntityConstraint
        {
            HelicopterEntity Helicopter;
            
            public HelicopterMotionConstraint(HelicopterEntity heli)
            {
                Helicopter = heli;
                Entity = heli.Body;
            }

            public override void ExclusiveUpdate()
            {
                if (Helicopter.DriverSeat.Sitter == null)
                {
                    return; // Don't fly when there's nobody driving this!
                }
                // Collect the helicopter's relative "up" vector
                Vector3 up = Quaternion.Transform(Vector3.UnitZ, Entity.Orientation);
                if (Helicopter.Sprint && !Helicopter.Walk)
                {
                    // Apply our maximum upward strength.
                    Vector3 upvel = up * Helicopter.LiftStrength * Delta;
                    Entity.ApplyLinearImpulse(ref upvel);
                }
                else if (Helicopter.Walk && !Helicopter.Sprint)
                {
                    // Apply the minimum lift strength allowed to sortof just fall downward.
                    Vector3 upvel = up * Helicopter.FallStrength * Delta;
                    Entity.ApplyLinearImpulse(ref upvel);
                }
                else // FlyHover
                {
                    // Apply the amount of force necessary to counteract downward force, within a limit.
                    // POTENTIAL: Adjust according to orientation?
                    Vector3 upvel = up * Math.Min(Helicopter.LiftStrength, -(Entity.LinearVelocity.Z + Entity.Space.ForceUpdater.Gravity.Z) * Entity.Mass) * Delta;
                    Entity.ApplyLinearImpulse(ref upvel);
                }
                // Rotate slightly to move in a direction.
                // At the same time, fight against existing rotation.
                Vector3 VecUp = new Vector3(Helicopter.RightLeft * 0.2f, Helicopter.ForwBack * -0.2f, 1);
                // TODO: Simplify yawrel calculation.
                float tyaw = (float)(Utilities.MatrixToAngles(Matrix.CreateFromQuaternion(Entity.Orientation)).Z * Utilities.PI180);
                Quaternion yawrel = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, tyaw);
                VecUp = Quaternion.Transform(VecUp, yawrel);
                VecUp.Y = -VecUp.Y;
                VecUp.Normalize();
                Vector3 axis = Vector3.Cross(VecUp, up);
                float len = axis.Length();
                if (len > 0)
                {
                    axis /= len;
                    float angle = (float)Math.Asin(len);
                    if (!float.IsNaN(angle))
                    {
                        float avel = Vector3.Dot(Entity.AngularVelocity, axis);
                        Vector3 torque = axis * ((-angle) - 0.3f * avel);
                        torque *= Entity.Mass * Delta * 30;
                        Entity.ApplyAngularImpulse(ref torque);
                    }
                }
                // Spin in place
                float rotation = (Helicopter.IRight ? -1f : 0f) + (Helicopter.ILeft ? 1f : 0f);
                if (rotation * rotation > 0f)
                {
                    Vector3 rot = new Vector3(0, 0, rotation * 15f * Delta * Entity.Mass);
                    Entity.ApplyAngularImpulse(ref rot);
                }
                // Apply air drag
                Entity.ModifyLinearDamping(0.3f); // TODO: arbitrary constant
                Entity.ModifyAngularDamping(0.3f); // TODO: arbitrary constant
                Entity.ModifyAngularDamping(0.3f); // TODO: arbitrary constant
                // Ensure we're active if flying!
                Entity.ActivityInformation.Activate();
            }

            public override float SolveIteration()
            {
                return 0; // Do nothing
            }

            float Delta;

            public override void Update(float dt)
            {
                Delta = dt;
            }
        }

        public override void HandleInput(CharacterEntity character)
        {
            ILeft = character.ItemLeft;
            IRight = character.ItemRight;
            ForwBack = character.YMove;
            RightLeft = character.XMove;
            Sprint = character.Sprint;
            Walk = character.Walk;
        }
    }
}
