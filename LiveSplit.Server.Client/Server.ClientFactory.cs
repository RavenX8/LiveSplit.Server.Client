using System;
using LiveSplit.Server.Client;
using LiveSplit.Model;
using LiveSplit.UI.Components;

[assembly: ComponentFactory(typeof(Server.ClientFactory))]

namespace LiveSplit.Server.Client
{
  class Server.ClientFactory : IComponentFactory
  {
    public string ComponentName => "Autosplitter Client";
    public string Description => "Support functionally to help auto splitting";
    public ComponentCategory Category => ComponentCategory.Control;
    public Version Version => Version.Parse("1.0.0");

    public string UpdateName => this.ComponentName;
    public string UpdateURL => "";
    public string XMLURL => "";

    public IComponent Create(LiveSplitState state) => new Server.ClientComponent(state);
  }
}
