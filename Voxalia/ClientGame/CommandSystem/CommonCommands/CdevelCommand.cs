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
using Voxalia.ClientGame.ClientMainSystem;
using FreneticScript.CommandSystem;
using Voxalia.Shared;
using Voxalia.ClientGame.WorldSystem;
using FreneticScript.TagHandlers;
using Voxalia.Shared.Collision;
using FreneticScript.TagHandlers.Objects;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.ClientGame.OtherSystems;
using OpenTK;
using BEPUphysics;
using FreneticGameCore;
using FreneticGameCore.Collision;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using FreneticGameGraphics.AudioSystem;
using FreneticGameGraphics.GraphicsHelpers;

namespace Voxalia.ClientGame.CommandSystem.CommonCommands
{
    public class CdevelCommand: AbstractCommand
    {
        Client TheClient;

        public CdevelCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "cdevel";
            Description = "Clientside developmental commands.";
            Arguments = "";
        }

        public static void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.Arguments.Count < 1)
            {
                ShowUsage(queue, entry);
                return;
            }
            Client TheClient = (entry.Command as CdevelCommand).TheClient;
            switch (entry.GetArgument(queue, 0))
            {
                case "lightDebug":
                    {
                        Location pos = TheClient.Player.GetPosition();
                        pos.Z = pos.Z + 1;
                        int XP = (int)Math.Floor(pos.X / Chunk.CHUNK_SIZE);
                        int YP = (int)Math.Floor(pos.Y / Chunk.CHUNK_SIZE);
                        int ZP = (int)Math.Floor(pos.Z / Chunk.CHUNK_SIZE);
                        int x = (int)(Math.Floor(pos.X) - (XP * Chunk.CHUNK_SIZE));
                        int y = (int)(Math.Floor(pos.Y) - (YP * Chunk.CHUNK_SIZE));
                        int z = (int)(Math.Floor(pos.Z) - (ZP * Chunk.CHUNK_SIZE));
                        while (true)
                        {
                            Chunk ch = TheClient.TheRegion.GetChunk(new Vector3i(XP, YP, ZP));
                            if (ch == null)
                            {
                                entry.Good(queue, "Passed with flying light sources!");
                                goto end;
                            }
                            while (z < Chunk.CHUNK_SIZE)
                            {
                                if (ch.GetBlockAt((int)x, (int)y, (int)z).IsOpaque())
                                {
                                    entry.Info(queue, "Died: " + x + ", " + y + ", " + z + " -- " + XP + ", " + YP + ", " + ZP);
                                    goto end;
                                }
                                z++;
                            }
                            ZP++;
                            z = 0;
                        }
                        end:
                        break;
                    }
                case "vramUsage":
                    {
                        long c = 0;
                        foreach (Tuple<string, long> val in TheClient.CalculateVRAMUsage())
                        {
                            entry.Info(queue, "-> " + val.Item1 + ": " + val.Item2 + " (" + (val.Item2 / 1024 / 1024) + "MB)");
                            c += val.Item2;
                        }
                        entry.Info(queue, "-> Total: " + c + " (" + (c / 1024 / 1024) + "MB)");
                        break;
                    }
                case "speakText":
                    {
                        if (entry.Arguments.Count < 3)
                        {
                            ShowUsage(queue, entry);
                            break;
                        }
                        bool male = !entry.GetArgument(queue, 1).ToString().ToLowerFast().StartsWith("f");
                        TextToSpeech.Speak(entry.GetArgument(queue, 2), male, entry.Arguments.Count > 3 ? (int)IntegerTag.TryFor(entry.GetArgumentObject(queue, 3)).Internal : 0);
                        break;
                    }
                case "chunkInfo":
                    {
                        Chunk ch = TheClient.TheRegion.GetChunk(TheClient.TheRegion.ChunkLocFor(TheClient.Player.GetPosition()));
                        if (ch == null)
                        {
                            entry.Info(queue, "Chunk is null!");
                            break;
                        }
                        Vector3i chunk_pos = ch.WorldPosition;
                        ChunkSLODHelper slod = TheClient.TheRegion.GetSLODHelp(ch.WorldPosition, false);
                        if (slod == null)
                        {
                            entry.Info(queue, "No SLOD.");
                        }
                        else
                        {
                            bool isgen = slod._VBO != null && slod._VBO.generated;
                            entry.Info(queue, "SLOD: "+ slod.Coordinate + ", live chunks contained: " + slod.Users + ", verts: " + slod.FullBlock.Vertices.Count + ", generated: " + isgen);
                            foreach (KeyValuePair<Vector3i, Chunk> entryx in TheClient.TheRegion.LoadedChunks)
                            {
                                if (entryx.Value.PosMultiplier < 5)
                                {
                                    continue;
                                }
                                Vector3i slodposser = new Vector3i((int)Math.Floor(entryx.Key.X / (float)Constants.CHUNKS_PER_SLOD), (int)Math.Floor(entryx.Key.Y / (float)Constants.CHUNKS_PER_SLOD), (int)Math.Floor(entryx.Key.Z / (float)Constants.CHUNKS_PER_SLOD));
                                if (slodposser == slod.Coordinate)
                                {
                                    entry.Info(queue, "Chunk at " + entryx.Key + " is held");
                                }
                            }
                        }
                        entry.Info(queue, "Plants: " + ch.Plant_C + ", generated as ID: " + ch.Plant_VAO);
                        int c = 0;
                        long verts = 0;
                        long verts_transp = 0;
                        int total = 0;
                        int total_rendered = 0;
                        int total_rendered_transp = 0;
                        foreach (Chunk chunk in TheClient.TheRegion.LoadedChunks.Values)
                        {
                            total++;
                            if (chunk._VBOSolid != null && ch._VBOSolid != null && chunk._VBOSolid._VAO == ch._VBOSolid._VAO)
                            {
                                c++;
                            }
                            if (chunk._VBOSolid != null && chunk._VBOSolid.generated)
                            {
                                verts += chunk._VBOSolid.vC;
                                total_rendered++;
                            }
                            if (chunk._VBOTransp != null && chunk._VBOTransp.generated)
                            {
                                verts_transp += chunk._VBOTransp.vC;
                                total_rendered_transp++;
                            }
                        }
                        entry.Info(queue, "Chunk rendering as " + (ch._VBOSolid == null ? "{NULL}" : ch._VBOSolid._VAO.ToString()) + ", which is seen in " + c + " chunks!");
                        entry.Info(queue, "Chunks: " + total + ", rendering " + verts + " solid verts and " + verts_transp +
                            " transparent verts, with " + total_rendered + " solid-existent chunks, and " + total_rendered_transp + " transparent-existent chunks!");
                        break;
                    }
                case "blockInfo":
                    {
                        BlockInternal bi = TheClient.TheRegion.GetBlockInternal(TheClient.Player.GetPosition());
                        entry.Info(queue, "BLOCK: Material=" + bi.Material + ", Shape=" + bi.BlockData + ", Damage=" + bi.Damage + ", Paint=" + bi.BlockPaint + ",Light=" + bi.BlockLocalData);
                        break;
                    }
                case "igniteBlock":
                    {
                        Location pos = TheClient.Player.GetPosition().GetUpperBlockBorder();
                        FireEntity fe = new FireEntity(pos, null, TheClient.TheRegion);
                        TheClient.TheRegion.SpawnEntity(fe);
                        break;
                    }
                case "torchBlocks":
                    {
                        Location pos = TheClient.Player.GetPosition().GetUpperBlockBorder();
                        for (int x = -3; x <= 3; x++)
                        {
                            for (int y = -3; y <= 3; y++)
                            {
                                FireEntity fe = new FireEntity(pos + new Location(x, y, 0), null, TheClient.TheRegion);
                                TheClient.TheRegion.SpawnEntity(fe);
                            }
                        }
                        break;
                    }
                case "lateVR":
                    {
                        if (!VRSupport.Available())
                        {
                            entry.Info(queue, "Can't load VR. Not available!");
                            break;
                        }
                        if (TheClient.VR != null)
                        {
                            entry.Info(queue, "Can't load VR. Already loaded!");
                            break;
                        }
                        TheClient.VR = VRSupport.TryInit(TheClient.CWindow);
                        TheClient.CWindow.VR = TheClient.VR;
                        if (TheClient.VR != null)
                        {
                            TheClient.VR.VRScale = 1.5f; // TODO: VR Scale CVar?
                        }
                        break;
                    }
                case "debugVR":
                    {
                        if (TheClient.VR == null)
                        {
                            entry.Info(queue, "VR not running!");
                            break;
                        }
                        entry.Info(queue, "Left: " + TheClient.VR.Left?.ToString() + " ||| Right: " + TheClient.VR.Right?.ToString());
                        break;
                    }
                case "fogEnhance":
                    {
                        double time = NumberTag.TryFor(entry.GetArgumentObject(queue, 1)).Internal;
                        double fogVal = NumberTag.TryFor(entry.GetArgumentObject(queue, 2)).Internal;
                        TheClient.FogEnhanceStrength = (float)fogVal;
                        TheClient.FogEnhanceTime = time;
                        break;
                    }
                case "flashBang":
                    {
                        double time = NumberTag.TryFor(entry.GetArgumentObject(queue, 1)).Internal;
                        TheClient.MainWorldView.Flashbang(time);
                        break;
                    }
                case "earRing":
                    {
                        double time = NumberTag.TryFor(entry.GetArgumentObject(queue, 1)).Internal;
                        // TODO: Fix!
                        //TheClient.Sounds.Deafen(time);
                        break;
                    }
                case "topInfo":
                    {
                        Location pos = TheClient.Player.GetPosition().GetBlockLocation();
                        Vector3i chunkLoc = TheClient.TheRegion.ChunkLocFor(pos);
                        Vector2i buaPos = new Vector2i(chunkLoc.X, chunkLoc.Y);
                        if (!TheClient.TheRegion.UpperAreas.TryGetValue(buaPos, out BlockUpperArea bua))
                        {
                            entry.Info(queue, "Failed to grab Top data: Out of map?");
                        }
                        else
                        {
                            entry.Info(queue, pos + ": " + bua.Blocks[bua.BlockIndex((int)pos.X - chunkLoc.X * Chunk.CHUNK_SIZE, (int)pos.Y - chunkLoc.Y * Chunk.CHUNK_SIZE)]);
                        }
                        break;
                    }
                case "testDecal":
                    {
                        Location pos = TheClient.Player.GetPosition() + new Location(0, 0, BEPUphysics.Settings.CollisionDetectionSettings.AllowedPenetration);
                        TheClient.AddDecal(pos, new Location(0, 0, 1), Vector4.One, 1f, "white", 15);
                        break;
                    }
                case "traceDecal":
                    {
                        Location pos = TheClient.Player.GetEyePosition();
                        Location forw = TheClient.Player.ForwardVector();
                        if (TheClient.TheRegion.SpecialCaseRayTrace(pos, forw, 50.0f, MaterialSolidity.FULLSOLID, TheClient.Player.IgnoreThis, out RayCastResult rcr))
                        {
                            Location nrm = new Location(rcr.HitData.Normal).Normalize();
                            TheClient.AddDecal(new Location(rcr.HitData.Location) + nrm * 0.005, nrm, Vector4.One, 1f, "white", 15);
                            entry.Info(queue, "Marked at normal " + nrm);
                        }
                        break;
                    }
                case "traceDecalTarg":
                    {
                        Location pos = TheClient.Player.GetEyePosition();
                        Location forw = TheClient.Player.ForwardVector();
                        if (TheClient.TheRegion.SpecialCaseRayTrace(pos, forw, 50.0f, MaterialSolidity.FULLSOLID, TheClient.Player.IgnoreThis, out RayCastResult rcr))
                        {
                            Location nrm = new Location(rcr.HitData.Normal).Normalize();
                            TheClient.AddDecal(new Location(rcr.HitData.Location) + nrm * 0.005, nrm, Vector4.One, 1f, "decal_target", 15);
                            entry.Info(queue, "Marked at normal " + nrm);
                        }
                        break;
                    }
                case "soundCount":
                    {
                        entry.Info(queue, "Sound effects: " + TheClient.Sounds.Effects.Count + ", playing now: " + TheClient.Sounds.PlayingNow.Count);
                        break;
                    }
                case "testCompute":
                    {
                        Chunk ch = TheClient.TheRegion.GetChunk(TheClient.TheRegion.ChunkLocFor(TheClient.Player.GetPosition()));
                        if (ch == null)
                        {
                            throw new Exception("Chunk is null!");
                        }
                        TheClient.VoxelComputer.sw1.Reset();
                        TheClient.VoxelComputer.sw1a.Reset();
                        TheClient.VoxelComputer.sw2.Reset();
                        TheClient.VoxelComputer.sw3.Reset();
                        TheClient.VoxelComputer.Calc(ch);
                        entry.Good(queue, "Took: " + TheClient.VoxelComputer.sw1.ElapsedMilliseconds + " (" + TheClient.VoxelComputer.sw1a.ElapsedMilliseconds + ") / "
                            + TheClient.VoxelComputer.sw2.ElapsedMilliseconds + " / " + TheClient.VoxelComputer.sw3.ElapsedMilliseconds);
                        break;
                    }
                case "testComputeAll":
                    {
                        TheClient.VoxelComputer.sw1.Reset();
                        TheClient.VoxelComputer.sw1a.Reset();
                        TheClient.VoxelComputer.sw2.Reset();
                        TheClient.VoxelComputer.sw3.Reset();
                        TheClient.VoxelComputer.Calc(TheClient.TheRegion.LoadedChunks.Values.ToArray());
                        entry.Good(queue, "Took: " + TheClient.VoxelComputer.sw1.ElapsedMilliseconds + " (" + TheClient.VoxelComputer.sw1a.ElapsedMilliseconds + ") / "
                            + TheClient.VoxelComputer.sw2.ElapsedMilliseconds + " / " + TheClient.VoxelComputer.sw3.ElapsedMilliseconds);
                        break;
                    }
                default:
                    ShowUsage(queue, entry);
                    break;
            }
        }
    }
}
