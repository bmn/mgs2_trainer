using CommandLine;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MGS2Trainer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private bool EnableWindow { get; set; } = true;
        private Trainer Train { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Train = new Trainer();

            //CLIOptions Options = new CLIOptions();
            ParseOptions(e.Args);

            if (EnableWindow)
            {
                new MainWindow(this, Train).ShowDialog();
            }

            this.Shutdown();
        }

        public void ParseOptions(string[] args)
        {
            Parser.Default.ParseArguments<CommandOptions, StateOptions>(args)
                .WithParsed<CommandOptions>(CommandExe)
                .WithParsed<StateOptions>(StateExe);
        }

        private void CommandExe(CommandOptions opts)
        {
            EnableWindow = false;
            Trainer t = Train;

            if (opts.UnlockCast)
            {
                t.UnlockCastingTheaterCharacters();
            }
            if (opts.VrScore != null)
            {
                t.SetAllVrScores((uint)opts.VrScore);
            }
            if (opts.VrName != null)
            {
                t.SetVrName(opts.VrName);
            }
            if (opts.VrUnlock)
            {
                t.UnlockAllVrMissions();
            }
            if (opts.LifeName != null)
            {
                t.SetName(opts.LifeName);
            }
            if (opts.Progress != null)
            {
                t.SetProgress((short)opts.Progress);
            }

            if (opts.Radar != null)
            {
                if ((opts.Radar.Equals("on", StringComparison.InvariantCultureIgnoreCase)) || (opts.Radar == "1"))
                {
                    t.SetRadarOn();
                }
                else if ((opts.Radar.Equals("off", StringComparison.InvariantCultureIgnoreCase)) || (opts.Radar == "0"))
                {
                    t.SetRadarOff();
                }
                else if (opts.Radar.Equals("toggle", StringComparison.InvariantCultureIgnoreCase))
                {
                    t.ToggleRadar();
                }
            }

            if (opts.EquipMode != null)
            {
                if ((opts.EquipMode.Equals("previous", StringComparison.InvariantCultureIgnoreCase)) || (opts.EquipMode == "1"))
                {
                    t.SetEquipPrevious();
                }
                else if ((opts.EquipMode.Equals("unequip", StringComparison.InvariantCultureIgnoreCase)) || (opts.EquipMode == "0"))
                {
                    t.SetEquipUnequip();
                }
                else if (opts.EquipMode.Equals("toggle", StringComparison.InvariantCultureIgnoreCase))
                {
                    t.ToggleEquipMode();
                }
            }

            if (opts.EnableGoid != null)
            {
                if ((opts.EnableGoid.Equals("on", StringComparison.InvariantCultureIgnoreCase)) || (opts.EquipMode == "1"))
                {
                    t.SetGOIDOn();
                }
                else if ((opts.EnableGoid.Equals("off", StringComparison.InvariantCultureIgnoreCase)) || (opts.EquipMode == "0"))
                {
                    t.SetGOIDOff();
                }
            }

            if (opts.State != null)
            {
                if (opts.State.Equals("save", StringComparison.InvariantCultureIgnoreCase))
                {
                    State.SaveState();
                }
                else if (opts.State.Equals("load", StringComparison.InvariantCultureIgnoreCase))
                {
                    State.LoadState();
                }
            }

            if (opts.PosX != null)
            {
                t.SetPositionX((float)opts.PosX);
            }
            if (opts.PosY != null)
            {
                t.SetPositionY((float)opts.PosY);
            }
            if (opts.PosZ != null)
            {
                t.SetPositionZ((float)opts.PosZ);
            }

            if (opts.Patch != null)
            {
                t.ApplyIPSPatch(opts.Patch);
            }
            if (opts.Unpatch != null)
            {
                t.RevertIPSPatch(opts.Unpatch);
            }

            if (opts.HostageHour != null)
            {
                t.SetHostageHour((int)opts.HostageHour);
            }

        }

        private void StateExe(StateOptions opts)
        {
            EnableWindow = false;
            if (opts.Save)
            {
                State.SaveState();
            }
            else if (opts.Load)
            {
                State.LoadState();
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, e.Exception.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

    }
}
