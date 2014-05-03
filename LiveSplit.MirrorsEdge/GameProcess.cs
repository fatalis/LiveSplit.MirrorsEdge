﻿using System;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LiveSplit.MirrorsEdge
{
    class GameProcess
    {
        public event EventHandler OnPause;
        public event EventHandler OnUnpause;

        private Task _thread;
        private CancellationTokenSource _cancelSource;
        private const string GAMEDLL = "menl_hooks.dll";
        private const string PIPE_NAME = "LiveSplit.MirrorsEdge";

        public void Run()
        {
            if (_thread != null && _thread.Status == TaskStatus.Running)
                throw new InvalidOperationException();

            _cancelSource = new CancellationTokenSource();
            _thread = Task.Factory.StartNew(NamedPipeThread);
        }

        public void Stop()
        {
            if (_cancelSource == null || _thread == null)
                throw new InvalidOperationException();

            if (_thread.Status != TaskStatus.Running)
                return;

            _cancelSource.Cancel();
            _thread.Wait();
        }

        void NamedPipeThread()
        {
            while (!_cancelSource.IsCancellationRequested)
            {
                try
                {
                    Process gameProcess;
                    while ((gameProcess = GetGameProcess()) == null)
                    {
                        Thread.Sleep(250);
                        if (_cancelSource.IsCancellationRequested)
                            return;
                    }

                    Debug.WriteLine("got process");

                    if (!ProcessHasModule(gameProcess, GAMEDLL))
                        InjectDLL(gameProcess, GetGameDLLPath());

                    Debug.WriteLine("dll injected");

                    using (var pipe = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.In))
                    using (var sr = new StreamReader(pipe))
                    {
                        while (!gameProcess.HasExited)
                        {
                            try
                            {
                                pipe.Connect(250);
                                break;
                            }
                            catch (TimeoutException) { }
                            catch (IOException) { }
                        }
                        if (gameProcess.HasExited || !pipe.IsConnected)
                            continue;

                        Debug.WriteLine("pipe connected");
                        
                        string line;
                        // TODO: readline blocks so when cancellation is supported in 1.4, go async
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line == "pause" && this.OnPause != null)
                                this.OnPause(this, EventArgs.Empty);
                            else if (line == "unpause")
                                this.OnUnpause(this, EventArgs.Empty);
                        }

                        Debug.WriteLine("pipe disconnected");
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                    Thread.Sleep(1000);
                }
            }
        }

        static Process GetGameProcess()
        {
            return Process.GetProcesses()
                .FirstOrDefault(p => p.ProcessName.ToLower() == "mirrorsedge" && !p.HasExited);
        }

        static string GetGameDLLPath()
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? String.Empty;
            return Path.Combine(dir, GAMEDLL);
        }

        static bool ProcessHasModule(Process process, string module)
        {
            return process.Modules.Cast<ProcessModule>().Any(m => Path.GetFileName(m.FileName).ToLower() == module);
        }

        static void InjectDLL(Process process, string path)
        {
            IntPtr loadLibraryAddr = SafeNativeMethods.GetProcAddress(SafeNativeMethods.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (loadLibraryAddr == IntPtr.Zero)
                throw new Exception("Couldn't locate LoadLibraryA");

            IntPtr mem = IntPtr.Zero;
            IntPtr hThread = IntPtr.Zero;
            uint len = 0;

            try
            {
                if ((mem = SafeNativeMethods.VirtualAllocEx(process.Handle, IntPtr.Zero, (uint)path.Length,
                    SafeNativeMethods.AllocationType.Commit | SafeNativeMethods.AllocationType.Reserve,
                    SafeNativeMethods.MemoryProtection.ReadWrite)) == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                byte[] bytes = Encoding.ASCII.GetBytes(path + "\0");
                len = (uint)bytes.Length;
                uint written;
                if (!SafeNativeMethods.WriteProcessMemory(process.Handle, mem, bytes, len, out written))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if ((hThread = SafeNativeMethods.CreateRemoteThread(process.Handle, IntPtr.Zero, 0, loadLibraryAddr, mem, 0, IntPtr.Zero))
                    == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                SafeNativeMethods.WaitForSingleObject(hThread, 0xFFFFFFFF); // INFINITE
            }
            finally
            {
                if (mem != IntPtr.Zero && len > 0)
                    SafeNativeMethods.VirtualFreeEx(process.Handle, mem, len, SafeNativeMethods.FreeType.Release);
                if (hThread != IntPtr.Zero)
                    SafeNativeMethods.CloseHandle(hThread);
            }
        }
    }
}