using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace MGS2Trainer
{
    abstract class Trainer : INotifyPropertyChanged
    {
        public Memory Mem { get; private set; }
        public static UInt32 WeaponAddr { get; }
        public static UInt32 WeaponCurrentAddrOffset { get; }
        public static UInt32 WeaponMaxAddrOffset { get; }
        public uint WeaponSelected { get; set; }
        public short WeaponAmmoCurrent { get; set; }
        public short WeaponAmmoMax { get; set; }
        public static UInt32 ItemAddr { get; }
        public static UInt32 ItemCurrentAddrOffset { get; }
        public static UInt32 ItemMaxAddrOffset { get; }
        public uint ItemSelected { get; set; }
        public short ItemAmmoCurrent { get; set; }
        public short ItemAmmoMax { get; set; }
        public short ProgressCurrent { get; set; }
        public short ProgressCurrentInList { get; set; }
        public static UInt32 ProgressAddr { get; };
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int PositionXDelta { get; set; }
        public int PositionYDelta { get; set; }
        public int PositionZDelta { get; set; }
        public static UInt32 PositionAddr { get; }
        public string Name { get; set; }
        public static UInt32 NameAddr { get; }


        public Trainer()
        {
            AttachToGame();
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

        public void SetCaution(ushort time)
        {
            Mem.WriteAddress<ushort>(0x6160C8, null, time);
        }

        public void SetCautionMax(ushort time)
        {
            Mem.WriteAddress<ushort>(0xD8F508, null, time);
        }

        public void SetCautionOn()
        {
            SetCautionMax(3600);
            SetCaution(3600);
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

        public void UnlockAllWeapons()
        {
            for (uint i = 0; i < 21; i++)
            {
                short max = ((i >= 11) && (i <= 13)) ? (short)1 : short.MaxValue;
                SetWeaponMax(i, max);
                SetWeaponCurrent(i, max);
            }
            RefreshWeaponValues();
        }

        public void UnlockAllItems()
        {
            for (uint i = 0; i < 40; i++)
            {

                short max = ((i == 0) || (i == 2) || (i == 3) || (i == 4) || (i == 17) || (i == 32)) ? short.MaxValue : (short)1;
                SetItemMax(i, max);
                SetItemCurrent(i, max);
            }
            RefreshItemValues();
        }

        public void UnlockAllEquips()
        {
            UnlockAllWeapons();
            UnlockAllItems();
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





    }
}
