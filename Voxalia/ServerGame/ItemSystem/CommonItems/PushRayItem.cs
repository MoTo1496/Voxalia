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

namespace Voxalia.ServerGame.ItemSystem.CommonItems
{
    public class PushRayItem : BaseForceRayItem
    {
        public PushRayItem()
        {
            Name = "pushray";
        }

        public override double GetStrength()
        {
            return -1;
        }
    }
}
