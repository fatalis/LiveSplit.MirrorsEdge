using System.Reflection;
using System.Windows.Forms;
using LiveSplit.UI.Components;
using System;
using LiveSplit.Model;

namespace LiveSplit.MirrorsEdge
{
    public class MirrorsEdgeFactory : IComponentFactory
    {
        private MirrorsEdgeComponent _instance;

        public string ComponentName
        {
            get { return "Mirror's Edge No Loads"; }
        }

        public IComponent Create(LiveSplitState state)
        {
            if (Environment.Is64BitProcess)
            {
                MessageBox.Show("LiveSplit.MirrorsEdgeNoLoads doesn't support x64 LiveSplit! Please run LiveSplit32BitPatcher at least once.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw new Exception("x64 not supported.");
            }

            // TODO: in LiveSplit 1.4, components will be IDisposable
            // this assumes the passed state is always the same one, until then
            return _instance ?? (_instance = new MirrorsEdgeComponent(state));

            // return new MirrorsEdgeComponent(state);
        }

        public string UpdateName
        {
            get { return this.ComponentName; }
        }

        public string UpdateURL
        {
            get { return "http://fatalis.hive.ai/livesplit/update/"; }
        }

        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public string XMLURL
        {
            get { return this.UpdateURL + "Components/update.LiveSplit.MirrorsEdgeNoLoads.xml"; }
        }
    }
}
