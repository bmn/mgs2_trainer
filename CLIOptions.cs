using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace MGS2Trainer
{
    [Verb("command", HelpText = "Run a command.")]
    class CommandOptions
    {
        /*
        [Option("difficulty", HelpText = "--difficulty 0/VE/\"Very Easy\" :: Sets the difficulty flag.")]
        public string Difficulty { get; set; }
        */
        [Option("cast-unlock", HelpText = "--cast-unlock :: Unlocks all characters in the Casting Theater. Only a few actually work.")]
        public bool UnlockCast { get; set; }

        [Option("equip", HelpText = "--equip previous/unequip/toggle :: Sets the equip mode.")]
        public string EquipMode { get; set; }

        [Option("goid", HelpText = "--goid on/off :: Sets Game Over If Discovered.")]
        public string EnableGoid { get; set; }

        [Option("hostages", HelpText = "--hostages 22 :: Sets the hour for the hostage variant. 13 = men; 22 = old ladies; 0 = Jennifer.")]
        public uint? HostageHour { get; set; }

        [Option("life-name", HelpText = "--life-name \"MGSR\" :: Sets the name displayed on the life bar. Quotes not needed for single words.")]
        public string LifeName { get; set; }

        [Option("patch", HelpText = "--patch caution :: Temporarily applies a patch (available: 1stperson, caution, drebin, overlay, radar).")]
        public string Patch { get; set; }
        /*
        [Option("save", HelpText = "--patch caution --save :: Applies a patch and saves it as mgs2_sse_patched.exe. Uses mgs2_sse_patched.exe as base if it exists.")]
        public bool Save { get; set; }
        */
        [Option("unpatch", HelpText = "--unpatch caution :: Reverts a patch that was previously applied with the --patch command.")]
        public string Unpatch { get; set; }

        [Option("posx", HelpText = "--posx 123.456 :: Sets the X position.")]
        public float? PosX { get; set; }
        [Option("posy", HelpText = "--posy 123.456 :: Sets the Y position.")]
        public float? PosY { get; set; }
        [Option("posz", HelpText = "--posz 123.456 :: Sets the Z position.")]
        public float? PosZ { get; set; }

        [Option("progress", HelpText = "--progress 123 :: Sets the progress flag to this number.")]
        public short? Progress { get; set; }

        [Option("radar", HelpText = "--radar on/1/2/off/toggle :: Sets the radar type.")]
        public string Radar { get; set; }

        [Option("state", HelpText = "--state save/load :: Saves or loads the game state (experimental).")]
        public string State { get; set; }

        [Option("vr-name", HelpText = "--vr-name \"MGSR\" :: Sets the name for this VR profile. Quotes not needed for single words.")]
        public string VrName { get; set; }

        [Option("vr-score", HelpText = "--vr-score 123456 :: Sets all VR scores to this number.")]
        public uint? VrScore { get; set; }

        [Option("vr-unlock", HelpText = "--vr-unlock :: Unlocks all missions on this VR profile.")]
        public bool VrUnlock { get; set; }
    }

    [Verb("state")]
    class StateOptions
    {
        [Option("save")]
        public bool Save { get; set; }

        [Option("load")]
        public bool Load { get; set; }
    }
}