using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using static CounterStrikeSharp.API.Core.Listeners;

namespace VipManager;

public class PlayerAdminsClass
{
  public required string SteamId { get; set; }
  public required string Group { get; set; }
  public required string CreatedAt { get; set; }
  public required string EndAt { get; set; }

}
public partial class VipManager : BasePlugin, IPluginConfig<VipManagerConfig>
{
  public override string ModuleName => "VipManager";
  public override string ModuleDescription => "Set admin by database";
  public override string ModuleAuthor => "1MaaaaaacK";
  public override string ModuleVersion => "1.4";
  public static int ConfigVersion => 6;

  private string DatabaseConnectionString = string.Empty;

  private readonly List<PlayerAdminsClass> PlayerAdmins = new();
  private readonly List<string> GroupsName = new();
  private readonly Dictionary<int, DateTime> commandCooldown = new();
  public override void Load(bool hotReload)
  {

    RegisterListener<OnClientAuthorized>(OnClientAuthorized);
    RegisterListener<OnMapStart>(OnMapStart);
    RegisterListener<OnClientDisconnect>(OnClientDisconnect);
    RegisterListener<OnClientPutInServer>(OnClientPutInServer);

    RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);


    AddCommand($"css_{Config.Commands.AddPrefix}", "Set Admin", SetAdmin);
    AddCommand($"css_{Config.Commands.RemovePrefix}", "Remove Admin", RemoveAdmin);
    AddCommand($"css_{Config.Commands.ReloadPrefix}", "Reload Admins", ReloadAdmins);
    AddCommand($"css_{Config.Commands.TestPrefix}", "Test VIP", TesteVip);
    AddCommand($"css_{Config.Commands.StatusPrefix}", "Check your vip time left", StatusVip);


    BuildDatabaseConnectionString();
    TestDatabaseConnection();

    if (Config.Groups.Enabled) HandleGroupsFile();
  }



}