//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using FreneticGameCore;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class FlashLightItem: BaseItemInfo
    {
        public FlashLightItem()
        {
            Name = "flashlight";
        }

        public double Distance = 20;

        public Location Color = Location.One;

        public static void Off(PlayerEntity player)
        {
            player.TheRegion.SendToAll(new FlashLightPacketOut(player, false, 0, Location.Zero));
            player.FlashLightOn = false;
        }

        public void On(PlayerEntity player)
        {
            player.TheRegion.SendToAll(new FlashLightPacketOut(player, true, Distance, Color));
            player.FlashLightOn = true;
        }

        public override void Click(Entity entity, ItemStack item)
        {
            if (!(entity is PlayerEntity))
            {
                // TODO: non-player support
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            On(player);
        }

        public override void AltClick(Entity entity, ItemStack item)
        {
            if (!(entity is PlayerEntity))
            {
                // TODO: non-player support
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            Off(player);
        }

        public override void ReleaseClick(Entity entity, ItemStack item)
        {
        }

        public override void ReleaseAltClick(Entity entity, ItemStack item)
        {
        }

        public override void PrepItem(Entity entity, ItemStack item)
        {
        }

        public override void SwitchFrom(Entity entity, ItemStack item)
        {
            if (!(entity is PlayerEntity))
            {
                // TODO: non-player support
                return;
            }
            PlayerEntity player = (PlayerEntity)entity;
            Off(player);
        }

        public override void SwitchTo(Entity entity, ItemStack item)
        {
        }

        public override void Use(Entity entity, ItemStack item)
        {
        }

        public override void Tick(Entity entity, ItemStack item)
        {
        }
    }
}
