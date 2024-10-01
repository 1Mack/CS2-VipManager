using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using static CounterStrikeSharp.API.Core.Listeners;


namespace VipManager;

[MinimumApiVersion(199)]
public partial class VipManager : BasePlugin, IPluginConfig<VipManagerConfig>
{
  public override string ModuleName => "VipManager";
  public override string ModuleDescription => "Manage players permissions and groups using database";
  public override string ModuleAuthor => "1MaaaaaacK";
  public override string ModuleVersion => "1.6";
  public static int ConfigVersion => 7;
  private readonly ConcurrentDictionary<ulong, PlayerAdminsClass[]> PlayerAdmins = [];
  private readonly List<string> GroupsName = [];
  private readonly Dictionary<int, DateTime> commandCooldown = [];
  public override void Load(bool hotReload)
  {

    RegisterListener<OnClientDisconnect>(OnClientDisconnect);

    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerFullConnect);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);


    AddCommand($"css_{Config.Commands.ReloadPrefix}", "Reload Admins", ReloadAdmins);
    AddCommand($"css_{Config.Commands.TestPrefix}", "Test VIP", TesteVip);
    AddCommand($"css_{Config.Commands.StatusPrefix}", "Check your vip time left", StatusVip);


    CreateDatabaseTables();
    if (Config.Groups.Enabled) HandleGroupsFile();
  }
}