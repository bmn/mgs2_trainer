using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using Binarysharp.MemoryManagement.Native;

namespace MGS2Trainer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class State : Window
    {
        public State()
        {
            InitializeComponent();
        }

            

        private const int PROCESS_WM_READ = 0x0010;
        public static string StateFile { get; set; } = "mgs2_state.bin";
        public static string StateFile2 { get; set; } = "mgs2_state2.bin";

        public static string StateFileFormat { get; } = "mgs2_state_{0}.bin";

        public static string StateFilename(string format, int number)
        {
            return string.Format(format, number);
        }


        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        private const int StartAddr = 0x99C000;
        private const int AddrLength = 0xE65000;

        private const int StartAddr2 = 0x3B05000;
        private const int AddrLength2 = 0x2501000;

        private const int MinKB = 5 * 1024;

        public static void SaveState()
        {

            Process process = Process.GetProcessesByName("mgs2_sse")[0];
            IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);

            Trainer t = new Trainer();
            t.Mem.RefreshProcess();
            t.Mem.SuspendProcess();

            bool debug = true;
            if (debug)
            {
                int i = 0;
                var reg = t.Mem.GameProcess.Memory.Regions.ToList();
                foreach (var r in reg)
                {
                    if (((int)r.BaseAddress == 0x7FFF0000) || (!r.IsValid))
                    {
                        break;
                    }
                    var info = r.Information;
                    //if (((info.Protect & MemoryProtectionFlags.ReadWrite) == MemoryProtectionFlags.ReadWrite) && (info.RegionSize >= MinKB * 1024))
                    if ((int)info.BaseAddress == StartAddr)
                    {
                        int bytesReaded = 0;
                        byte[] bufferer = new byte[AddrLength];
                        ReadProcessMemory((int)processHandle, (int)info.BaseAddress, bufferer, AddrLength, ref bytesReaded);
                        File.WriteAllBytes(StateFilename(StateFileFormat, i++), bufferer);
                    }
                    else if (info.RegionSize == AddrLength2)
                    {
                        int bytesReaded = 0;
                        byte[] bufferer = new byte[AddrLength2];
                        ReadProcessMemory((int)processHandle, (int)info.BaseAddress, bufferer, AddrLength2, ref bytesReaded);
                        File.WriteAllBytes(StateFilename(StateFileFormat, i++), bufferer);
                    }
                }
            }
            else
            {

                int bytesRead = 0;
                byte[] buffer = new byte[AddrLength];
                ReadProcessMemory((int)processHandle, StartAddr, buffer, buffer.Length, ref bytesRead);
                File.WriteAllBytes(StateFile, buffer);

                bytesRead = 0;
                buffer = new byte[AddrLength2];
                ReadProcessMemory((int)processHandle, StartAddr2, buffer, buffer.Length, ref bytesRead);
                File.WriteAllBytes(StateFile2, buffer);

            }
            

            t.Mem.ResumeProcess();

        }

        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        public static void LoadState()
        {
            Process process = Process.GetProcessesByName("mgs2_sse")[0];
            IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

            Trainer t = new Trainer();
            t.Mem.RefreshProcess();
            t.Mem.SuspendProcess();

            //Thread.Sleep(1000);

            bool debug = true;
            if (debug)
            {
                int i = 0;
                var reg = t.Mem.GameProcess.Memory.Regions.ToList();
                foreach (var r in reg)
                {
                    if (((int)r.BaseAddress == 0x7FFF0000) || (!r.IsValid))
                    {
                        break;
                    }
                    var info = r.Information;
                    //if (((int)r.BaseAddress != 0x400000) && (info.Protect == MemoryProtectionFlags.ReadWrite) && (info.RegionSize >= MinKB * 1024))
                    if ((int)info.BaseAddress == StartAddr)
                    {
                        int bytesWrittened = 0;
                        byte[] bufferer = File.ReadAllBytes(StateFilename(StateFileFormat, i++));
                        WriteProcessMemory((int)processHandle, (int)info.BaseAddress, bufferer, AddrLength, ref bytesWrittened);
                    }
                    else if (info.RegionSize == AddrLength2)
                    {
                        int bytesWrittened = 0;
                        byte[] bufferer = File.ReadAllBytes(StateFilename(StateFileFormat, i++));
                        WriteProcessMemory((int)processHandle, (int)info.BaseAddress, bufferer, AddrLength2, ref bytesWrittened);
                    }
                }
            }
            else
            {

                int bytesWritten = 0;
                byte[] buffer = File.ReadAllBytes(StateFile);
                WriteProcessMemory((int)processHandle, StartAddr, buffer, buffer.Length, ref bytesWritten);

                bytesWritten = 0;
                buffer = File.ReadAllBytes(StateFile2);
                WriteProcessMemory((int)processHandle, StartAddr2, buffer, buffer.Length, ref bytesWritten);

            }
            

            //Thread.Sleep(1000);

            t.Mem.ResumeProcess();
        }
        
    }

}
