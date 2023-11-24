using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace VipManager
{
  public class VipManagerConfig : BasePluginConfig
  {
    public override int Version { get; set; } = 2;

    [JsonPropertyName("DatabaseHost")]
    public string DatabaseHost { get; set; } = "";

    [JsonPropertyName("DatabasePort")]
    public int DatabasePort { get; set; } = 3306;

    [JsonPropertyName("DatabaseUser")]
    public string DatabaseUser { get; set; } = "";

    [JsonPropertyName("DatabasePassword")]
    public string DatabasePassword { get; set; } = "";

    [JsonPropertyName("DatabaseName")]
    public string DatabaseName { get; set; } = "";

    [JsonPropertyName("Prefix")]
    public string Prefix { get; set; } = "[VipManager]";

    [JsonPropertyName("VipTestTime")]
    public int VipTestTime { get; set; } = 10;

    [JsonPropertyName("VipTestGroup")]
    public string VipTestGroup { get; set; } = "#css/vip";

    [JsonPropertyName("CooldownRefreshCommandSeconds")]
    public int CooldownRefreshCommandSeconds { get; set; } = 60;

  }
}