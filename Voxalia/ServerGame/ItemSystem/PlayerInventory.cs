//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;

namespace Voxalia.ServerGame.ItemSystem
{
    public class PlayerInventory: EntityInventory
    {
        public PlayerInventory(PlayerEntity player)
            : base(player.TheRegion, player)
        {
        }

        protected override ItemStack GiveItemNoDup(ItemStack item)
        {
            // TODO: Better handling method here.
            ItemStack it = base.GiveItemNoDup(item);
            if (ReferenceEquals(it, item))
            {
                ((PlayerEntity)Owner).Network.SendPacket(new SpawnItemPacketOut(Items.Count - 1, it));
            }
            else
            {
                ((PlayerEntity)Owner).Network.SendPacket(new SetItemPacketOut(GetSlotForItem(it) - 1, it));
            }
            return it;
        }

        public override void SetSlot(int slot, ItemStack item)
        {
            base.SetSlot(slot, item);
            ((PlayerEntity)Owner).Network.SendPacket(new SetItemPacketOut(slot, item));
        }

        public override void RemoveItem(int item)
        {
            base.RemoveItem(item);
            ((PlayerEntity)Owner).Network.SendPacket(new RemoveItemPacketOut(item - 1));
        }

        public override void cItemBack()
        {
            ((PlayerEntity)Owner).Network.SendPacket(new SetHeldItemPacketOut(cItem));
        }
    }
}
