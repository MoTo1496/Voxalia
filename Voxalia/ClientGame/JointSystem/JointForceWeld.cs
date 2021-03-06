//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ClientGame.EntitySystem;
using BEPUutilities;
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ClientGame.JointSystem
{
    class JointForceWeld: BaseFJoint
    {
        public JointForceWeld(Entity one, Entity two)
        {
            One = one;
            Two = two;
            Matrix worldTrans = Matrix.CreateFromQuaternion(One.GetOrientation()) * Matrix.CreateTranslation(One.GetPosition().ToBVector());
            Matrix.Invert(ref worldTrans, out worldTrans);
            Relative = (Matrix.CreateFromQuaternion(two.GetOrientation())
                * Matrix.CreateTranslation(two.GetPosition().ToBVector()))
                * worldTrans;
        }

        public Matrix Relative;

        public override void Solve()
        {
            Matrix worldTrans = Matrix.CreateFromQuaternion(One.GetOrientation()) * Matrix.CreateTranslation(One.GetPosition().ToBVector());
            Matrix tmat = Relative * worldTrans;
            Location pos = new Location(tmat.Translation);
            BEPUutilities.Quaternion quat = BEPUutilities.Quaternion.CreateFromRotationMatrix(tmat);
            Two.SetPosition(pos);
            Two.SetOrientation(quat);
        }
    }
}
