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

        [Option("life-name", HelpText = "--life-name \"MGSR\" :: Sets the name displayed on the life bar.")]
        public string LifeName { get; set; }

        [Option("progress", HelpText = "--progress 123 :: Sets the progress flag to this number.")]
        public short? Progress { get; set; }

        [Option("radar", HelpText = "--radar on :: Sets the radar on or off.")]
        public string Radar { get; set; }

        [Option("vr-name", HelpText = "--vr-name \"MGSR\" :: Sets the name for this VR profile.")]
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