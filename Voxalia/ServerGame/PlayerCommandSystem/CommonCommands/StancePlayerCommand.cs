//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;
using BEPUphysics.Character;
using FreneticScript;

namespace Voxalia.ServerGame.PlayerCommandSystem.CommonCommands
{
    public class StancePlayerCommand: AbstractPlayerCommand
    {
        public StancePlayerCommand()
        {
            Name = "stance";
            Silent = true;
        }
        
        // TODO: Clientside?
        public override void Execute(PlayerCommandEntry entry)
        {
            if (entry.InputArguments.Count < 1)
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "^r^1/stance <stance>"); // TODO: ShowUsage
                return;
            }
            string stance = entry.InputArguments[0].ToLowerFast();
            // TOOD: Implement!
            if (stance == "stand")
            {
                entry.Player.DesiredStance = Stance.Standing;
            }
            else if (stance == "crouch")
            {
                entry.Player.DesiredStance = Stance.Crouching;
            }
            else
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "^r^1Unknown stance input."); // TODO: Languaging
            }
        }
    }
}
