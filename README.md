# CS2 VIP Manager
Plugin for CS2 that stores admins in mysql and apply permissions on the game

## Installation
1. Install **[CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)** and **[Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)**;
3. Download **[CS2-VipManager](https://github.com/1Mack/CS2-VipManager/releases)**;
4. Unzip the archive and upload it into **`csgo/addons/counterstrikesharp/plugins`**;

## Config
The config is created automatically. ***(Path: `csgo/addons/counterstrikesharp/configs/plugins/VipManager`)***
```
{
  "Version": 6,
  "CooldownRefreshCommandSeconds": 60,
  "DateFormat": "dd/MM/yyyy HH:mm:ss",
  "TimeZone": -3,
  "ShowWelcomeMessageConnectedPublic": true,
  "ShowWelcomeMessageConnectedPrivate": true,
  "ShowWelcomeMessageDisconnectedPublic": true,
  "Database": {
    "Host": "",
    "Port": 3306,
    "User": "",
    "Password": "",
    "Name": "",
    "PrefixVipManager": "vip_manager",
    "PrefixTestVip": "vip_manager_testvip",
    "PrefixGroups": "vip_manager_groups"
  },
  "VipTest": {
    "VipTestTime": 10, // 0 - disabled
    "VipTestGroup": "vip" // empty - disabled
  },
  "Commands": {
    "AddPrefix": "vm_add",
    "AddPermission": "@css/root",
    "RemovePrefix": "vm_remove",
    "RemovePermission": "@css/root",
    "ReloadPrefix": "vm_reload",
    "ReloadPermission": "@css/root",
    "TestPrefix": "vm_test",
    "TestPermission": "",
    "StatusPrefix": "vm_status",
    "StatusPermission": "@css/reservation"
  },
  "Groups": {
    "Enabled": true,
    "OverwriteMainFile": true // True = will overwrite admins_group.json located at addons/counterstrikesharp/configs
  },
  "ConfigVersion": 6
}
```

## Commands
- **`css_vm_add [steamid64] [group] [time (minutes) | 0 (permanent)]`** - Adds an admin; **(`#css/admin` group is required for use)**
- **`css_vm_remove [steamid64] [group]`** - Remove an admin; **(`#css/admin` group is required for use)**
- **`css_vm_reload`** - Reloads the configuration; **(`#css/admin` group is required for use)**
- **`css_vm_status`** - Check your admins roles time left; **(`#css/vip` group is required for use)**
- **`css_vm_test`** - Claim a vip test for "X" minutes;
