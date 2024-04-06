using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using Dapper;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;


namespace VipManager;

[MinimumApiVersion(199)]
public partial class VipManager : BasePlugin, IPluginConfig<VipManagerConfig>
{
  public override string ModuleName => "VipManager";
  public override string ModuleDescription => "Manage players permissions and groups using database";
  public override string ModuleAuthor => "1MaaaaaacK";
  public override string ModuleVersion => "1.5";
  public static int ConfigVersion => 7;
  private readonly List<PlayerAdminsClass> PlayerAdmins = new();
  private readonly List<string> GroupsName = new();
  private readonly Dictionary<int, DateTime> commandCooldown = new();
  private bool isSynced = false;
  public override void Load(bool hotReload)
  {

    RegisterListener<OnClientAuthorized>(OnClientAuthorized);
    RegisterListener<OnMapStart>(OnMapStart);
    RegisterListener<OnMapEnd>(OnMapEnd);
    RegisterListener<OnClientDisconnect>(OnClientDisconnect);
    RegisterListener<OnClientPutInServer>(OnClientPutInServer);

    RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);


    AddCommand($"css_{Config.Commands.AddPrefix}", "Set Admin", SetAdmin);
    AddCommand($"css_{Config.Commands.RemovePrefix}", "Remove Admin", RemoveAdmin);
    AddCommand($"css_{Config.Commands.ReloadPrefix}", "Reload Admins", ReloadAdmins);
    AddCommand($"css_{Config.Commands.TestPrefix}", "Test VIP", TesteVip);
    AddCommand($"css_{Config.Commands.StatusPrefix}", "Check your vip time left", StatusVip);

    Task.Run(async () =>
    {
      await CreateDatabaseTables();
      if (Config.Groups.Enabled) await HandleGroupsFile();
      await GetAdminsFromDatabase();
    });
  }
}