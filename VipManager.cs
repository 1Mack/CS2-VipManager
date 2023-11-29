using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;

namespace VipManager;

public interface IPlayerAdmins
{
  string SteamId { get; set; }
  string Groups { get; set; }
  string Timestamp { get; set; }
  string EndDate { get; set; }

}

public class PlayerAdminsClass : IPlayerAdmins
{
  public required string SteamId { get; set; }
  public required string Groups { get; set; }
  public required string Timestamp { get; set; }
  public required string EndDate { get; set; }
  // Outras propriedades e métodos...
}
public partial class VipManager : BasePlugin, IPluginConfig<VipManagerConfig>
{
  public override string ModuleName => "VipManager";
  public override string ModuleDescription => "Set admin by database";
  public override string ModuleAuthor => "1MaaaaaacK";
  public override string ModuleVersion => "1.2";
  public static int ConfigVersion => 5;

  private string DatabaseConnectionString = string.Empty;

  private List<IPlayerAdmins> PlayerAdmins = new();

  private List<string> Teste = new();
  private DateTime reloadCommandCooldown = new();
  private DateTime[] commandCooldown = new DateTime[Server.MaxPlayers];
  public override void Load(bool hotReload)
  {
    RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
    RegisterListener<Listeners.OnMapStart>(OnMapStart);

    RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
    RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);


    AddCommand($"css_{Config.Commands.AddPrefix}", "Set Admin", SetAdmin);
    AddCommand($"css_{Config.Commands.RemovePrefix}", "Remove Admin", RemoveAdmin);
    AddCommand($"css_{Config.Commands.ReloadPrefix}", "Reload Admins", ReloadAdmins);
    AddCommand($"css_{Config.Commands.TestPrefix}", "Test VIP", TesteVip);
    AddCommand($"css_{Config.Commands.StatusPrefix}", "Check your vip time left", StatusVip);


    BuildDatabaseConnectionString();
    TestDatabaseConnection();
  }



}