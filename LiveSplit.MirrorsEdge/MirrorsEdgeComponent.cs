using System.IO;
using System.Security.Cryptography;
using LiveSplit.Model;
using LiveSplit.TimeFormatters;
using LiveSplit.UI.Components;
using LiveSplit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Windows.Forms;

namespace LiveSplit.MirrorsEdge
{
    class MirrorsEdgeComponent : IComponent
    {
        public string ComponentName
        {
            get { return "Mirror's Edge"; }
        }

        public IDictionary<string, Action> ContextMenuControls { get; protected set; }
        protected InfoTimeComponent InternalComponent { get; set; }

        private LiveSplitState _state;
        private GameProcess _gameProcess;
        private GraphicsCache _cache;

        public MirrorsEdgeComponent(LiveSplitState state)
        {
            this.ExtractGameHooksDLL();

            this.ContextMenuControls = new Dictionary<String, Action>();

            this.InternalComponent = new InfoTimeComponent(null, null, new RegularTimeFormatter(TimeAccuracy.Hundredths));

            _cache = new GraphicsCache();

            _state = state;

            _gameProcess = new GameProcess();
            _gameProcess.OnPause += gameProcess_OnPause;
            _gameProcess.OnUnpause += gameProcess_OnUnpause;
            _gameProcess.Run();
        }

        public void Dispose()
        {
            if (_gameProcess != null)
                _gameProcess.Stop();
        }

        public void ExtractGameHooksDLL()
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
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            this.InternalComponent.TimeValue =
                state.CurrentTime[state.CurrentTimingMethod == TimingMethod.GameTime
                    ? TimingMethod.RealTime : TimingMethod.GameTime];
            this.InternalComponent.InformationName = state.CurrentTimingMethod == TimingMethod.GameTime
                ? "Real Time" : "Game Time";

            _cache.Restart();
            _cache["TimeValue"] = this.InternalComponent.ValueLabel.Text;
            _cache["TimingMethod"] = state.CurrentTimingMethod;
            if (invalidator != null && _cache.HasChanged)
                invalidator.Invalidate(0f, 0f, width, height);
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region region)
        {
            this.PrepareDraw(state);
            this.InternalComponent.DrawVertical(g, state, width, region);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region region)
        {
            this.PrepareDraw(state);
            this.InternalComponent.DrawHorizontal(g, state, height, region);
        }

        void PrepareDraw(LiveSplitState state)
        {
            this.InternalComponent.NameLabel.ForeColor = state.LayoutSettings.TextColor;
            this.InternalComponent.ValueLabel.ForeColor = state.LayoutSettings.TextColor;
            this.InternalComponent.NameLabel.HasShadow = this.InternalComponent.ValueLabel.HasShadow = state.LayoutSettings.DropShadows;
        }

        void gameProcess_OnPause(object sender, EventArgs e)
        {
            _state.IsGameTimePaused = true;
        }

        void gameProcess_OnUnpause(object sender, EventArgs e)
        {
            _state.IsGameTimePaused = false;
        }

        public XmlNode GetSettings(XmlDocument document) { return document.CreateElement("Settings"); }
        public Control GetSettingsControl(LayoutMode mode) { return null; }
        public void SetSettings(XmlNode settings) { }
        public void RenameComparison(string oldName, string newName) { }
        public float VerticalHeight { get { return this.InternalComponent.VerticalHeight; } }
        public float MinimumWidth { get { return this.InternalComponent.MinimumWidth; } }
        public float HorizontalWidth { get { return this.InternalComponent.HorizontalWidth; } }
        public float MinimumHeight { get { return this.InternalComponent.MinimumHeight; } }
        public float PaddingLeft { get { return this.InternalComponent.PaddingLeft; } }
        public float PaddingRight { get { return this.InternalComponent.PaddingRight; } }
        public float PaddingTop { get { return this.InternalComponent.PaddingTop; } }
        public float PaddingBottom { get { return this.InternalComponent.PaddingBottom; } }
    }
}
