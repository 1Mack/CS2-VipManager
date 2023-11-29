using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;

namespace VipManager
{
  public partial class VipManager
  {
    public required VipManagerConfig Config { get; set; }

    public void OnConfigParsed(VipManagerConfig config)
    {
      if (config.Version != ConfigVersion) throw new Exception($"You have a wrong config version. Delete it and restart the server to get the right version ({ConfigVersion})!");

      if (config.Database.Host.Length < 1 || config.Database.Name.Length < 1 || config.Database.User.Length < 1)
      {
        throw new Exception($"You need to setup Database credentials in config!");
      }
      else if (config.Commands.AddPrefix.Length < 1 ||
      config.Commands.RemovePrefix.Length < 1 ||
      config.Commands.StatusPrefix.Length < 1 ||
      config.Commands.ReloadPrefix.Length < 1 ||
      config.Commands.TestPrefix.Length < 1)
      {
        throw new Exception($"You need to setup CommandsPrefix in config!");
      }

      config.Prefix = ChatTags(config, config.Prefix);

      foreach (var message in config.Messages.GetType().GetProperties())
      {
        var value = message.GetValue(config.Messages)?.ToString();


        if (string.IsNullOrEmpty(value)) throw new Exception($"You need to setup the message `{message.Name}` in config!");

        message.SetValue(config.Messages, ChatTags(config, value));
      }
      foreach (var welcomeMessages in config.WelcomeMessage.GetType().GetProperties())
      {
        var value = welcomeMessages.GetValue(config.WelcomeMessage)?.ToString();


        if (string.IsNullOrEmpty(value)) throw new Exception($"You need to setup the message `{welcomeMessages.Name}` in config!");

        welcomeMessages.SetValue(config.WelcomeMessage, ChatTags(config, value));
      }
      Config = config;
    }
    private static string ChatTags(VipManagerConfig config, string input)
    {
      Dictionary<string, dynamic> tags = new()
        {
          { "{DEFAULT}", ChatColors.Default },
          { "{WHITE}", ChatColors.White },
          { "{DARKRED}", ChatColors.Darkred },
          { "{GREEN}", ChatColors.Green },
          { "{LIGHTYELLOW}", ChatColors.LightYellow },
          { "{LIGHTBLUE}", ChatColors.LightBlue },
          { "{OLIVE}", ChatColors.Olive },
          { "{LIME}", ChatColors.Lime },
          { "{RED}", ChatColors.Red },
          { "{LIGHTPURPLE}", ChatColors.LightPurple },
          { "{PURPLE}", ChatColors.Purple },
          { "{GREY}", ChatColors.Grey },
          { "{YELLOW}", ChatColors.Yellow },
          { "{GOLD}", ChatColors.Gold },
          { "{SILVER}", ChatColors.Silver },
          { "{BLUE}", ChatColors.Blue },
          { "{DARKBLUE}", ChatColors.DarkBlue },
          { "{BLUEGREY}", ChatColors.BlueGrey },
          { "{MAGENTA}", ChatColors.Magenta },
          { "{LIGHTRED}", ChatColors.LightRed },
          { "{COOLDOWNSECONDS}", config.CooldownRefreshCommandSeconds },
          { "{VIPTESTTIME}", config.VipTest.Time },
          { "{BREAKLINE}", "\u2029" }

      };

      foreach (var color in tags)
      {
        input = input.Replace(color.Key, color.Value.ToString());

      }

      return input;
    }

  }
  public class VipManagerConfig : BasePluginConfig
  {
    public override int Version { get; set; } = 5;
    [JsonPropertyName("Prefix")]
    public string Prefix { get; set; } = "{DEFAULT}[{GREEN}VipManager{DEFAULT}]";
    [JsonPropertyName("CooldownRefreshCommandSeconds")]
    public int CooldownRefreshCommandSeconds { get; set; } = 60;
    [JsonPropertyName("WelcomeMessage")]
    public WelcomeMessage WelcomeMessage { get; set; } = new();
    [JsonPropertyName("Database")]
    public Database Database { get; set; } = new();
    [JsonPropertyName("VipTest")]
    public VipTest VipTest { get; set; } = new();
    [JsonPropertyName("Commands")]
    public Commands Commands { get; set; } = new();
    [JsonPropertyName("Messages")]
    public Messages Messages { get; set; } = new();

  }
  public class WelcomeMessage
  {
    [JsonPropertyName("WelcomePrivate")]
    public string WelcomePrivate { get; set; } = "{DEFAULT}Welcome to the server, thanks for supporting the server being {GOLD}VIP";
    [JsonPropertyName("WelcomePublic")]
    public string WelcomePublic { get; set; } = "{DEFAULT}VIP player {GREEN}connected";
    [JsonPropertyName("DisconnectedPublic")]
    public string DisconnectPublic { get; set; } = "{DEFAULT}VIP player {RED}disconnect";

  }
  public class Database
  {
    [JsonPropertyName("Host")]
    public string Host { get; set; } = "";
    [JsonPropertyName("Port")]
    public int Port { get; set; } = 3306;
    [JsonPropertyName("User")]
    public string User { get; set; } = "";
    [JsonPropertyName("Password")]
    public string Password { get; set; } = "";
    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("PrefixVipManager")]
    public string PrefixVipManager { get; set; } = "vip_manager";
    [JsonPropertyName("PrefixTestVip")]
    public string PrefixTestVip { get; set; } = "vip_manager_testvip";
  }
  public class VipTest
  {
    [JsonPropertyName("VipTestTime")]
    public int Time { get; set; } = 10;

    [JsonPropertyName("VipTestGroup")]
    public string Group { get; set; } = "#css/vip";
  }
  public class Commands
  {
    [JsonPropertyName("AddPrefix")]
    public string AddPrefix { get; set; } = "vm_add";
    [JsonPropertyName("AddPermission")]
    public string AddPermission { get; set; } = "@css/root";
    [JsonPropertyName("RemovePrefix")]
    public string RemovePrefix { get; set; } = "vm_remove";
    [JsonPropertyName("RemovePermission")]
    public string RemovePermission { get; set; } = "@css/root";
    [JsonPropertyName("ReloadPrefix")]
    public string ReloadPrefix { get; set; } = "vm_reload";
    [JsonPropertyName("ReloadPermission")]
    public string ReloadPermission { get; set; } = "@css/root";
    [JsonPropertyName("TestPrefix")]
    public string TestPrefix { get; set; } = "vm_test";
    [JsonPropertyName("TestPermission")]
    public string TestPermission { get; set; } = "";

    [JsonPropertyName("StatusPrefix")]
    public string StatusPrefix { get; set; } = "vm_status";
    [JsonPropertyName("StatusPermission")]
    public string StatusPermission { get; set; } = "@css/reservation";
  }
  public class Messages
  {
    [JsonPropertyName("MissingCommandPermission")]
    public string MissingCommandPermission { get; set; } = "{DEFAULT}You don't have permission to use this command";
    [JsonPropertyName("AlreadyRegistryWithSteamidAndGroup")]
    public string AlreadyRegistryWithSteamidAndGroup { get; set; } = "{DEFAULT}There is already a registry with this steamid and group!";
    [JsonPropertyName("AdminAddSuccess")]
    public string AdminAddSuccess { get; set; } = "{DEFAULT}Admin has been added";
    [JsonPropertyName("InternalError")]
    public string InternalError { get; set; } = "{DEFAULT}There was an internal error";
    [JsonPropertyName("NoAdminWithSteamidAndGroup")]
    public string NoAdminWithSteamidAndGroup { get; set; } = "{DEFAULT}There is no admin with this steamID and group!";
    [JsonPropertyName("AdminDeleteSuccess")]
    public string AdminDeleteSuccess { get; set; } = "{DEFAULT}Admin has been deleted!";
    [JsonPropertyName("AdminReloadSuccess")]
    public string AdminReloadSuccess { get; set; } = "{DEFAULT}Admins reloaded successfully!";
    [JsonPropertyName("CoolDown")]
    public string CoolDown { get; set; } = "{DEFAULT}You are on a cooldown...wait {COOLDOWNSECONDS} seconds and try again!";
    [JsonPropertyName("CommandBlocked")]
    public string CommandBlocked { get; set; } = "{DEFAULT}This command is blocked by the server!";
    [JsonPropertyName("TestVipAlreadyClaimed")]
    public string TestVipAlreadyClaimed { get; set; } = "{DEFAULT}Vou have already claimed your test vip!";
    [JsonPropertyName("AlreadyNormalVip")]
    public string AlreadyNormalVip { get; set; } = "{DEFAULT}Vou have already a normal vip!";
    [JsonPropertyName("TestVipActivated")]
    public string TestVipActivated { get; set; } = "{DEFAULT}You have activated your VIP successfully for {VIPTESTTIME} minutes";
    [JsonPropertyName("NoAdminsRole")]
    public string NoAdminsRole { get; set; } = "{DEFAULT}You don't have any admin roles!";
    [JsonPropertyName("RoleNotFound")]
    public string RoleNotFound { get; set; } = "{DEFAULT}Role not found. Rejoin the server and try again.";
    [JsonPropertyName("Status")]
    public string Status { get; set; } = "{DEFAULT}------------------------------{BREAKLINE}Role: {GROUP}{BREAKLINE}Created At: {TIMESTAMP}{BREAKLINE}End At: {ENDDATE}{BREAKLINE}------------------------------";
  }
}