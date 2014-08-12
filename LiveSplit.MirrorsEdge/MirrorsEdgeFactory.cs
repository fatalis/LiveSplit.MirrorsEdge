using System.Reflection;
using LiveSplit.UI.Components;
using System;
using LiveSplit.Model;

namespace LiveSplit.MirrorsEdge
{
    public class MirrorsEdgeFactory : IComponentFactory
    {
        public string ComponentName
        {
            get { return "Mirror's Edge"; }
        }

        public string Description
        {
            get { return "Automatic load time remover for Mirror's Edge."; }
        }

        public ComponentCategory Category
        {
            get { return ComponentCategory.Control; }
        }

        public IComponent Create(LiveSplitState state)
        {
            return new MirrorsEdgeComponent(state);
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
