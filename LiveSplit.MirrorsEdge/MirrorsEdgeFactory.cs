using System.Reflection;
using LiveSplit.MirrorsEdge;
using LiveSplit.UI.Components;
using System;
using LiveSplit.Model;

[assembly: ComponentFactory(typeof(MirrorsEdgeFactory))]

namespace LiveSplit.MirrorsEdge
{
    public class MirrorsEdgeFactory : IComponentFactory
    {
        public string ComponentName => "Mirror's Edge";
        public string Description => "Automatic load time remover for Mirror's Edge.";
        public ComponentCategory Category => ComponentCategory.Control;

        public IComponent Create(LiveSplitState state)
        {
            return new MirrorsEdgeComponent(state);
        }

        public string UpdateName => this.ComponentName;
        public string UpdateURL => "http://fatalis.pw/livesplit/update/";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public string XMLURL => this.UpdateURL + "Components/update.LiveSplit.MirrorsEdgeNoLoads.xml";
    }
}
