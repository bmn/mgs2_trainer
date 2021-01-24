using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

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
        private Timer TimeoutHiRes { get; set; }


        public ICommand LoadWarpsProfileCommand { get; internal set; }

        public bool HotkeysEnabled
        {
            get
            {
                return HotkeyManager.Current.IsEnabled;
            }
            set
            {
                HotkeyManager.Current.IsEnabled = value;
            }
        }

        public MainWindow(App app, Trainer t)
        {
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            Application = app;

            Train = t;
            DataContext = Train;

            Train.SetSelectedWarpGroup("Tanker");

            Closed += new EventHandler(Window_Closed);
            InitializeComponent();

            btnContent = new Dictionary<string, Button>()
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

            RegisterHotKeys();

            txtWeaponCurrent.ValueChanged += SetWeaponCurrent;
            txtWeaponMax.ValueChanged += SetWeaponMax;
            txtWeaponCurrent.KeyUp += SetWeaponCurrent;
            txtWeaponMax.KeyUp += SetWeaponMax;

            txtItemCurrent.ValueChanged += SetItemCurrent;
            txtItemCurrent.KeyUp += SetItemCurrent;
            txtItemMax.ValueChanged += SetItemMax;
            txtItemMax.KeyUp += SetItemMax;

            cmbProgress.SelectionChanged += SetProgress;
            txtProgress.ValueChanged += SetProgress;
            txtProgress.KeyUp += SetProgress;

            txtName.KeyUp += SetName;

            //cmbDifficulty.SelectionChanged += SetDifficulty;

            Timeout = new Timer(1000);
            Timeout.Elapsed += UpdateValues;
            Timeout.Enabled = true;

            TimeoutPos = new Timer(100);
            TimeoutPos.Elapsed += UpdateDeltaMovement;
            TimeoutPos.Enabled = true;

            TimeoutHiRes = new Timer(16);
            TimeoutHiRes.Elapsed += PollPadHotkeys;
            TimeoutHiRes.Enabled = true;

            var commandHelp = new List<string>();
            var commandProps = typeof(CommandOptions).GetProperties();
            foreach (PropertyInfo prop in commandProps)
            {
                string txt = prop.GetCustomAttribute<OptionAttribute>().HelpText;
                int pos = txt.IndexOf(" :: ");
                string left = txt.Substring(0, pos);
                string right = txt.Substring(pos + 4);

                commandHelp.Add(left.PadRight(33) + right);
            }
            txtCLICommandToolTip.Text = txtCLICommandToolTip.Text.Replace("\\n", Environment.NewLine) + Environment.NewLine + string.Join(Environment.NewLine, commandHelp);

            CountAreaMods();

            LoadWarpsProfileCommand = new RelayCommand(new Action<object>(LoadWarpsProfile));
        }

        private void btnRunCLICommand_Click(object sender, RoutedEventArgs e) => RunCLICommand(txtCLICommand.Text);
        private void RunCLICommand(string command) {
            if (!command.TrimStart().StartsWith("--"))
            {
                command = "--" + command;
            }

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

        private JsonElement GetHotkeyData(string name)
        {
            if (HotkeyData.ContainsKey(name))
            {
                return HotkeyData[name];
            }
            return new JsonElement();
        }

        public Dictionary<string, JsonElement> HotkeyData = new Dictionary<string, JsonElement>();
        public Dictionary<string, int> HotkeyCounter = new Dictionary<string, int>();
        public ContinueState cdata;
        private void RegisterHotKeys()
        {
            // todo remove button lambdas and roll into button defs
            var lambdas = new Dictionary<string, EventHandler<string>>()
            {
                { "SaveContinue", (sender, e) => {
                    //Train.Test();
                    cdata = Train.ContinueData;
                    int mode = 4;
                    if (mode == 0)
                    {
                        Clipboard.SetText(
                            @"{ """", new ContinueState(""" + Convert.ToBase64String(cdata.Data1) + @""", """ +
                            Convert.ToBase64String(cdata.Data2) + @""") }," + "\n"
                        );
                    }
                    else if (mode == 1)
                    {
                        Clipboard.SetText(
                            Convert.ToBase64String(cdata.Data1)
                            + "\n" +
                            Convert.ToBase64String(cdata.Data2)
                        );
                    }
                    else if (mode == 2)
                    {
                        Clipboard.SetText(
                            BitConverter.ToString(cdata.Data1).Replace("-","") +
                            BitConverter.ToString(cdata.Data2).Replace("-","")
                        );
                    }
                    else if (mode == 3)
                    {
                        Clipboard.SetText(BitConverter.ToString(cdata.Data2).Replace("-",""));
                        Thread.Sleep(500);
                        Clipboard.SetText(BitConverter.ToString(cdata.Data1).Replace("-",""));
                    }
                    else if (mode == 4)
                    {
                        Clipboard.SetText(
                            "31323334000000000000000000000000" +
                            BitConverter.ToString(cdata.Data1).Replace("-","") +
                            BitConverter.ToString(cdata.Data2).Replace("-","")
                        );
                    }
                } },
                { "LoadContinue", (sender, e) => {
                    Train.ContinueData = cdata;
                    Train.DoRestartRoom();
                } },
                { "SaveState", (sender, e) => State.SaveState() },
                { "LoadState", (sender, e) => State.LoadState() },
                { "ResetGame", (sender, e) => Train.DoResetGame() },
                { "SetProgress", (sender, e) => {
                    var d = GetHotkeyData(e);
                    if (d.ValueKind == JsonValueKind.Number) {
                        d.TryGetInt16(out short v);
                        Train.SetProgress(v);
                    }
                    else if (d.ValueKind == JsonValueKind.Array)
                    {
                        if (!HotkeyCounter.ContainsKey(e))
                        {
                            HotkeyCounter[e] = 0;
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
                        Train.SetProgress((short)(vals[HotkeyCounter[e] % vals.Count]));
                        HotkeyCounter[e]++;
                    }
                    else
                    {
                        Train.SetProgress((short)(Train.ProgressCurrent + 1));
                    }
                } },
                { "RunCommand", (sender, e) => {
                    var d = GetHotkeyData(e);
                    if (d.ValueKind == JsonValueKind.String)
                    {
                        string v = d.GetString();
                        RunCLICommand(v);
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

            var padMask = new Dictionary<string, uint>
            {
                { "up", 0x1000 }, { "stickup", 0xF0001000 }, { "padup", 0xE1001000 },
                { "right", 0x2000 }, { "stickright", 0xF0002000 }, { "padright", 0xD2002000 },
                { "down", 0x4000 }, { "stickdown", 0xF0004000 }, { "paddown", 0xB4004000 },
                { "left", 0x8000 }, { "stickleft", 0xF0008000 }, { "padleft", 0x78008000 },
                { "rstickup", 0x100000 },
                { "rstickright", 0x200000 },
                { "rstickdown", 0x400000 },
                { "rstickleft", 0x800000 },
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
                            HotkeyManager.Current.AddOrReplace(name, hotkey, modkey, (sender, e) => lambdas[key.Name].Invoke(sender, e.Name));
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

                    int padLength = key.Pad.Length;
                    if (padLength > 0)
                    {
                        uint sig = 0;
                        foreach (string k in key.Pad)
                        {
                            string l = k.ToLowerInvariant();
                            if (padMask.ContainsKey(l))
                            {
                                sig += padMask[l];
                            }
                        }
                        if (lambdas.ContainsKey(key.Name))
                        {
                            string name = key.Name + j++;
                            HotkeyData.Add(name, key.Data);
                            PadHotkeys.Add(new Tuple<uint, string, EventHandler<string>>(sig, name, (sender, e) => lambdas[key.Name].Invoke(sender, e)));
                            // lambdas[key.Name]
                        }
                        else if (btnContent.ContainsKey(key.Name))
                        {
                            var btn = btnContent[key.Name];
                            PadHotkeys.Add(new Tuple<uint, string, EventHandler<string>>(sig, key.Name, (sender, e) =>
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    btn.RaiseEvent(eventArgs);
                                });
                            }));
                        }
                    }
                }
                catch (HotkeyAlreadyRegisteredException)
                {
                    //MessageBox.Show("Failed to register at least one global hotkey.");
                }
            }
        }
        private List<Tuple<uint, string, EventHandler<string>>> PadHotkeys = new List<Tuple<uint, string, EventHandler<string>>>();
        private Dictionary<string, Button> btnContent;

        uint PrevPadInput;
        private void PollPadHotkeys(object sender, ElapsedEventArgs e)
        {
            if ((!HotkeysEnabled) || (PadHotkeys.Count == 0))
            {
                return;
            }
            uint input = Train.PadInput;
            foreach (var hk in PadHotkeys)
            {
                uint key = hk.Item1;
                if (((PrevPadInput & key) == key) && ((input & key) != key))
                {
                    string name = hk.Item2;
                    EventHandler<string> lambda = hk.Item3;

                    PrevPadInput = input;
                    lambda.Invoke(this, name);
                    return;
                }
            }
            PrevPadInput = input;

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

        private void SetProgress(object sender, RoutedEventArgs e) => Train.SetProgress(false);

        //private void SetDifficulty(object sender, SelectionChangedEventArgs e) => Train.SetDifficulty((byte)cmbDifficulty.SelectedIndex);

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

        private void btnRadarToggle_Click(object sender, RoutedEventArgs e) => Train.ToggleRadar();
        private void btnEquipModeToggle_Click(object sender, RoutedEventArgs e) => Train.ToggleEquipMode();
        private void btnDifficultyToggle_Click(object sender, RoutedEventArgs e) => Train.ToggleDifficulty();
        private void btnPracticeModeToggle_Click(object sender, RoutedEventArgs e) => Train.TogglePracticeMode();
        private void btnRoomModToggle_Click(object sender, RoutedEventArgs e) => Train.ToggleRoomMods();
        private void btnHotkeyToggle_Click(object sender, RoutedEventArgs e) => HotkeysEnabled ^= true;
        private void btnRestartRoom_RightClick(object sender, MouseButtonEventArgs e) => Train.DoResetGame();

        private void btnContinueRoomSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("The Warps feature is a work in progress. This will be available in a later update.");
        }

        private void btnContinueProfile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("The Warps feature is a work in progress. This will be available in a later update.");
        }

        private void btnContinueRoom_Click(object sender, RoutedEventArgs e)
        {
            //Train.ApplySelectedContinueState();
            Train.ApplySelectedWarp();
            Train.DoRestartRoom();
        }


        private void cmbContinueStateRoom_Changed(object sender, SelectionChangedEventArgs e)
        {
            //Train.UpdateContinueStateRoomVariants();
            Train.UpdateWarpEntries();
        }

        private void LoadWarpsProfile(object sender/*, ExecutedRoutedEventArgs e*/)
        {
            Train.SetSelectedWarpGroup(sender as string);
        }
        private void LoadWarpsProfile_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }




        private void cmbInitialState_ItemSelectionChanged(object sender, Xceed.Wpf.Toolkit.Primitives.ItemSelectionChangedEventArgs e) => CountAreaMods();
        private void CountAreaMods()
        {
            int count = Train.SelectedAreaMods.Count();
            if (count != 1)
            {
                cmbInitialState.Text = "Area Modifications: " + count +" selected";
            }
            Train.TriggerOnPropertyChanged("SelectedAreaMods");
        }

        private void ContextMenu_TargetUpdated(object sender, DataTransferEventArgs e)
        {

        }
    }
}
