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
using FreneticGameCore;
using System.Threading.Tasks;
using Voxalia.Shared;

namespace Voxalia.ClientGame.JointSystem
{
    public class ConnectorBeam : BaseFJoint
    {
        public Color4F color = new Color4F(0f, 0.5f, 0.5f);

        public override void Solve()
        {
            // Do nothing.
        }

        public BeamType type;
    }
}
