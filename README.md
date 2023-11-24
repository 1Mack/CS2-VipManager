# CS2-VipManager
Plugin for CS2 that stores admins in mysql and apply permissions on the game

# Installation
1. Install [CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp/releases) and [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)
3. Download [CS2-VipManager](https://github.com/1Mack/CS2-VipManager/releases/tag/V1.1)
4. Unzip the archive and upload it into csgo/addons/counterstrikesharp/plugins

# Config
The config is created automatically. Path: csgo/addons/counterstrikesharp/configs/plugins/VipManager
```
{
  "DatabaseHost": "YourDatabaseHost",
  "DatabasePort": 3306,
  "DatabaseUser": "User",
  "DatabasePassword": "Password",
  "DatabaseName": "DBName",
  "Prefix": "[VipManager]",
  "VipTestTime": 10,
  "VipTestGroup": "#css/vip",
  "CooldownRefreshCommandSeconds": 60,
  "ConfigVersion": 2
}
```

If you don't want vip test system, just set "VipTestTime" to 0

# Commands
`css_vm_remove [steamid64] [group]` - remove an admin. The `#css/admin` group is required for use.<br />
`css_vm_add [steamid64] [group]` - adds an admin. The `#css/admin` group is required for use.<br />
`css_vm_reload` - reloads the configuration. The `#css/admin` group is required for use.<br />
`css_vm_status` - check your admins roles time left. The `#css/vip` group is required for use.<br />
`css_vm_test` - claim a vip test for "X" minutes.<br />

