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
using Voxalia.Shared.Files;
using Voxalia.Shared.Collision;

namespace Voxalia.Shared
{
    public class Program
    {
        public const string GameName = "Voxalia";

        public const string GameVersion = "0.1.1";

        public const string GameVersionDescription = "Pre-Alpha";

        public const string GlobalServerAddress = "https://frenetic.xyz/";
        
        public static void Init()
        {
            FileHandler files = new FileHandler();
            files.Init();
            MaterialHelpers.Populate(files); // TODO: Non-static material helper data?!
            BlockShapeRegistry.Init();
            FullChunkObject.RegisterMe();
        }
    }
}
