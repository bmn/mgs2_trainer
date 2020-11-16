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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //CLIOptions Options = new CLIOptions();
            ParseOptions(e.Args);

            if (EnableWindow)
            {
                new MainWindow(this).ShowDialog();
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

            Trainer t = new Trainer();
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

            if ( (opts.Radar.Equals("on", StringComparison.InvariantCultureIgnoreCase)) || (opts.Radar == "1") )
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
