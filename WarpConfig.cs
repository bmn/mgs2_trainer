using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MGS2Trainer
{
    public class WarpConfig
    {
        public dynamic ParsedData;
        public ObservableCollection<Warp> WarpGroupsList { get; set; } = new ObservableCollection<Warp>();
        public Dictionary<string, Warp> WarpGroupsDict { get; set; } = new Dictionary<string, Warp>();


        public WarpConfig(string json)
        {
            ParsedData = JArray.Parse(json);

            foreach (var e in ParsedData)
            {
                Warp group = new Warp(e);
                WarpGroupsList.Add(group);
                WarpGroupsDict.Add(group.Name, group);
            }

            SetAllParents();
        }

        public Warp GetParent(JArray parent)
        {
            string left = (string)parent[0];
            if (WarpGroupsDict.ContainsKey(left))
            {
                if (parent.Count > 1)
                {
                    string right = (string)parent[1];
                    if (right != null)
                    {
                        var groupDict = WarpGroupsDict[left].WarpsDict;
                        if (groupDict.ContainsKey(right))
                        {
                            return groupDict[right];
                        }
                        return null;
                    }
                }
                return WarpGroupsDict[left];
            }
            return null;
        }

        private void SetAllParents()
        {
            foreach (var group in WarpGroupsList)
            {
                group.SetParent(this);
            }
        }

        private Dictionary<string, byte[]> _Data1;
        private Dictionary<string, byte[]> _Data2;
        public List<byte[]> DataFor(string id)
        {
            if (_Data1 == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourcePath = $"MGS2Trainer.Resources.Warps.warps.bin";

                try
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                    using (BinaryReader bin = new BinaryReader(stream))
                    {
                        _Data1 = new Dictionary<string, byte[]>();
                        _Data2 = new Dictionary<string, byte[]>();
                        while (stream.Position < stream.Length)
                        {
                            string name = new string(bin.ReadChars(0x10)).Replace("\0", string.Empty);
                            byte[] data1 = bin.ReadBytes(0x20);
                            byte[] data2 = bin.ReadBytes(0x90);
                            _Data1.Add(name, data1);
                            _Data2.Add(name, data2);
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }
            if (_Data1.ContainsKey(id))
            {
                return new List<byte[]>() { _Data1[id], _Data2[id] };
            }
            return null;
        }
    }

    public class Warp
    {
        public Warp Parent;
        public JArray ParentString;
        public string Name;

        public string Location;
        public ushort? Progress;
        public JArray Weapons;
        public JArray Items;
        public JArray Equip;

        public string ResolvedLocation;
        public ushort? ResolvedProgress;
        public JArray ResolvedWeapons;
        public JArray ResolvedItems;
        public JArray ResolvedEquip;

        private bool Resolved;

        public Dictionary<string, Warp> Difficulties { get; set; } = new Dictionary<string, Warp>();
        public ObservableCollection<Warp> WarpsList { get; set; } = new ObservableCollection<Warp>();
        public Dictionary<string, Warp> WarpsDict { get; set; } = new Dictionary<string, Warp>();

        public override string ToString() => Name;

        public Warp(JToken entry)
        {
            Name = (string)entry["name"];
            ParentString = (JArray)entry["parent"];
            Location = (string)entry["location"];
            Progress = (ushort?)entry["progress"];
            Weapons = (JArray)entry["weapons"];
            Items = (JArray)entry["items"];
            Equip = (JArray)entry["equip"];

            var diffs = (JObject)entry["difficulty"];
            if (diffs != null)
            {
                foreach (var diff in diffs)
                {
                    if (diff.Value != null)
                    {
                        Warp warp = new Warp(diff.Value);
                        if (warp.ParentString == null)
                        {
                            warp.Parent = this;
                        }
                        Difficulties.Add(diff.Key, warp);
                    }
                }
            }

            var warps = (JArray)entry["warps"];
            if (warps != null)
            {
                foreach (var w in warps)
                {
                    if (w != null)
                    {
                        Warp warp = new Warp(w);

                        if (warp.ParentString == null)
                        {
                            warp.Parent = this;
                        }

                        WarpsList.Add(warp);
                        WarpsDict.Add(warp.Name, warp);
                    }
                }
            }
           
        }

        public void Apply(WarpConfig conf, Trainer train)
        {
            string diff = (train.Mem.ReadAddress<byte>(0xD8ADD0, null) / 10).ToString();
            if (Difficulties.ContainsKey(diff))
            {
                Difficulties[diff].Apply(conf, train);
                return;
            }

            if (!Resolved)
            {
                ResolveProperties();
            }

            var data = conf.DataFor(ResolvedLocation);
            // General Data 1
            train.Mem.WriteBytes(0x601F38, new UInt32[] { 0x1C }, data[0]);
            // General Data 2
            train.Mem.WriteBytes(0x601F40, new UInt32[] { 0 }, new byte[0x700 * 4]);
            train.Mem.WriteBytes(0x601F40, new UInt32[] { 0x930 }, data[1]);
            // Room Signature (changing this triggers full area reload)
            train.Mem.WriteAddress<byte>(0x601F38, new UInt32[] { 0xBC }, (byte)(train.Mem.ReadAddress<byte>(0x601F38, new UInt32[] { 0xBC }) + 1));
            // Holds timer to 10 mins
            train.Mem.WriteAddress<int>(0x601F40, new UInt32[] { 0x3A0 }, 36000);
            // Progress
            if (ResolvedProgress != null)
            {
                train.Mem.WriteAddress<short>(0x601F40, new UInt32[] { 0x32 }, (short)ResolvedProgress); // Plant
                train.Mem.WriteAddress<short>(0x601F40, new UInt32[] { 0x4C }, (short)ResolvedProgress); // Tanker
            }
            // TODO weapons
            // TODO items

            // Equips
            UInt32[] offsets = new UInt32[] { 0x104, 0x106, 0x116, 0x118 };
            if (ResolvedEquip != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (ResolvedEquip[i] != null)
                    {
                        train.Mem.WriteAddress<short>(0x601F38, new UInt32[] { offsets[i] }, (short)ResolvedEquip[i]);
                    }
                }
            }
        }

        protected void ResolveProperties()
        {
            ResolvedLocation = Location;
            ResolvedProgress = Progress;
            ResolvedWeapons = Weapons;
            ResolvedItems = Items;
            ResolvedEquip = Equip;

            if (Parent == null)
            {
                return;
            }

            Parent.ResolveProperties();

            if (Location == null)
            {
                ResolvedLocation = Parent.ResolvedLocation;
            }

            if (Progress == null)
            {
                ResolvedProgress = Parent.ResolvedProgress;
            }

            if (Weapons == null)
            {
                ResolvedWeapons = Parent.ResolvedWeapons;
            }
            else
            {
                ResolvedWeapons = Weapons;
                for (int i = 0; i < 22; i++)
                {
                    if ((short?)Weapons[i] == null)
                    {
                        ResolvedWeapons[i] = Parent.ResolvedWeapons[i];
                    }
                }
            }

            if (Items == null)
            {
                ResolvedItems = Parent.ResolvedItems;
            }
            else
            {
                ResolvedItems = Items;
                for (int i = 0; i < 41; i++)
                {
                    if ((short?)Items[i] == null)
                    {
                        ResolvedItems[i] = Parent.ResolvedItems[i];
                    }
                }
            }

            if (Equip == null)
            {
                ResolvedEquip = Parent.Equip;
            }
            else
            {
                ResolvedEquip = Parent.ResolvedEquip;
                for (int i = 0; i < 4; i++)
                {
                    if ((short?)Equip[i] == null)
                    {
                        ResolvedEquip[i] = Parent.ResolvedEquip[i];
                    }
                }
            }

            Resolved = true;
        }

        public void SetParent(WarpConfig conf)
        {
            Parent = (ParentString == null) ? null : conf.GetParent(ParentString);
            foreach (var warp in WarpsList)
            {
                warp.SetParent(conf);
            }
            foreach (var diff in Difficulties)
            {
                diff.Value.SetParent(conf);
            }
        }

    }

}
