using LiveSplit.Model;
using LiveSplit.UI.Components;
using LiveSplit.UI;
using System;
using System.Xml;
using System.Windows.Forms;

namespace LiveSplit.MirrorsEdge
{
    class MirrorsEdgeComponent : LogicComponent
    {
        public override string ComponentName => "Mirror's Edge";

        public MirrorsEdgeSettings Settings { get; set; }

        private TimerModel _timer;
        private GameProcess _gameProcess;

        public MirrorsEdgeComponent(LiveSplitState state)
        {
            _timer = new TimerModel() { CurrentState = state };
            this.Settings = new MirrorsEdgeSettings(_timer);

            //this.ExtractGameHooksDLL();

            _gameProcess = new GameProcess();
            _gameProcess.OnPause += gameProcess_OnPause;
            _gameProcess.OnUnpause += gameProcess_OnUnpause;
            _gameProcess.OnSplit += gameProcess_OnSplit;
            _gameProcess.OnResetAndStart += gameProcess_OnResetAndStart;
            _gameProcess.Run();
        }

        public override void Dispose()
        {
            _gameProcess?.Stop();
        }

        /*public void ExtractGameHooksDLL()
        {
            // if an IO exception is thrown anywhere in here, the component will fail to load. this is intended.

            string path = Path.Combine("Components", GameProcess.GAMEDLL);
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, Properties.Resources.menl_hooks);
                return;
            }

            byte[] installedVersion = File.ReadAllBytes(path);
            using (var md5 = new MD5CryptoServiceProvider())
            {
                string installedVersionHash = Convert.ToBase64String(md5.ComputeHash(installedVersion));
                string currentVersionHash = Convert.ToBase64String(md5.ComputeHash(Properties.Resources.menl_hooks));

                if (installedVersionHash != currentVersionHash)
                {
                retry: // ?v=fiVr34QCF_c
                    try
                    {
                        File.WriteAllBytes(path, Properties.Resources.menl_hooks);
                    }
                    catch (IOException)
                    {
                        if (DialogResult.Retry == 
                            MessageBox.Show("Couldn't update " + GameProcess.GAMEDLL + "! Close the game and click Retry.", "Error",
                            MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning))
                            goto retry;
                    }
                }
            }
        }*/

        void gameProcess_OnPause(object sender, EventArgs e)
        {
            _timer.CurrentState.IsGameTimePaused = true;
        }

        void gameProcess_OnUnpause(object sender, EventArgs e)
        {
            _timer.CurrentState.IsGameTimePaused = false;
        }

        void gameProcess_OnSplit(object sender, SplitType type)
        {
            if ((type == SplitType.Chapter && this.Settings.AutoChapterSplit) ||
                 (type == SplitType.End && this.Settings.AutoEndingSplit) ||
                 (type == SplitType.Stormdrain && this.Settings.AutoStormdrainSplit))
                _timer.Split();
        }

        void gameProcess_OnResetAndStart(object sender, EventArgs e)
        {
            if (this.Settings.AutoResetStart)
            {
                _timer.Reset();
                _timer.Start();
            }
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return this.Settings;
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return this.Settings.GetSettings(document);
        }

        public override void SetSettings(XmlNode settings)
        {
            this.Settings.SetSettings(settings);
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode) { }
    }
}
