//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using BEPUphysics.Character;
using FreneticGameCore;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class YourPositionPacketOut: AbstractPacketOut
    {
        public YourPositionPacketOut(double delta, int tID, Location pos, Location vel, Stance stance, bool pup)
        {
            UsageType = NetUsageType.PLAYERS;
            ID = ServerToClientPacket.YOUR_POSITION;
            Data = new byte[4 + 24 + 24 + 1 + 8];
            Utilities.IntToBytes(tID).CopyTo(Data, 0);
            pos.ToDoubleBytes().CopyTo(Data, 4);
            vel.ToDoubleBytes().CopyTo(Data, 4 + 24);
            Data[4 + 24 + 24] = (byte)((stance == Stance.Standing ? 0 : 1) | (pup ? 2: 0));
            Utilities.DoubleToBytes(delta).CopyTo(Data, 4 + 24 + 24 + 1);
        }
    }
}
