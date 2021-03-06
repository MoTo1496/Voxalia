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
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.NetworkSystem.PacketsOut;
using Voxalia.Shared;

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class JetpackItem: GenericItem
    {
        public JetpackItem()
        {
            Name = "jetpack";
        }

        public override void Tick(Entity entity, ItemStack item)
        {
            if (!(entity is HumanoidEntity))
            {
                // TODO: Non-human support?
                return;
            }
            HumanoidEntity human = (HumanoidEntity)entity;
            human.JPBoost = human.ItemLeft;
            human.JPHover = human.ItemRight;
        }
        
        public override void SwitchTo(Entity entity, ItemStack item)
        {
            if (!(entity is HumanoidEntity))
            {
                // TODO: Non-human support?
                return;
            }
            HumanoidEntity human = (HumanoidEntity)entity;
            bool has_fuel = human.ConsumeFuel(0);
            human.TheRegion.SendToVisible(human.GetPosition(), new FlagEntityPacketOut(human, EntityFlag.HAS_FUEL, has_fuel ? 1f : 0f));
        }

        public override void SwitchFrom(Entity entity, ItemStack item)
        {
            if (!(entity is HumanoidEntity))
            {
                // TODO: Non-human support?
                return;
            }
            HumanoidEntity human = (HumanoidEntity)entity;
            human.JPBoost = false;
            human.JPHover = false;
        }
    }
}
