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
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.NetworkSystem.PacketsOut;
using BEPUutilities;
using BEPUphysics.BroadPhaseEntries;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.Shared.Collision;
using BEPUphysics.Character;
using OpenTK.Graphics.OpenGL4;
using Voxalia.ClientGame.JointSystem;
using FreneticScript.TagHandlers.Objects;
using FreneticGameCore;
using FreneticGameCore.Collision;
using FreneticGameGraphics.ClientSystem;
using FreneticGameGraphics.GraphicsHelpers;

namespace Voxalia.ClientGame.EntitySystem
{
    public class PlayerEntity : CharacterEntity
    {
        public YourStatusFlags ServerFlags = YourStatusFlags.NONE;

        public static readonly BEPUutilities.Quaternion PreFlyOrient = BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitX, Math.PI * 0.5);

        public void Fly()
        {
            if (IsFlying)
            {
                return;
            }
            IsFlying = true;
            PreFlyMass = GetMass();
            //PreFlyOrient = GetOrientation();
            SetMass(0);
            CBody.Body.AngularVelocity = Vector3.Zero;
        }

        public void Unfly()
        {
            if (!IsFlying)
            {
                return;
            }
            IsFlying = false;
            SetMass(PreFlyMass);
            CBody.Body.LocalInertiaTensorInverse = new Matrix3x3();
            CBody.Body.Orientation = PreFlyOrient;
            CBody.Body.AngularVelocity = Vector3.Zero;
        }

        public Location ServerLocation = new Location(0, 0, 0);

        public float tmass = 70;

        bool pup = false;

        public Model model;

        public PlayerEntity(Region tregion)
            : base(tregion)
        {
            SetMass(tmass);
            mod_scale = 1.5f;
            CanRotate = false;
            EID = -1;
            model = TheClient.Models.GetModel("players/human_male_004");
            model.LoadSkin(TheClient.Textures);
            CGroup = CollisionUtil.Player;
            SetPosition(new Location(0, 0, 1000));
            for (int i = 0; i < PACKET_CAP; i++)
            {
                GTTs[i] = -1.0;
            }
        }

        public override void SpawnBody()
        {
            base.SpawnBody();
            for (int i = 0; i < PACKET_CAP; i++)
            {
                GTTs[i] = -1.0;
            }
        }

        public override ItemStack GetHeldItem()
        {
            return TheClient.GetItemForSlot(TheClient.QuickBarPos);
        }

        public bool IgnorePlayers(BroadPhaseEntry entry)
        {
            if (entry.CollisionRules.Group == CollisionUtil.Player)
            {
                return false;
            }
            return CollisionUtil.ShouldCollide(entry);
        }

        public bool InVehicle = false;

        public Entity Vehicle = null;

        public int CurrentMovePacketID = 0;

        public const int PACKET_CAP = 1024;

        public Location[] Positions = new Location[PACKET_CAP];

        public Location[] Velocities = new Location[PACKET_CAP];

        public double[] GTTs = new double[PACKET_CAP];

        public double CurrentRemoteGTT = 1.0;

        public struct VehicleStore
        {
            public Location Pos, Vel, AVel;

            public BEPUutilities.Quaternion Quat;

            public double Time;
        }

        public VehicleStore[] VehicleMarks = new VehicleStore[PACKET_CAP];

        public int CurrentVehiclePacketID = 0;

        const double VEH_MINIMUM = 5.0;

        public void VehiclePacketFromServer(int ID, Location pos, Location vel, Location avel, BEPUutilities.Quaternion quat, double gtt, Location prel)
        {
            if (VehicleMarks[ID].Time > 0)
            {
                Location off_pos = pos - VehicleMarks[ID].Pos;
                Location off_vel = vel - VehicleMarks[ID].Vel;
                Location off_avel = avel - VehicleMarks[ID].AVel;
                double off_gtt = MathHelper.Clamp(gtt - VehicleMarks[ID].Time, 0.001, 0.3) * 2.0;
                VehicleMarks[ID].Time = 0;
                if (!InVehicle || Vehicle == null)
                {
                    return;
                }
                PhysicsEntity ve = (Vehicle as PhysicsEntity);
                if ((off_pos / off_gtt).LengthSquared() > vel.LengthSquared() * VEH_MINIMUM + VEH_MINIMUM)
                {
                    SysConsole.Output(OutputType.DEBUG, "Own-Vehicle is insufficiently correcting! (Server lag?)");
                }
                //ve.SetPosition(ve.GetPosition() + off_pos * off_gtt);
                //ve.SetVelocity(ve.GetVelocity() + off_vel * off_gtt);
                ve.ServerKnownLocation = pos;
                ve.MoveToOffsetWithJoints(off_pos * off_gtt, off_vel * off_gtt, BEPUutilities.Quaternion.Slerp(ve.GetOrientation(), quat, off_gtt));
                ve.SetAngularVelocity(ve.GetAngularVelocity() + off_avel * off_gtt);
                //ve.SetOrientation(Quaternion.Slerp(ve.GetOrientation(), quat, off_gtt));
                //SetPosition(ve.GetPosition() + prel);
            }
        }

        public void UpdateVehicle()
        {
            PhysicsEntity ve = (Vehicle as PhysicsEntity);
            int id = CurrentVehiclePacketID;
            CurrentVehiclePacketID = (CurrentVehiclePacketID + 1) % PACKET_CAP;
            VehicleMarks[id] = new VehicleStore() { Pos = ve.GetPosition(), Vel = ve.GetVelocity(), AVel = ve.GetAngularVelocity(), Quat = ve.GetOrientation(), Time = CurrentRemoteGTT };
            TheClient.Network.SendPacket(new MyVehiclePacketOut(id));
        }

        public void PacketFromServer(double gtt, int ID, Location pos, Location vel, bool _pup)
        {
            CurrentRemoteGTT = gtt;
            ServerLocation = pos;
            if (ServerFlags.HasFlag(YourStatusFlags.INSECURE_MOVEMENT))
            {
                return;
            }
            if (InVehicle && Vehicle != null)
            {
                GTTs[ID] = -1.0;
                return;
            }
            Location cur_pos = GetPosition();
            Location cur_vel = GetVelocity();
            Location old_pos = Positions[ID];
            Location old_vel = Velocities[ID];
            Location off_pos = pos - old_pos;
            Location off_vel = vel - old_vel;
            double off_gtt = MathHelper.Clamp(gtt - GTTs[ID], 0.001, 0.3) * 2.0;
            if ((cur_pos - pos).LengthSquared() > GetVelocity().LengthSquared() * 25.0)
            {
                SetPosition(pos);
                SetVelocity(vel);
            }
            else
            {
                SetPosition(cur_pos + off_pos * off_gtt);
                SetVelocity(cur_vel + off_vel * off_gtt);
            }
            GTTs[ID] = -1.0;
            // TODO: match this movement logic for vehicles too (pos + vel + direction)
        }

        public double MoveTransmitWaiting = 0.0;

        public const double MoveTransmitTime = 0.016;

        double LagRateLimit = 0.0;

        public void UpdateLocalMovement()
        {
            if (!TheClient.Network.IsAlive)
            {
                return;
            }
            Location dirro = Direction;
            if (ServerFlags.HasFlag(YourStatusFlags.NO_ROTATE))
            {
                dirro.Yaw = tyaw;
                dirro.Pitch = tpitch;
            }
            MoveTransmitWaiting += TheClient.Delta;
            if (MoveTransmitWaiting < MoveTransmitTime)
            {
                return;
            }
            MoveTransmitWaiting = 0.0;
            KeysPacketData kpd = (Upward ? KeysPacketData.UPWARD : 0)
                  | (Click ? KeysPacketData.CLICK : 0)
                  | (AltClick ? KeysPacketData.ALTCLICK : 0)
                  | (Downward ? KeysPacketData.DOWNWARD : 0)
                  | (Use ? KeysPacketData.USE : 0)
                  | (ItemLeft ? KeysPacketData.ITEMLEFT : 0)
                  | (ItemRight ? KeysPacketData.ITEMRIGHT : 0)
                  | (ItemUp ? KeysPacketData.ITEMUP : 0)
                  | (ItemDown ? KeysPacketData.ITEMDOWN : 0);
            if (GTTs[CurrentMovePacketID] >= 0.0)
            {
                LagRateLimit += TheClient.Delta;
                if (LagRateLimit > 2.5)
                {
                    LagRateLimit = 0.0;
                    SysConsole.Output(OutputType.WARNING, "Lagging: Movement tracker full (" + CurrentMovePacketID + ", " + GTTs[CurrentMovePacketID] + ")");
                    for (int i = 0; i < PACKET_CAP; i++)
                    {
                        GTTs[i] = -1; // TODO: Temporary!
                    }
                }
                return;
            }
            Location p = GetPosition();
            Location v = GetVelocity();
            TheClient.Network.SendPacket(new KeysPacketOut(CurrentMovePacketID, kpd, dirro, XMove, YMove, p, v, SprintOrWalk, ItemDir(), ItemSourceRelative()));
            Positions[CurrentMovePacketID] = p;
            Velocities[CurrentMovePacketID] = v;
            GTTs[CurrentMovePacketID] = CurrentRemoteGTT;
            CurrentMovePacketID = (CurrentMovePacketID + 1) % PACKET_CAP;
            if (InVehicle && Vehicle != null)
            {
                UpdateVehicle();
            }
        }

        public Location ItemDir()
        {
            return (TheClient.VR == null || TheClient.VR.Right == null) ? ForwardVector() : TheClient.VR.Right.ForwardVector();
        }

        public Location ItemSourceRelative()
        {
            return ItemSource() - GetPosition();
        }

        public Location ItemSource()
        {
            return (TheClient.VR == null || TheClient.VR.Right == null) ? GetEyePosition() : GetPosition() + ClientUtilities.Convert(TheClient.VR.Right.Position.ExtractTranslation());
        }

        public void SetBodyMovement(CharacterController cc)
        {
            Vector2 movement = InVehicle && Vehicle != null ? Vector2.Zero : new Vector2(XMove, YMove);
            if (movement.LengthSquared() > 0)
            {
                movement.Normalize();
            }
            cc.ViewDirection = Utilities.ForwardVector_Deg(Direction.Yaw, Direction.Pitch).ToBVector();
            cc.HorizontalMotionConstraint.MovementDirection = movement;
            if (Downward)
            {
                cc.StanceManager.DesiredStance = Stance.Crouching;
            }
            else
            {
                cc.StanceManager.DesiredStance = DesiredStance;
            }
            if (IsSwimlogic)
            {
                Location forw = Utilities.RotateVector(new Location(movement.X, movement.Y, 0), Direction.Yaw * Utilities.PI180, Direction.Pitch * Utilities.PI180);
                if (Upward)
                {
                    forw.Z = 1;
                }
                else if (Downward)
                {
                    forw.Z = -1;
                }
                SwimForce(forw.ToBVector());
            }
        }

        public void FlyForth(CharacterController cc, double delta)
        {
            if (IsFlying)
            {
                Location move = new Location(-cc.HorizontalMotionConstraint.MovementDirection.Y, cc.HorizontalMotionConstraint.MovementDirection.X, 0);
                if (Upward)
                {
                    move.Z = 1;
                    move = move.Normalize();
                }
                else if (Downward)
                {
                    move.Z = -1;
                    move = move.Normalize();
                }
                Location forw = Utilities.RotateVector(move, Direction.Yaw * Utilities.PI180, Direction.Pitch * Utilities.PI180);
                cc.Body.Position += (forw * delta * CBStandSpeed * 4 * (new Vector2(XMove, YMove).Length())).ToBVector();
                cc.HorizontalMotionConstraint.MovementDirection = Vector2.Zero;
                cc.Body.LinearVelocity = new Vector3(0, 0, 0);
            }
        }

        public void TryToJump()
        {
            if (!InVehicle && Vehicle == null && Upward && !IsFlying && !pup && CBody.SupportFinder.HasSupport)
            {
                CBody.Jump();
                pup = true;
            }
            else if (!Upward)
            {
                pup = false;
            }
        }
        
        public void SetMoveSpeed(CharacterController cc)
        {
            float speedmod = (float)new Vector2(XMove, YMove).Length() * 1.25f;
            speedmod *= (1f + SprintOrWalk * 0.5f);
            if (Click)
            {
                ItemStack item = TheClient.GetItemForSlot(TheClient.QuickBarPos);
                bool has = item.SharedAttributes.ContainsKey("charge");
                BooleanTag bt = has ? BooleanTag.TryFor(item.SharedAttributes["charge"]) : null;
                if (bt != null && bt.Internal && item.SharedAttributes.ContainsKey("cspeedm"))
                {
                    NumberTag nt = NumberTag.TryFor(item.SharedAttributes["cspeedm"]);
                    if (nt != null)
                    {
                        speedmod *= (float)nt.Internal;
                    }
                }
            }
            RigidTransform transf = new RigidTransform(Vector3.Zero, Body.Orientation);
            cc.Body.CollisionInformation.Shape.GetBoundingBox(ref transf, out BoundingBox box);
            Location pos = new Location(cc.Body.Position) + new Location(0, 0, box.Min.Z);
            Material mat = TheRegion.GetBlockMaterial(pos + new Location(0, 0, -0.05f));
            speedmod *= (float)mat.GetSpeedMod();
            cc.StandingSpeed = CBStandSpeed * speedmod;
            cc.CrouchingSpeed = CBCrouchSpeed * speedmod;
            float frictionmod = 1f;
            frictionmod *= (float)mat.GetFrictionMod();
            cc.SlidingForce = CBSlideForce * frictionmod * Mass;
            cc.AirForce = CBAirForce * frictionmod * Mass;
            cc.TractionForce = CBTractionForce * frictionmod * Mass;
            cc.VerticalMotionConstraint.MaximumGlueForce = CBGlueForce * Mass;
        }
        
        public bool Forward;
        public bool Backward;
        public bool Leftward;
        public bool Rightward;
        public bool Sprint;
        public bool Walk;
        public bool ItemLeft;
        public bool ItemRight;
        public bool ItemUp;
        public bool ItemDown;
        double tyaw = 0;
        double tpitch = 0;

        public enum VRControlMode
        {
            NONE = 0,
            VIVE = 1,
            OCULUS = 2
        }

        public VRControlMode VRControls = VRControlMode.NONE;

        public bool PVRJump;
        public bool PVRCrouch;
        public bool PVRPrimary;
        public bool PVRSecondary;
        public bool PVRUse;
        public bool PVRItemLeft;
        public bool PVRItemRight;
        public bool PVRItemUp;
        public bool PVRItemDown;

        public override void Tick()
        {
            if (CBody == null || Body == null)
            {
                return;
            }
            Body.ActivityInformation.Activate();
            if (ServerFlags.HasFlag(YourStatusFlags.NO_ROTATE))
            {
                // TODO: Logic for this section?
                tyaw = MouseHandler.MouseDelta.X;
                tpitch = MouseHandler.MouseDelta.Y;
                tyaw += TheClient.Gamepad.DirectionControl.X * 90f * TheRegion.Delta;
                tpitch += TheClient.Gamepad.DirectionControl.Y * 45f * TheRegion.Delta;
            }
            else
            {
                Direction.Yaw += MouseHandler.MouseDelta.X;
                Direction.Pitch += MouseHandler.MouseDelta.Y;
                Direction.Yaw += TheClient.Gamepad.DirectionControl.X * 90f * TheRegion.Delta;
                Direction.Pitch += TheClient.Gamepad.DirectionControl.Y * 45f * TheRegion.Delta;
            }
            SprintOrWalk = 0.0f;
            // TODO: Easy converter from OpenTK to BEPU
            Vector2 tmove;
            tmove.X = TheClient.Gamepad.MovementControl.X;
            tmove.Y = TheClient.Gamepad.MovementControl.Y;
            if (tmove.LengthSquared() > 1)
            {
                tmove.Normalize();
            }
            SprintOrWalk = ((float)tmove.Length() * 2.0f) - 1.0f;
            if (tmove.X == 0 && tmove.Y == 0)
            {
                if (Forward)
                {
                    tmove.Y = 1;
                }
                if (Backward)
                {
                    tmove.Y = -1;
                }
                if (Rightward)
                {
                    tmove.X = 1;
                }
                if (Leftward)
                {
                    tmove.X = -1;
                }
                if (tmove.LengthSquared() > 1)
                {
                    tmove.Normalize();
                }
            }
            XMove = (float)tmove.X;
            YMove = (float)tmove.Y;
            if (Sprint)
            {
                SprintOrWalk = 1;
            }
            if (Walk)
            {
                SprintOrWalk = -1;
            }
            while (Direction.Yaw < 0)
            {
                Direction.Yaw += 360;
            }
            while (Direction.Yaw > 360)
            {
                Direction.Yaw -= 360;
            }
            if (Direction.Pitch > 89.9f)
            {
                Direction.Pitch = 89.9f;
            }
            if (Direction.Pitch < -89.9f)
            {
                Direction.Pitch = -89.9f;
            }
            if (TheClient.VR != null)
            {
                if (VRControls == VRControlMode.NONE)
                {
                    VRControls = TheClient.VR.VRModel.ToLowerFast().Contains("ocul") ? VRControlMode.OCULUS : VRControlMode.VIVE;
                }
                OpenTK.Quaternion oquat = TheClient.VR.HeadMatRot.ExtractRotation(true);
                BEPUutilities.Quaternion quat = new BEPUutilities.Quaternion(oquat.X, oquat.Y, oquat.Z, oquat.W);
                Vector3 face = -BEPUutilities.Quaternion.Transform(Vector3.UnitZ, quat);
                Direction = Utilities.VectorToAngles(new Location(face));
                Direction.Yaw += 180;
                //OpenTK.Vector3 headSpot = TheClient.VR.BasicHeadMat.ExtractTranslation();
                if (TheClient.VR.Left != null)
                {
                    if (TheClient.VR.Left.Trigger > 0.01f)
                    {
                        Location lforw = TheClient.VR.Left.ForwardVector();
                        Location ldir = Utilities.VectorToAngles(lforw);
                        ldir.Yaw += 180;
                        double goalyaw = ldir.Yaw - Direction.Yaw;
                        Vector2 resmove = new Vector2(Math.Sin(goalyaw * Utilities.PI180), Math.Cos(goalyaw * Utilities.PI180));
                        double len = resmove.Length();
                        SprintOrWalk = (float)(len * 2.0 - 1.0);
                        if (len > 1.0)
                        {
                            resmove /= len;
                        }
                        XMove = -(float)resmove.X;
                        YMove = (float)resmove.Y;
                    }
                    if (TheClient.VR.Left.Pressed.HasFlag(VRButtons.SIDE_GRIP))
                    {
                        Downward = true;
                        PVRCrouch = true;
                    }
                    else if (PVRCrouch)
                    {
                        Downward = false;
                        PVRCrouch = false;
                    }
                    if (TheClient.VR.Left.Pressed.HasFlag(VRButtons.MENU_BUTTON_BY))
                    {
                        Upward = true;
                        PVRJump = true;
                    }
                    else if (PVRJump)
                    {
                        Upward = false;
                        PVRJump = false;
                    }
                }
                if (TheClient.VR.Right != null)
                {
                    if (TheClient.VR.Right.Pressed.HasFlag(VRButtons.TRIGGER))
                    {
                        Click = true;
                        PVRPrimary = true;
                    }
                    else if (PVRPrimary)
                    {
                        Click = false;
                        PVRPrimary = false;
                    }
                    if (TheClient.VR.Right.Pressed.HasFlag(VRButtons.MENU_BUTTON_BY))
                    {
                        AltClick = true;
                        PVRSecondary = true;
                    }
                    else if (PVRSecondary)
                    {
                        AltClick = false;
                        PVRSecondary = false;
                    }
                    if (TheClient.VR.Right.Pressed.HasFlag(VRButtons.SIDE_GRIP))
                    {
                        Use = true;
                        PVRUse = true;
                    }
                    else if (PVRUse)
                    {
                        Use = false;
                        PVRUse = false;
                    }
                    bool rtppressed = TheClient.VR.Right.Pressed.HasFlag(VRButtons.TRACKPAD);
                    OpenTK.Vector2 rtp = TheClient.VR.Right.TrackPad;
                    float yxdiff = Math.Abs(rtp.Y) - Math.Abs(rtp.X);
                    float min = VRControls == VRControlMode.OCULUS ? 0.5f : 0.01f;
                    if (rtppressed && rtp.Y > min && (yxdiff > 0.01))
                    {
                        ItemUp = true;
                        PVRItemUp = true;
                    }
                    else if (PVRItemUp)
                    {
                        ItemUp = false;
                        PVRItemUp = false;
                    }
                    if (rtppressed && rtp.Y < min && (yxdiff > 0.01))
                    {
                        ItemDown = true;
                        PVRItemDown = true;
                    }
                    else if (PVRItemDown)
                    {
                        ItemDown = false;
                        PVRItemDown = false;
                    }
                    if (rtppressed && rtp.X > min && (yxdiff < -0.01))
                    {
                        ItemRight = true;
                        PVRItemRight = true;
                    }
                    else if (PVRItemRight)
                    {
                        ItemRight = false;
                        PVRItemRight = false;
                    }
                    if (rtppressed && rtp.X < min && (yxdiff < -0.01))
                    {
                        ItemLeft = true;
                        PVRItemLeft = true;
                    }
                    else if (PVRItemLeft)
                    {
                        ItemLeft = false;
                        PVRItemLeft = false;
                    }
                    bool rtptouched = TheClient.VR.Right.Touched.HasFlag(VRButtons.TRACKPAD);
                    if (VRControls == VRControlMode.OCULUS)
                    {
                        if (!rtppressed && rtptouched)
                        {
                            VRRTouchDown = TheClient.VR.Right.TrackPad;
                            if (VRRTouchDown.X < -0.7 && VRRTouchLast.X >= -0.7)
                            {
                                TheClient.Commands.ExecuteCommands("itemprev"); // TODO: Less lazy!
                            }
                            else if (VRRTouchDown.X > 0.7 && VRRTouchLast.X <= 0.7)
                            {
                                TheClient.Commands.ExecuteCommands("itemnext"); // TODO: Less lazy!
                            }
                            else if (VRRTouchDown.Y < -0.7 && VRRTouchLast.Y >= -0.7)
                            {
                                TheClient.Commands.ExecuteCommands("weaponreload"); // TODO: Less lazy!
                            }
                            else if (VRRTouchDown.Y > 0.7 && VRRTouchLast.Y <= 0.7)
                            {
                                TheClient.Commands.ExecuteCommands("echo 'Wow! You swiped up! Behavior for this coming SOON!'"); // TODO: Less lazy!
                            }
                            VRRTouchLast = TheClient.VR.Right.TrackPad;
                        }
                        else
                        {
                            VRRTouchLast = OpenTK.Vector2.Zero;
                        }
                    }
                    else
                    {
                        if (rtptouched && !VRRTouched)
                        {
                            VRRTouchDown = TheClient.VR.Right.TrackPad;
                            VRRTouched = true;
                        }
                        if (rtptouched)
                        {
                            VRRTouchLast = TheClient.VR.Right.TrackPad;
                        }
                        const float VR_ADJMIN = 0.5f;
                        if (!rtptouched && (VRRTouchLast.X != 0.0f || VRRTouchLast.Y != 0.0f))
                        {
                            if (VRRTouchDown.X < -VR_ADJMIN && VRRTouchLast.X > VR_ADJMIN)
                            {
                                TheClient.Commands.ExecuteCommands("itemprev"); // TODO: Less lazy!
                            }
                            else if (VRRTouchDown.X > VR_ADJMIN && VRRTouchLast.X < -VR_ADJMIN)
                            {
                                TheClient.Commands.ExecuteCommands("itemnext"); // TODO: Less lazy!
                            }
                            if (VRRTouchDown.Y < -VR_ADJMIN && VRRTouchLast.Y > VR_ADJMIN)
                            {
                                TheClient.Commands.ExecuteCommands("echo 'Wow! You swiped up! Behavior for this coming SOON!'"); // TODO: Less lazy!
                            }
                            else if (VRRTouchDown.Y > VR_ADJMIN && VRRTouchLast.Y < -VR_ADJMIN)
                            {
                                TheClient.Commands.ExecuteCommands("weaponreload"); // TODO: Less lazy!
                            }
                            VRRTouchLast = OpenTK.Vector2.Zero;
                        }
                    }
                }
            }
            TryToJump();
            UpdateLocalMovement();
            SetMoveSpeed(CBody);
            FlyForth(CBody, TheRegion.Delta);
            SetBodyMovement(CBody);
            PlayRelevantSounds();
            MoveVehicle();
            if (Flashlight != null)
            {
                Flashlight.Direction = Utilities.ForwardVector_Deg(Direction.Yaw, Direction.Pitch);
                Flashlight.Reposition(GetEyePosition() + Utilities.ForwardVector_Deg(Direction.Yaw, 0) * 0.3f);
            }
            base.Tick();
            //base.SetOrientation(Quaternion.Identity);
            aHTime += TheClient.Delta;
            aTTime += TheClient.Delta;
            aLTime += TheClient.Delta;
            if (hAnim != null)
            {
                if (aHTime >= hAnim.Length)
                {
                    aHTime = 0;
                }
            }
            if (tAnim != null)
            {
                if (aTTime >= tAnim.Length)
                {
                    aTTime = 0;
                }
            }
            if (lAnim != null)
            {
                if (aLTime >= lAnim.Length)
                {
                    aLTime = 0;
                }
            }
            bool hasjp = HasJetpack();
            JPBoost = hasjp && ItemLeft;
            JPHover = hasjp && ItemRight;
            // TODO: Triggered by console opening/closing directly, rather than monitoring it on the tick?
            if (TheClient.Network.IsAlive)
            {
                if (UIConsole.Open && !ConsoleWasOpen)
                {
                    TheClient.Network.SendPacket(new SetStatusPacketOut(ClientStatus.TYPING, 1));
                    ConsoleWasOpen = true;
                }
                else if (!UIConsole.Open && ConsoleWasOpen)
                {
                    TheClient.Network.SendPacket(new SetStatusPacketOut(ClientStatus.TYPING, 0));
                    ConsoleWasOpen = false;
                }
            }
        }

        public OpenTK.Vector2 VRRTouchDown = OpenTK.Vector2.Zero;

        public OpenTK.Vector2 VRRTouchLast = OpenTK.Vector2.Zero;

        public bool VRRTouched = false;

        public bool ConsoleWasOpen = false;

        public void PostTick()
        {
        }

        public JointWeld Welded = null;
        
        public Location UpDir()
        {
            return Location.UnitZ;
        }

        public bool InPlane()
        {
            return InVehicle && Vehicle != null && Vehicle is ModelEntity && (Vehicle as ModelEntity).Plane != null;
        }

        public BEPUutilities.Quaternion GetRelativeQuaternion()
        {
            if (InPlane() && TheClient.CVars.g_firstperson.ValueB)
            {
                return Vehicle.GetOrientation();
            }
            else if (AutoGravityScale > 0.0)
            {
                Vector3 up = Vector3.UnitZ;
                Vector3 antigrav = -Body.Gravity.Value;
                antigrav.Normalize();
                BEPUutilities.Quaternion.GetQuaternionBetweenNormalizedVectors(ref antigrav, ref up, out BEPUutilities.Quaternion q);
                return q;
            }
            return BEPUutilities.Quaternion.Identity;
        }

        public float Health;

        public float MaxHealth;

        public static OpenTK.Matrix4d PlayerAngleMat = OpenTK.Matrix4d.CreateRotationZ((float)(270 * Utilities.PI180));

        public override Location GetWeldSpot()
        {
            if (Welded != null && Welded.Enabled)
            {
                RigidTransform relative;
                RigidTransform start;
                if (Welded.Two == this)
                {
                    start = new RigidTransform(Welded.Ent1.Body.Position, Welded.Ent1.Body.Orientation);
                    RigidTransform.Invert(ref Welded.Relative, out relative);
                }
                else
                {
                    start = new RigidTransform(Welded.Ent2.Body.Position, Welded.Ent2.Body.Orientation);
                    relative = Welded.Relative;
                }
                RigidTransform.Multiply(ref start, ref relative, out RigidTransform res);
                return new Location(res.Position);
            }
            return GetPosition();
        }

        public override void RenderForMap()
        {
            TheClient.Textures.Black.Bind(); // TODO: Player icon of some form.
            TheClient.Rendering.RenderRectangle(-5, -5, 5, 5, OpenTK.Matrix4.CreateTranslation(0, 0, 1));
        }

        // TODO: Merge with base.Render() as much as possible!
        public override void Render()
        {
            GraphicsUtil.CheckError("Render - Player - Pre");
            Location renderrelpos = GetWeldSpot();
            if (TheClient.IsMainMenu || !TheClient.CVars.r_drawself.ValueB)
            {
                return;
            }
            TheClient.SetEnts(true);
            if (TheClient.CVars.n_debugmovement.ValueB)
            {
                if (ServerLocation.IsInfinite() || ServerLocation.IsNaN() || renderrelpos.IsInfinite() || renderrelpos.IsNaN())
                {
                    SysConsole.Output(OutputType.WARNING, "NaN server data");
                }
                else
                {
                    TheClient.Rendering.RenderLine(ServerLocation, renderrelpos, TheClient.MainWorldView);
                    GraphicsUtil.CheckError("Render - Player - Line");
                    TheClient.Rendering.RenderLineBox(ServerLocation + new Location(-0.2), ServerLocation + new Location(0.2), TheClient.MainWorldView);
                    GraphicsUtil.CheckError("Render - Player - LineBox");
                    /*if (GraphicsUtil.CheckError("Render - Player - LineBox"))
                    {
                        SysConsole.Output(OutputType.DEBUG, "Caught: " + (ServerLocation + new Location(-0.2)) + "::: " + (ServerLocation + new Location(0.2)));
                    }*/
                }
            }
            if (TheClient.VR != null)
            {
                return;
            }
            GraphicsUtil.CheckError("Render - Player - 0");
            OpenTK.Matrix4d mat = OpenTK.Matrix4d.Scale(1.5f)
                * OpenTK.Matrix4d.CreateRotationZ((Direction.Yaw * Utilities.PI180))
                * PlayerAngleMat
                * OpenTK.Matrix4d.CreateTranslation(ClientUtilities.ConvertD(renderrelpos));
            TheClient.MainWorldView.SetMatrix(2, mat);
            TheClient.Rendering.SetMinimumLight(0.0f, TheClient.MainWorldView);
            model.CustomAnimationAdjustments = new Dictionary<string, OpenTK.Matrix4>(SavedAdjustmentsOTK)
            {
                // TODO: safe (no-collision) rotation check?
                { "spine04", GetAdjustmentOTK("spine04") * OpenTK.Matrix4.CreateRotationX(-(float)(Direction.Pitch / 2f * Utilities.PI180)) }
            };
            GraphicsUtil.CheckError("Render - Player - 1");
            if (!TheClient.MainWorldView.RenderingShadows && TheClient.CVars.g_firstperson.ValueB)
            {
                model.CustomAnimationAdjustments["neck01"] = GetAdjustmentOTK("neck01") * OpenTK.Matrix4.CreateRotationX(-(float)(160f * Utilities.PI180));
            }
            else
            {
                model.CustomAnimationAdjustments["neck01"] = GetAdjustmentOTK("neck01");
            }
            model.Draw(aHTime, hAnim, aTTime, tAnim, aLTime, lAnim);
            Model mod = TheClient.GetItemForSlot(TheClient.QuickBarPos).Mod;
            bool hasjp = HasJetpack();
            GraphicsUtil.CheckError("Render - Player - 2");
            if (!hasjp && tAnim != null && mod != null)
            {
                mat = OpenTK.Matrix4d.CreateTranslation(ClientUtilities.ConvertD(renderrelpos));
                TheClient.MainWorldView.SetMatrix(2, mat);
                Dictionary<string, Matrix> adjs = new Dictionary<string, Matrix>(SavedAdjustments);
                // TODO: Logic of this rotation math?
                Matrix rotforw = Matrix.CreateFromQuaternion(BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitX, ((float)(Direction.Pitch / 2f * Utilities.PI180) % 360f)));
                adjs["spine04"] = GetAdjustment("spine04") * rotforw;
                SingleAnimationNode hand = tAnim.GetNode("metacarpal2.r");
                Matrix m4 = Matrix.CreateScale(1.5f, 1.5f, 1.5f)
                    * (Matrix.CreateFromQuaternion(BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)((-Direction.Yaw + 90) * Utilities.PI180) % 360f))
                    * hand.GetBoneTotalMatrix(aTTime, adjs))
                    * Matrix.CreateFromQuaternion(BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)((-90) * Utilities.PI180) % 360f));
                OpenTK.Matrix4 bonemat = new OpenTK.Matrix4((float)m4.M11, (float)m4.M12, (float)m4.M13, (float)m4.M14,
                    (float)m4.M21, (float)m4.M22, (float)m4.M23, (float)m4.M24,
                    (float)m4.M31, (float)m4.M32, (float)m4.M33, (float)m4.M34,
                    (float)m4.M41, (float)m4.M42, (float)m4.M43, (float)m4.M44);
                GL.UniformMatrix4(100, false, ref bonemat);
                mod.LoadSkin(TheClient.Textures);
                mod.Draw();
                bonemat = OpenTK.Matrix4.Identity;
                GL.UniformMatrix4(100, false, ref bonemat);
            }
            GraphicsUtil.CheckError("Render - Player - 3");
            if (hasjp)
            {
                // TODO: Abstractify!
                Model jetp = GetHeldItem().Mod;
                mat = OpenTK.Matrix4d.CreateTranslation(ClientUtilities.ConvertD(renderrelpos));
                TheClient.MainWorldView.SetMatrix(2, mat);
                Dictionary<string, Matrix> adjs = new Dictionary<string, Matrix>();
                Matrix rotforw = Matrix.CreateFromQuaternion(BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitX, ((float)(Direction.Pitch / 2f * Utilities.PI180) % 360f)));
                adjs["spine04"] = GetAdjustment("spine04") * rotforw;
                SingleAnimationNode spine = tAnim.GetNode("spine04");
                Matrix m4 = Matrix.CreateScale(1.5f, 1.5f, 1.5f)
                    * (Matrix.CreateFromQuaternion(BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)((-Direction.Yaw + 90) * Utilities.PI180) % 360f))
                    * spine.GetBoneTotalMatrix(aTTime, adjs))
                     * Matrix.CreateFromQuaternion(BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)((90) * Utilities.PI180) % 360f));
                OpenTK.Matrix4 bonemat = new OpenTK.Matrix4((float)m4.M11, (float)m4.M12, (float)m4.M13, (float)m4.M14, (float)m4.M21, (float)m4.M22, (float)m4.M23, (float)m4.M24,
                    (float)m4.M31, (float)m4.M32, (float)m4.M33, (float)m4.M34, (float)m4.M41, (float)m4.M42, (float)m4.M43, (float)m4.M44);
                GL.UniformMatrix4(100, false, ref bonemat);
                jetp.LoadSkin(TheClient.Textures);
                jetp.Draw();
                bonemat = OpenTK.Matrix4.Identity;
                GL.UniformMatrix4(100, false, ref bonemat);
            }
            GraphicsUtil.CheckError("Render - Player - 4");
            if (IsTyping)
            {
                TheClient.Textures.GetTexture("ui/game/typing").Bind(); // TODO: store!
                TheClient.Rendering.RenderBillboard(renderrelpos + new Location(0, 0, 4), new Location(2), TheClient.MainWorldView.CameraPos, TheClient.MainWorldView);
            }
            GraphicsUtil.CheckError("Render - Player - Post");
        }

        /// <summary>
        /// How far back to set the view when in a vehicle.
        /// </summary>
        public float VehicleViewBackMultiplier = 7;

        public float ViewBackMod()
        {
            return (InVehicle && Vehicle != null) ? VehicleViewBackMultiplier : 2;
        }

        public Location GetCameraPosition()
        {
            if (TheClient.VR != null)
            {
                return GetBasicEyePos();
            }
            if (!InVehicle || Vehicle == null || TheClient.CVars.g_firstperson.ValueB)
            {
                return GetEyePosition();
            }
            Location vpos = Vehicle.GetPosition();
            return vpos;
        }
    }
}
