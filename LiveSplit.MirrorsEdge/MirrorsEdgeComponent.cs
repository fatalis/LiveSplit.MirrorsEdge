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
            get { return "Mirror's Edge No Loads"; }
        }

        public IDictionary<string, Action> ContextMenuControls { get; protected set; }
        protected InfoTimeComponent InternalComponent { get; set; }

        private LiveSplitState _state;
        private GameProcess _gameProcess;
        private TimeSpan _loadTime;
        private TripleDateTime _pauseStartTime;
        private TimeSpan _timeAtPause;
        private bool _isPaused;
        private GraphicsCache _cache;

        public MirrorsEdgeComponent(LiveSplitState state)
        {
            this.ContextMenuControls = new Dictionary<String, Action>();

            this.InternalComponent = new InfoTimeComponent(null, null, new RegularTimeFormatter(TimeAccuracy.Hundredths));

            _cache = new GraphicsCache();
            _timeAtPause = new TimeSpan();
            _loadTime = new TimeSpan();

            _state = state;
            _state.OnReset += state_OnReset;

            _gameProcess = new GameProcess();
            _gameProcess.OnPause += gameProcess_OnPause;
            _gameProcess.OnUnpause += gameProcess_OnUnpause;
            _gameProcess.Run();
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            if (_isPaused)
                this.InternalComponent.TimeValue = (_timeAtPause - _loadTime);
            else
                this.InternalComponent.TimeValue = (_state.CurrentTime.Value - _loadTime);

            _cache.Restart();
            _cache["TimeValue"] = this.InternalComponent.ValueLabel.Text;
            if (invalidator != null && _cache.HasChanged)
                invalidator.Invalidate(0f, 0f, width, height);
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region region)
        {
            this.InternalComponent.NameLabel.Text = "Without Loads";
            this.InternalComponent.NameLabel.ForeColor = state.LayoutSettings.TextColor;
            this.InternalComponent.ValueLabel.ForeColor = state.LayoutSettings.TextColor;
            this.InternalComponent.DrawVertical(g, state, width, region);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region region)
        {
            this.InternalComponent.NameLabel.Text = "Without Loads";
            this.InternalComponent.NameLabel.ForeColor = state.LayoutSettings.TextColor;
            this.InternalComponent.ValueLabel.ForeColor = state.LayoutSettings.TextColor;
            this.InternalComponent.DrawHorizontal(g, state, height, region);
        }

        void state_OnReset(object sender, EventArgs e)
        {
            _loadTime = new TimeSpan();
            _isPaused = false;
        }

        void gameProcess_OnPause(object sender, EventArgs e)
        {
            if (!_isPaused && _state.CurrentPhase == TimerPhase.Running)
            {
                _pauseStartTime = TripleDateTime.Now;
                _timeAtPause = _state.CurrentTime.Value;
                _isPaused = true;
            }
        }

        void gameProcess_OnUnpause(object sender, EventArgs e)
        {
            if (_isPaused && _state.CurrentPhase == TimerPhase.Running)
            {
                _loadTime = _loadTime.Add(TripleDateTime.Now - _pauseStartTime);
                _isPaused = false;
            }
        }

        ~MirrorsEdgeComponent()
        {
            // TODO: in LiveSplit 1.4, components will be IDisposable
            //_gameProcess.Stop();
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
