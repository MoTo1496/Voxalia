//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using Voxalia.ClientGame.EntitySystem;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class SetStatusPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 8 + 1 + 1)
            {
                return false;
            }
            long eid = Utilities.BytesToLong(Utilities.BytesPartial(data, 0, 8));
            Entity e = TheClient.TheRegion.GetEntity(eid);
            if (!(e is CharacterEntity))
            {
                return false;
            }
            switch ((ClientStatus)data[8])
            {
                case ClientStatus.TYPING:
                    ((CharacterEntity)e).IsTyping = (data[8 + 1] != 0);
                    return true;
                default:
                    return false;
            }
        }
    }
}
