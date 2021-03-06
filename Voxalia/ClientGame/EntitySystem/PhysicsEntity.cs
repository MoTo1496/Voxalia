//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using BEPUutilities;
using BEPUphysics.CollisionShapes;
using Voxalia.ClientGame.JointSystem;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.BroadPhaseEntries;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.Shared.Collision;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ClientGame.EntitySystem
{
    public abstract class PhysicsEntity: Entity
    {
        public PhysicsEntity(Region tregion, bool ticks, bool cast_shadows)
            : base(tregion, ticks, cast_shadows)
        {
            Gravity = new Location(TheRegion.PhysicsWorld.ForceUpdater.Gravity);
            CGroup = CollisionUtil.Solid;
        }

        /// <summary>
        /// All information on the physical version of this entity as it exists within the physics world.
        /// </summary>
        public BEPUphysics.Entities.Entity Body = null;

        /// <summary>
        /// The mass of the entity.
        /// </summary>
        internal float Mass = 0f;

        /// <summary>
        /// The friction value of the entity.
        /// </summary>
        internal float Friction = 0.5f;

        /// <summary>
        /// The bounciness (restitution coefficient) of the entity.
        /// </summary>
        internal float Bounciness = 0f;

        /// <summary>
        /// The gravity power of this entity.
        /// </summary>
        internal Location Gravity;

        public bool GenBlockShadows = false;

        /// <summary>
        /// Whether this entity can rotate. Only updated at SpawnBody().
        /// </summary>
        internal bool CanRotate = true;

        /// <summary>
        /// The position, rotation, etc. of this entity.
        /// TODO: Position + rotation as variables, rather than a matrix.
        /// </summary>
        internal Matrix WorldTransform = Matrix.Identity;

        /// <summary>
        /// The linear velocity of this entity.
        /// </summary>
        internal Location LVel = Location.Zero;

        /// <summary>
        /// The angular velocity of this entity.
        /// </summary>
        internal Location AVel = Location.Zero;

        /// <summary>
        /// The shape of the entity.
        /// </summary>
        public EntityShape Shape = null;
        
        public Location InternalOffset;

        public Location ServerKnownLocation;

        public CollisionGroup CGroup;

        bool IgnoreEverythingButWater(BroadPhaseEntry entry)
        {
            return entry.CollisionRules.Group == CollisionUtil.Water;
        }

        //private List<KeyValuePair<PhysicsEntity, RigidTransform>> relatives_joffs = new List<KeyValuePair<PhysicsEntity, RigidTransform>>();

        private HashSet<long> relative_fixed = new HashSet<long>();

        public void MoveToOffsetWithJoints(Location pos, Location vel, BEPUutilities.Quaternion new_orient)
        {
            relative_fixed.Add(EID);
            SetPosition(GetPosition() + pos);
            SetVelocity(GetVelocity() + vel);
            SetOrientation(new_orient);
            for (int i = 0; i < Joints.Count; i++)
            {
                if (Joints[i].PullsAlong) // TODO: Maybe a dedicated 'pull along' joint for simplifying this, and reducing erroneous pulls?
                {
                    if (Joints[i].One.EID == EID)
                    {
                        if (!relative_fixed.Contains(Joints[i].Two.EID))
                        {
                            relative_fixed.Add(Joints[i].Two.EID);
                            Joints[i].Two.SetPosition(Joints[i].Two.GetPosition() + pos);
                            if (Joints[i].Two is PhysicsEntity pe)
                            {
                                pe.SetVelocity(pe.GetVelocity() + vel);
                            }
                        }
                    }
                    else
                    {
                        if (!relative_fixed.Contains(Joints[i].One.EID))
                        {
                            relative_fixed.Add(Joints[i].One.EID);
                            Joints[i].One.SetPosition(Joints[i].One.GetPosition() + pos);
                            if (Joints[i].One is PhysicsEntity pe)
                            {
                                pe.SetVelocity(pe.GetVelocity() + vel);
                            }
                        }
                    }
                }
            }
            relative_fixed.Clear();
            //relatives_joffs.Clear();
        }

        public void WeakenThisAndJointed(bool strong = false)
        {
            StrongArmNetworking = strong;
            for (int i = 0; i < Joints.Count; i++)
            {
                if (Joints[i].PullsAlong)
                {
                    if (Joints[i].One is PhysicsEntity pe)
                    {
                        pe.StrongArmNetworking = strong;
                    }
                    if (Joints[i].Two is PhysicsEntity pe2)
                    {
                        pe2.StrongArmNetworking = strong;
                    }
                }
            }
        }

        /// <summary>
        /// Whether this particular entity is 'strong arm' networked, meaning it will be forced to the correct position. Otherwise, it will be weakly networked.
        /// </summary>
        public bool StrongArmNetworking = true;

        public override void Tick()
        {
            if (AutoGravityScale > 0.0)
            {
                Location pos = new Location(Body.Position);
                Location gravDir;
                if (pos.LengthSquared() > AutoGravityScale * AutoGravityScale)
                {
                    double len = pos.Length();
                    gravDir = (pos / (len * AutoGravityScale));
                }
                else
                {
                    gravDir = pos / AutoGravityScale;
                }
                Body.Gravity = (gravDir * (-9.8 * 3.0 / 2.0)).ToBVector(); // TODO: Correct gravity field, non-constant
            }
        }

        /// <summary>
        /// Builds and spawns the body into the world.
        /// </summary>
        public virtual void SpawnBody()
        {
            if (Body != null)
            {
                DestroyBody();
            }
            Body = new BEPUphysics.Entities.Entity(Shape, Mass);
            Body.CollisionInformation.CollisionRules.Group = CGroup;
            InternalOffset = new Location(Body.Position);
            Body.AngularVelocity = new Vector3((float)AVel.X, (float)AVel.Y, (float)AVel.Z);
            Body.LinearVelocity = new Vector3((float)LVel.X, (float)LVel.Y, (float)LVel.Z);
            Body.WorldTransform = WorldTransform; // TODO: Position + Quaternion
            Body.PositionUpdateMode = BEPUphysics.PositionUpdating.PositionUpdateMode.Passive;
            Body.CollisionInformation.LocalPosition = LocalPositionOffset.ToBVector();
            if (!CanRotate)
            {
                Body.AngularDamping = 1;
            }
            Body.LinearDamping = 0;
            // TODO: Other settings
            // TODO: Gravity
            Body.Tag = this;
            SetFriction(Friction);
            SetBounciness(Bounciness);
            TheRegion.PhysicsWorld.Add(Body);
            for (int i = 0; i < Joints.Count; i++)
            {
                if (Joints[i] is BaseJoint joint)
                {
                    joint.CurrentJoint = joint.GetBaseJoint();
                    TheRegion.PhysicsWorld.Add(joint.CurrentJoint);
                }
            }
            ShadowCastShape = Shape.GetCollidableInstance().BoundingBox;
            ShadowMainDupe = Shape.GetCollidableInstance().BoundingBox;
            ShadowCenter = GetPosition();
        }

        public BoundingBox ShadowCastShape;

        public double ShadowRadiusSquaredXY;

        public BoundingBox ShadowMainDupe;

        public Location ShadowCenter;

        /// <summary>
        /// Destroys the body, removing it from the physics world.
        /// </summary>
        public virtual void DestroyBody()
        {
            if (Body == null)
            {
                return;
            }
            if (Body.Space == null)
            {
                Body = null;
                return;
            }
            LVel = new Location(Body.LinearVelocity);
            AVel = new Location(Body.AngularVelocity);
            Friction = GetFriction();
            // TODO: Gravity = new Location(Body.Gravity.X, Body.Gravity.Y, Body.Gravity.Z);
            WorldTransform = Body.WorldTransform;
            for (int i = 0; i < Joints.Count; i++)
            {
                if (Joints[i] is BaseJoint joint && joint.CurrentJoint != null)
                {
                    TheRegion.PhysicsWorld.Remove(joint.CurrentJoint);
                    joint.CurrentJoint = null;
                }
            }
            TheRegion.PhysicsWorld.Remove(Body);
            Body = null;
        }

        /// <summary>
        /// Returns the gravity value for this physics entity, if it has one.
        /// Otherwise, returns the body's default gravity.
        /// </summary>
        /// <returns>The gravity value.</returns>
        public Location GetGravity()
        {
            if (Body != null && Body.Gravity.HasValue)
            {
                return new Location(Body.Gravity.Value);
            }
            return Gravity;
        }

        /// <summary>
        /// Returns the friction level of this entity.
        /// </summary>
        public virtual float GetFriction()
        {
            if (Body == null)
            {
                return Friction;
            }
            return (float)Body.Material.KineticFriction;
        }

        /// <summary>
        /// Sets the friction level of this entity.
        /// </summary>
        /// <param name="fric">The friction level.</param>
        public virtual void SetFriction(float fric)
        {
            Friction = fric;
            if (Body != null)
            {
                // TODO: Separate
                Body.Material.StaticFriction = fric;
                Body.Material.KineticFriction = fric;
            }
        }

        /// <summary>
        /// Returns the bounciness (restitution coefficient) of this entity.
        /// </summary>
        public virtual float GetBounciness()
        {
            if (Body == null)
            {
                return Bounciness;
            }
            return (float)Body.Material.Bounciness;
        }

        /// <summary>
        /// Sets the bounciness (restitution coefficient)  of this entity.
        /// </summary>
        /// <param name="bounce">The bounciness (restitution coefficient) .</param>
        public virtual void SetBounciness
            (float bounce)
        {
            Bounciness = bounce;
            if (Body != null)
            {
                Body.Material.Bounciness = bounce;
            }
        }

        /// <summary>
        /// Returns the mass of the entity.
        /// </summary>
        public virtual float GetMass()
        {
            return Body == null ? Mass : (float)Body.Mass;
        }

        /// <summary>
        /// Sets the mass of this entity.
        /// </summary>
        /// <param name="mass">The new mass value.</param>
        public virtual void SetMass(float mass)
        {
            Mass = mass;
            if (Body != null)
            {
                Body.Mass = mass;
            }
        }

        /// <summary>
        /// Returns the position of this entity within the world.
        /// </summary>
        public override Location GetPosition()
        {
            if (Body == null)
            {
                return new Location(WorldTransform.Translation);
            }
            return new Location(Body.Position);
        }

        /// <summary>
        /// Sets the position of this entity within the world.
        /// </summary>
        /// <param name="pos">The position to move the entity to.</param>
        public override void SetPosition(Location pos)
        {
            if (Body != null)
            {
                Body.Position = pos.ToBVector();
            }
            else
            {
                WorldTransform.Translation = pos.ToBVector();
            }
        }

        /// <summary>
        /// Returns the velocity of this entity.
        /// </summary>
        public virtual Location GetVelocity()
        {
            if (Body != null)
            {
                return new Location(Body.LinearVelocity);
            }
            else
            {
                return LVel;
            }
        }

        /// <summary>
        /// Sets the velocity of this entity.
        /// </summary>
        /// <param name="vel">The new velocity.</param>
        public virtual void SetVelocity(Location vel)
        {
            LVel = vel;
            if (Body != null)
            {
                Body.LinearVelocity = vel.ToBVector();
            }
        }

        /// <summary>
        /// Returns the angular velocity of this entity.
        /// </summary>
        public virtual Location GetAngularVelocity()
        {
            if (Body != null)
            {
                return new Location(Body.AngularVelocity);
            }
            else
            {
                return AVel;
            }
        }

        /// <summary>
        /// Sets the angular velocity of this entity.
        /// </summary>
        /// <param name="vel">The new velocity.</param>
        public virtual void SetAngularVelocity(Location vel)
        {
            AVel = vel;
            if (Body != null)
            {
                Body.AngularVelocity = vel.ToBVector();
            }
        }

        /// <summary>
        /// Gets the transformation matrix of this entity as an OpenTK matrix.
        /// </summary>
        /// <returns>.</returns>
        public OpenTK.Matrix4d GetTransformationMatrix()
        {
            if (Body == null)
            {
                return ClientUtilities.ConvertD(WorldTransform);
            }
            return ClientUtilities.ConvertD(Body.WorldTransform);
        }

        public OpenTK.Matrix4d GetOrientationMatrix()
        {
            if (Body == null)
            {
                return OpenTK.Matrix4d.Identity;
            }
            return ClientUtilities.ConvertD(Matrix3x3.ToMatrix4X4(Body.OrientationMatrix));
        }

        /// <summary>
        /// Returns the orientation of an entity.
        /// </summary>
        public override BEPUutilities.Quaternion GetOrientation()
        {
            if (Body != null)
            {
                return Body.Orientation;
            }
            return BEPUutilities.Quaternion.CreateFromRotationMatrix(WorldTransform);
        }

        /// <summary>
        /// Sets the direction of the entity.
        /// </summary>
        /// <param name="rot">The new angles.</param>
        public override void SetOrientation(BEPUutilities.Quaternion rot)
        {
            if (Body != null)
            {
                Body.Orientation = rot;
            }
            else
            {
                WorldTransform = Matrix.CreateFromQuaternion(rot) * Matrix.CreateTranslation(WorldTransform.Translation);
            }
        }

        public double AutoGravityScale = 0.0;

        public const int PhysicsNetworkDataLength = 4 + 24 + 24 + 16 + 24 + 4 + 4 + 1 + 1 + 8 + 24;

        public bool ApplyPhysicsNetworkData(byte[] dat)
        {
            if (dat.Length < PhysicsNetworkDataLength)
            {
                return false;
            }
            SetMass(Utilities.BytesToFloat(Utilities.BytesPartial(dat, 0, 4)));
            SetPosition(Location.FromDoubleBytes(dat, 4));
            SetVelocity(Location.FromDoubleBytes(dat, 4 + 24));
            SetOrientation(Utilities.BytesToQuaternion(dat, 4 + 24 + 24));
            SetAngularVelocity(Location.FromDoubleBytes(dat, 4 + 24 + 24 + 16));
            SetFriction(Utilities.BytesToFloat(Utilities.BytesPartial(dat, 4 + 24 + 24 + 16 + 24, 4)));
            SetBounciness(Utilities.BytesToFloat(Utilities.BytesPartial(dat, 4 + 24 + 24 + 16 + 24 + 4, 4)));
            // TODO: Proper flags thingy here?
            byte fl = dat[4 + 24 + 24 + 16 + 24 + 4 + 4];
            Visible = (fl & 1) == 1;
            GenBlockShadows = (fl & 2) == 2;
            CanDistanceRender = GenBlockShadows;
            byte cg = dat[4 + 24 + 24 + 16 + 24 + 4 + 4 + 1];
            if (cg == 2)
            {
                CGroup = CollisionUtil.Solid;
            }
            else if (cg == 4)
            {
                CGroup = CollisionUtil.NonSolid;
            }
            else if (cg == (2 | 4))
            {
                CGroup = CollisionUtil.Item;
            }
            else if (cg == 8)
            {
                CGroup = CollisionUtil.Player;
            }
            else if (cg == (2 | 8))
            {
                CGroup = CollisionUtil.Water;
            }
            else if (cg == (2 | 4 | 8))
            {
                CGroup = CollisionUtil.WorldSolid;
            }
            else // if (cg == 16)
            {
                CGroup = CollisionUtil.Player;
            }
            const int coord = 4 + 24 + 24 + 16 + 24 + 4 + 4 + 1 + 1;
            AutoGravityScale = Utilities.BytesToDouble(Utilities.BytesPartial(dat, coord, 8));// TODO: non-entity property
            TheClient.Player.AutoGravityScale = AutoGravityScale;
            LocalPositionOffset = Location.FromDoubleBytes(dat, coord + 8);
            if (Body != null)
            {
                Body.CollisionInformation.LocalPosition = LocalPositionOffset.ToBVector();
            }
            return true;
        }

        public Location LocalPositionOffset = Location.Zero;
    }
}
