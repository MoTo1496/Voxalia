//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using FreneticScript.CommandSystem;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.UISystem;
using OpenTK.Input;
using FreneticScript.TagHandlers;

namespace Voxalia.ClientGame.CommandSystem.UICommands
{
    /// <summary>
    /// A quick command to bind a key to a complex script.
    /// </summary>
    public class BindblockCommand : AbstractCommand
    {
        public override void AdaptBlockFollowers(CommandEntry entry, List<CommandEntry> input, List<CommandEntry> fblock)
        {
            entry.BlockEnd -= input.Count;
            input.Clear();
            base.AdaptBlockFollowers(entry, input, fblock);
        }

        public Client TheClient;

        public BindblockCommand(Client tclient)
        {
            TheClient = tclient;
            Name = "bindblock";
            Description = "Binds a script block to a key.";
            Arguments = "<key>";
        }

        public static void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.Arguments.Count < 1)
            {
                ShowUsage(queue, entry);
                return;
            }
            Client TheClient = (entry.Command as BindblockCommand).TheClient;
            string key = entry.GetArgument(queue, 0);
            if (key == "\0CALLBACK")
            {
                return;
            }
            if (entry.InnerCommandBlock == null)
            {
                queue.HandleError(entry, "Must have a block of commands!");
                return;
            }
            Key k = KeyHandler.GetKeyForName(key);
            // TODO: Bad key error
            KeyHandler.BindKey(k, entry.InnerCommandBlock, entry.BlockStart);
            entry.Good(queue, "Keybind updated for " + KeyHandler.keystonames[k] + ".");
            CommandStackEntry cse = queue.CommandStack.Peek();
            cse.Index = entry.BlockEnd + 2;
        }
    }
}
