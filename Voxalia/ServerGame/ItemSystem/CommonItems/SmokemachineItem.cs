﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.Shared;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class SmokemachineItem: GenericItem
    {
        public SmokemachineItem()
        {
            Name = "smokemachine";
        }

        public override void Click(Entity entity, ItemStack item)
        {
            // TODO: Work for non-players
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            if (player.WaitingForClickRelease)
            {
                return;
            }
            player.WaitingForClickRelease = true;
            player.TheRegion.SendToAll(new ParticleEffectPacketOut(ParticleEffectNetType.SMOKE, 7, player.GetPosition())); // TODO: only send to those in range
        }

        public override void ReleaseClick(Entity entity, ItemStack item)
        {
            // TODO: Work for non-players
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            player.WaitingForClickRelease = false;
        }

        public override void SwitchFrom(Entity entity, ItemStack item)
        {
            // TODO: Work for non-players
            if (!(entity is PlayerEntity))
            {
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            player.WaitingForClickRelease = false;
        }
    }
}
