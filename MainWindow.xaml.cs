﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Binarysharp.MemoryManagement.Native;
using mrousavy;
using NHotkey;
using NHotkey.Wpf;
using System.Text.Json;
using System.CodeDom;
using Path = System.IO.Path;
using System.Numerics;
using System.Dynamic;
using CommandLine;
using CommandLine.Text;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace MGS2Trainer
{

    public class HotkeyJson
    {
        public string Name { get; set; }
        public string Key { get; set; } = null;
        public string[] Modifiers { get; set; } = new string[0];
        public string[] Pad { get; set; } = new string[0];
        public JsonElement Data { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private App Application { get; set; }
        private Trainer Train { get; set; }
        private Timer Timeout { get; set; }
        private Timer TimeoutPos { get; set; }

        public MainWindow(App app)
        {
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            Application = app;

            Train = new Trainer();
            DataContext = Train;

            Closed += new EventHandler(Window_Closed);
            InitializeComponent();

            RegisterHotKeys();

            txtWeaponCurrent.KeyUp += SetWeaponCurrent;
            txtWeaponMax.KeyUp += SetWeaponMax;

            txtItemCurrent.KeyUp += SetItemCurrent;
            txtItemMax.KeyUp += SetItemMax;

            cmbProgress.SelectionChanged += SetProgress;
            txtProgress.KeyUp += SetProgress;

            txtName.KeyUp += SetName;

            cmbDifficulty.SelectionChanged += SetDifficulty;

            Timeout = new Timer(1000);
            Timeout.Elapsed += UpdateValues;
            Timeout.Enabled = true;

            TimeoutPos = new Timer(100);
            TimeoutPos.Elapsed += UpdateDeltaMovement;
            TimeoutPos.Enabled = true;

            var commandHelp = new List<string>();
            var commandProps = typeof(CommandOptions).GetProperties();
            foreach (PropertyInfo prop in commandProps)
            {
                string txt = prop.GetCustomAttribute<OptionAttribute>().HelpText;
                int pos = txt.IndexOf(" :: ");
                string left = txt.Substring(0, pos);
                string right = txt.Substring(pos + 4);

                commandHelp.Add(left.PadRight(20) + right);
            }
            txtCLICommandToolTip.Text += Environment.NewLine + string.Join(Environment.NewLine, commandHelp); 
        }

        private void btnRunCLICommand_Click(object sender, RoutedEventArgs e) => RunCLICommand(txtCLICommand.Text);
        private void RunCLICommand(string command) {
            string cmd = "command " + command;
            string[] cmds = CommandLineParser.SplitCommandLineIntoArguments(cmd, true).ToArray<string>();
            Application.ParseOptions(cmds);
            if (!txtCLICommand.IsFocused)
            {
                txtCLICommand.Focus();
            }
            txtCLICommand.SelectAll();
        }

        private void txtCLICommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RunCLICommand(txtCLICommand.Text);
            }
        }

        private void btnOpenAbout_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                sldPosXDelta.Value = 0;
                sldPosYDelta.Value = 0;
                sldPosZDelta.Value = 0;
            }
        }

        private JsonElement GetHotkeyData(HotkeyEventArgs e)
        {
            if (HotkeyData.ContainsKey(e.Name))
            {
                return HotkeyData[e.Name];
            }
            return new JsonElement();
        }

        public Dictionary<string, JsonElement> HotkeyData = new Dictionary<string, JsonElement>();
        public Dictionary<string, int> HotkeyCounter = new Dictionary<string, int>();
        private void RegisterHotKeys()
        {
            // todo remove button lambdas and roll into button defs
            var lambdas = new Dictionary<string, EventHandler<HotkeyEventArgs>>()
            {
                { "SaveState", (sender, e) => State.SaveState() },
                { "LoadState", (sender, e) => State.LoadState() },
                { "SetProgress", (sender, e) => {
                    var d = GetHotkeyData(e);
                    if (d.ValueKind == JsonValueKind.Number) {
                        d.TryGetInt16(out short v);
                        Train.SetProgress(v);
                    }
                    else if (d.ValueKind == JsonValueKind.Array)
                    {
                        if (!HotkeyCounter.ContainsKey(e.Name))
                        {
                            HotkeyCounter[e.Name] = 0;
                        }
                        var vals = new List<short>();
                        foreach (var k in d.EnumerateArray())
                        {
                            if (k.ValueKind == JsonValueKind.Number)
                            {
                                k.TryGetInt16(out short l);
                                vals.Add(l);
                            }
                        }
                        Train.SetProgress((short)(vals[HotkeyCounter[e.Name] % vals.Count]));
                        HotkeyCounter[e.Name]++;
                    }
                    else
                    {
                        Train.SetProgress((short)(Train.ProgressCurrent + 1));
                    }
                } }
            };
            for (byte i = 0; i < 10; i++)
            {
                var copy = i;
                lambdas.Add($"weaponGroup{i}", (sender, e) => { Train.SwitchWeaponGroup(copy); });
            }
            for (byte i = 0; i < 21; i++)
            {
                var copy = i;
                lambdas.Add($"weapon{i}", (sender, e) => { Train.SwitchWeapon(copy); });
            }

            var btnContent = new Dictionary<string, Button>()
            {
                { "RestartRoom", btnRestartRoom },
                { "Suicide", btnSuicide },
                { "HealthFill", btnHealthFill },
                { "CautionToggle", btnCautionToggle },
                { "CautionOff", btnCautionOff },
                { "CautionOn", btnCautionOn },
                { "AlertToggle", btnAlertToggle },
                { "AlertOff", btnAlertOff },
                { "AlertOn", btnAlertOn },
                { "UnlockEquips", btnUnlockEquips },
                { "UpdatePos", btnUpdatePos },
                { "PosZToggle", btnPosZToggle },
                { "PosZRecover", btnPosZRecover }
            };

            string filename = Path.Combine(Directory.GetCurrentDirectory(), "HotKeys.json");

            string json = (File.Exists(filename)) ?
                File.ReadAllText(filename) :
                @"[{""name"":""btnUnlockEquips"",""modifiers"":[],""key"":""NumPad7""},{""name"":""toggleAlert"",""modifiers"":[],""key"":""NumPad1""},{""name"":""btnAlertOn"",""modifiers"":[],""key"":""NumPad2""},{""name"":""btnAlertOff"",""modifiers"":[],""key"":""NumPad3""},{""name"":""toggleCaution"",""modifiers"":[],""key"":""NumPad4""},{""name"":""btnCautionOn"",""modifiers"":[],""key"":""NumPad5""},{""name"":""btnCautionOff"",""modifiers"":[],""key"":""NumPad6""},{""name"":""btnHealthFill"",""modifiers"":[],""key"":""NumPad9""},{""name"":""btnSuicide"",""modifiers"":[],""key"":""NumPad8""}]";
            var data = JsonSerializer.Deserialize<HotkeyJson[]>(json, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                
            });

            RoutedEventArgs eventArgs = new RoutedEventArgs(Button.ClickEvent);

            var padMask = new Dictionary<string, int>
            {
                { "up", 0x1000 }, { "stickup", 0x1000 },
                { "down", 0x4000 }, { "stickdown", 0x4000 },
                { "left", 0x8000 }, { "stickleft", 0x8000 },
                { "action", 0x10 }, { "triangle", 0x10 },
                { "punch", 0x20 }, { "circle", 0x20 },
                { "crouch", 0x40 }, { "cross", 0x40 }, { "x", 0x40 },
                { "weapon", 0x80 }, { "square", 0x80 },
                { "start", 0x800 },
                { "select", 0x100 }, { "back", 0x100 },
                { "lockon", 1 }, { "l1", 1 },
                { "firstperson", 2 }, { "r1", 2 },
                { "itemmenu", 4 }, { "l2", 4 },
                { "weaponmenu", 8 }, { "r2", 8 },
                { "leftstick", 0x60000 }, { "l3", 0x60000 },
                { "rightstick", 0x400 }, { "r3", 0x400 }
            };

            int j = 0;
            foreach (var key in data)
            {
                try
                {
                    if (key.Key != null)
                    {
                        Key hotkey = (Key)Enum.Parse(typeof(Key), key.Key);
                        ModifierKeys modkey = 0;
                        string hotkeyname = "";
                        foreach (var mod in key.Modifiers)
                        {
                            modkey |= (ModifierKeys)Enum.Parse(typeof(ModifierKeys), mod);
                            hotkeyname += $"{mod}+";
                        }

                        if (lambdas.ContainsKey(key.Name))
                        {
                            string name = key.Name + j++;
                            HotkeyData.Add(name, key.Data);
                            HotkeyManager.Current.AddOrReplace(name, hotkey, modkey, lambdas[key.Name]);
                        }

                        else if (btnContent.ContainsKey(key.Name))
                        {
                            Button btn = btnContent[key.Name];

                            HotkeyManager.Current.AddOrReplace(key.Name + j++, hotkey, modkey, (sender, e) => { btn.RaiseEvent(eventArgs); });

                            //btnContent[key.Name].Content = $"[{hotkeyname}{key.Key}] {btnContent[key.Name].Content.ToString()}";
                            if (btn.ToolTip != null)
                            {
                                btn.ToolTip = " " + btn.ToolTip;
                            }
                            btn.ToolTip = $"[{hotkeyname}{key.Key}]{btn.ToolTip}";
                        }
                    }
                    if (key.Pad.Length > 0)
                    {

                    }
                }
                catch (HotkeyAlreadyRegisteredException)
                {
                    //MessageBox.Show("Failed to register at least one global hotkey.");
                }
            }
        }

        void Window_Closed(object sender, EventArgs e)
        {
        }

        private void UpdateValues(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    Train.Mem.RefreshProcess();

                    Train.WatchInitialStates();
                    Train.WatchBossPractice();

                    if ((!cmbWeaponName.IsDropDownOpen) && (!txtWeaponCurrent.IsFocused) && (!txtWeaponMax.IsFocused))
                    {
                        Train.RefreshWeaponValues();
                    }
                    if ((!cmbItemName.IsDropDownOpen) && (!txtItemCurrent.IsFocused) && (!txtItemMax.IsFocused))
                    {
                        Train.RefreshItemValues();
                    }
                    if (!txtName.IsFocused)
                    {
                        Train.RefreshName();
                    }

                    RefreshProgressValue();
                    Train.RefreshDifficulty();

                    Train.RelockDogTags();
                }
                catch (NullReferenceException)
                {
                    return;
                }
            });
        }

        private void UpdateDeltaMovement(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    Train.Mem.RefreshProcess();

                    RefreshPositionValues();
                    if (Train.PositionXDelta != 0)
                    {
                        Train.SetPositionX((float)Train.PositionXDelta + Train.PositionX);
                    }
                    if (Train.PositionYDelta != 0)
                    {
                        Train.SetPositionY((float)Train.PositionYDelta + Train.PositionY);
                    }
                    if (Train.PositionZDelta != 0)
                    {
                        if ((!ZMovementActive) && (Train.ZMovementEnabled))
                        {
                            ZMovementActive = true;
                            Train.DisableZMovement();
                        }
                        Train.SetPositionZ((float)Train.PositionZDelta + Train.PositionZ);
                    }
                    else if (PosZEnableNextFrame)
                    {
                        
                        PosZEnableNextFrame = false;
                        Train.EnableZMovement();
                    }
                    else if (ZMovementActive)
                    {
                        ZMovementActive = false;
                        Train.EnableZMovement();
                    }
                }
                catch (NullReferenceException)
                {
                    return;
                }
            });
        }


        private void btnUnlockEquips_Click(object sender, RoutedEventArgs e) => btnUnlockEquips_Click();
        private void btnUnlockEquips_Click() => Train.UnlockAllEquips();
        private void btnUnlockEquips_RightClick(object sender, RoutedEventArgs e) => btnUnlockEquips_RightClick();
        private void btnUnlockEquips_RightClick() => Train.UnlockAllEquips(true);

        private void btnAlertToggle_Click(object sender, RoutedEventArgs e) => btnAlertToggle_Click();
        private void btnAlertToggle_Click() => Train.ToggleAlert();

        private void btnAlertOn_Click(object sender, RoutedEventArgs e) => btnAlertOn_Click();
        private void btnAlertOn_Click() => Train.SetAlertOn();
        
        private void btnAlertOff_Click(object sender, RoutedEventArgs e) => btnAlertOff_Click();
        private void btnAlertOff_Click() => Train.SetAlertOff();

        private void btnCautionToggle_Click(object sender, RoutedEventArgs e) => btnCautionToggle_Click();
        private void btnCautionToggle_Click() => Train.ToggleCaution();

        private void btnCautionOn_Click(object sender, RoutedEventArgs e) => btnCautionOn_Click();
        private void btnCautionOn_Click() => Train.SetCautionOn();
        private void btnCautionOn_RightClick(object sender, RoutedEventArgs e)
        {
            if (++Train.CautionDurationIndex >= Trainer.CautionDurations.Length)
            {
                Train.CautionDurationIndex = 0;
            }

            /*
            string s = btnCautionOn.Content.ToString();
            string prefix = s.Substring(0, s.LastIndexOf("("));
            btnCautionOn.Content = $"{prefix}({Trainer.CautionDurations[Train.CautionDurationIndex]} secs)";
            */

            btnCautionOn.Content = $"{Trainer.CautionDurations[Train.CautionDurationIndex]} secs";
        }

        private void btnCautionOff_Click(object sender, RoutedEventArgs e) => btnCautionOff_Click();
        private void btnCautionOff_Click() => Train.SetCautionOff();

        private void btnRestartRoom_Click(object sender, RoutedEventArgs e) => btnRestartRoom_Click();
        private void btnRestartRoom_Click() => Train.DoRestartRoom();

        private void btnSuicide_Click(object sender, RoutedEventArgs e) => btnSuicide_Click();
        private void btnSuicide_Click() => Train.DoSuicide();

        private void btnHealthFill_Click(object sender, RoutedEventArgs e) => btnHealthFill_Click();
        private void btnHealthFill_Click() => Train.SetHealthFull();

        private void btnHealthFill_RightClick(object sender, RoutedEventArgs e) => Train.ToggleHealthLock();

        private void RefreshWeaponValues(object sender, RoutedEventArgs e) => Train.RefreshWeaponValues();

        private void SetWeaponCurrent(object sender, RoutedEventArgs e) => Train.SetWeaponCurrent();

        private void SetWeaponMax(object sender, RoutedEventArgs e) => Train.SetWeaponMax();

        private void RefreshItemValues(object sender, RoutedEventArgs e) => Train.RefreshItemValues();

        private void SetItemCurrent(object sender, RoutedEventArgs e) => Train.SetItemCurrent();

        private void SetItemMax(object sender, RoutedEventArgs e) => Train.SetItemMax();

        private void RefreshProgressValue()
        {
            cmbProgress.SelectionChanged -= SetProgress;
            Train.RefreshProgressValue();
            cmbProgress.SelectionChanged += SetProgress;
        }

        private void SetProgress(object sender, SelectionChangedEventArgs e) => Train.SetProgress(true);

        private void SetProgress(object sender, KeyEventArgs e) => Train.SetProgress(false);

        private void SetDifficulty(object sender, SelectionChangedEventArgs e) => Train.SetDifficulty((byte)cmbDifficulty.SelectedIndex);

        private void SetName(object sender, KeyEventArgs e) => Train.SetName();

        private void RefreshPositionValues() => Train.RefreshPositionValues();

        private void chkPosXLock_Checked(object sender, RoutedEventArgs e)
        {
            txtPosX.Text = Train.PositionX.ToString();
            txtPosX.Focus();
            txtPosX.SelectAll();
        }

        private void chkPosYLock_Checked(object sender, RoutedEventArgs e)
        {
            txtPosY.Text = Train.PositionY.ToString();
            txtPosY.Focus();
            txtPosY.SelectAll();
        }

        private void chkPosZLock_Checked(object sender, RoutedEventArgs e)
        {
            txtPosZ.Text = Train.PositionZ.ToString();
            txtPosZ.Focus();
            txtPosZ.SelectAll();
        }
        private void chkPosXLock_Unchecked(object sender, RoutedEventArgs e) => txtPosX.Text = "";
        private void chkPosYLock_Unchecked(object sender, RoutedEventArgs e) => txtPosY.Text = "";
        private void chkPosZLock_Unchecked(object sender, RoutedEventArgs e) => txtPosZ.Text = "";

        private bool ZMovementActive { get; set; } = false;
        private void btnUpdatePos_Click(object sender, RoutedEventArgs e)
        {
            if (chkPosXLock.IsChecked == true)
            {
                Train.SetPositionX(float.Parse(txtPosX.Text, CultureInfo.InvariantCulture.NumberFormat));
            }
            if (chkPosYLock.IsChecked == true)
            {
                Train.SetPositionY(float.Parse(txtPosY.Text, CultureInfo.InvariantCulture.NumberFormat));
            }
            if (chkPosZLock.IsChecked == true)
            {
                if (Train.ZMovementEnabled)
                {
                    Train.DisableZMovement();
                    PosZEnableNextFrame = true;
                }
                Train.SetPositionZ(float.Parse(txtPosZ.Text, CultureInfo.InvariantCulture.NumberFormat));
            }
        }

        private bool PosZEnableNextFrame { get; set; } = false;
        private void btnPosZRecover_Click(object sender, RoutedEventArgs e)
        {
            Train.DisableZMovement();
            Train.SetPositionZ(50000);
            PosZEnableNextFrame = true;
        }
        private void btnPosZToggle_Click(object sender, RoutedEventArgs e) => Train.ToggleZMovement();

        private void sldPosDelta_DoubleClick(object sender, MouseButtonEventArgs e) => ((Slider)sender).Value = 0;

        private void chkBossPractice_RightClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
