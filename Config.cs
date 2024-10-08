using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace VipManager;

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

    Config = config;
  }

}
public class VipManagerConfig : BasePluginConfig
{
  public override int Version { get; set; } = 7;
  [JsonPropertyName("CooldownRefreshCommandSeconds")]
  public int CooldownRefreshCommandSeconds { get; set; } = 60;
  [JsonPropertyName("DateFormat")]
  public string DateFormat { get; set; } = "dd/MM/yyyy HH:mm:ss";
  [JsonPropertyName("TimeZone")]
  public int TimeZone { get; set; } = -3;
  [JsonPropertyName("ShowWelcomeMessageConnectedPublic")]
  public bool ShowWelcomeMessageConnectedPublic { get; set; } = true;
  [JsonPropertyName("ShowWelcomeMessageConnectedPrivate")]
  public bool ShowWelcomeMessageConnectedPrivate { get; set; } = true;
  [JsonPropertyName("ShowWelcomeMessageDisconnectedPublic")]
  public bool ShowWelcomeMessageDisconnectedPublic { get; set; } = true;
  [JsonPropertyName("ServerID")]
  public int ServerID { get; set; } = 1;
  [JsonPropertyName("UseCenterHtmlMenu")]
  public bool UseCenterHtmlMenu { get; set; } = true;
  [JsonPropertyName("CloseMenuAfterUse")]
  public bool CloseMenuAfterUse { get; set; } = true;
  [JsonPropertyName("Database")]
  public Database Database { get; set; } = new();
  [JsonPropertyName("VipTest")]
  public VipTest VipTest { get; set; } = new();
  [JsonPropertyName("Commands")]
  public Commands Commands { get; set; } = new();
  [JsonPropertyName("Groups")]
  public Groups Groups { get; set; } = new();
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
  [JsonPropertyName("PrefixGroups")]
  public string PrefixGroups { get; set; } = "vip_manager_groups";
}
public class VipTest
{
  [JsonPropertyName("Time")]
  public int Time { get; set; } = 10;

  [JsonPropertyName("Group")]
  public string Group { get; set; } = "vip";
  [JsonPropertyName("FollowServerID")]
  public bool FollowServerID { get; set; } = true;
}
public class Commands
{
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
public class Groups
{
  [JsonPropertyName("Enabled")]
  public bool Enabled { get; set; } = true;
  [JsonPropertyName("OverwriteMainFile")]
  public bool OverwriteMainFile { get; set; } = false;
}
