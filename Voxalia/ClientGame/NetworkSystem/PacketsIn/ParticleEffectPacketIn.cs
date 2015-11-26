﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class ParticleEffectPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length != 1 + 4 + 12
                && data.Length != 1 + 4 + 12 + 12)
            {
                return false;
            }
            ParticleEffectNetType type = (ParticleEffectNetType)data[0];
            float fdata1 = Utilities.BytesToFloat(Utilities.BytesPartial(data, 1, 4));
            Location ldata2 = Location.NaN;
            if (data.Length == 1 + 4 + 12 + 12)
            {
                ldata2 = Location.FromBytes(data, 1 + 4 + 12);
            }
            Location pos = Location.FromBytes(data, 1 + 4);
            switch (type)
            {
                case ParticleEffectNetType.EXPLOSION:
                    TheClient.Particles.Explode(pos, fdata1);
                    break;
                case ParticleEffectNetType.SMOKE:
                    TheClient.Particles.Smoke(pos, fdata1, ldata2);
                    break;
                case ParticleEffectNetType.BIG_SMOKE:
                    TheClient.Particles.BigSmoke(pos, fdata1, ldata2);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
