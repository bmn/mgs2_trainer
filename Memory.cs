using Binarysharp.MemoryManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MGS2Trainer
{
    class Memory : INotifyPropertyChanged
    {
        public MemorySharp GameProcess { get; set; }
        public string ProcessName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public Memory(string process)
        {
            ProcessName = process;
        }

        public void RefreshProcess()
        {
            if ((GameProcess == null) || (!GameProcess.IsRunning))
            {
                try
                {
                    Process[] gameProcesses = Process.GetProcessesByName(ProcessName);
                    if (gameProcesses.Length == 0)
                    {
                        throw new NullReferenceException();
                    }
                    else
                    {
                        GameProcess = new MemorySharp(gameProcesses.First());
                    }
                }
                catch (NullReferenceException e)
                {
                    throw new NullReferenceException("Couldn't connect to the game process. Is the game open?", e);
                }
            }
        }
      
        public T ReadAddress<T>(uint addr, UInt32[] offsets)
        {
            IntPtr address = FindAddress(addr, offsets);
            return GameProcess.Read<T>(address, false);
        }

        public string ReadStringAddress(uint addr, UInt32[] offsets, int length = 1)
        {
            IntPtr address = FindAddress(addr, offsets);
            return GameProcess.ReadString(address, false, length);
        }

        public bool WriteAddress<T>(UInt32 addr, UInt32[] offsets, T value)
        {
            IntPtr address = FindAddress(addr, offsets);
            if (address.ToInt32() == 0)
            {
                return false;
            }
            GameProcess.Write(address, value, false);
            return true;
        }
        public bool WriteBytes(UInt32 addr, UInt32[] offsets, byte[] value)
        {
            IntPtr address = FindAddress(addr, offsets);
            if (address.ToInt32() == 0)
            {
                return false;
            }
            int i = 0;
            foreach (byte b in value) {
                GameProcess.Write((address + i++), b, false);
            }
            return true;
        }
        public byte[] ReadBytes(UInt32 addr, UInt32[] offsets, int length)
        {
            IntPtr address = FindAddress(addr, offsets);
            if (address.ToInt32() == 0)
            {
                return new byte[0];
            }
            var result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = GameProcess.Read<byte>(address + i, false);
            }
            return result;
        }

        public bool WriteStringAddress(UInt32 addr, UInt32[] offsets, string value, int length = -1)
        {
            IntPtr address = FindAddress(addr, offsets);
            if (address.ToInt32() == 0)
            {
                return false;
            }
            if ( (length > 0) && (length < value.Length) )
            {
                value = value.Substring(0, length);
            }
            else if (length > value.Length)
            {
                value = value.PadRight(length);
            }
            GameProcess.WriteString(address, value, false);
            return true;
        }

        public IntPtr FindAddress(UInt32 addr, UInt32[] offsets)
        {
            RefreshProcess();
            UInt32 current = addr + 0x400000;
            if (offsets != null)
            {
                foreach (UInt32 off in offsets)
                {
                    IntPtr address = new IntPtr(current);
                    UInt32 pointer = GameProcess.Read<UInt32>(address, false);
                    if (pointer == 0)
                    {
                        return new IntPtr(0);
                    }
                    current = pointer + off;
                }
            }
            return new IntPtr(current);
        }

        public void SuspendProcess()
        {
            GameProcess.Threads.SuspendAll();
        }
        public void ResumeProcess()
        {
            GameProcess.Threads.ResumeAll();
        }


    }
}
