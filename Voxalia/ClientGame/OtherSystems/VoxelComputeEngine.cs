﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.GraphicsSystems;
using OpenTK;
using OpenTK.Graphics;
using FreneticGameCore;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;
using FreneticGameCore.Collision;
using System.Diagnostics;

namespace Voxalia.ClientGame.OtherSystems
{
    public class VoxelComputeEngine
    {
        public int Texture_IDs;

        public Client TheClient;

        public static readonly int[] Reppers = new int[] { 30, 15, 6, 5, 2 };

        public int[] Program_Counter = new int[Reppers.Length];

        public int[] Program_Cruncher1 = new int[Reppers.Length];
        public int[] Program_Cruncher2 = new int[Reppers.Length];
        public int[] Program_Cruncher3 = new int[Reppers.Length];

        public int[] Program_CounterTRANSP = new int[Reppers.Length];

        public int[] Program_CruncherTRANSP1 = new int[Reppers.Length];
        public int[] Program_CruncherTRANSP2 = new int[Reppers.Length];
        public int[] Program_CruncherTRANSP3 = new int[Reppers.Length];

        public int[] Program_Combo = new int[Reppers.Length];
        
        public int Buf_Shapes = 0;

        public static readonly Dictionary<int, int> lookuper = new Dictionary<int, int>(128)
        {
            { 30, 0 },
            { 15, 1 },
            { 6, 2 },
            { 5, 3 },
            { 2, 4 }
        };

        public int[] EmptyChunkRep = new int[Reppers.Length];

        public int ZeroChunkRep;

        public void Init(Client tclient)
        {
            TheClient = tclient;
            for (int i = 0; i < Reppers.Length; i++)
            {
                Program_Counter[i] = TheClient.Shaders.CompileCompute("vox_count", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MCM_SOLIDONLY 1\n");
                Program_Cruncher1[i] = TheClient.Shaders.CompileCompute("vox_crunch", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MODE_ONE 1\n#define MCM_SOLIDONLY 1\n");
                Program_Cruncher2[i] = TheClient.Shaders.CompileCompute("vox_crunch", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MODE_TWO 1\n#define MCM_SOLIDONLY 1\n");
                Program_Cruncher3[i] = TheClient.Shaders.CompileCompute("vox_crunch", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MODE_THREE 1\n#define MCM_SOLIDONLY 1\n");
                Program_CounterTRANSP[i] = TheClient.Shaders.CompileCompute("vox_count", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MCM_TRANSP 1\n");
                Program_CruncherTRANSP1[i] = TheClient.Shaders.CompileCompute("vox_crunch", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MODE_ONE 1\n#define MCM_TRANSP 1\n");
                Program_CruncherTRANSP2[i] = TheClient.Shaders.CompileCompute("vox_crunch", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MODE_TWO 1\n#define MCM_TRANSP 1\n");
                Program_CruncherTRANSP3[i] = TheClient.Shaders.CompileCompute("vox_crunch", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MODE_THREE 1\n#define MCM_TRANSP 1\n");
                Program_Combo[i] = TheClient.Shaders.CompileCompute("vox_combo", "#define MCM_VOX_COUNT " + Reppers[i] + "\n");
            }
            View3D.CheckError("Compute - Startup - Shaders");
            float[] df = new float[MaterialHelpers.ALL_MATS.Count * (6 * 7 + 7)];
            for (int i = 0; i < MaterialHelpers.ALL_MATS.Count; i++)
            {
                for (int x = 0; x < 6; x++)
                {
                    int cnt = Math.Min(6, MaterialHelpers.ALL_MATS[i].TID[x].Length);
                    for (int y = 0; y < cnt; y++)
                    {
                        df[(1 + y + x * 7) * MaterialHelpers.ALL_MATS.Count + i] = MaterialHelpers.ALL_MATS[i].TID[x][y];
                    }
                    df[(x * 7) * MaterialHelpers.ALL_MATS.Count + i] = cnt;
                }
                df[(6 * 7 + 0) * MaterialHelpers.ALL_MATS.Count + i] = MaterialHelpers.ALL_MATS[i].RendersAtAll ? 1 : 0;
                df[(6 * 7 + 1) * MaterialHelpers.ALL_MATS.Count + i] = MaterialHelpers.ALL_MATS[i].Opaque ? 1 : 0;
                df[(6 * 7 + 2) * MaterialHelpers.ALL_MATS.Count + i] = MaterialHelpers.ALL_MATS[i].AnyOpaque ? 1 : 0;
                df[(6 * 7 + 3) * MaterialHelpers.ALL_MATS.Count + i] = (float)MaterialHelpers.ALL_MATS[i].LightEmit.X;
                df[(6 * 7 + 4) * MaterialHelpers.ALL_MATS.Count + i] = (float)MaterialHelpers.ALL_MATS[i].LightEmit.X;
                df[(6 * 7 + 5) * MaterialHelpers.ALL_MATS.Count + i] = (float)MaterialHelpers.ALL_MATS[i].LightEmit.X;
                df[(6 * 7 + 6) * MaterialHelpers.ALL_MATS.Count + i] = (float)MaterialHelpers.ALL_MATS[i].LightEmitRange;
            }
            Texture_IDs = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, Texture_IDs);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, MaterialHelpers.ALL_MATS.Count, 6 * 7 + 7, 0, PixelFormat.Red, PixelType.Float, df);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Finish();
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            View3D.CheckError("Compute - Startup - Texture");
            for (int i = 0; i < Reppers.Length; i++)
            {
                int csize = Reppers[i];
                int[] btemp = new int[csize * csize * csize * 4];
                BlockInternal bi = new BlockInternal((ushort)Material.STONE, 0, 0, 0);
                for (int rz = 0; rz < csize; rz++)
                {
                    for (int ry = 0; ry < csize; ry++)
                    {
                        for (int rx = 0; rx < csize; rx++)
                        {
                            int ind = (rz * (csize * csize) + ry * csize + rx) * 4;
                            btemp[ind + 0] = bi._BlockMaterialInternal;
                            btemp[ind + 1] = bi.BlockLocalData;
                            btemp[ind + 2] = bi.BlockData;
                            btemp[ind + 3] = bi._BlockPaintInternal;
                        }
                    }
                }
                EmptyChunkRep[i] = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, EmptyChunkRep[i]);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, btemp.Length * sizeof(int), btemp, BufferUsageHint.StaticDraw);
            }
            ZeroChunkRep = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ZeroChunkRep);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, (Constants.CHUNK_BLOCK_COUNT) * sizeof(int), IntPtr.Zero, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            View3D.CheckError("Compute - Startup - Empty Buffers");
            float[] preBuf = new float[256 * 12];
            for (int i = 0; i < 256; i++)
            {
                preBuf[i * 12 + 0] = (float)BlockShapeRegistry.BSD[i].RequiresToFill_XP + 0.01f;
                preBuf[i * 12 + 1] = (float)BlockShapeRegistry.BSD[i].RequiresToFill_XM + 0.01f;
                preBuf[i * 12 + 2] = (float)BlockShapeRegistry.BSD[i].RequiresToFill_YP + 0.01f;
                preBuf[i * 12 + 3] = (float)BlockShapeRegistry.BSD[i].RequiresToFill_YM + 0.01f;
                preBuf[i * 12 + 4] = (float)BlockShapeRegistry.BSD[i].RequiresToFill_ZP + 0.01f;
                preBuf[i * 12 + 5] = (float)BlockShapeRegistry.BSD[i].RequiresToFill_ZM + 0.01f;
                preBuf[i * 12 + 6] = (float)BlockShapeRegistry.BSD[i].AbleToFill_XP + 0.01f;
                preBuf[i * 12 + 7] = (float)BlockShapeRegistry.BSD[i].AbleToFill_XM + 0.01f;
                preBuf[i * 12 + 8] = (float)BlockShapeRegistry.BSD[i].AbleToFill_YP + 0.01f;
                preBuf[i * 12 + 9] = (float)BlockShapeRegistry.BSD[i].AbleToFill_YM + 0.01f;
                preBuf[i * 12 + 10] = (float)BlockShapeRegistry.BSD[i].AbleToFill_ZP + 0.01f;
                preBuf[i * 12 + 11] = (float)BlockShapeRegistry.BSD[i].AbleToFill_ZM + 0.01f;
            }
            float[] buf = new float[preBuf.Length + 256 * 64 * 4];
            preBuf.CopyTo(buf, 0);
            int coord = buf.Length;
            for (int shape = 0; shape < 256; shape++)
            {
                for (int damage = 0; damage < 4; damage++)
                {
                    BlockShapeSubDetails bssd = BlockShapeRegistry.BSD[shape].Damaged[damage].BSSD;
                    for (int subDat = 0; subDat < 64; subDat++)
                    {
                        int id = preBuf.Length + shape * (64 * 4) + subDat * 4 + damage;
                        int len = bssd.Verts[subDat].Count * 9 + 1;
                        buf[id] = BitConverter.ToSingle(BitConverter.GetBytes(coord), 0);
                        coord += len;
                    }
                }
            }
            float[] resX = new float[buf.Length + coord];
            buf.CopyTo(resX, 0);
            coord = buf.Length;
            for (int shape = 0; shape < 256; shape++)
            {
                for (int damage = 0; damage < 4; damage++)
                {
                    BlockShapeSubDetails bssd = BlockShapeRegistry.BSD[shape].Damaged[damage].BSSD;
                    for (int subDat = 0; subDat < 64; subDat++)
                    {
                        int cnt = bssd.Verts[subDat].Count;
                        resX[coord] = BitConverter.ToSingle(BitConverter.GetBytes(cnt), 0);
                        for (int subvert = 0; subvert < cnt; subvert++)
                        {
                            resX[coord + 1 + cnt * 0 + subvert * 3 + 0] = (float)bssd.Verts[subDat][subvert].X;
                            resX[coord + 1 + cnt * 0 + subvert * 3 + 1] = (float)bssd.Verts[subDat][subvert].Y;
                            resX[coord + 1 + cnt * 0 + subvert * 3 + 2] = (float)bssd.Verts[subDat][subvert].Z;
                            resX[coord + 1 + cnt * 3 + subvert * 3 + 0] = (float)bssd.Norms[subDat][subvert].X;
                            resX[coord + 1 + cnt * 3 + subvert * 3 + 1] = (float)bssd.Norms[subDat][subvert].Y;
                            resX[coord + 1 + cnt * 3 + subvert * 3 + 2] = (float)bssd.Norms[subDat][subvert].Z;
                            resX[coord + 1 + cnt * 6 + subvert * 3 + 0] = (float)bssd.TCrds[subDat][subvert].X;
                            resX[coord + 1 + cnt * 6 + subvert * 3 + 1] = (float)bssd.TCrds[subDat][subvert].Y;
                            resX[coord + 1 + cnt * 6 + subvert * 3 + 2] = (float)bssd.TCrds[subDat][subvert].Z;
                        }
                        coord += cnt * 9 + 1;
                    }
                }
            }
            Buf_Shapes = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, Buf_Shapes);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, resX.Length * sizeof(float), resX, BufferUsageHint.StaticRead);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            View3D.CheckError("Compute - Startup - Shape Buffer");
        }

        public static readonly Vector3i[] Relatives = new Vector3i[]
        {
            new Vector3i(0, 0, 0),
            new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
            new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
            new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
        };

        const BufferUsageHint hintter = BufferUsageHint.DynamicRead;

        public Stopwatch sw1 = new Stopwatch(), sw2 = new Stopwatch(), sw3 = new Stopwatch(), sw1a = new Stopwatch();

        byte[] EmptyBytes = new byte[0];

        public void Calc(params Chunk[] chs)
        {
            int maxRad = TheClient.CVars.r_renderdist.ValueI;
            Vector3i centerChunk = TheClient.TheRegion.ChunkLocFor(TheClient.Player.GetPosition());
            centerChunk = new Vector3i(-centerChunk.X, -centerChunk.Y, -centerChunk.Z);
            sw1.Start();
            // Create voxel buffer data
            for (int chz = 0; chz < chs.Length; chz++)
            {
                Chunk ch = chs[chz];
                ch.Render_BufsRel = new int[7];
                int len = ch.CSize * ch.CSize * ch.CSize * 4;
                for (int x = 0; x < Relatives.Length; x++)
                {
                    Chunk rel = x == 0 ? ch : TheClient.TheRegion.GetChunk(ch.WorldPosition + Relatives[x]);
                    int[] btemp;
                    if (rel == null)
                    {
                        Vector3i reldist = ch.WorldPosition + Relatives[x] + centerChunk;
                        ch.Render_BufsRel[x] = TheClient.TheRegion.AirChunks.Contains(ch.WorldPosition + Relatives[x])
                            ? ZeroChunkRep : EmptyChunkRep[lookuper[ch.CSize]];
                    }
                    else if (x == 0 || rel.Render_VoxelBuffer == null || rel.Render_VoxelBuffer[lookuper[ch.CSize]] <= 0)
                    {
                        btemp = new int[len];
                        for (int rz = 0; rz < ch.CSize; rz++)
                        {
                            for (int ry = 0; ry < ch.CSize; ry++)
                            {
                                for (int rx = 0; rx < ch.CSize; rx++)
                                {
                                    BlockInternal bi = ch.GetLODRelative(rel, rx, ry, rz);
                                    int ind = (rz * (ch.CSize * ch.CSize) + ry * ch.CSize + rx) * 4;
                                    btemp[ind + 0] = bi._BlockMaterialInternal;
                                    btemp[ind + 1] = bi.BlockLocalData;
                                    btemp[ind + 2] = bi.BlockData;
                                    btemp[ind + 3] = bi._BlockPaintInternal;
                                }
                            }
                        }
                        int VoxelBuffer = GL.GenBuffer();
                        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, VoxelBuffer);
                        GL.BufferData(BufferTarget.ShaderStorageBuffer, btemp.Length * sizeof(int), btemp, BufferUsageHint.StaticDraw);
                        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                        if (rel.Render_VoxelBuffer == null)
                        {
                            rel.Render_VoxelBuffer = new int[Reppers.Length];
                        }
                        rel.Render_VoxelBuffer[lookuper[ch.CSize]] = VoxelBuffer;
                        ch.Render_BufsRel[x] = VoxelBuffer;
                    }
                    else
                    {
                        ch.Render_BufsRel[x] = rel.Render_VoxelBuffer[lookuper[ch.CSize]];
                    }
                }
                View3D.CheckError("Compute - Prep -1");
                // Combine buffers
                sw1a.Start();
                int fbufVoxels = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, fbufVoxels);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, len * 7 * sizeof(uint), IntPtr.Zero, hintter);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                View3D.CheckError("Compute - Prep -0.6");
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, fbufVoxels);
                View3D.CheckError("Compute - Prep -0.5");
                for (int i = 0; i < 7; i++)
                {
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1 + i, ch.Render_BufsRel[i]);
                }
                View3D.CheckError("Compute - Prep -0.4");
                GL.UseProgram(Program_Combo[lookuper[ch.CSize]]);
                View3D.CheckError("Compute - Prep -0.37");
                GL.DispatchCompute(1, ch.CSize, 1);
                View3D.CheckError("Compute - Prep -0.35");
                GL.UseProgram(0);
                View3D.CheckError("Compute - Prep -0.3");
                for (int i = 0; i < 8; i++)
                {
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, i, 0);
                }
                ch.Render_FBB = fbufVoxels;
                View3D.CheckError("Compute - Prep -0.25");
                sw1a.Stop();
            }
            sw1.Stop();
            sw2.Start();
            View3D.CheckError("Compute - Prep 0");
            GL.BindImageTexture(0, Texture_IDs, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.R32f);
            // Create a results buffer
            for (int chz = 0; chz < chs.Length; chz++)
            {
                Chunk ch = chs[chz];
                ch.Render_IndBuf = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ch.Render_IndBuf);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, ch.CSize * ch.CSize * ch.CSize * sizeof(uint), IntPtr.Zero, hintter);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                GL.UseProgram(Program_Counter[lookuper[ch.CSize]]);
                int resBuf = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, resBuf);
                uint[] resses = new uint[1];
                GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(uint), resses, hintter);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                View3D.CheckError("Compute - Run - Setup A");
                // Run the shader
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, ch.Render_FBB);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, resBuf);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, ch.Render_IndBuf);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, Buf_Shapes);
                GL.DispatchCompute(1, ch.CSize, 1);
                ch.Render_ResBuf = resBuf;
                View3D.CheckError("Compute - Run - Ran A");
                // TRANSPARENT MODE:
                ch.Render_IndBufTRANSP = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ch.Render_IndBufTRANSP);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, ch.CSize * ch.CSize * ch.CSize * sizeof(uint), IntPtr.Zero, hintter);
                int resBufTRANSP = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, resBufTRANSP);
                uint[] ressesTRANSP = new uint[1];
                GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(uint), ressesTRANSP, hintter);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                GL.UseProgram(Program_CounterTRANSP[lookuper[ch.CSize]]);
                View3D.CheckError("Compute - Run - Setup B");
                // Run the shader
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, resBufTRANSP);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, ch.Render_IndBufTRANSP);
                GL.DispatchCompute(1, ch.CSize, 1);
                ch.Render_ResBufTRANSP = resBufTRANSP;
                View3D.CheckError("Compute - Run - Ran B");
            }
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, 0);
            GL.UseProgram(0);
            View3D.CheckError("Compute - Run");
            // Gather results
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            /*if (!TheClient.Shaders.MCM_GOOD_GRAPHICS)
            {
                GL.Finish();
                GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            }*/
            for (int chz = 0; chz < chs.Length; chz++)
            {
                uint[] resses = new uint[1];
                uint[] ressesTRANSP = new uint[1];
                Chunk ch = chs[chz];
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ch.Render_ResBuf);
                GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(uint), resses);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ch.Render_ResBufTRANSP);
                GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(uint), ressesTRANSP);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                ch.CountForRender = (int)resses[0];
                ch.CountForRenderTRANSP = (int)ressesTRANSP[0];
                //Console.WriteLine("Found: " + ch.CountForRender + ", transp: " + ch.CountForRenderTRANSP);
            }
            sw2.Stop();
            sw3.Start();
            View3D.CheckError("Compute - Read Count");
            // Start new buffers
            for (int chz = 0; chz < chs.Length; chz++)
            {
                Chunk ch = chs[chz];
                int resdSOLID = ch.CountForRender;
                int resdTRANSP = ch.CountForRenderTRANSP;
                if (resdSOLID == 0)
                {
                    ch._VBOSolid?.Destroy();
                    ch._VBOSolid = null;
                    // Clean up buffers
                    GL.DeleteBuffer(ch.Render_ResBuf);
                    GL.DeleteBuffer(ch.Render_IndBuf);
                    ch.Render_IndBuf = 0;
                    ch.Render_ResBuf = 0;
                }
                if (resdTRANSP == 0)
                {
                    ch._VBOTransp?.Destroy();
                    ch._VBOTransp = null;
                    // Clean up buffers
                    GL.DeleteBuffer(ch.Render_ResBufTRANSP);
                    GL.DeleteBuffer(ch.Render_IndBufTRANSP);
                    ch.Render_IndBufTRANSP = 0;
                    ch.Render_ResBufTRANSP = 0;
                }
                if (resdSOLID == 0 && resdTRANSP == 0)
                {
                    GL.DeleteBuffer(ch.Render_FBB);
                    ch.Render_BufsRel = null;
                    continue;
                }
                int lbuf = 0;
                if (ch.CSize == Constants.CHUNK_WIDTH)
                {
                    List<Chunk> potentials = new List<Chunk>();
                    int cnt = 0;
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            for (int z = -1; z <= 1; z++)
                            {
                                Chunk tch = TheClient.TheRegion.GetChunk(ch.WorldPosition + new Vector3i(x, y, z));
                                if (tch != null && tch.Lits.Length > 0)
                                {
                                    cnt += tch.Lits.Length;
                                    potentials.Add(tch);
                                }
                            }
                        }
                    }
                    int id = 1;
                    uint[] bres = new uint[cnt * 7 + 1];
                    bres[0] = (uint)cnt;
                    foreach (Chunk tch in potentials)
                    {
                        for (int i = 0; i < tch.Lits.Length; i++)
                        {
                            KeyValuePair<Vector3i, Material> lit = tch.Lits[i];
                            bres[id + 0] = (uint)(lit.Key.X + ((tch.WorldPosition.X - ch.WorldPosition.X) * Constants.CHUNK_WIDTH));
                            bres[id + 1] = (uint)(lit.Key.Y + ((tch.WorldPosition.Y - ch.WorldPosition.Y) * Constants.CHUNK_WIDTH));
                            bres[id + 2] = (uint)(lit.Key.Z + ((tch.WorldPosition.Z - ch.WorldPosition.Z) * Constants.CHUNK_WIDTH));
                            bres[id + 3] = BitConverter.ToUInt32(BitConverter.GetBytes((float)lit.Value.GetLightEmit().X), 0);
                            bres[id + 4] = BitConverter.ToUInt32(BitConverter.GetBytes((float)lit.Value.GetLightEmit().Y), 0);
                            bres[id + 5] = BitConverter.ToUInt32(BitConverter.GetBytes((float)lit.Value.GetLightEmit().Z), 0);
                            bres[id + 6] = BitConverter.ToUInt32(BitConverter.GetBytes((float)lit.Value.GetLightEmitRange()), 0);
                            id += 7;
                        }
                    }
                    lbuf = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, lbuf);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, bres.Length * sizeof(uint), bres, BufferUsageHint.StaticRead);
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                }
                Action<bool> calc = (transp) =>
                {
                    View3D.CheckError("Compute - Pre New Buffers");
                    int resd = transp ? resdTRANSP : resdSOLID;
                    if (resd < 0 || resd > 100 * 1000 * 1000)
                    {
                        SysConsole.Output(OutputType.WARNING, "Tried to render chunk of " + resd + " polygons! Denied! (May read as float: " + 
                            BitConverter.ToSingle(BitConverter.GetBytes((uint)resd), 0) + ")");
                        return;
                    }
                    /*
                    else
                    {
                        SysConsole.Output(OutputType.DEBUG, "Passing for " + resd);
                    }*/
                    if (resd == 0)
                    {
                        return;
                    }
                    byte[] empty = null;
                    /*if (!TheClient.Shaders.MCM_GOOD_GRAPHICS)
                    {
                        SysConsole.Output(OutputType.DEBUG, "Alloc: " + resd);
                        if (EmptyBytes.Length < resd * Vector4.SizeInBytes)
                        {
                            EmptyBytes = new byte[resd * Vector4.SizeInBytes];
                        }
                        empty = EmptyBytes;
                    }*/
                    int bufc = ch.PosMultiplier == 1 ? 11 : 7;
                    uint[] newBufs = new uint[bufc];
                    GL.GenBuffers(bufc, newBufs);
                    View3D.CheckError("Compute - New Buffers - Prep");
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[0]);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, empty, hintter);
                    View3D.CheckError("Compute - New Buffers - 0");
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[1]);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, empty, hintter);
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[2]);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, empty, hintter);
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[3]);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, empty, hintter);
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[4]);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, empty, hintter);
                    View3D.CheckError("Compute - New Buffers - 4");
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[5]);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, empty, hintter);
                    if (ch.PosMultiplier == 1)
                    {
                        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[6]);
                        GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, empty, hintter);
                        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[7]);
                        GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, empty, hintter);
                        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[9]);
                        GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, empty, hintter);
                        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[10]);
                        GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, empty, hintter);
                    }
                    View3D.CheckError("Compute - New Buffers - 7");
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[ch.PosMultiplier == 1 ? 8 : 6]);
                    GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * sizeof(uint), empty, hintter);
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                    View3D.CheckError("Compute - New Buffers");
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, ch.Render_FBB);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, transp ? ch.Render_IndBufTRANSP : ch.Render_IndBuf);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, Buf_Shapes);
                    // Run computation MODE_ONE
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, newBufs[0]);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, newBufs[1]);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, newBufs[2]);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, newBufs[3]);
                    GL.UseProgram(transp ? Program_CruncherTRANSP1[lookuper[ch.CSize]] : Program_Cruncher1[lookuper[ch.CSize]]);
                    GL.DispatchCompute(1, ch.CSize, 1);
                    // Run computation MODE_TWO
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, newBufs[4]);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, newBufs[5]);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, newBufs[ch.PosMultiplier == 1 ? 8 : 6]);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, ch.CSize == Constants.CHUNK_WIDTH ? lbuf : ZeroChunkRep);
                    GL.UseProgram(transp ? Program_CruncherTRANSP2[lookuper[ch.CSize]] : Program_Cruncher2[lookuper[ch.CSize]]);
                    GL.DispatchCompute(1, ch.CSize, 1);
                    // Run computation MODE_THREE
                    if (ch.PosMultiplier == 1)
                    {
                        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, newBufs[6]);
                        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, newBufs[7]);
                        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, newBufs[9]);
                        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, newBufs[10]);
                        GL.UseProgram(transp ? Program_CruncherTRANSP3[lookuper[ch.CSize]] : Program_Cruncher3[lookuper[ch.CSize]]);
                        GL.DispatchCompute(1, ch.CSize, 1);
                    }
                    GL.UseProgram(0);
                    View3D.CheckError("Compute - Crunch");
                    // Unbind new buffers
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, 0);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, 0);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, 0);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, 0);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, 0);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, 0);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, 0);
                    View3D.CheckError("Compute - End Buffs");
                    // Prep VAO
                    int vao = GL.GenVertexArray();
                    GL.BindVertexArray(vao);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[0]);
                    GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[1]);
                    GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[2]);
                    GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 0, 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[3]);
                    GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 0, 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[4]);
                    GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 0, 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[5]);
                    GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, 0, 0);
                    if (ch.PosMultiplier == 1)
                    {
                        GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[6]);
                        GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, 0, 0);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[7]);
                        GL.VertexAttribPointer(7, 4, VertexAttribPointerType.Float, false, 0, 0);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[9]);
                        GL.VertexAttribPointer(8, 4, VertexAttribPointerType.Float, false, 0, 0);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[10]);
                        GL.VertexAttribPointer(9, 4, VertexAttribPointerType.Float, false, 0, 0);
                    }
                    GL.EnableVertexAttribArray(0);
                    GL.EnableVertexAttribArray(1);
                    GL.EnableVertexAttribArray(2);
                    GL.EnableVertexAttribArray(3);
                    GL.EnableVertexAttribArray(4);
                    GL.EnableVertexAttribArray(5);
                    if (ch.PosMultiplier == 1)
                    {
                        GL.EnableVertexAttribArray(6);
                        GL.EnableVertexAttribArray(7);
                        GL.EnableVertexAttribArray(8);
                        GL.EnableVertexAttribArray(9);
                    }
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, newBufs[ch.PosMultiplier == 1 ? 8 : 6]);
                    GL.BindVertexArray(0);
                    // Move buffers to chunk VBO
                    ChunkVBO vbo = new ChunkVBO()
                    {
                        generated = true,
                        _VertexVBO = newBufs[0],
                        _NormalVBO = newBufs[1],
                        _TexCoordVBO = newBufs[2],
                        _TangentVBO = newBufs[3],
                        _ColorVBO = newBufs[4],
                        _TCOLVBO = newBufs[5],
                        _IndexVBO = newBufs[ch.PosMultiplier == 1 ? 8 : 6],
                        _VAO = vao,
                        vC = resd,
                        colors = true,
                        tcols = true,
                        usethvs = false,
                        reusable = false
                    };
                    if (ch.PosMultiplier == 1)
                    {
                        vbo._THVVBO = newBufs[6];
                        vbo._THWVBO = newBufs[7];
                        vbo._THV2VBO = newBufs[9];
                        vbo._THW2VBO = newBufs[10];
                        vbo.usethvs = true;
                    }
                    if (transp)
                    {
                        ch._VBOTransp?.Destroy();
                        ch._VBOTransp = vbo;
                    }
                    else
                    {
                        ch._VBOSolid?.Destroy();
                        ch._VBOSolid = vbo;
                    }
                };
                calc(true);
                calc(false);
                // Clean up buffers
                GL.DeleteBuffer(ch.Render_ResBuf);
                GL.DeleteBuffer(ch.Render_FBB);
                GL.DeleteBuffer(ch.Render_IndBuf);
                ch.Render_ResBuf = 0;
                ch.Render_IndBuf = 0;
                ch.Render_BufsRel = null;
                GL.DeleteBuffer(ch.Render_ResBufTRANSP);
                GL.DeleteBuffer(ch.Render_IndBufTRANSP);
                ch.Render_ResBufTRANSP = 0;
                ch.Render_IndBufTRANSP = 0;
                if (ch.CSize == Constants.CHUNK_WIDTH)
                {
                    GL.DeleteBuffer(lbuf);
                }
            }
            GL.BindImageTexture(0, 0, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.R32f);
            View3D.CheckError("Compute - Finalize");
            sw3.Stop();
        }

        public void Destroy()
        {
            // Clean up shader
            GL.DeleteTexture(Texture_IDs);
            for (int i = 0; i < Reppers.Length; i++)
            {
                GL.DeleteProgram(Program_Counter[i]);
                GL.DeleteProgram(Program_Cruncher1[i]);
                GL.DeleteProgram(Program_Cruncher2[i]);
                GL.DeleteProgram(Program_Cruncher3[i]);
            }
            GL.DeleteBuffer(Buf_Shapes);
            View3D.CheckError("Compute - Shutdown");
        }
    }
}
