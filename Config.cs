using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace VipManager
{
  public class VipManagerConfig : BasePluginConfig
  {
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

  }
}