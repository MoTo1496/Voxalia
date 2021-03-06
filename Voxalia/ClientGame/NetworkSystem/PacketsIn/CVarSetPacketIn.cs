//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;
using FreneticGameCore.Files;
using FreneticGameCore;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class CVarSetPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            DataReader dr = new DataReader(new DataStream(data));
            int cvarname_id = dr.ReadInt();
            string cvarvalue = dr.ReadFullString();
            string cvarname = TheClient.Network.Strings.StringForIndex(cvarname_id);
            FreneticScript.CVar cvar = TheClient.CVars.system.Get(cvarname);
            if (cvar == null || !cvar.Flags.HasFlag(FreneticScript.CVarFlag.ServerControl))
            {
                SysConsole.Output(OutputType.WARNING, "Invalid CVar " + cvarname);
                return false;
            }
            cvar.Set(cvarvalue, true);
            return true;
        }
    }
}
