﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Frenetic.TagHandlers;
using Frenetic.TagHandlers.Objects;
using Voxalia.ServerGame.TagSystem.TagObjects;
using Voxalia.ServerGame.ServerMainSystem;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.TagSystem.TagBases
{
    class LivingEntityTagBase : TemplateTagBase
    {
        // <--[tagbase]
        // @Base living_entity[<LivingEntityTag>]
        // @Group Entities
        // @ReturnType LivingEntityTag
        // @Returns the living entity with the given entity ID or name.
        // -->
        Server TheServer;

        public LivingEntityTagBase(Server tserver)
        {
            Name = "living_entity";
            TheServer = tserver;
        }

        public override string Handle(TagData data)
        {
            long eid;
            string input = data.GetModifier(0).ToLower();
            if (long.TryParse(input, out eid))
            {
                foreach (Region r in TheServer.LoadedRegions)
                {
                    foreach (Entity e in r.Entities)
                    {
                        if (e.EID == eid && e is LivingEntity)
                        {
                            return new LivingEntityTag((LivingEntity)e).Handle(data.Shrink());
                        }
                    }
                }
            }
            else
            {
                foreach (PlayerEntity p in TheServer.Players)
                {
                    if (p.Name.ToLower() == input)
                    {
                        return new LivingEntityTag((LivingEntity)p).Handle(data.Shrink());
                    }
                }
            }
            return new TextTag("&{NULL}").Handle(data.Shrink());
        }
    }
}
