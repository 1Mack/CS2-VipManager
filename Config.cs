using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace VipManager
{
  public class VipManagerConfig : BasePluginConfig
  {
    public override int Version { get; set; } = 4;

    [JsonPropertyName("Prefix")]
    public string Prefix { get; set; } = "[VipManager]";

    [JsonPropertyName("CooldownRefreshCommandSeconds")]
    public int CooldownRefreshCommandSeconds { get; set; } = 60;

    [JsonPropertyName("Database")]
    public Database Database { get; set; } = new();

    [JsonPropertyName("VipTest")]
    public VipTest VipTest { get; set; } = new();

    [JsonPropertyName("CommandsPrefix")]
    public CommandsPrefix CommandsPrefix { get; set; } = new();

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
  }
  public class VipTest
  {
    [JsonPropertyName("VipTestTime")]
    public int Time { get; set; } = 10;

    [JsonPropertyName("VipTestGroup")]
    public string Group { get; set; } = "#css/vip";
  }
  public class CommandsPrefix
  {
    [JsonPropertyName("Add")]
    public string Add { get; set; } = "vm_add";

    [JsonPropertyName("Remove")]
    public string Remove { get; set; } = "vm_remove";

    [JsonPropertyName("Reload")]
    public string Reload { get; set; } = "vm_reload";

    [JsonPropertyName("Test")]
    public string Test { get; set; } = "vm_test";

    [JsonPropertyName("Status")]
    public string Status { get; set; } = "vm_status";
  }
}