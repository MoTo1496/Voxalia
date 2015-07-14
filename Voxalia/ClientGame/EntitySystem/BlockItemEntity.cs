﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using Voxalia.ClientGame.WorldSystem;
using BEPUphysics.CollisionShapes.ConvexShapes;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.ClientGame.GraphicsSystems;

namespace Voxalia.ClientGame.EntitySystem
{
    class BlockItemEntity: PhysicsEntity
    {
        public Material Mat;

        public BlockItemEntity(World tworld, Material tmat)
            : base(tworld, false, true)
        {
            Mat = tmat;
            Shape = new BoxShape(1, 1, 1);
            SetMass(5);
        }

        public VBO vbo = null;

        public override void SpawnBody()
        {
            vbo = new VBO();
            vbo.Prepare();
            vbo.AddSide(new Location(0, 0, 1), new TextureCoordinates(), true, Mat.TextureID(MaterialSide.TOP));
            vbo.AddSide(new Location(0, 0, -1), new TextureCoordinates(), true, Mat.TextureID(MaterialSide.BOTTOM));
            vbo.AddSide(new Location(0, 1, 0), new TextureCoordinates(), true, Mat.TextureID(MaterialSide.YP));
            vbo.AddSide(new Location(0, -1, 0), new TextureCoordinates(), true, Mat.TextureID(MaterialSide.YM));
            vbo.AddSide(new Location(1, 0, 0), new TextureCoordinates(), true, Mat.TextureID(MaterialSide.XP));
            vbo.AddSide(new Location(-1, 0, 0), new TextureCoordinates(), true, Mat.TextureID(MaterialSide.XM));
            vbo.GenerateVBO();
            base.SpawnBody();
        }

        public override void DestroyBody()
        {
            if (vbo != null)
            {
                vbo.Destroy();
            }
            base.DestroyBody();
        }

        public override void Render()
        {
            if (TheClient.FBO)
            { 
                 // TODO: Remove this block
                TheClient.s_fbov.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TheClient.TBlock.TextureID);
            }
            Matrix4 mat = GetTransformationMatrix();
            GL.UniformMatrix4(2, false, ref mat);
            vbo.Render(false);
            if (TheClient.FBO)
            {
                // TODO: Remove this block
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
                TheClient.s_fbo.Bind();
            }
        }
    }
}