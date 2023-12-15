using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
namespace VipManager;

public partial class VipManager
{
  public string ParseDateTime(string milisseconds)
  {
    if (milisseconds == "0") return milisseconds;


    return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(milisseconds) * 1000).ToOffset(TimeSpan.FromHours(Config.TimeZone)).ToString(Config.DateFormat);
  }
  public class GetPlayerClass
  {
    public string? Name { get; set; }
    public required string Steamid { get; set; }
  }
  public GetPlayerClass? GetPlayer(string value, CommandInfo command)
  {

    CCSPlayerController? player = null;

    string? steamid = "";

    if (value.StartsWith("#") && int.TryParse(value.AsSpan(1), out var userid))
    {
      player = Utilities.GetPlayerFromUserid(userid);
      steamid = player.SteamID.ToString();

    }
    else if (SteamID.TryParse(value, out var steamId))
    {
      if (steamId != null)
      {
        steamid = steamId.SteamId64.ToString();
        player = Utilities.GetPlayerFromSteamId(ulong.Parse(value));
      }
    }
    else
    {
      var findPlayer = Utilities.GetPlayers().FindAll(obj => obj.PlayerName.Contains(value, StringComparison.OrdinalIgnoreCase));
      if (findPlayer.Count > 1)
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["MoreThanOnePlayerWithSameName"]}");
        return null;
      }
      else if (findPlayer.Count == 0)
      {
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["NoPlayersFound"]}");
        return null;
      }
      else
      {
        player = findPlayer[0];
        steamid = findPlayer[0].SteamID.ToString();
      }
    }



    if (player == null || player.IsBot || !player.IsValid)
    {
      if (steamid != null)
      {
        GetPlayerClass playerClassSteamId = new()
        {
          Steamid = steamid
        };
        return playerClassSteamId;
      }
      else
        command.ReplyToCommand($"{Localizer["Prefix"]} {Localizer["PlayerNotAvailable"]}");
      return null;
    }

    GetPlayerClass playerClass = new()
    {
      Steamid = steamid,
      Name = player.PlayerName
    };

    return playerClass;
  }

  public void ReloadUserPermissions(string steamId64, string group, string type, int? time = null)
  {
    CCSPlayerController? player = Utilities.GetPlayerFromSteamId(ulong.Parse(steamId64));

    if (type == "add" && time != null)
    {
      if (PlayerAdmins.Find(obj => obj.SteamId == steamId64 && obj.Group == group) == null)
      {
        var endAt = time == 0 ? 0 : DateTimeOffset.UtcNow.AddMinutes((int)time).ToUnixTimeMilliseconds() / 1000;
        var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;
        PlayerAdminsClass playerAdminFormat = new() { SteamId = steamId64, Group = group, CreatedAt = ParseDateTime(createdAt.ToString()), EndAt = ParseDateTime(endAt.ToString()) };

        PlayerAdmins.Add(playerAdminFormat);
      }

      AdminManager.AddPlayerToGroup(player, PlayerAdmins.FindAll(obj => obj.SteamId == steamId64).Select(obj => $"#css/{obj.Group}").ToArray());
    }
    else if (type == "remove")
    {
      if (PlayerAdmins.Find(obj => obj.SteamId == steamId64 && obj.Group == group) != null)
        PlayerAdmins.RemoveAll(obj => obj.SteamId == steamId64 && obj.Group == group);


      AdminManager.RemovePlayerFromGroup(player, true, $"#css/{group}");
    }
  }
  public void HandleGroupsFile()
  {
    string path = Path.GetFullPath(Path.Combine(ModulePath, "../admin_groups.json"));

    GroupsName.Clear();

    Task.Run(async () =>
    {
      var result = await GetGroupsFromDatabase() ?? throw new Exception("Couldn't get groups from database");

      string groupsParsed = "";
      List<dynamic> resultList = result.ToList();
      for (int i = 0; i < result.Count; i++)
      {
        GroupsName.Add(result[i].name);

        string[] flags = resultList[i].flags.Split(",");

        string flagsFormat = string.Join("", flags.Select(flag => $@"""@css/{flag}"",{"\n"}      "));

        groupsParsed += $@"
  ""#css/{resultList[i].name}"": {{
    ""flags"": [
      {flagsFormat.Remove(flagsFormat.LastIndexOf(","))}
    ],
    ""immunity"": {resultList[i].immunity}
  }}";
        if (i != result.Count - 1)
        {
          groupsParsed += ",";
        }
      }


      await File.WriteAllTextAsync(path, "{" + groupsParsed + "\n}");

      AdminManager.LoadAdminGroups(path);

      if (Config.Groups.OverwriteMainFile)
      {

        await File.WriteAllTextAsync(Path.GetFullPath(Path.Combine(ModulePath, "../../../configs/admin_groups.json")), "{" + groupsParsed + "\n}");
      }

    });
  }
}

