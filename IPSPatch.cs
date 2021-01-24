using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MGS2Trainer
{
    class IPSPatch
    {
        public List<Patch> Patches = new List<Patch>();
        public bool Valid;

        private Memory Mem;

        public IPSPatch(string name, Memory mem)
        {
            Mem = mem;

            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = $"MGS2Trainer.Resources.IPS.{name}.ips";

            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                using (BinaryReaderBE ips = new BinaryReaderBE(stream))
                {
                    string header = new string(ips.ReadChars(5));
                    if (!header.Equals("PATCH", StringComparison.InvariantCulture))
                    {
                        return;
                    }
                    while (stream.Position < (stream.Length - 3))
                    {
                        UInt32 address = (UInt32)ips.ReadInt24();
                        int length = ips.ReadInt16();
                        byte[] data = null;

                        if (length == 0) // RLE time
                        {
                            length = ips.ReadInt16();
                            byte content = ips.ReadByte();
                            data = new byte[length];

                            if (content != 0)
                            {
                                for (int i = 0; i < length; i++)
                                {
                                    data[i] = content;
                                }
                            }
                        }
                        else
                        {
                            data = ips.ReadBytes(length);
                        }

                        Patches.Add(new Patch(address, length, data, mem));
                    }
                }
                Valid = true;
            }
            catch
            {
                throw new FileNotFoundException($"Patch {name} not found or could not be parsed.");
            }
        }

        public void Apply(bool suspend = true)
        {
            if (suspend)
            {
                Mem.SuspendProcess();
            }
            foreach (var patch in Patches)
            {
                patch.Apply(false);
            }
            if (suspend)
            {
                Mem.ResumeProcess();
            }
        }

        public void Save()
        {
            foreach (var patch in Patches)
            {
                patch.Save();
            }
        }

        public void Revert(bool suspend = true)
        {
            if (suspend)
            {
                Mem.SuspendProcess();
            }
            foreach (var patch in Patches)
            {
                patch.Revert(false);
            }
            if (suspend)
            {
                Mem.ResumeProcess();
            }
        }

    }

    class Patch
    {
        public UInt32 Address;
        public int Length;
        public byte[] PatchData;
        public byte[] OriginalData;

        private Memory Mem;

        public Patch(UInt32 address, int length, byte[] patchData, Memory mem)
        {
            Address = address;
            Length = length;
            PatchData = patchData;
            Mem = mem;
        }

        public void Apply(bool suspend = true)
        {
            if (suspend)
            {
                Mem.SuspendProcess();
            }
            if (OriginalData == null)
            {
                OriginalData = Mem.ReadBytes(Address, null, Length);
            }
            Mem.WriteBytes(Address, null, PatchData);
            if (suspend)
            {
                Mem.ResumeProcess();
            }
        }

        public void Save()
        {

        }

        public void Revert(bool suspend = true)
        {
            if (suspend)
            {
                Mem.SuspendProcess();
            }
            Mem.WriteBytes(Address, null, OriginalData);
            if (suspend)
            {
                Mem.ResumeProcess();
            }
        }
    }

    class BinaryReaderBE : BinaryReader
    {
        public BinaryReaderBE(System.IO.Stream stream) : base(stream) { }

        public override int ReadInt32()
        {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public override Int16 ReadInt16()
        {
            var data = base.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public override Int64 ReadInt64()
        {
            var data = base.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public override UInt32 ReadUInt32()
        {
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public int ReadInt24()
        {
            return (base.ReadByte() << 16) + (base.ReadByte() << 8) + base.ReadByte();
        }

    }
}
