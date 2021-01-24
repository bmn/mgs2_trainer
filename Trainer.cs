using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using Timer = System.Timers.Timer;

namespace MGS2Trainer
{
    public class Trainer : INotifyPropertyChanged
    {
        public Memory Mem { get; private set; }
        //public static UInt32 WeaponAddr { get; } = 0x653E08;
        //public static UInt32 WeaponCurrentAddrOffset { get; } = 0x2;
        //public static UInt32 WeaponMaxAddrOffset { get; } = 0x4A;
        public static UInt32 WeaponCurrentAddrOffset { get; } = 0x15E;
        public static UInt32 WeaponMaxAddrOffset { get; } = 0x1A6;
        public bool SetContinueAmmo { get; set; }
        public uint WeaponSelected { get; set; }
        public short WeaponAmmoCurrent { get; set; }
        public short WeaponAmmoMax { get; set; }
        public static UInt32 ItemAddr { get; } = 0x653E10;// = 0xD8AFAE;
        //public static UInt32 ItemCurrentAddrOffset { get; } = 0x2;
        //public static UInt32 ItemMaxAddrOffset { get; } = 0x62;
        public static UInt32 ItemCurrentAddrOffset { get; } = 0x1EE;
        public static UInt32 ItemMaxAddrOffset { get; } = 0x24E;
        public uint ItemSelected { get; set; }
        public short ItemAmmoCurrent { get; set; }
        public short ItemAmmoMax { get; set; }
        public short ProgressCurrent { get; set; }
        public short ProgressCurrentInList { get; set; }
        public static UInt32 ProgressTankerAddr { get; } = 0xD8D93C;
        public static UInt32 ProgressPlantAddr { get; } = 0xD8D912;
        public static UInt32 ProgressPlantContinueAddr { get; } = 0xD8F512;
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int PositionXDelta { get; set; }
        public int PositionYDelta { get; set; }
        public int PositionZDelta { get; set; }
        public static UInt32 PositionAddr { get; } = 0xB609D0;
        public string Name { get; set; }
        public static UInt32 NameAddr { get; } = 0x5A84DC;
        public static UInt32 EngineRoomRavenAddr { get; } = 0xD8DC29;
        public static UInt32 EngineRoomRepairAddr { get; } = 0xD8E420;
        public static UInt32 EngineRoomPatternAddr { get; } = 0xD8E4E4;
        public int InitialState { get; set; } = 0;
        public static UInt32 DogTagsArrayAddr { get; } = 0xD8C394;
        public static byte[] DogTagsArrayClear { get; } = new byte[0x80];
        public bool LockDogTags { get; set; } = false;
        public bool BossPractice { get; set; } = false;
        public float BossPracticeDelay { get; set; } = 0;
        private byte[] ContinueBytes { get; set; }
        public static UInt32 ContinueBytesAddr { get; } = 0xD8FE00;
        public static UInt32 RoomCodeAddr { get; } = 0xD8ADEC;
        public static UInt32 WeaponSelectedAddr { get; } = 0xD8AEC4;
        public bool RoomModsEnabled { get; set; }
        public int RadarType { get; set; }
        public WarpConfig WarpData { get; set; }
        public Warp SelectedWarpGroup { get; set; }
        public Warp SelectedWarp { get; set; }
        public List<Warp> WarpEntries { get; set; }

        public static Dictionary<byte, List<byte>> WeaponGroups = new Dictionary<byte, List<byte>>() {
            {  0, new List<byte> { 0 } },
            {  1, new List<byte> { 1, 3 } },
            {  2, new List<byte> { 18, 15, 4, 19 } },
            {  3, new List<byte> { 10, 11, 16, 17 } },
            {  4, new List<byte> { 5, 6, 7 } },
            {  5, new List<byte> { 13 } },
            {  6, new List<byte> { 12 } },
            {  7, new List<byte> { 21 } },
            {  8, new List<byte> { 8, 9 } },
            {  9, new List<byte> { 14 } }
        };

        public static ushort[] CautionDurations = new ushort[] { 45, 60, 120, ushort.MaxValue };
        public int CautionDurationIndex { get; set; } = 1;

        public static Dictionary<uint, string> WeaponNames { get; set; } = new Dictionary<uint, string>()
        {
            { 0, "M9" },
            { 1, "USP" },
            { 2, "SOCOM" },
            { 14, "AKS-74u" },
            { 17, "M4" },
            { 3, "PSG-1" },
            { 18, "PSG-1T" },
            { 16, "Grenade" },
            { 9, "Chaff Grenade" },
            { 10, "Stun Grenade" },
            { 15, "Magazine" },
            { 4, "RGB6" },
            { 5, "Nikita" },
            { 6, "Stinger" },
            { 12, "HF Blade" },
            { 11, "D.Mic" },
            { 20, "Book" },
            { 7, "Claymore" },
            { 8, "C4" },
            { 19, "D.Mic (cutscene)" }
        };
        public static byte[] ValidWeaponsTanker = new byte[] { 0, 1, 9, 10, 15, 16 };
        public static byte[] ValidWeaponsPlant = new byte[] { 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20 };
        // +1 for the equipped weapon id
        // (0)M9, (1)USP, (2)SOCOM, (3)PSG-1, (4)RGB6, (5)Nikita, (6)Stinger, (7)Claymore, (8)C4, (9)Chaff
        // (10)Stun, (11)D.Mic, (12)HF Blade, (13)Coolant, (14)AK-74u, (15)Magazine, (16)Grenade, (17)M4, (18)PSG-1T, (19)D.Mic(CS)
        // (20)Book

        public static Dictionary<uint, string> ItemNames { get; set; } = new Dictionary<uint, string>()
        {
            { 0, "Ration" },
            { 13, "Scope" },
            { 14, "Digital Camera" },
            { 20, "Camera" },
            { 11, "Night Vision Goggles" },
            { 12, "Thermal Goggles" },
            { 9, "Sensor A" },
            { 10, "Sensor B" },
            { 8, "Mine Detector" },
            { 24, "AP Sensor" },
            { 15, "Box 1" },
            { 23, "Wet Box" },
            { 21, "Box 2" },
            { 22, "Box 3" },
            { 25, "Box 4" },
            { 26, "Box 5" },
            { 6, "Body Armor" },
            { 5, "B.D.U." },
            { 36, "Wig A (Blue)" },
            { 37, "Wig B (Orange)" },
            { 31, "Bandana" },
            { 35, "Infinity Wig (Brown)" },
            { 7, "Stealth" },
            { 32, "Dog Tags" },
            { 18, "Shaver" },
            { 19, "Phone" },
            { 28, "SOCOM Suppressor" },
            { 29, "AK Suppressor" },
            { 34, "USP Suppressor" },
            { 17, "Card" },
            { 33, "MO Disc" },
            { 16, "Cigs" },
            { 3, "Bandage" },
            { 4, "Pentazemin" },
            { 2, "Medicine" },
            { 1, "Scope (cutscene)" },
            { 30, "Camera (cutscene)" },
            { 38, "Wig C" },
            { 39, "Wig D" }
        };
        public static byte[] ValidItemsTanker = new byte[] { 0, 2, 3, 6, 7, 12, 14, 15, 16, 20, 23, 31, 34 };
        public static byte[] ValidItemsPlant = new byte[] { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 24, 25, 26, 28, 29, 33, 35, 36, 37 };
        // (0)Ration, (1)Scope(CS), (2)Medicine, (3)Bandage, (4)Pentazemin, (5)BDU, (6)B.Armor, (7)Stealth, (8)Mine.D, (9)Sensor A
        // (10)Sensor B, (11)NVG, (12)Therm.G, (13)Scope, (14)D.Camera, (15)Box 1, (16)Cigs, (17)Card, (18)Shaver, (19)Phone
        // (20)Camera, (21)Box 2, (22)Box 3, (23)Wet Box, (24)AP Sensor, (25)Box 4, (26)Box 5, (27)?, (28)SOCOM Supp, (29)AK Supp
        // (30)Camera(CS), (31)Bandana, (32)Dog Tags, (33)MO Disc, (34)USP Supp, (35)Inf.Wig, (36)Blue Wig, (37)Orange Wig, (38)Wig C, (39)Wig D

        public static List<string> Difficulties { get; } = new List<string> { "Very Easy", "Easy", "Normal", "Hard", "Extreme", "European Extreme" };
        public static List<string> Diffs { get; } = new List<string> { "VE", "Ez", "Nm", "Hd", "Ex", "EE" };
        public static List<Brush> DiffBrushes { get; } = new List<Brush> { Brushes.LightBlue, Brushes.LightCyan, Brushes.LightGreen, Brushes.LightYellow, Brushes.Orange, Brushes.IndianRed };
        public byte DifficultyCurrent { get; set; }



        public Timer _timer { get; set; }


        public Trainer()
        {
            AttachToGame();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AttachToGame()
        {
            Mem = new Memory("mgs2_sse");
        }

        public uint PadInput
        {
            get
            {
                uint main = Mem.ReadAddress<uint>(0xADADDC, null);

                uint neg = Mem.ReadAddress<uint>(0xAD55AC, null);
                uint pos = (0xF000 - (neg & 0xF000)) << 12;
                neg = (neg & 0xF000) << 16;

                ushort right = Mem.ReadAddress<ushort>(0xADADEC, null);
                byte rightx = (byte)(right & 0xFF);
                byte righty = (byte)((right & 0xFF00) >> 8);
                uint rstick = 0;
                if (righty < 0x50) rstick += 1;
                if (rightx > 0xA0) rstick += 2;
                if (righty > 0xA0) rstick += 4;
                if (rightx < 0x50) rstick += 8;
                rstick = rstick << 20;

                // 0x12344444 where 1=negative dpad, 2=positive dpad, 3=positive right stick, 4=everything else
                return (main + pos + neg + rstick);
            }
        }

        public void SetAlert(bool on)
        {
            byte val = (byte)(on ? 1 : 0);
            Mem.WriteAddress<byte>(0xD8AEDA, null, val);
        }

        public void SetAlertOn() => SetAlert(true);

        public void SetAlertOff() => SetAlert(false);

        public void ToggleAlert()
        {
            SetAlert(Mem.ReadAddress<byte>(0xD8AEDA, null) == 0);
        }

        public void SetCaution(ushort time)
        {
            Mem.WriteAddress<ushort>(0x6160C8, null, time);
        }

        public void SetCautionMax(ushort time)
        {
            Mem.WriteAddress<ushort>(0xD8F508, null, time);
        }

        public void ToggleCaution()
        {
            if (Mem.ReadAddress<byte>(0x6160C8, null) == 0)
            {
                SetCautionOn();
            }
            else
            {
                SetCautionOff();
            }
        }

        public void SetCautionOn()
        {
            ushort time = (ushort)(CautionDurations[CautionDurationIndex] * 60);
            SetCautionMax(time);
            SetCaution(time);
        }

        public void SetCautionOff() => SetCaution(0);

        public void SetHealth(byte hp)
        {
            // Attempt to write in-area address
            if (!Mem.WriteAddress<byte>(0xB60918, new UInt32[] { 0xD2 }, hp))
            {
                // Write out-of-area address
                Mem.WriteAddress<byte>(0x3E315E, new UInt32[] { 0x2D }, hp);
            }
        }

        public void SetHealthFull() => SetHealth(200);
        public void DoSuicide()
        {
            SetItemCurrent(0, 0); // remove rations 
            SetHealth(0);
        }

        public void ToggleHealthLock()
        {

        }

        // short: 1 = Vibration OFF, 4 = No Radar, 8 = Blood OFF
        // 0x20 = Radar 2, 0x40 = Reverse view, 0x80 = Linear menu, 0x200 = Previous equip
        UInt32 OptionsAddr = 0x601F34;
        UInt32 OptionsContinueAddr = 0x601F38;
        UInt32[] OptionsOffset = new UInt32[] { 6 };
        public void SetRadar(int type)
        {
            if (type == -1)
            {
                type = ((RadarType + 1) % 3);
            }

            if ((type >= 0) && (type <= 2))
            {
                short opts = Mem.ReadAddress<short>(OptionsAddr, OptionsOffset);
                opts &= (short.MaxValue - 0x24);

                if (type == 0)
                {
                    opts |= 4;
                }
                else if (type == 2)
                {
                    opts |= 0x20;
                }
                Mem.WriteAddress<short>(OptionsAddr, OptionsOffset, opts);
                Mem.WriteAddress<short>(OptionsContinueAddr, OptionsOffset, opts);
                RefreshRadar();
            }
        }
        public void SetRadarOn() => SetRadar(1);
        public void SetRadarOff() => SetRadar(0);
        public void ToggleRadar() => SetRadar(-1);
        public void RefreshRadar()
        {
            short opts = Mem.ReadAddress<short>(OptionsAddr, OptionsOffset);

            int type = 1;
            if ((opts & 0x20) == 0x20)
            {
                type = 2;
            }
            else if ((opts & 4) == 4)
            {
                type = 0;
            }

            if (RadarType != type)
            {
                RadarType = type;
                OnPropertyChanged("RadarType");
            }

        }

        public void SetGOID(bool on)
        {
            var offset = new UInt32[] { 7 };
            byte opts = Mem.ReadAddress<byte>(OptionsAddr, offset);

            if (on)
            {
                opts |= 8;
            }
            else
            {
                opts &= byte.MaxValue - 8;
            }

            Mem.WriteAddress<byte>(OptionsAddr, offset, opts);
            Mem.WriteAddress<byte>(OptionsContinueAddr, offset, opts);
        }
        public void SetGOIDOn() => SetGOID(true);
        public void SetGOIDOff() => SetGOID(false);

        public void SetEquipMode(bool previous, bool toggle = false)
        {
            short opts = Mem.ReadAddress<short>(OptionsAddr, OptionsOffset);

            if (toggle)
            {
                opts ^= 0x200;
            }
            else
            {
                opts = (short)(previous ? (opts | 0x200) : (opts & (short.MaxValue - 0x200)));
            }

            Mem.WriteAddress<short>(OptionsAddr, OptionsOffset, opts);
            Mem.WriteAddress<short>(OptionsContinueAddr, OptionsOffset, opts);
            RefreshEquipMode();
        }
        public void SetEquipPrevious() => SetEquipMode(true);
        public void SetEquipUnequip() => SetEquipMode(false);
        public void ToggleEquipMode() => SetEquipMode(true, true);
        public void RefreshEquipMode()
        {
            short opts = Mem.ReadAddress<short>(OptionsAddr, OptionsOffset);
            // ....
        }

        public void TogglePracticeMode() => BossPractice ^= true;
        public void ToggleRoomMods() => RoomModsEnabled ^= true;



        public void DoRestartRoom(bool reset = false)
        {
            // Don't attempt to restart inside a codec
            if (Mem.ReadAddress<byte>(0xD8ADB0, null) == 1)
            {
                /*
                if (RestartAttempts < 3)
                {
                    RestartAttempts++;
                    uint input = Mem.ReadAddress<uint>(0xADADDC, null);
                    Mem.WriteAddress<uint>(0xADADDC, null, input | 0x100);
                    Thread.Sleep(1000);
                    DoRestartRoom(reset);
                }
                else
                {
                    RestartAttempts = 0;
                }
                */
            }
            else {
                UInt32 continuesAddr = 0x3E315E;
                UInt32[] continuesOffset = new UInt32[] { 0x65 };
                short continues = Mem.ReadAddress<short>(continuesAddr, continuesOffset);
                if (continues >= 0)
                {
                    Mem.WriteAddress<short>(continuesAddr, continuesOffset, (short)(continues - 1));
                }
                //Mem.WriteAddress<byte>(0x601F38, new UInt32[] { 0xBC }, (byte)(Mem.ReadAddress<byte>(0x601F38, new UInt32[] { 0xBC }) + 1));
                Mem.GameProcess[(IntPtr)0x477de0, true].Execute(reset ? 1 : 0);
            }
        }
        public void DoRestartRoom(bool reset, float delay)
        {
            DoRestartRoom(reset);

            if (delay > 0)
            {
                Mem.SuspendProcess();
                Thread.Sleep((int)(delay * 1000));
                Mem.ResumeProcess();
            }
        }
        public void DoResetGame() => DoRestartRoom(true);

        public void RefreshName()
        {
            Name = Mem.ReadStringAddress(NameAddr, null, 20);
            OnPropertyChanged("Name");
        }

        public void SetName() => SetName(Name);
        public void SetName(string name)
        {
            Mem.WriteStringAddress(NameAddr, null, name);
        }



        public void RefreshWeaponValues()
        {
            WeaponAmmoCurrent = Mem.ReadAddress<short>(OptionsAddr, new UInt32[] { WeaponCurrentAddrOffset + (WeaponSelected * 2) });
            WeaponAmmoMax = Mem.ReadAddress<short>(OptionsAddr, new UInt32[] { WeaponMaxAddrOffset + (WeaponSelected * 2) });
            OnPropertyChanged("WeaponAmmoCurrent");
            OnPropertyChanged("WeaponAmmoMax");
        }

        public void SetWeaponCurrent(uint? item = null, short? ammo = null)
        {
            if (item == null) item = WeaponSelected;
            if (ammo == null) ammo = WeaponAmmoCurrent;
            Mem.WriteAddress<short>(OptionsAddr, new UInt32[] { WeaponCurrentAddrOffset + ((uint)item * 2) }, (short)ammo);
            if (SetContinueAmmo)
            {
                Mem.WriteAddress<short>(OptionsContinueAddr, new UInt32[] { WeaponCurrentAddrOffset + ((uint)item * 2) }, (short)ammo);
            }
        }

        public void SetWeaponMax(uint? item = null, short? ammo = null)
        {
            if (item == null) item = WeaponSelected;
            if (ammo == null) ammo = WeaponAmmoMax;
            Mem.WriteAddress<short>(OptionsAddr, new UInt32[] { WeaponMaxAddrOffset + ((uint)item * 2) }, (short)ammo);
            if (SetContinueAmmo)
            {
                Mem.WriteAddress<short>(OptionsContinueAddr, new UInt32[] { WeaponMaxAddrOffset + ((uint)item * 2) }, (short)ammo);
            }
        }

        public void RefreshItemValues()
        {
            ItemAmmoCurrent = Mem.ReadAddress<short>(OptionsAddr, new UInt32[] { ItemCurrentAddrOffset + (ItemSelected * 2) });
            ItemAmmoMax = Mem.ReadAddress<short>(OptionsAddr, new UInt32[] { ItemMaxAddrOffset + (ItemSelected * 2) });
            OnPropertyChanged("ItemAmmoCurrent");
            OnPropertyChanged("ItemAmmoMax");
        }

        public void RefreshPositionValues()
        {
            if (Mem.ReadAddress<UInt32>(PositionAddr, null) != 0)
            {
                PositionX = Mem.ReadAddress<float>(PositionAddr, new UInt32[] { 0 });
                PositionZ = Mem.ReadAddress<float>(PositionAddr, new UInt32[] { 4 });
                PositionY = Mem.ReadAddress<float>(PositionAddr, new UInt32[] { 8 });
                OnPropertyChanged("PositionX");
                OnPropertyChanged("PositionZ");
                OnPropertyChanged("PositionY");
            }
        }

        public void SetPosition(float v, UInt32 offset)
        {
            Mem.WriteAddress<float>(PositionAddr, new UInt32[] { offset }, v);
        }
        public void SetPositionX(float v) => SetPosition(v, 0);
        public void SetPositionZ(float v) => SetPosition(v, 4);
        public void SetPositionY(float v) => SetPosition(v, 8);

        public void SetItemCurrent(uint? item = null, short? ammo = null)
        {
            if (item == null) item = ItemSelected;
            if (ammo == null) ammo = ItemAmmoCurrent;
            Mem.WriteAddress<short>(OptionsAddr, new UInt32[] { ItemCurrentAddrOffset + ((uint)item * 2) }, (short)ammo);
            if (SetContinueAmmo)
            {
                Mem.WriteAddress<short>(OptionsContinueAddr, new UInt32[] { ItemCurrentAddrOffset + ((uint)item * 2) }, (short)ammo);
            }
        }

        public void SetItemMax(uint? item = null, short? ammo = null)
        {
            if (item == null) item = ItemSelected;
            if (ammo == null) ammo = ItemAmmoMax;
            Mem.WriteAddress<short>(OptionsAddr, new UInt32[] { ItemMaxAddrOffset + ((uint)item * 2) }, (short)ammo);
            if (SetContinueAmmo)
            {
                Mem.WriteAddress<short>(OptionsContinueAddr, new UInt32[] { ItemMaxAddrOffset + ((uint)item * 2) }, (short)ammo);
            }
        }

        public void UnlockAllWeapons(bool everything = false)
        {
            byte[] valid = (CurrentProgressArea == Area_Plant) ? ValidWeaponsPlant : ValidWeaponsTanker;
            for (byte i = 0; i < 21; i++)
            {
                if ((everything) || (valid.Contains(i)))
                {
                    short max = ((i >= 11) && (i <= 13)) ? (short)1 : short.MaxValue;
                    SetWeaponMax(i, max);
                    SetWeaponCurrent(i, max);
                }
            }
            RefreshWeaponValues();
        }

        public void UnlockAllItems(bool everything = false)
        {
            byte[] valid = (CurrentProgressArea == Area_Plant) ? ValidItemsPlant : ValidItemsTanker;
            byte[] boxes = new byte[] { 15, 21, 22, 23, 25, 26 };
            byte[] full = new byte[] { 0, 2, 3, 4, 17, 32 };
            for (byte i = 0; i < 40; i++)
            {
                if ((everything) || (valid.Contains(i)))
                {
                    short max = full.Contains(i) ? short.MaxValue : (boxes.Contains(i) ? (short)25 : (short)1);
                    SetItemMax(i, max);
                    SetItemCurrent(i, max);
                }
            }
            RefreshItemValues();
        }

        public void UnlockAllEquips(bool everything = false)
        {
            UnlockAllWeapons(everything);
            UnlockAllItems(everything);
        }

        public UInt32 ItemAddrCharOffset
        {
            get
            {
                if (Mem.ReadAddress<short>(ItemAddr - 2, null) == 1)
                {
                    return 0;
                }
                else
                {
                    return 0x150;
                }
            }
        }

        public void SwitchWeaponGroup(byte group)
        {
            var g = WeaponGroups[group];
            byte current = Mem.ReadAddress<byte>(WeaponSelectedAddr, null);
            int index = g.IndexOf(current);
            if ((index != -1) && ((index + 2) > g.Count))
            {
                Mem.WriteAddress<byte>(WeaponSelectedAddr, null, 0);
                return;
            }

            while (++index < g.Count)
            {
                byte next = g[index];
                if (Mem.ReadAddress<short>(OptionsAddr, new UInt32[] { WeaponCurrentAddrOffset + (((uint)next - 1) * 2) }) >= 0)
                {
                    Mem.WriteAddress<byte>(WeaponSelectedAddr, null, next);
                    return;
                }
            }
        }

        public void SwitchWeapon(byte weapon)
        {
            if (CurrentProgressArea == Area_Plant)
            {
                if (weapon == 2) weapon = 3;
            }
            if (Mem.ReadAddress<short>(OptionsAddr, new UInt32[] { WeaponCurrentAddrOffset + (((uint)weapon - 1) * 2) }) >= 0)
            {
                Mem.WriteAddress<byte>(WeaponSelectedAddr, null, weapon);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public const string Area_Tanker = "Tanker";
        public const string Area_Plant = "Plant";
        public string CurrentProgressArea { get; private set; } = "None";
        public SortedList<short, string> ProgressNames { get; private set; } = new SortedList<short, string>();
        public SortedList<short, string> ProgressNamesTanker { get; } = new SortedList<short, string>()
        {
            { 0, "[0] Tanker start" },
            { 2, "[2] Codec start" },
            { 14, "[14] Codec end (Aft Deck)" },
            { 15, "[15] Enter tanker" },
            { 16, "[16] Enter bridge" },
            { 25, "[25] Olga start" },
            { 26, "[26] Olga defeated" },
            { 29, "[29] Codec end (Nav deck)" },
            { 30, "[30] Semtex sensors shot" },
            { 31, "[31] Deck 2 starboard completed" },
            { 32, "[32] Guard Rush start" },
            { 33, "[33] Guard Rush defeated" },
            { 34, "[34] Codec end (Hold 1)" },
            { 43, "[43] Hold 3 start" },
            { 46, "[46] 2nd part of ending cutscene" },
            { 56, "[56] Results screen/Tanker-Plant transition" },
            { 58, "[58] Save prompt" }
        };
        public SortedList<short, string> ProgressNamesPlant { get; } = new SortedList<short, string>()
        {
            { 3, "[3] Codec start" },
            { 9, "[9] Codec end (dock)" },
            { 22, "[22] Dock end" },
            { 29, "[29] Roof start" },
            { 36, "[36] Codec end (AB)" },
            { 37, "[37] Vamp cutscene start" },
            { 58, "[58] Strut B start after Vamp" },
            { 59, "[59] Fortune cutscene start" },
            { 62, "[62] BC start after Fortune" },
            { 63, "[63] Stillman cutscene start" },
            { 92, "[92] Strut C start after Stillman" },
            { 94, "[94] 1 bomb codec" },
            { 96, "[96] 2 bomb codecs" },
            { 98, "[98] 3 bomb codecs" },
            { 100, "[100] 4 bomb codecs" },
            { 102, "[102] 362% start" },
            { 104, "[104] Stillman ded codec end" },
            { 106, "[106] Dock bomb defused" },
            { 108, "[108] Dock codec received" },
            { 109, "[109] Fortune cutscene start" },
            { 110, "[110] Fortune start" },
            { 115, "[115] Fortune end" },
            { 116, "[116] Roof start" },
            { 117, "[117] Fatman cutscene start" },
            { 118, "[118] Fatman start" },
            { 119, "[119] Fatman end" },
            { 120, "[120] Last bomb start" },
            { 122, "[122] Codec end after last bomb" },
            { 123, "[123] Ninja cutscene start" },
            { 148, "[148] Heliport start" },
            { 150, "[150] After Rose codec S1C" },
            { 151, "[151] B1 biometric tutorial start" },
            { 152, "[152] B1 start" },
            { 153, "[153] Biometric completed" },
            { 154, "[154] B1 Hall start (before codec)" },
            { 155, "[155] Found Ames" },
            { 173, "[173] Equip minigame start" },
            { 175, "[175] Equip end" },
            { 176, "[176] B1 start (before codec)" },
            { 180, "[180] B1 start (after codec)" },
            { 182, "[182] 1-2 sniping tutorial start" },
            { 185, "[185] 1-2 sensors destroyed" },
            { 188, "[188] Harrier cutscene start" },
            { 189, "[189] Harrier start" },
            { 190, "[190] Harrier end" },
            { 193, "[193] 1-2 bridge start" },
            { 194, "[194] S2C eavesdrop start" },
            { 203, "[203] S2C start" },
            { 204, "[204] Elec floor disabled" },
            { 205, "[205] Gain control" },
            { 206, "[206] Prez cutscene start" },
            { 227, "[227] Prez cutscene end" },
            { 241, "[241] Snake codec end" },
            { 246, "[246] Vamp cutscene start" },
            { 253, "[253] Vamp start" },
            { 254, "[254] Vamp defeated" },
            { 257, "[257] Codec end" },
            { 258, "[258] Reach Emma" },
            { 273, "[273] Swim start" },
            { 274, "[274] Emma cutscenes @ Vamp start" },
            { 281, "[281] Regain control" },
            { 282, "[282] Emma cutscenes @ swim 2 start" },
            { 297, "[297] Swim 2 start" },
            { 298, "[298] Card 5 cutscene" },
            { 299, "[299] Strut L start" },
            { 302, "[302] Ladder cutscene start" },
            { 313, "[313] Sniping start" },
            { 315, "[315] End of Snake arrival codec" },
            { 316, "[316] Vamp 2 cutscene start" },
            { 317, "[317] Vamp 2 start" },
            { 318, "[318] Vamp ded" },
            { 327, "[327] Strut E start" },
            { 328, "[328] Arsenal cutscenes start" },
            { 351, "[351] Choke 1 start" },
            { 354, "[354] Stomach cutscenes start" },
            { 371, "[371] NUT" },
            { 374, "[374] Gain control after NUT" },
            { 377, "[377] Gain control in Jejunum" },
            { 379, "[379] After stairs codec" },
            { 382, "[382] Asc Colon cutscenes start" },
            { 389, "[389] Sword tutorial start" },
            { 390, "[390] Tutorial end" },
            { 397, "[397] Tengus 1 intro" },
            { 400, "[400] Tengus 1 defeated (codec afterwards)" },
            { 401, "[401] Regain control" },
            { 402, "[402] Tengus 2 cutscene start" },
            { 403, "[403] Tengus 2 start" },
            { 404, "[404] Tengus 2 defeated" },
            { 411, "[411] Rays start" },
            { 412, "[412] Rays defeated" },
            { 418, "[418] Save prompt" },
            { 420, "[420] Post-choke cutscenes start" },
            { 469, "[469] Solidus start" },
            { 470, "[470] Solidus defeated" },
            { 484, "[484] Snake monologue" },
            { 486, "[486] Phone call" },
            //{ 487, "[P487] Hits 487, then game complete split when it goes back to 486" },
            { 490, "[490] Credits start" }
        };

        public void RefreshProgressValue()
        {
            string newArea = Area_Plant;
            short newProgress = Mem.ReadAddress<short>(ProgressPlantAddr, null);
            if (newProgress == 0)
            {
                newArea = Area_Tanker;
                newProgress = Mem.ReadAddress<short>(ProgressTankerAddr, null);
            }

            if (newArea != CurrentProgressArea)
            {
                CurrentProgressArea = newArea;
                ProgressNames = (newArea == Area_Tanker) ? ProgressNamesTanker : ProgressNamesPlant;
                OnPropertyChanged("ProgressNames");
            }

            if (newProgress != ProgressCurrent)
            {
                ProgressCurrent = newProgress;
                OnPropertyChanged("ProgressCurrent");
            }

            if (ProgressNames.ContainsKey(ProgressCurrent))
            {
                short newInList = (short)ProgressNames.IndexOfKey(ProgressCurrent);
                if (newInList != ProgressCurrentInList)
                {
                    ProgressCurrentInList = newInList;
                    OnPropertyChanged("ProgressCurrentInList");
                }
            }

        }

        public void SetProgress(bool combo)
        {
            short val = (combo) ? ProgressNames.Keys[ProgressCurrentInList] : ProgressCurrent;
            SetProgress(val);
        }

        public void SetProgress(short progress)
        {
            UInt32 addr = (CurrentProgressArea == Area_Plant) ? ProgressPlantAddr : ProgressTankerAddr;
            Mem.WriteAddress<short>(addr, null, progress);
            RefreshProgressValue();
        }

        public static UInt32 DifficultyAddr = 0xD8ADD0;
        public static UInt32 DifficultyContinueAddr = 0xD8C368;
        public void RefreshDifficulty()
        {
            byte diff = Mem.ReadAddress<byte>(DifficultyAddr, null);
            if ((diff != 0) && ((diff % 10) == 0))
            {
                DifficultyCurrent = (byte)((diff / 10) - 1);
                OnPropertyChanged("DifficultyCurrent");
            }
        }

        public void SetDifficulty(byte diff)
        {
            diff = (byte)((diff + 1) * 10);
            Mem.WriteAddress<byte>(DifficultyAddr, null, diff);
            Mem.WriteAddress<byte>(DifficultyContinueAddr, null, diff);
            RefreshDifficulty();
        }
        public void ToggleDifficulty()
        {
            byte cur = (byte)(Mem.ReadAddress<byte>(DifficultyAddr, null) / 10); // this is 1-based, so it's already just about right!
            SetDifficulty((byte)(cur % 6));
        }

        public static byte[] NOP = new byte[] { 0x90, 0x90, 0x90 };
        public static byte[] PositionZOp = new byte[] { 0xD9, 0x5E, 0x04 };
        public static UInt32[] PositionZOpAddr = new UInt32[] { 0x48079C, 0x480A7A };
        public void DisableZMovement()
        {
            Mem.SuspendProcess();
            Mem.WriteBytes(PositionZOpAddr[0], null, NOP);
            Mem.WriteBytes(PositionZOpAddr[1], null, NOP);
            Mem.ResumeProcess();
        }
        public void EnableZMovement()
        {
            Mem.SuspendProcess();
            Mem.WriteBytes(PositionZOpAddr[0], null, PositionZOp);
            Mem.WriteBytes(PositionZOpAddr[1], null, PositionZOp);
            Mem.ResumeProcess();
        }
        public void ToggleZMovement()
        {
            if (ZMovementEnabled) {
                DisableZMovement();
            }
            else
            {
                EnableZMovement();
            }
        }
        public bool ZMovementEnabled
        {
            get
            {
                return (Mem.ReadAddress<byte>(PositionZOpAddr[0], null) != 0x90);
            }
        }

        public static UInt32 RoomTimeAddr { get; } = 0xD8AEA4;
        public List<string> InitialStateAreas { get; } = new List<string>()
        {
            "No area modification",
            "Engine Room",
            "Shell 1 Core, B1",
            "Arsenal Gear - Jejunum"
            /*,
            "Shell 1 Core, B1 Hall (hostage variant 1)",
            "Shell 1 Core, B1 Hall (hostage variant 2)",
            "Shell 1 Core, B1 Hall (businessmen)",
            "Shell 1 Core, B1 Hall (middle-aged women)",
            "Shell 1 Core, B1 Hall (Jennifers)"*/
        };
        public List<string> AreaModifications { get; } = new List<string>()
        {
            "Boss Practice Mode",
            "Dog Tags always available",
            "[1st Time] Engine Room",
            "[1st Time] Shell 1 Core, B1",
            "[1st Time] Arsenal Gear - Jejunum",
            "[Hostages] Normal variant",
            "[Hostages] Beasts variant",
            "[Hostages] Beauties variant",
            "[Hostages] Old Beauties variant"
        };
        public ObservableCollection<string> SelectedAreaMods { get; set; } = new ObservableCollection<string>();
        public void WatchInitialStates()
        {
            if (SelectedAreaMods == null)
            {
                return;
            }

            string room = Mem.ReadStringAddress(RoomCodeAddr, null, 4);

            //if (InitialState == 1) // Engine Room
            if (SelectedAreaMods.Contains("[1st Time] Engine Room"))
            {
                if (room == "w02a")
                {
                    if ((ContinueBytes == null) || (ContinueBytes.Length == 0)) {
                        ContinueBytes = Mem.ReadBytes(ContinueBytesAddr, null, 0x100);
                    }
                    else
                    {
                        Mem.WriteBytes(ContinueBytesAddr, null, ContinueBytes);
                    }
                }
                byte cutscene = Mem.ReadAddress<byte>(EngineRoomRavenAddr, null);
                if ((cutscene & 0x10) == 0x10)
                {
                    cutscene &= (0xFF - 0x10);
                    Mem.WriteAddress<byte>(EngineRoomRavenAddr, null, cutscene);
                }

                byte repair = Mem.ReadAddress<byte>(EngineRoomRepairAddr, null);
                if ((repair & 4) == 4)
                {
                    repair &= (0xFF - 4);
                    Mem.WriteAddress<byte>(EngineRoomRepairAddr, null, repair);
                }

                byte guards = Mem.ReadAddress<byte>(EngineRoomPatternAddr, null);
                if ((guards & 2) == 2)
                {
                    guards &= (0xFF - 2);
                    Mem.WriteAddress<byte>(EngineRoomPatternAddr, null, guards);
                }
            }

            //else if (InitialState == 2) // Shell 1 Core B1 (before Ames)
            if (SelectedAreaMods.Contains("[1st Time] Shell 1 Core, B1"))
            {
                if (ProgressCurrent == 152)
                {
                    Mem.WriteAddress<short>(ProgressPlantAddr, null, 150);
                    Mem.WriteAddress<short>(ProgressPlantContinueAddr, null, 150);
                }
            }

            //else if (InitialState == 3) // Jejunum
            if (SelectedAreaMods.Contains("[1st Time] Arsenal Gear - Jejunum"))
            {
                if ((ProgressCurrent > 374) && (Mem.ReadAddress<int>(RoomTimeAddr, null) < 180))
                {
                    SetProgress(374);
                }
            }

            byte hour = 255;
            if (SelectedAreaMods.Contains("[Hostages] Normal variant"))
            {
                hour = 1;
            }
            else if (SelectedAreaMods.Contains("[Hostages] Beasts variant"))
            {
                hour = 13;
            }
            else if (SelectedAreaMods.Contains("[Hostages] Beauties variant"))
            {
                hour = 0;
            }
            else if (SelectedAreaMods.Contains("[Hostages] Old Beauties variant"))
            {
                hour = 22;
            }
            if (hour != 255)
            {
                SetHostageHour(hour);
            }
        }

        public Dictionary<string, UInt32[]> BossHealthAddrs = new Dictionary<string, UInt32[]>
        {
            { "w00b_25", new UInt32[] { 0xAD4F6C, 0x0, 0x1E0, 0x44, 0x1F8, 0x13C } }, // Olga
            { "w25a_189", new UInt32[] { 0x619BB0, 0x5C } }, // Harrier
            { "w31c_253", new UInt32[] { 0x618988, 0xE9A } }, // Vamp 1 (Stamina)
            { "w31c_253-2", new UInt32[] { 0x618988, 0xE98 } }, // Vamp 1 (Health)
            { "w61a_469", new UInt32[] { 0x664E7C, 0xB8 } }, // Solidus (Health)
            { "w61a_469-2", new UInt32[] { 0x664E78, 0xC8 } }, // Solidus (Stamina)
            { "w32b_317", new UInt32[] { 0x61FBB8, 0x2AE } }, // Vamp 2 (Health)
            { "w32b_317-2", new UInt32[] { 0x664E7C, 0x48 } }, // Vamp 2 (Stamina)
            { "w46a_411", new UInt32[] { 0xAD4EA4, 0x54, 0x10, 0x10, 0x170, 0x7E0 } }, // Rays
        };
        public Dictionary<int, byte> TenguCounts = new Dictionary<int, byte>
        {
            { 1, 48 },
            { 2, 48 },
            { 3, 64 },
            { 4, 96 },
            { 5, 128 },
            { 6, 128 }
        };
        public bool BossActive { get; set; } = false;
        public int LastRoomTime { get; set; }
        public string[] PracticeFadeRooms = new string[] { /* Guard Rush */ "w03b_33", /* Tengus 1 */ "w44a_397", /* Tengus 2 */ "w45a_403" };

        public void WatchBossPractice()
        {
            if (SelectedAreaMods == null)
            {
                return;
            }

            //if (BossPractice)
            if (SelectedAreaMods.Contains("Boss Practice Mode"))
            {
                string[] suffixes = new string[] { "", "-2" };
                string room = Mem.ReadStringAddress(RoomCodeAddr, null, 4) + "_" + ProgressCurrent;

                // fatman
                if (room == "w20c_118")
                {
                    if (
                        (Mem.ReadAddress<short>(0xB6DEC4, new UInt32[] { 0x24E }) == 0) ||
                        (Mem.ReadAddress<short>(0x664E78, new UInt32[] { 0x88 }) == 0)
                    )
                    {
                        if (Mem.ReadAddress<byte>(0x664E7C, new UInt32[] { 0x280 }) == 0)
                        {
                            DoRestartRoom(false, BossPracticeDelay);
                        }
                    }
                }

                else if (room == "w24c_154") // Ames
                {
                    UInt32 AmesContinueLocAddr = 0xD8FB9F;
                    byte newAmes = (byte)((Mem.ReadAddress<byte>(AmesContinueLocAddr, null) + 1) % 20);
                    Mem.WriteAddress<byte>(AmesContinueLocAddr, null, newAmes);

                    if (Mem.ReadAddress<byte>(0x653988, null) == 15)
                    {
                        DoRestartRoom();
                    }
                }

                else if (BossHealthAddrs.ContainsKey(room))
                {
                    foreach (var suff in suffixes)
                    {
                        UInt32[] addrs = BossHealthAddrs[room + suff];
                        UInt32 addr = addrs[0];
                        var offsets = (addrs.Length > 1) ? addrs.Skip(1).ToArray() : null;

                        if (Mem.ReadAddress<short>(addr, offsets) <= 0)
                        {
                            DoRestartRoom(false, BossPracticeDelay);
                        }
                    }
                }

                else if (Mem.ReadAddress<byte>(0x653988, null) == 47) // fades
                {
                    if (PracticeFadeRooms.Contains(room))
                    {
                        DoRestartRoom();
                    }

                }

                /*
                else
                {
                    if (Mem.ReadAddress<byte>(0x7B6F4B, null) == 63)
                    {
                        DoRestartRoom();
                    }
                }
                */
            }
        }

        public void RelockDogTags()
        {
            //if (LockDogTags)
            if (SelectedAreaMods.Contains("Dog Tags always available"))
            {
                Mem.WriteBytes(DogTagsArrayAddr, null, DogTagsArrayClear);
            }
        }


        public static UInt32 VrMissionsAddr = 0xAD5268;
        public void UnlockAllVrMissions()
        {
            UInt32[] offset = new UInt32[] { 0x1A04008 };
            byte state = Mem.ReadAddress<byte>(VrMissionsAddr, offset);

            state |= 4;
            Mem.WriteAddress<byte>(VrMissionsAddr, offset, state);
        }

        public void SetAllVrScores(uint score, bool cleared = false)
        {
            UnlockAllVrMissions();

            if (score > 999999)
            {
                score = 999999;
            }

            if (cleared)
            {
                score += 800000;
            }

            for (int i = 0; i <= 531; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int offset = ((0x1a0401 + i) * 0x10) + (j * 4);
                    Mem.WriteAddress<uint>(VrMissionsAddr, new UInt32[] { (UInt32)offset }, score);
                }
            }

        }

        public void SetVrName(string name)
        {
            Mem.WriteStringAddress(0xD8C314, null, name, 16);
        }


        public void UnlockCastingTheaterCharacters()
        {
            int count = 32 * 4;
            byte[] bytes = new byte[count];
            for (int i = 0; i < count; i++)
            {
                bytes[i] = 255;
            }
            Mem.WriteBytes(0xAD4FD0, new UInt32[] { 0xA4 }, bytes);
        }

        public static UInt32[] NullOffset = new UInt32[] { 0 };
        public static UInt32[] ContinueDataAddr = new UInt32[] { 0x601F38, 0x601F40 };
        public ContinueState ContinueData
        {
            get
            {
                /*
                return new ContinueState(
                    Mem.ReadBytes(ContinueDataAddr[0], NullOffset, 0x566 * 4),
                    Mem.ReadBytes(ContinueDataAddr[1], NullOffset, 0x700 * 4)
                );
                */
        
                
                return new ContinueState(
                    Mem.ReadBytes(ContinueDataAddr[0], new UInt32[] { 0x1C }, 0x20),
                    Mem.ReadBytes(ContinueDataAddr[1], new UInt32[] { 0x930 }, 0x90)
                );
            }
            set
            {
                var keep = new int[] { 0x32, 0x33, 0x4c, 0x4d, 0x231, 0x3A0 };

                var data2 = new byte[0x700*4];
                foreach (var a in keep)
                {
                    data2[a] = value.Data2[a];
                }

                Array.Copy(value.Data2, 0x930, data2, 0x930, 0x90);


                //data2 = value.Data2;


                Mem.WriteBytes(ContinueDataAddr[0], NullOffset, value.Data1);
                Mem.WriteBytes(ContinueDataAddr[1], NullOffset, data2);
            }
        }

        public void TriggerOnPropertyChanged(string name)
        {
            OnPropertyChanged(name);
        }

        public void SetSelectedWarpGroup(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = $"MGS2Trainer.Resources.WarpProfiles.{name}.json";
            string json = "";
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                json = reader.ReadToEnd();
            }
            WarpData = new WarpConfig(json);
            SelectedWarpGroup = WarpData.WarpGroupsList.FirstOrDefault();
            OnPropertyChanged("WarpData");
            OnPropertyChanged("SelectedWarpGroup");
        }

        public void UpdateWarpEntries()
        {
            //WarpEntries = SelectedWarpGroup.WarpsList;
            //OnPropertyChanged("WarpEntries");
            if (SelectedWarpGroup == null)
            {
                SelectedWarpGroup = WarpData.WarpGroupsList.First();
            }
            SelectedWarp = SelectedWarpGroup.WarpsList.First();
            OnPropertyChanged("SelectedWarp");
            OnPropertyChanged("SelectedWarpGroup");
        }

        public void ApplySelectedWarp()
        {
            SelectedWarp.Apply(WarpData, this);
            DoRestartRoom();
        }

        private Dictionary<string, IPSPatch> IPSPatches = new Dictionary<string, IPSPatch>();
        public void ApplyIPSPatch(string name)
        {
            if (IPSPatches.ContainsKey(name))
            {
                IPSPatches[name].Apply();
            }
            else
            {
                var patch = new IPSPatch(name, Mem);
                if (patch.Valid)
                {
                    patch.Apply();
                    IPSPatches.Add(name, patch);
                }
            }  
        }

        public void RevertIPSPatch(string name)
        {
            if (IPSPatches.ContainsKey(name))
            {
                IPSPatches[name].Revert();
            }
            else
            {
                throw new Exception($"The patch {name} hasn't been applied yet, can't revert it.");
            }
        }


        public void Test()
        {
            Mem.GameProcess[(IntPtr)0x889440, true].Execute(0, 100, 100, 0);
            //Mem.GameProcess[(IntPtr)0x889b40, true].Execute("TESTING");

        }


        public void SetHostageHour(int hour)
        {
            byte hr = (byte)(hour % 24);
            UInt32[] offset = new UInt32[] { 0x6F9 };
            Mem.WriteAddress<byte>(0x601F3C, offset, hr);
            Mem.WriteAddress<byte>(0x601F40, offset, hr);
            //Mem.WriteAddress<byte>(0x601F34, new UInt32[] { 0xBC }, 0);
        }

        public List<string> WarpProfiles { get; } = new List<string>()
        {
            "Tanker",
            "Plant (early Shell 1)",
            "Plant (bomb disposal, clockwise)",
            "Plant (bomb disposal, anticlockwise)",
            "Plant (bomb disposal, AC conveyor)",
            "Plant (bomb disposal, FB conveyor)",
            "Plant (late Shell 1)",
            "Plant (Shell 2)",
            "Plant (Arsenal Gear)"
        };

    }

    
    public class ContinueState
    {
        public byte[] Data1;
        public byte[] Data2;
        
        public ContinueState(byte[] d1, byte[] d2)
        {
            Data1 = d1; // mgs2_sse.exe+601F34
            Data2 = d2; // mgs2_sse.exe+601F40
        }

        public ContinueState(string d1, string d2)
        {
            Data1 = Convert.FromBase64String(d1);
            Data2 = Convert.FromBase64String(d2);
        }

        public byte HostageHour
        {
            get { return Data2[0x6F9]; }
            set { Data2[0x6F9] = value; }
        }
    }


}


