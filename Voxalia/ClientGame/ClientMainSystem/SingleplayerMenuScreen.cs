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
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.UISystem.MenuSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;

namespace Voxalia.ClientGame.ClientMainSystem
{
    class SingleplayerMenuScreen: UIScreen
    {
        public SingleplayerMenuScreen(Client tclient) : base(tclient)
        {
            AddChild(new UIButton("ui/menus/buttons/basic", "Back", TheClient.FontSets.SlightlyBigger, () => TheClient.ShowMainMenu(), UIAnchor.BOTTOM_LEFT, () => 350, () => 70, () => 10, () => -100));
            int start = 150;
            List<string> found = TheClient.Files.ListFolders("saves");
            foreach (string str in found)
            {
                if (str.LastIndexOf('/') == "/saves/".Length - 1)
                {
                    AddChild(new UIButton("ui/menus/buttons/sp", "== " + str.Substring("/saves/".Length) + " ==", TheClient.FontSets.Standard, () => UIConsole.WriteLine("OPEN " + str), UIAnchor.TOP_LEFT, () => 600, () => 70, () => 10, () => start));
                    start += 100;
                }
            }
            AddChild(new UILabel("^!^e^0  Voxalia\nSingleplayer", TheClient.FontSets.SlightlyBigger, UIAnchor.TOP_CENTER, () => 0, () => 0));
        }

        public override void SwitchTo()
        {
            MouseHandler.ReleaseMouse();
        }
    }
}
