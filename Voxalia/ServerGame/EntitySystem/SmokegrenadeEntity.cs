﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ServerGame.WorldSystem;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.Shared;

namespace Voxalia.ServerGame.EntitySystem
{
    public class SmokegrenadeEntity: GrenadeEntity
    {
        Location colo;
        ParticleEffectNetType SmokeType;

        public SmokegrenadeEntity(int col, Region tregion, ParticleEffectNetType smokeType):
            base(tregion)
        {
            System.Drawing.Color tcol = System.Drawing.Color.FromArgb(col);
            colo = new Location(tcol.R / 255f, tcol.G / 255f, tcol.B / 255f);
            SmokeType = smokeType;
        }

        public double timer = 0;

        public double pulse = 1.0 / 15.0;

        public override void Tick()
        {
            timer += TheRegion.Delta;
            while (timer > pulse)
            {
                TheRegion.SendToAll(new ParticleEffectPacketOut(SmokeType, 5, GetPosition(), colo));
                timer -= pulse;
            }
            base.Tick();
        }
    }
}