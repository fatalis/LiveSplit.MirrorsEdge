using System;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.Model;

namespace LiveSplit.MirrorsEdge
{
    public partial class MirrorsEdgeSettings : UserControl
    {
        public bool AutoResetStart { get; set; }
        public bool AutoChapterSplit { get; set; }
        public bool AutoEndingSplit { get; set; }
        public bool AutoStormdrainSplit { get; set; }

        private const bool DEFAULT_AUTO_RESET_START = true;
        private const bool DEFAULT_AUTO_CHAPTER_SPLIT = true;
        private const bool DEFAULT_AUTO_ENDING_SPLIT = true;
        private const bool DEFAULT_AUTO_STORMDRAIN_SPLIT = false;

        private TimerModel _timer;

        public MirrorsEdgeSettings(TimerModel timer)
        {
            InitializeComponent();
            _timer = timer;

            this.chkResetStart.DataBindings.Add("Checked", this, "AutoResetStart", false, DataSourceUpdateMode.OnPropertyChanged);
            this.chkChapterSplit.DataBindings.Add("Checked", this, "AutoChapterSplit", false, DataSourceUpdateMode.OnPropertyChanged);
            this.chkEndingSplit.DataBindings.Add("Checked", this, "AutoEndingSplit", false, DataSourceUpdateMode.OnPropertyChanged);
            this.chkStormdrainSplit.DataBindings.Add("Checked", this, "AutoStormdrainSplit", false, DataSourceUpdateMode.OnPropertyChanged);

            // defaults
            this.AutoResetStart = DEFAULT_AUTO_RESET_START;
            this.AutoChapterSplit = DEFAULT_AUTO_CHAPTER_SPLIT;
            this.AutoEndingSplit = DEFAULT_AUTO_ENDING_SPLIT;
            this.AutoStormdrainSplit = DEFAULT_AUTO_STORMDRAIN_SPLIT;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            rbGameTime.Checked = _timer.CurrentState.CurrentTimingMethod == TimingMethod.GameTime;
            rbRealTime.Checked = !rbGameTime.Checked;
        }

        private void TimingMethodsCheckedChanged(object sender, EventArgs e)
        {
            _timer.CurrentState.CurrentTimingMethod = rbGameTime.Checked ? TimingMethod.GameTime : TimingMethod.RealTime;
            _timer.SwitchComparisonNext(); // hack to get the "compare against" menu updated with the new timing method
            _timer.SwitchComparisonPrevious();
        }

        public XmlNode GetSettings(XmlDocument doc)
        {
            XmlElement settingsNode = doc.CreateElement("Settings");

            settingsNode.AppendChild(ToElement(doc, "Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3)));
            settingsNode.AppendChild(ToElement(doc, "AutoResetStart", this.AutoResetStart));
            settingsNode.AppendChild(ToElement(doc, "AutoChapterSplit", this.AutoChapterSplit));
            settingsNode.AppendChild(ToElement(doc, "AutoEndingSplit", this.AutoEndingSplit));
            settingsNode.AppendChild(ToElement(doc, "AutoStormdrainSplit", this.AutoStormdrainSplit));

            return settingsNode;
        }

        public void SetSettings(XmlNode settings)
        {
            bool bval;

            this.AutoResetStart = settings["AutoResetStart"] != null ?
                (Boolean.TryParse(settings["AutoResetStart"].InnerText, out bval) ? bval : DEFAULT_AUTO_RESET_START)
                : DEFAULT_AUTO_RESET_START;

            this.AutoChapterSplit = settings["AutoChapterSplit"] != null ?
                (Boolean.TryParse(settings["AutoChapterSplit"].InnerText, out bval) ? bval : DEFAULT_AUTO_CHAPTER_SPLIT)
                : DEFAULT_AUTO_CHAPTER_SPLIT;

            this.AutoEndingSplit = settings["AutoEndingSplit"] != null ?
                (Boolean.TryParse(settings["AutoEndingSplit"].InnerText, out bval) ? bval : DEFAULT_AUTO_ENDING_SPLIT)
                : DEFAULT_AUTO_ENDING_SPLIT;

            this.AutoStormdrainSplit = settings["AutoStormdrainSplit"] != null ?
                (Boolean.TryParse(settings["AutoStormdrainSplit"].InnerText, out bval) ? bval : DEFAULT_AUTO_STORMDRAIN_SPLIT)
                : DEFAULT_AUTO_STORMDRAIN_SPLIT;
        }

        static XmlElement ToElement<T>(XmlDocument document, string name, T value)
        {
            XmlElement str = document.CreateElement(name);
            str.InnerText = value.ToString();
            return str;
        }
    }
}
