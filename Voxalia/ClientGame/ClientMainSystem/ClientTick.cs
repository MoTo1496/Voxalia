//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Input;
using Voxalia.Shared;
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.OtherSystems;
using System.Drawing;
using FreneticScript;
using FreneticScript.CommandSystem;
using FreneticScript.TagHandlers.Common;
using System.Linq;
using Voxalia.Shared.Collision;
using BEPUphysics;
using BEPUutilities;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Voxalia.ClientGame.NetworkSystem;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.ClientGame.NetworkSystem.PacketsOut;
using Voxalia.ClientGame.WorldSystem;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public partial class Client
    {
        /// <summary>
        /// All items in the player's quick bar.
        /// </summary>
        public List<ItemStack> Items = new List<ItemStack>();

        /// <summary>
        /// Which index in the player's quick bar is selected.
        /// </summary>
        public int QuickBarPos = 0;

        /// <summary>
        /// Which index in the player's quick bar was selected, prior to a temporary quickbar update (QuickItem command in particular).
        /// -1 when not in use.
        /// </summary>
        public int PrevQuickItem = -1;

        /// <summary>
        /// The current "use type" for the current quick item usage, if any. (EG: Hold, throw, click, ...).
        /// </summary>
        public string QuickItemUseType;

        /// <summary>
        /// Locker object that is always grabbed while ticking, only lock on this if the scheduler does not fit your needs. (Consider requesting new scheduler features if that is the case!)
        /// </summary>
        public Object TickLock = new Object();

        /// <summary>
        /// Returns an item in the quick bar.
        /// Can return air.
        /// </summary>
        /// <param name="slot">The slot, any number is permitted.</param>
        /// <returns>A valid item.</returns>
        public ItemStack GetItemForSlot(int slot)
        {
            while (slot < 0)
            {
                slot += Items.Count + 1;
            }
            while (slot > Items.Count)
            {
                slot -= Items.Count + 1;
            }
            if (slot == 0)
            {
                return new ItemStack(this, "Air")
                {
                    DrawColor = Color4F.White,
                    Tex = Textures.Clear,
                    Description = "An empty slot.",
                    Count = 0,
                    Datum = 0,
                    DisplayName = "Air",
                    Name = "air",
                    TheClient = this
                };
            }
            else
            {
                return Items[slot - 1];
            }
        }

        /// <summary>
        /// Changes the player's currently selected item slot.
        /// Sends the update to the server automatically.
        /// Can optionally display additional item slots for a specified time (useful to help indicate item bar movement).
        /// </summary>
        public void SetHeldItemSlot(int slot, double extraItemsTime = 0)
        {
            QuickBarPos = slot;
            Network.SendPacket(new HoldItemPacketOut(QuickBarPos));
            RenderExtraItems = extraItemsTime;
        }

        /// <summary>
        /// OncePerSecondActions timer
        /// </summary>
        public double opsat = 0;

        /// <summary>
        /// True if the save data hasn't been saved, false if it has been saved.
        /// </summary>
        public bool first = true;

        /// <summary>
        /// Header of the client settings save file.
        /// </summary>
        const string HSaveStr = "// THIS FILE IS AUTOMATICALLY GENERATED.\n" + "// This file is run very early in startup, be careful with it!\n" + "debug minimal;\n";

        /// <summary>
        /// CVar save data.
        /// </summary>
        string CSaveStr = null;

        /// <summary>
        /// Binds save data.
        /// </summary>
        string BSaveStr = null;

        /// <summary>
        /// Gamepad binds save data.
        /// </summary>
        string GPBSaveStr = null;

        /// <summary>
        /// Fires once per second, every second, for the client.
        /// </summary>
        public void OncePerSecondActions()
        {
            FixMouse();
            gFPS = gTicks;
            gTicks = 0;
            bool edited = false;
            if (CVars.system.Modified || first)
            {
                edited = true;
                CVars.system.Modified = false;
                StringBuilder cvarsave = new StringBuilder(CVars.system.CVarList.Count * 100);
                for (int i = 0; i < CVars.system.CVarList.Count; i++)
                {
                    if (!CVars.system.CVarList[i].Flags.HasFlag(CVarFlag.ServerControl)
                        && !CVars.system.CVarList[i].Flags.HasFlag(CVarFlag.ReadOnly)
                        && !CVars.system.CVarList[i].Flags.HasFlag(CVarFlag.DoNotSave))
                    {
                        string val = CVars.system.CVarList[i].Value;
                        if (val.Contains('\"'))
                        {
                            val = "<{unescape[" + EscapeTagBase.Escape(val) + "]}>";
                        }
                        cvarsave.Append("set \"" + CVars.system.CVarList[i].Name + "\" \"" + val + "\";\n");
                    }
                }
                lock (saveLock)
                {
                    CSaveStr = cvarsave.ToString();
                }
            }
            if (KeyHandler.Modified || first)
            {
                edited = true;
                KeyHandler.Modified = false;
                StringBuilder keybindsave = new StringBuilder(KeyHandler.keystonames.Count * 100);
                keybindsave.Append("wait 0.5;\n");
                foreach (KeyValuePair<string, Key> keydat in KeyHandler.namestokeys)
                {
                    CommandScript cs = KeyHandler.GetBind(keydat.Value);
                    if (cs == null)
                    {
                        keybindsave.Append("unbind \"" + keydat.Key + "\";\n");
                    }
                    else
                    {
                        keybindsave.Append("bindblock \"" + keydat.Key + "\"\n{\n" + cs.FullString("\t") + "}\n");
                    }
                }
                lock (saveLock)
                {
                    BSaveStr = keybindsave.ToString();
                }
            }
            if (Gamepad.Modified || first)
            {
                edited = true;
                Gamepad.Modified = false;
                StringBuilder gamepadbindsave = new StringBuilder(GamePadHandler.GP_BUTTON_COUNT * 256);
                gamepadbindsave.Append("wait 0.5;\n");
                for (int i = 0; i < GamePadHandler.GP_BUTTON_COUNT; i++)
                {
                    CommandScript cs = Gamepad.ButtonBinds[i];
                    GamePadButton button = (GamePadButton)i;
                    if (cs == null)
                    {
                        gamepadbindsave.Append("gp_unbind \"" + button.ToString().ToLowerFast() + "\";\n");
                    }
                    else
                    {
                        gamepadbindsave.Append("gp_bindblock \"" + button.ToString().ToLowerFast() + "\"\n{\n" + cs.FullString("\t") + "}\n");
                    }
                }
                lock (saveLock)
                {
                    GPBSaveStr = gamepadbindsave.ToString();
                }
            }
            if (edited)
            {
                first = false;
                Schedule.StartAsyncTask(SaveCFG);
            }
            ops_spike++;
            if (ops_spike >= 5)
            {
                ops_spike = 0;
                MainWorldView.ShadowSpikeTime = 0;
                TickSpikeTime = 0;
                MainWorldView.FBOSpikeTime = 0;
                MainWorldView.LightsSpikeTime = 0;
                FinishSpikeTime = 0;
                TWODSpikeTime = 0;
                TotalSpikeTime = 0;
                TheRegion.CrunchSpikeTime = 0;
                gFPS_Min = 0;
                gFPS_Max = 0;
            }
            for (int i = 0; i < (int)NetUsageType.COUNT; i++)
            {
                Network.UsagesLastSecond[i] = Network.UsagesThisSecond[i];
                Network.UsagesThisSecond[i] = 0;
            }
            ops_extra++;
            if (CVars.r_compute.ValueB)
            {
                HashSet<Vector3i> handled = new HashSet<Vector3i>();
                foreach (Chunk chk in TheRegion.LoadedChunks.Values)
                {
                    if (chk.PosMultiplier < 5)
                    {
                        continue;
                    }
                    Vector3i slodpos = TheRegion.SLODLocFor(chk.WorldPosition);
                    if (handled.Contains(slodpos))
                    {
                        continue;
                    }
                    handled.Add(slodpos);
                    if (!TheRegion.SLODs.TryGetValue(slodpos, out ChunkSLODHelper slod))
                    {
                        slod = new ChunkSLODHelper() { Coordinate = slodpos, OwningRegion = TheRegion };
                        TheRegion.SLODs[slodpos] = slod;
                    }
                    Chunk[] res = TheRegion.LoadedChunks.Values.Where((c) => c.PosMultiplier >= 5 && TheRegion.SLODLocFor(c.WorldPosition) == slodpos).ToArray();
                    bool recomp = slod.NeedsComp;
                    foreach (Chunk c in res)
                    {
                        if (c.SLODComputed && c._VBOSolid != null)
                        {
                            recomp = true;
                            c.SLODMode = true;
                            c.SLODComputed = false;
                            slod.NeedsComp = false;
                        }
                    }
                    if (recomp)
                    {
                        if (!VoxelComputer.Combinulate(slod, res))
                        {
                            TheRegion.SLODs.Remove(slodpos);
                        }
                    }
                }
                List<Vector3i> needsRem = new List<Vector3i>();
                foreach (ChunkSLODHelper slod in TheRegion.SLODs.Values)
                {
                    if (slod.NeedsComp)
                    {
                        slod._VBO?.Destroy();
                        needsRem.Add(slod.Coordinate);
                        continue;
                    }
                    int count = TheRegion.LoadedChunks.Values.Where((c) => c.PosMultiplier >= 5 && TheRegion.SLODLocFor(c.WorldPosition) == slod.Coordinate).Count();
                    if (count == 0)
                    {
                        slod._VBO?.Destroy();
                        needsRem.Add(slod.Coordinate);
                        continue;
                    }
                }
                foreach (Vector3i vec in needsRem)
                {
                    TheRegion.SLODs.Remove(vec);
                }
            }
            else
            {
                foreach (ChunkSLODHelper slod in new List<ChunkSLODHelper>(TheRegion.SLODs.Values))
                {
                    if (slod.NeedsComp)
                    {
                        slod.CompileInternal();
                        if (slod._VBO == null)
                        {
                            TheRegion.SLODs.Remove(slod.Coordinate);
                        }
                    }
                    else if (ops_extra > 3 && ((slod.Coordinate.ToLocation() + new Location(0.5, 0.5, 0.5)) * Constants.CHUNK_SLOD_WIDTH).DistanceSquared(Player.GetPosition()) < Constants.CHUNK_SLOD_WIDTH * Constants.CHUNK_SLOD_WIDTH * 2)
                    {
                        TheRegion.RecalcSLODExact(slod.Coordinate);
                    }
                }
            }
            if (ops_extra > 3)
            {
                ops_extra = 0;
            }
        }

        int ops_extra = 0;

        /// <summary>
        /// Once per second spike counter. Used to reset the spike timings (debug display).
        /// </summary>
        int ops_spike = 0;

        /// <summary>
        /// Locker object to avoid multiple saves at once.
        /// </summary>
        Object saveLock = new Object();

        /// <summary>
        /// Run the File save for client settings, generally should be done off-thread.
        /// </summary>
        public void SaveCFG()
        {
            try
            {
                lock (saveLock)
                {
                    Files.WriteText("clientdefaultsettings.cfg", HSaveStr + CSaveStr + BSaveStr + GPBSaveStr);
                }
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.ERROR, "Saving settings: " + ex.ToString());
            }
        }

        /// <summary>
        /// The time, in seconds, since the client started.
        /// </summary>
        public double GlobalTickTimeLocal = 1;

        /// <summary>
        /// The delta time (time between this frame and the previous) for the client tick.
        /// This will be modified by the timescale and any other delta modifiers.
        /// </summary>
        public double Delta;

        /// <summary>
        /// How much the gamepad should vibrate, from 0 to 1.
        /// </summary>
        public float GamePadVibration = 0f;
        
        /// <summary>
        /// Handles the full main tick.
        /// </summary>
        /// <param name="delt">The current delta timings (See Delta).</param>
        void Tick (double delt)
        {
            lock (TickLock)
            {
                Delta = delt * CVars.g_timescale.ValueD;
                GlobalTickTimeLocal += Delta;
                Engine.GlobalTickTime = GlobalTickTimeLocal;
                try
                {
                    opsat += Delta;
                    if (opsat >= 1)
                    {
                        opsat -= 1;
                        OncePerSecondActions();
                    }
                    Schedule.RunAllSyncTasks(Delta);
                    Textures.Update(GlobalTickTimeLocal);
                    Shaders.Update(GlobalTickTimeLocal);
                    Models.Update(GlobalTickTimeLocal);
                    KeyHandler.Tick();
                    if (RawGamePad != null)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (GamePad.GetCapabilities(i).IsConnected)
                            {
                                RawGamePad.SetVibration(i, GamePadVibration, GamePadVibration);
                            }
                        }
                    }
                    Gamepad.Tick(Delta);
                    MouseHandler.Tick();
                    UIConsole.Tick();
                    Commands.Tick();
                    TickWorld(Delta);
                    TickChatSystem();
                    TickInvMenu();
                    CWindow.ClientEngineTick();
                    CWindow.MouseX = MouseHandler.MouseX();
                    CWindow.MouseY = MouseHandler.MouseY();
                    CScreen.FullTick(Delta);
                    Sounds.Update(MainWorldView.CameraPos, MainWorldView.CameraTarget - MainWorldView.CameraPos, MainWorldView.CameraUp(), Player.GetVelocity(), Window.Focused);
                    Schedule.RunAllSyncTasks(0);
                    Player.PostTick();
                    TheRegion.SolveJoints();
                    //ProcessChunks();
                }
                catch (Exception ex)
                {
                    SysConsole.Output(OutputType.ERROR, "Ticking: " + ex.ToString());
                }
                PlayerEyePosition = Player.ItemSource();
                Location forw = Player.ItemDir();
                bool h = TheRegion.SpecialCaseRayTrace(PlayerEyePosition, forw, 100, MaterialSolidity.ANY, IgnorePlayer, out RayCastResult rcr);
                Location loc = new Location(rcr.HitData.Location);
                Location nrm = new Location(rcr.HitData.Normal);
                h = h && !loc.IsInfinite() && !loc.IsNaN() && nrm.LengthSquared() > 0;
                if (h)
                {
                    nrm = nrm.Normalize();
                }
                CameraFinalTarget = h ? loc - nrm * 0.01: PlayerEyePosition + forw * 100;
                CameraImpactNormal = h ? new Location(rcr.HitData.Normal).Normalize() : Location.Zero;
                CameraDistance = h ? rcr.HitData.T: 100;
                if (CameraDistance <= 0.01)
                {
                    CameraDistance = 0.01;
                }
                double cping = Math.Max(LastPingValue, GlobalTickTimeLocal - LastPingTime);
                AveragePings.Push(new KeyValuePair<double, double>(GlobalTickTimeLocal, cping));
                while ((GlobalTickTimeLocal - AveragePings.Peek().Key) > 1)
                {
                    AveragePings.Pop();
                }
                APing = 0;
                for (int i = 0; i < AveragePings.Length; i++)
                {
                    APing += AveragePings[i].Value;
                }
                APing /= (double)AveragePings.Length;
                if (FogEnhanceTime > 0.0)
                {
                    FogEnhanceTime -= Delta;
                }
                else
                {
                    FogEnhanceTime = 0.0;
                }
            }
        }

        /// <summary>
        /// The location of the player's eye (where their camera sees from).
        /// </summary>
        public Location PlayerEyePosition;

        /// <summary>
        /// The 3D vector normal of what the player's camera is looking at.
        /// </summary>
        public Location CameraImpactNormal;

        /// <summary>
        /// Used for physics ray traces when the main player and connected objects should not be considered solid.
        /// Particularly useful for camera position offsetting.
        /// </summary>
        bool IgnorePlayer(BEPUphysics.BroadPhaseEntries.BroadPhaseEntry entry)
        {
            if (entry is EntityCollidable)
            {
                Entity e = (Entity)((EntityCollidable)entry).Entity.Tag;
                if (e == Player || e == Player.Vehicle)
                {
                    return false;
                }
            }
            return CollisionUtil.ShouldCollide(entry);
        }

        /// <summary>
        /// Where the player is currently targeting in the 3D world, in precise 3D coordinates.
        /// </summary>
        public Location CameraFinalTarget;

        /// <summary>
        /// How far the camera is from the CameraFinalTarget.
        /// </summary>
        public double CameraDistance;

        /// <summary>
        /// Resets the game region. Particularly useful when moving between regions.
        /// </summary>
        public void Resetregion()
        {
            Items.Clear();
            UpdateInventoryMenu();
            QuickBarPos = 0;
            BuildWorld();
        }

        /// <summary>
        /// The GlobalTickTimeLocal value for when the last client/server ping arrived.
        /// </summary>
        public double LastPingTime = 0;

        /// <summary>
        /// What the client/server ping currently is as it was most recently measured.
        /// </summary>
        public double LastPingValue = 0;

        /// <summary>
        /// List data to help calculate the average client/server ping.
        /// </summary>
        public ListQueue<KeyValuePair<double, double>> AveragePings = new ListQueue<KeyValuePair<double, double>>();

        /// <summary>
        /// The current average client/server ping.
        /// </summary>
        public double APing = 0;
    }
}
