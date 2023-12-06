using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;

namespace VipManager;

public partial class VipManager
{
  public string ParseConfigMessage(string message, CCSPlayerController? player, string? name = null, string? group = null, string? timestamp = null, string? dateEnd = null, string? timeleft = null)
  {
    Dictionary<string, dynamic> tags = new()
        {
          { "{PLAYERNAME}", !string.IsNullOrEmpty(name) ? name : string.IsNullOrEmpty(player?.PlayerName) ? "":player.PlayerName },
          { "{GROUP}", !string.IsNullOrEmpty(group) ? group : string.Join(", ",PlayerAdmins.SelectMany(obj => obj.Group)) },
          { "{TIMELEFT}", !string.IsNullOrEmpty(timeleft) ? timeleft : Timeleft() },
          { "{ENDDATE}", !string.IsNullOrEmpty(dateEnd) ? dateEnd : DateEnd() },
          { "{TIMESTAMP}", !string.IsNullOrEmpty(timestamp) ? timestamp : Timestamp() },

      };

    foreach (var tag in tags)
    {
      message = message.Replace(tag.Key, tag.Value.ToString());
    }

    string Timestamp()
    {
      string message = "";

      foreach (var item in PlayerAdmins)
      {
        message += $"{item.Group} - {item.CreatedAt}\u2029";
      }

      return message;
    }

    string Timeleft()
    {
      string message = "";

      foreach (var item in PlayerAdmins)
      {
        if (item.EndAt == "0")
          message += $"{item.Group} - {Config.Messages.VipPermanent}";
        else
        {
          DateTime endAt = DateTime.Parse(item.EndAt);
          TimeSpan timeleft = endAt - DateTime.UtcNow;
          message += $"{item.Group} - ({(timeleft.Days > 0 ? timeleft.Days + " d" : "")} {(timeleft.Hours > 0 ? timeleft.Hours + " h" : "")} {(timeleft.Minutes > 0 ? timeleft.Minutes + " m" : "")})\u2029";
        }
      }

      return message;
    }
    string DateEnd()
    {
      string message = "";

      foreach (var item in PlayerAdmins)
      {
        message += $"{item.Group} - {(item.CreatedAt == "0" ? Config.Messages.VipPermanent : item.CreatedAt)}\u2029";
      }

      return message;
    }


    return message;
  }
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
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.MoreThanOnePlayerWithSameName}");
        return null;
      }
      else if (findPlayer.Count == 0)
      {
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.NoPlayersFound}");
        return null;
      }
      else
      {
        player = findPlayer[0];
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
        command.ReplyToCommand($"{Config.Prefix} {Config.Messages.PlayerNotAvailable}");
      return null;
    }

    GetPlayerClass playerClass = new()
    {
      Steamid = steamid,
      Name = player.PlayerName
    };

    return playerClass;
  }
}

