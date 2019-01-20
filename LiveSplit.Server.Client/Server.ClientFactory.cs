using System;
using LiveSplit.Server.Client;
using LiveSplit.Model;
using LiveSplit.UI.Components;

[assembly: ComponentFactory(typeof(ServerClientComponentFactory))]

namespace LiveSplit.Server.Client
{
  class ServerClientComponentFactory : IComponentFactory
  {
    public string ComponentName => "Server Client";
    public string Description => "Support functionally to help auto splitting using a remote LiveSplit Server instace";
    public ComponentCategory Category => ComponentCategory.Control;
    public Version Version => Version.Parse("1.0.0");

    public string UpdateName => this.ComponentName;
    public string UpdateURL => "";
    public string XMLURL => "";

    public IComponent Create(LiveSplitState state) => new ServerClientComponent(state);
  }
}
