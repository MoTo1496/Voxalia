//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using FreneticGameCore;
using FreneticGameGraphics;
using FreneticGameGraphics.GraphicsHelpers;

namespace Voxalia.ClientGame.GraphicsSystems
{
    public class ChunkVBO
    {
        public uint _VertexVBO;
        public uint _IndexVBO;
        public uint _NormalVBO;
        public uint _TexCoordVBO;
        public uint _ColorVBO;
        public uint _TCOLVBO;
        public uint _THVVBO;
        public uint _THWVBO;
        public uint _THV2VBO;
        public uint _THW2VBO;
        public uint _TangentVBO;
        public int _VAO = -1;

        public Texture Tex;
        public Texture Tex_Specular;
        public Texture Tex_Reflectivity;
        public Texture Tex_Normal;

        public List<Vector3> Vertices;
        public List<uint> Indices;
        public List<Vector3> Normals;
        public List<Vector3> Tangents;
        public List<Vector3> TexCoords;
        public List<Vector4> Colors;
        public List<Vector4> TCOLs;
        public List<Vector4> THVs;
        public List<Vector4> THWs;
        public List<Vector4> THVs2;
        public List<Vector4> THWs2;

        public long LastVRAM = 0;

        public long GetVRAMUsage()
        {
            if (generated)
            {
                return LastVRAM;
            }
            return 0;
        }

        public void CleanLists()
        {
            Vertices = null;
            Indices = null;
            Normals = null;
            Tangents = null;
            TexCoords = null;
            Colors = null;
            verts = null;
            indices = null;
            normals = null;
            tangents = null;
            texts = null;
            TCOLs = null;
            THVs = null;
            THWs = null;
            THVs2 = null;
            THWs2 = null;
            v4_colors = null;
            v4_tcolors = null;
            v4_thvs = null;
            v4_thws = null;
            v4_thvs2 = null;
            v4_thws2 = null;
        }

        public int vC;
        
        public void Prepare()
        {
            Vertices = new List<Vector3>();
            Indices = new List<uint>();
            Normals = new List<Vector3>();
            TexCoords = new List<Vector3>();
            Colors = new List<Vector4>();
        }

        public bool generated = false;

        public bool reusable = true;

        public void Destroy()
        {
            if (generated)
            {
                GL.DeleteVertexArray(_VAO);
                GL.DeleteBuffer(_VertexVBO);
                GL.DeleteBuffer(_IndexVBO);
                GL.DeleteBuffer(_NormalVBO);
                GL.DeleteBuffer(_TexCoordVBO);
                GL.DeleteBuffer(_TangentVBO);
                if (colors)
                {
                    GL.DeleteBuffer(_ColorVBO);
                    colors = false;
                }
                if (tcols)
                {
                    GL.DeleteBuffer(_TCOLVBO);
                    if (usethvs)
                    {
                        GL.DeleteBuffer(_THVVBO);
                        GL.DeleteBuffer(_THWVBO);
                        GL.DeleteBuffer(_THV2VBO);
                        GL.DeleteBuffer(_THW2VBO);
                    }
                    tcols = false;
                }
                generated = false;
            }
        }

        public void Oldvert()
        {
            verts = Vertices.ToArray();
            normals = Normals.ToArray();
            texts = TexCoords.ToArray();
            tangents = Tangents != null ? Tangents.ToArray() : TangentsFor(verts, normals, texts);
            v4_colors = Colors?.ToArray();
            v4_tcolors = TCOLs?.ToArray();
            v4_thvs = THVs?.ToArray();
            v4_thws = THWs?.ToArray();
            v4_thvs2 = THVs2?.ToArray();
            v4_thws2 = THWs2?.ToArray();
            Vertices = null;
            Normals = null;
            TexCoords = null;
            Tangents = null;
            Colors = null;
            TCOLs = null;
            THVs = null;
            THWs = null;
            THVs2 = null;
            THWs2 = null;
            // TODO: Other arrays?
        }

        public bool colors;
        public bool tcols;
        public bool usethvs = true;

        public Vector3[] verts = null;
        public uint[] indices = null;
        Vector3[] normals = null;
        Vector3[] texts = null;
        Vector3[] tangents = null;
        Vector4[] v4_colors = null;
        Vector4[] v4_tcolors = null;
        Vector4[] v4_thvs = null;
        Vector4[] v4_thws = null;
        Vector4[] v4_thvs2 = null;
        Vector4[] v4_thws2 = null;

        public void UpdateBuffer()
        {
            if (Vertices == null && verts == null)
            {
                SysConsole.Output(OutputType.ERROR, "Failed to update VBO, null vertices!");
                return;
            }
            LastVRAM = 0;
            Vector3[] vecs = verts ?? Vertices.ToArray();
            uint[] inds = indices ?? Indices.ToArray();
            Vector3[] norms = normals ?? Normals.ToArray();
            Vector3[] texs = texts ?? TexCoords.ToArray();
            Vector3[] tangs = Tangents != null ? Tangents.ToArray() : (tangents ?? TangentsFor(vecs, norms, texs));
            Vector4[] cols = Colors != null ? Colors.ToArray() : v4_colors;
            Vector4[] tcols = TCOLs != null ? TCOLs.ToArray() : v4_tcolors;
            Vector4[] thvs = THVs != null ? THVs.ToArray() : v4_thvs;
            Vector4[] thws = THWs != null ? THWs.ToArray() : v4_thws;
            Vector4[] thvs2 = THVs2 != null ? THVs2.ToArray() : v4_thvs2;
            Vector4[] thws2 = THWs2 != null ? THWs2.ToArray() : v4_thws2;
            vC = vecs.Length;
            // Vertex buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, _VertexVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vecs.Length * Vector3.SizeInBytes), vecs, BufferMode);
            LastVRAM += vecs.Length * Vector3.SizeInBytes;
            // Normal buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, _NormalVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(norms.Length * Vector3.SizeInBytes), norms, BufferMode);
            LastVRAM += norms.Length * Vector3.SizeInBytes;
            // TexCoord buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, _TexCoordVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(texs.Length * Vector3.SizeInBytes), texs, BufferMode);
            LastVRAM += texs.Length * Vector3.SizeInBytes;
            // Tangent buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, _TangentVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(tangs.Length * Vector3.SizeInBytes), tangs, BufferMode);
            LastVRAM += tangs.Length * Vector3.SizeInBytes;
            // Color buffer
            if (cols != null)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _ColorVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(cols.Length * Vector4.SizeInBytes), cols, BufferMode);
                LastVRAM += cols.Length * Vector4.SizeInBytes;
            }
            // TCOL buffer
            if (tcols != null)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _TCOLVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(tcols.Length * Vector4.SizeInBytes), tcols, BufferMode);
                LastVRAM += tcols.Length * Vector4.SizeInBytes;
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THVVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(thvs.Length * Vector4.SizeInBytes), thvs, BufferMode);
                LastVRAM += thvs.Length * Vector4.SizeInBytes;
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THWVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(thws.Length * Vector4.SizeInBytes), thws, BufferMode);
                LastVRAM += thws.Length * Vector4.SizeInBytes;
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THV2VBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(thvs2.Length * Vector4.SizeInBytes), thvs2, BufferMode);
                LastVRAM += thvs2.Length * Vector4.SizeInBytes;
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THW2VBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(thws2.Length * Vector4.SizeInBytes), thws2, BufferMode);
                LastVRAM += thws2.Length * Vector4.SizeInBytes;
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            // Index buffer
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _IndexVBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(inds.Length * sizeof(uint)), inds, BufferMode);
            LastVRAM += inds.Length * sizeof(uint);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        public void GenerateOrUpdate()
        {
            if (generated && reusable)
            {
                UpdateBuffer();
            }
            else
            {
                GenerateVBO();
            }
        }

        public BufferUsageHint BufferMode = BufferUsageHint.StaticDraw;

        public static Vector3[] TangentsFor(Vector3[] vecs, Vector3[] norms, Vector3[] texs)
        {
            Vector3[] tangs = new Vector3[vecs.Length];
            if (vecs.Length != norms.Length || texs.Length != vecs.Length || (vecs.Length % 3) != 0)
            {
                for (int i = 0; i < tangs.Length; i++)
                {
                    tangs[i] = new Vector3(0, 0, 0);
                }
                return tangs;
            }
            for (int i = 0; i < vecs.Length; i += 3)
            {
                Vector3 v1 = vecs[i];
                Vector3 dv1 = vecs[i + 1] - v1;
                Vector3 dv2 = vecs[i + 2] - v1;
                Vector3 t1 = texs[i];
                Vector3 dt1 = texs[i + 1] - t1;
                Vector3 dt2 = texs[i + 2] - t1;
                Vector3 tangent = (dv1 * dt2.Y - dv2 * dt1.Y) / (dt1.X * dt2.Y - dt1.Y * dt2.X);
                Vector3 normal = norms[i];
                tangent = (tangent - normal * Vector3.Dot(normal, tangent)).Normalized(); // TODO: Necessity of this correction?
                tangs[i] = tangent;
                tangs[i + 1] = tangent;
                tangs[i + 2] = tangent;
            }
            return tangs;
        }

        public static Vector3[] TangentsFor(Vector3[] vecs, Vector3[] norms, Vector2[] texs)
        {
            Vector3[] tangs = new Vector3[vecs.Length];
            if (vecs.Length != norms.Length || texs.Length != vecs.Length || (vecs.Length % 3) != 0)
            {
                for (int i = 0; i < tangs.Length; i++)
                {
                    tangs[i] = new Vector3(0, 0, 0);
                }
                return tangs;
            }
            for (int i = 0; i < vecs.Length; i += 3)
            {
                Vector3 v1 = vecs[i];
                Vector3 dv1 = vecs[i + 1] - v1;
                Vector3 dv2 = vecs[i + 2] - v1;
                Vector2 t1 = texs[i];
                Vector2 dt1 = texs[i + 1] - t1;
                Vector2 dt2 = texs[i + 2] - t1;
                Vector3 tangent = (dv1 * dt2.Y - dv2 * dt1.Y) / (dt1.X * dt2.Y - dt1.Y * dt2.X);
                Vector3 normal = norms[i];
                tangent = (tangent - normal * Vector3.Dot(normal, tangent)).Normalized(); // TODO: Necessity of this correction?
                tangs[i] = tangent;
                tangs[i + 1] = tangent;
                tangs[i + 2] = tangent;
            }
            return tangs;
        }

        public void GenerateVBO()
        {
            if (generated)
            {
                Destroy();
            }
            GL.BindVertexArray(0);
            if (Vertices == null && verts == null)
            {
                SysConsole.Output(OutputType.ERROR, "Failed to render VBO, null vertices!");
                return;
            }
            Vector3[] vecs = verts ?? Vertices.ToArray();
            if (vecs.Length == 0)
            {
                return;
            }
            LastVRAM = 0;
            uint[] inds = indices ?? Indices.ToArray();
            Vector3[] norms = normals ?? Normals.ToArray();
            Vector3[] texs = texts ?? TexCoords.ToArray();
            Vector3[] tangs = Tangents != null ? Tangents.ToArray() : (tangents ?? TangentsFor(vecs, norms, texs));
            Vector4[] cols = Colors != null ? Colors.ToArray() : v4_colors;
            Vector4[] tcols = TCOLs != null ? TCOLs.ToArray() : v4_tcolors;
            Vector4[] thvs = THVs != null ? THVs.ToArray() : v4_thvs;
            Vector4[] thws = THWs != null ? THWs.ToArray() : v4_thws;
            Vector4[] thvs2 = THVs2 != null ? THVs2.ToArray() : v4_thvs2;
            Vector4[] thws2 = THWs2 != null ? THWs2.ToArray() : v4_thws2;
            vC = inds.Length;
            // Vertex buffer
            GL.GenBuffers(1, out _VertexVBO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _VertexVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vecs.Length * Vector3.SizeInBytes), vecs, BufferMode);
            LastVRAM += vecs.Length * Vector3.SizeInBytes;
            // Normal buffer
            GL.GenBuffers(1, out _NormalVBO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _NormalVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(norms.Length * Vector3.SizeInBytes), norms, BufferMode);
            LastVRAM += norms.Length * Vector3.SizeInBytes;
            // TexCoord buffer
            GL.GenBuffers(1, out _TexCoordVBO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _TexCoordVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(texs.Length * Vector3.SizeInBytes), texs, BufferMode);
            LastVRAM += texs.Length * Vector3.SizeInBytes;
            GL.GenBuffers(1, out _TangentVBO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _TangentVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(tangs.Length * Vector3.SizeInBytes), tangs, BufferMode);
            LastVRAM += tangs.Length * Vector3.SizeInBytes;
            // Color buffer
            if (cols != null)
            {
                colors = true;
                GL.GenBuffers(1, out _ColorVBO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _ColorVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(cols.Length * Vector4.SizeInBytes), cols, BufferMode);
                LastVRAM += cols.Length * Vector4.SizeInBytes;
            }
            // TCOL buffer
            if (tcols != null)
            {
                this.tcols = true;
                GL.GenBuffers(1, out _TCOLVBO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _TCOLVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (tcols.Length * Vector4.SizeInBytes), tcols, BufferMode);
                LastVRAM += tcols.Length * Vector4.SizeInBytes;
                GL.GenBuffers(1, out _THVVBO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THVVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (thvs.Length * Vector4.SizeInBytes), thvs, BufferMode);
                LastVRAM += thvs.Length * Vector4.SizeInBytes;
                GL.GenBuffers(1, out _THWVBO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THWVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (thws.Length * Vector4.SizeInBytes), thws, BufferMode);
                LastVRAM += thws.Length * Vector4.SizeInBytes;
                GL.GenBuffers(1, out _THV2VBO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THV2VBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (thvs2.Length * Vector4.SizeInBytes), thvs2, BufferMode);
                LastVRAM += thvs2.Length * Vector4.SizeInBytes;
                GL.GenBuffers(1, out _THW2VBO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THW2VBO);
                GL.BufferData(BufferTarget.ArrayBuffer, (thws2.Length * Vector4.SizeInBytes), thws2, BufferMode);
                LastVRAM += thws2.Length * Vector4.SizeInBytes;
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            // Index buffer
            GL.GenBuffers(1, out _IndexVBO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _IndexVBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(inds.Length * sizeof(uint)), inds, BufferMode);
            LastVRAM += inds.Length * sizeof(uint);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            // VAO
            GL.GenVertexArrays(1, out _VAO);
            GL.BindVertexArray(_VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _VertexVBO);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _NormalVBO);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _TexCoordVBO);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _TangentVBO);
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 0, 0);
            if (cols != null)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _ColorVBO);
                GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 0, 0);
            }
            if (tcols != null)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _TCOLVBO);
                GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, 0, 0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THVVBO);
                GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, 0, 0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THWVBO);
                GL.VertexAttribPointer(7, 4, VertexAttribPointerType.Float, false, 0, 0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THV2VBO);
                GL.VertexAttribPointer(8, 4, VertexAttribPointerType.Float, false, 0, 0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _THW2VBO);
                GL.VertexAttribPointer(9, 4, VertexAttribPointerType.Float, false, 0, 0);
            }
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);
            if (cols != null)
            {
                GL.EnableVertexAttribArray(4);
            }
            if (tcols != null)
            {
                GL.EnableVertexAttribArray(5);
                GL.EnableVertexAttribArray(6);
                GL.EnableVertexAttribArray(7);
                GL.EnableVertexAttribArray(8);
                GL.EnableVertexAttribArray(9);
            }
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _IndexVBO);
            // Clean up
            GL.BindVertexArray(0);
            generated = true;

        }
        
        public void Render()
        {
            if (!generated)
            {
                return;
            }
            GL.BindVertexArray(_VAO);
            GL.DrawElements(PrimitiveType.Triangles, vC, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);
        }
    }
}
