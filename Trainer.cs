using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace MGS2Trainer
{
    class Trainer : INotifyPropertyChanged
    {
        public Memory Mem { get; private set; }
        public static UInt32 WeaponAddr { get; } = 0x653E08;
        public static UInt32 WeaponCurrentAddrOffset { get; } = 0x2;
        public static UInt32 WeaponMaxAddrOffset { get; } = 0x4A;
        public uint WeaponSelected { get; set; }
        public short WeaponAmmoCurrent { get; set; }
        public short WeaponAmmoMax { get; set; }
        public static UInt32 ItemAddr { get; } = 0x653E10;// = 0xD8AFAE;
        public static UInt32 ItemCurrentAddrOffset { get; } = 0x2;
        public static UInt32 ItemMaxAddrOffset { get; } = 0x62;
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
        private byte[] ContinueBytes { get; set; }
        public static UInt32 ContinueBytesAddr { get; } = 0xD8FE00;
        public static UInt32 RoomCodeAddr { get; } = 0xD8ADEC;
        public static UInt32 WeaponSelectedAddr { get; } = 0xD8AEC4;

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

        public static byte[] ValidWeaponsTanker = new byte[] { 0, 1, 9, 10, 15, 16 };
        public static byte[] ValidWeaponsPlant = new byte[] { 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20 };
        // +1 for the equipped weapon id
        // (0)M9, (1)USP, (2)SOCOM, (3)PSG-1, (4)RGB6, (5)Nikita, (6)Stinger, (7)Claymore, (8)C4, (9)Chaff
        // (10)Stun, (11)D.Mic, (12)HF Blade, (13)Coolant, (14)AK-74u, (15)Magazine, (16)Grenade, (17)M4, (18)PSG-1T, (19)D.Mic(CS)
        // (20)Book

        public static byte[] ValidItemsTanker = new byte[] { 0, 2, 3, 6, 7, 12, 14, 15, 16, 20, 23, 31, 34 };
        public static byte[] ValidItemsPlant = new byte[] { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 24, 25, 26, 28, 29, 33, 35, 36, 37 };
        // (0)Ration, (1)Scope(CS), (2)Medicine, (3)Bandage, (4)Pentazemin, (5)BDU, (6)B.Armor, (7)Stealth, (8)Mine.D, (9)Sensor A
        // (10)Sensor B, (11)NVG, (12)Therm.G, (13)Scope, (14)D.Camera, (15)Box 1, (16)Cigs, (17)Card, (18)Shaver, (19)Phone
        // (20)Camera, (21)Box 2, (22)Box 3, (23)Wet Box, (24)AP Sensor, (25)Box 4, (26)Box 5, (27)?, (28)SOCOM Supp, (29)AK Supp
        // (30)Camera(CS), (31)Bandana, (32)Dog Tags, (33)MO Disc, (34)USP Supp, (35)Inf.Wig, (36)Blue Wig, (37)Orange Wig, (38)Wig C, (39)Wig D




        public Timer _timer { get; set; }


        public Trainer()
        {
            AttachToGame();

            //RefreshWeaponValues();
            //RefreshItemValues();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AttachToGame()
        {
            Mem = new Memory("mgs2_sse");
        }

        public void SetAlert(bool on)
        {
            byte val = (byte)(on ? 1 : 0);
            Mem.WriteAddress<byte>(0xD8AEDA, null, val);
        }

        public void SetAlertOn()
        {
            SetAlert(true);
        }

        public void SetAlertOff()
        {
            SetAlert(false);
        }

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

        public void SetCautionOff()
        {
            SetCaution(0);
        }

        public void SetHealth(byte hp)
        {
            // Attempt to write in-area address
            if (!Mem.WriteAddress<byte>(0xB60918, new UInt32[] { 0xD2 }, hp))
            {
                // Write out-of-area address
                Mem.WriteAddress<byte>(0x3E315E, new UInt32[] { 0x2D }, hp);
            }
        }

        public void SetHealthFull()
        {
            SetHealth(200);
        }

        public void DoSuicide()
        {
            SetHealth(0);
        }

        public void DoRestartRoom()
        {
            Mem.GameProcess[(IntPtr)0x477de0, true].Execute();
        }

        public void RefreshName()
        {
            Name = Mem.ReadStringAddress(NameAddr, null, 20);
            OnPropertyChanged("Name");
        }

        public void SetName()
        {
            Mem.WriteStringAddress(NameAddr, null, Name);
        }



        public void RefreshWeaponValues()
        {
            WeaponAmmoCurrent = Mem.ReadAddress<short>(WeaponAddr, new UInt32[] { WeaponCurrentAddrOffset + (WeaponSelected * 2) });
            WeaponAmmoMax = Mem.ReadAddress<short>(WeaponAddr, new UInt32[] { WeaponMaxAddrOffset + (WeaponSelected * 2) });
            OnPropertyChanged("WeaponAmmoCurrent");
            OnPropertyChanged("WeaponAmmoMax");
        }

        public void SetWeaponCurrent(uint? item = null, short? ammo = null)
        {
            if (item == null) item = WeaponSelected;
            if (ammo == null) ammo = WeaponAmmoCurrent;
            Mem.WriteAddress<short>(WeaponAddr, new UInt32[] { WeaponCurrentAddrOffset + ((uint)item * 2) }, (short)ammo);
        }

        public void SetWeaponMax(uint? item = null, short? ammo = null)
        {
            if (item == null) item = WeaponSelected;
            if (ammo == null) ammo = WeaponAmmoCurrent;
            Mem.WriteAddress<short>(WeaponAddr, new UInt32[] { WeaponMaxAddrOffset + ((uint)item * 2) }, (short)ammo);
        }

        public void RefreshItemValues()
        {
            ItemAmmoCurrent = Mem.ReadAddress<short>(ItemAddr, new UInt32[] { ItemCurrentAddrOffset + (ItemSelected * 2) });
            ItemAmmoMax = Mem.ReadAddress<short>(ItemAddr, new UInt32[] { ItemMaxAddrOffset + (ItemSelected * 2) });
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
        public void SetPositionX(float v)
        {
            SetPosition(v, 0);
        }
        public void SetPositionZ(float v)
        {
            //SetPosition(v, 0x5984);
            SetPosition(v, 4);
        }
        public void SetPositionY(float v)
        {
            SetPosition(v, 8);
        }

        public void SetItemCurrent(uint? item = null, short? ammo = null)
        {
            if (item == null) item = ItemSelected;
            if (ammo == null) ammo = ItemAmmoCurrent;
            Mem.WriteAddress<short>(ItemAddr, new UInt32[] { ItemCurrentAddrOffset + ((uint)item * 2) }, (short)ammo);
        }

        public void SetItemMax(uint? item = null, short? ammo = null)
        {
            if (item == null) item = ItemSelected;
            if (ammo == null) ammo = ItemAmmoMax;
            Mem.WriteAddress<short>(ItemAddr, new UInt32[] { ItemMaxAddrOffset + ((uint)item * 2) }, (short)ammo);
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
                    short max = full.Contains(i) ? short.MaxValue : ( boxes.Contains(i) ? (short)25 : (short)1 );
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
            if ( (index != -1) && ((index + 2) > g.Count) )
            {
                Mem.WriteAddress<byte>(WeaponSelectedAddr, null, 0);
                return;
            }

            while (++index < g.Count)
            {
                byte next = g[index];
                if (Mem.ReadAddress<short>(WeaponAddr, new UInt32[] { WeaponCurrentAddrOffset + (((uint)next - 1) * 2) }) >= 0)
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
            if (Mem.ReadAddress<short>(WeaponAddr, new UInt32[] { WeaponCurrentAddrOffset + (((uint)weapon - 1) * 2) }) >= 0)
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
            UInt32 addr = (CurrentProgressArea == Area_Plant) ? ProgressPlantAddr : ProgressTankerAddr;
            Mem.WriteAddress<short>(addr, null, val);
            RefreshProgressValue();
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

        public List<string> InitialStateAreas { get; } = new List<string>()
        {
            "No area",
            "Engine Room",
            "Shell 1 Core, B1"
        };
        public void WatchInitialStates()
        {
            if (InitialState == 1) // Engine Room
            {
                if (Mem.ReadStringAddress(RoomCodeAddr, null, 4) == "w02a")
                {
                    if ( (ContinueBytes == null) || (ContinueBytes.Length == 0) ) {
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
            else if (InitialState == 2) // Shell 1 Core B1 (before Ames)
            {
                if (ProgressCurrent == 152)
                {
                    Mem.WriteAddress<short>(ProgressPlantAddr, null, 150);
                    Mem.WriteAddress<short>(ProgressPlantContinueAddr, null, 150);
                }
            }
        }

        public void RelockDogTags()
        {
            if (LockDogTags)
            {
                Mem.WriteBytes(DogTagsArrayAddr, null, DogTagsArrayClear);
            }
        }





    }
}
