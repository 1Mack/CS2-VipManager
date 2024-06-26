using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;
namespace VipManager;

public partial class VipManager
{
  public string ParseDateTime(string milisseconds)
  {
    if (milisseconds == "0") return milisseconds;


    return DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(milisseconds) * 1000).ToOffset(TimeSpan.FromHours(Config.TimeZone)).ToString(Config.DateFormat);
  }

  public GetPlayerClass? GetPlayer(string value, CommandInfo command)
  {

    CCSPlayerController? player = null;

    string? steamid = "";

    if (value.StartsWith("#") && int.TryParse(value.AsSpan(1), out var userid))
    {
      player = Utilities.GetPlayerFromUserid(userid);
      steamid = player!.SteamID.ToString();

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
  public async Task HandleGroupsFile()
  {
    string path = Path.GetFullPath(Path.Combine(ModulePath, "../admin_groups.json"));
    GroupsName.Clear();

    var result = await GetGroupsFromDatabase() ?? throw new Exception("Couldn't get groups from database");

    string groupsParsed = "";

    for (int i = 0; i < result.Count; i++)
    {
      GroupsName.Add(result[i].name);

      string[] flags = result[i].flags.Split(",");

      string flagsFormat = string.Join("", flags.Select(flag => $@"""@css/{flag}"",{"\n"}      "));

      groupsParsed += $@"
  ""#css/{result[i].name}"": {{
    ""flags"": [
      {flagsFormat.Remove(flagsFormat.LastIndexOf(","))}
    ],
    ""immunity"": {result[i].immunity}
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

  }
  public bool CanExecuteCommand(int playerSlot)
  {
    if (commandCooldown.ContainsKey(playerSlot))
    {
      if (DateTime.UtcNow >= commandCooldown[playerSlot])
      {
        commandCooldown[playerSlot] = commandCooldown[playerSlot].AddSeconds(Config.CooldownRefreshCommandSeconds);
        return true;
      }
      else
      {
        return false;
      }
    }
    else
    {
      commandCooldown.Add(playerSlot, DateTime.UtcNow.AddSeconds(Config.CooldownRefreshCommandSeconds));
      return true;
    }
  }
  async public Task<List<GroupsClass>?> GetGroupsFromDatabase()
  {
    try
    {
      var queryResult = await QueryAsync<GroupsClass>($"SELECT * FROM {Config.Database.PrefixGroups}");

      return queryResult;

    }
    catch (Exception e)
    {
      Logger.LogError($"{Localizer["Prefix"]} Error on GetGroupsFromDatabase: " + e.Message);
      return null;
    }
  }
  async public Task<List<AdminsDatabaseClass>?> GetAdmins(string steamid, string group)
  {
    try
    {
      string query = @$"SELECT * FROM `{Config.Database.PrefixVipManager}` 
      WHERE {(!string.IsNullOrEmpty(steamid) ? $"steamid = @steamid AND group = @group AND" : "")} 
      server_id = @server OR server_id = 0  ORDER BY steamid";

      var queryResult = await QueryAsync<AdminsDatabaseClass>(query, new { server = Config.ServerID, steamid, group });

      return queryResult;
    }
    catch (Exception e)
    {
      Logger.LogError($"{Localizer["Prefix"]} Erro on loading admins: " + e.Message);
      return null;
    }

  }

  async public Task GetAdminsFromDatabase()
  {
    try
    {

      var endAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;

      string query = @$"DELETE FROM `{Config.Database.PrefixVipManager}` 
      WHERE end_at <= @endAt 
      AND end_at != 0 
      AND (server_id = @server OR server_id = 0)";

      await ExecuteAsync(query, new { server = Config.ServerID, endAt });

      query = $"select * from `{Config.Database.PrefixVipManager}` WHERE server_id = @server OR server_id = 0 order by steamid";

      var queryResult = await QueryAsync<AdminsDatabaseClass>(query, new { server = Config.ServerID });

      PlayerAdmins.Clear();

      if (queryResult != null)
      {
        Server.NextFrame(() =>
        {

          foreach (var result in queryResult.ToList())
          {

            if (result.group.Length == 0) Console.WriteLine($"{Localizer["Prefix"]} there is a wrong value at  {result.steamid} - {result.group}");

            PlayerAdminsClass playerAdminFormat = new()
            {
              SteamId = result.steamid,
              Group = result.group,
              EndAt = $"{ParseDateTime($"{result.end_at}")}",
              CreatedAt = $"{ParseDateTime($"{result.created_at}")}"
            };

            PlayerAdmins.Add(playerAdminFormat);
          }

          foreach (var player in Utilities.GetPlayers())
          {
            if (player != null && player.IsValid && !player.IsBot)
            {
              var findPlayerAdmin = PlayerAdmins.FindAll(obj => obj.SteamId == player.SteamID.ToString());
              if (findPlayerAdmin != null)
              {
                AdminManager.AddPlayerToGroup(player, findPlayerAdmin.Select(obj => $"#css/{obj.Group}").ToArray());
              }
            }
          };
          isSynced = true;
        });
      }

    }
    catch (Exception e)
    {
      Logger.LogError($"{Localizer["Prefix"]} Erro on loading admins: " + e.Message);
    }

  }

  public async Task CreateDatabaseTables()
  {
    BuildDatabaseConnectionString();
    try
    {

      try
      {
        string[] createTables = new[]
          {
        @$"CREATE TABLE IF NOT EXISTS `{Config.Database.PrefixVipManager}` 
        (`id` INT NOT NULL AUTO_INCREMENT, `name` varchar(128), `steamid` varchar(64) NOT NULL, `group` varchar(200) NOT NULL,`server_id` INT NOT NULL,
         `discord_id` varchar(100), `created_at` INT NOT NULL, end_at INT NOT NULL, PRIMARY KEY (`id`)) 
         ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci",
        @$"CREATE TABLE IF NOT EXISTS `{Config.Database.PrefixTestVip}` 
        (`id` INT NOT NULL AUTO_INCREMENT, `name` varchar(128), `steamid` varchar(64) NOT NULL,`server_id` INT NOT NULL,
        `created_at` INT NOT NULL, end_at INT NOT NULL, PRIMARY KEY (`id`)) 
        ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci",
        @$"CREATE TABLE IF NOT EXISTS `{Config.Database.PrefixGroups}` 
        (`id` INT NOT NULL AUTO_INCREMENT, `name` varchar(30) UNIQUE, `flags` text NOT NULL, 
        immunity INT NOT NULL, PRIMARY KEY (`id`)) 
        ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci",
      };

        foreach (var query in createTables)
        {
          await ExecuteAsync(query);
        }

      }
      catch (Exception)
      {
        throw new Exception($"{Localizer["Prefix"]} Unable to create tables!");
      }
    }
    catch (Exception ex)
    {
      throw new Exception($"{Localizer["Prefix"]} Unknown mysql exception! " + ex.Message);
    }
  }


  public void Menu(string title, CCSPlayerController player, Action<CCSPlayerController, ChatMenuOption> handleMenu, List<string> list, bool? closeMenu = false)
  {
    if (Config.UseCenterHtmlMenu)
    {
      CenterHtmlMenu menu = new(title, this);

      list.ForEach(item => menu.AddMenuOption(item, handleMenu));

      if (closeMenu == true) menu.PostSelectAction = PostSelectAction.Close;

      MenuManager.OpenCenterHtmlMenu(this, player, menu);

    }
    else
    {
      ChatMenu menu = new(title);

      list.ForEach(item => menu.AddMenuOption(item, handleMenu));

      if (closeMenu == true) menu.PostSelectAction = PostSelectAction.Close;

      MenuManager.OpenChatMenu(player, menu);
    }
  }

  public async Task<bool> HasVipGroupOnDatabase(string steamid, string group, int serverID)
  {
    try
    {
      string query = $"SELECT id FROM {Config.Database.PrefixVipManager} WHERE steamid = @steamid AND `group` = @group AND server_id = @serverID";

      var result = await QueryAsync<AdminsDatabaseClass>(query, new { steamid, group, serverID });

      return result.Count > 0;
    }
    catch (Exception e)
    {

      throw new Exception(e.Message);
    }
  }
  public async void SetRoleOnVipManagerDatabase(string name, string steamid, string group, int serverID, long createdAt, long endAt)
  {
    try
    {
      string query = @$"INSERT INTO `{Config.Database.PrefixVipManager}` 
      (`name`, `steamid`, `group`, `server_id`, `created_at`,`end_at`) 
      VALUES(@name, @steamid, @group, @serverID, @createdAt, @endAt)";

      await ExecuteAsync(query, new { name, steamid, group, serverID, createdAt, endAt });

    }
    catch (Exception e)
    {
      throw new Exception(e.Message);
    }
  }
}
