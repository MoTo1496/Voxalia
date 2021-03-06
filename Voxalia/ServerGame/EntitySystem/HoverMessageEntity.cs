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
using System.Threading.Tasks;
using LiteDB;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;
using FreneticGameCore.Files;
using FreneticGameCore;

namespace Voxalia.ServerGame.EntitySystem
{
    public class HoverMessageEntity : PrimitiveEntity
    {
        public System.Drawing.Color BackColor = System.Drawing.Color.FromArgb(64, 0, 255, 255);

        public string Text;

        public HoverMessageEntity(Region tregion, string message)
            : base(tregion)
        {
            Text = message ?? "";
            Gravity = Location.Zero;
            Scale = Location.One;
        }

        public override EntityType GetEntityType()
        {
            return EntityType.HOVER_MESSAGE;
        }

        public override string GetModel()
        {
            return "";
        }

        public override byte[] GetNetData()
        {
            byte[] b = GetPrimitiveNetData();
            DataStream ds = new DataStream();
            DataWriter dw = new DataWriter(ds);
            dw.WriteBytes(b);
            dw.WriteInt(BackColor.ToArgb());
            dw.WriteFullString(Text);
            byte[] res = ds.ToArray();
            return res;
        }

        public override NetworkEntityType GetNetType()
        {
            return NetworkEntityType.HOVER_MESSAGE;
        }

        public override BsonDocument GetSaveData()
        {
            BsonDocument doc = base.GetSaveData();
            doc["hm_color"] = BackColor.ToArgb();
            doc["hm_text"] = Text;
            return doc;
        }
    }

    public class HoverMessageEntityConstructor : EntityConstructor
    {
        public override Entity Create(Region tregion, BsonDocument doc)
        {
            HoverMessageEntity ent = new HoverMessageEntity(tregion, doc["hm_text"].AsString)
            {
                BackColor = System.Drawing.Color.FromArgb(doc["hm_color"].AsInt32)
            };
            ent.ApplyPrimitiveSaveData(doc);
            return ent;
        }
    }
}
