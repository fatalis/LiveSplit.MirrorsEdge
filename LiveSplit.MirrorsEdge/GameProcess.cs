using System;
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
    public enum SplitType
    {
        Chapter,
        End,
        Stormdrain
    }

    class GameProcess
    {
        public event EventHandler OnPause;
        public event EventHandler OnUnpause;
        public event EventHandler<SplitType> OnSplit;
        public event EventHandler OnResetAndStart;

        private Task _thread;
        private CancellationTokenSource _cancelSource;
        public const string GAMEDLL = "menl_hooks.dll";
        private const string PIPE_NAME = "LiveSplit.MirrorsEdge";
        private bool _pipeConnected;

        public void Run()
        {
            if (_thread != null && _thread.Status == TaskStatus.Running)
                throw new InvalidOperationException();

            _cancelSource = new CancellationTokenSource();
            _thread = Task.Factory.StartNew(NamedPipeThread);
        }

        public void Stop()
        {
            if (_cancelSource == null || _thread == null || _thread.Status != TaskStatus.Running)
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

                    using (var pipe = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.In, PipeOptions.Asynchronous))
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

                        _pipeConnected = true;

                        var buf = new byte[2048];
                        pipe.BeginRead(buf, 0, buf.Length, PipeRead, new PipeState { Buffer = buf, Pipe = pipe });

                        while (_pipeConnected)
                        {
                            Thread.Sleep(250);

                            if (_cancelSource.IsCancellationRequested)
                                return;
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

        void PipeRead(IAsyncResult ar)
        {
            var state = (PipeState)ar.AsyncState;

            int read;
            if ((read = state.Pipe.EndRead(ar)) == 0)
            {
                _pipeConnected = false;
                return;
            }

            string message = Encoding.ASCII.GetString(state.Buffer, 0, read);
            if (message == "pause\n" && this.OnPause != null)
                this.OnPause(this, EventArgs.Empty);
            else if (message == "unpause\n" && this.OnUnpause != null)
                this.OnUnpause(this, EventArgs.Empty);
            else if (message == "split\n" && this.OnSplit != null)
                this.OnSplit(this, SplitType.Chapter);
            else if (message == "end\n" && this.OnSplit != null)
                this.OnSplit(this, SplitType.End);
            else if (message == "stormdrain\n" && this.OnSplit != null)
                this.OnSplit(this, SplitType.Stormdrain);
            else if (message == "start\n" && this.OnResetAndStart != null)
                this.OnResetAndStart(this, EventArgs.Empty);

            state.Pipe.BeginRead(state.Buffer, 0, state.Buffer.Length, PipeRead, state);
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

    struct PipeState
    {
        public NamedPipeClientStream Pipe;
        public byte[] Buffer;
    }
}
