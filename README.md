# CS2 VIP Manager
Plugin for CS2 that stores admins in mysql and apply permissions on the game

## Installation
1. Install **[CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)** and **[Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)**;
3. Download **[CS2-VipManager](https://github.com/1Mack/CS2-VipManager/releases/tag/V1.1)**;
4. Unzip the archive and upload it into **`csgo/addons/counterstrikesharp/plugins`**;

## Config
The config is created automatically. ***(Path: `csgo/addons/counterstrikesharp/configs/plugins/VipManager`)***
```
{
  "Version": 5,
  "Prefix": "{DEFAULT}[{GREEN}VipManager{DEFAULT}]",
  "CooldownRefreshCommandSeconds": 60,
  "WelcomeMessage": {
    "WelcomePrivate": "{DEFAULT}Welcome to the server, thanks for supporting the server being {GOLD}VIP",
    "WelcomePublic": "{DEFAULT}VIP player {GREEN}connected",
    "DisconnectedPublic": "{DEFAULT}VIP player {RED}disconnect"
  },
  "Database": {
    "Host": "",
    "Port": 3306,
    "User": "",
    "Password": "",
    "Name": "",
    "PrefixVipManager": "vip_manager",
    "PrefixTestVip": "vip_manager_testvip"
  },
  "VipTest": {
    "VipTestTime": 10,
    "VipTestGroup": "#css/vip"
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
  "Messages": {
    "MissingCommandPermission": "{DEFAULT}You don\u0027t have permission to use this command",
    "AlreadyRegistryWithSteamidAndGroup": "{DEFAULT}There is already a registry with this steamid and group!",
    "AdminAddSuccess": "{DEFAULT}Admin has been added",
    "InternalError": "{DEFAULT}There was an internal error",
    "NoAdminWithSteamidAndGroup": "{DEFAULT}There is no admin with this steamID and group!",
    "AdminDeleteSuccess": "{DEFAULT}Admin has been deleted!",
    "AdminReloadSuccess": "{DEFAULT}Admins reloaded successfully!",
    "CoolDown": "{DEFAULT}You are on a cooldown...wait {COOLDOWNSECONDS} seconds and try again!",
    "CommandBlocked": "{DEFAULT}This command is blocked by the server!",
    "TestVipAlreadyClaimed": "{DEFAULT}Vou have already claimed your test vip!",
    "AlreadyNormalVip": "{DEFAULT}Vou have already a normal vip!",
    "TestVipActivated": "{DEFAULT}You have activated your VIP successfully for {VIPTESTTIME} minutes",
    "NoAdminsRole": "{DEFAULT}You don\u0027t have any admin roles!",
    "RoleNotFound": "{DEFAULT}Role not found. Rejoin the server and try again.",
    "Status": "{DEFAULT}------------------------------{BREAKLINE}Role: {GROUP}{BREAKLINE}Created At: {TIMESTAMP}{BREAKLINE}End At: {ENDDATE}{BREAKLINE}------------------------------"
  },
  "ConfigVersion": 5
}
```
If you don't want vip test system, just set **"VipTestTime"** to **0**.

## Commands
- **`css_vm_add [steamid64] [group] [time (minutes)]`** - Adds an admin; **(`#css/admin` group is required for use)**
- **`css_vm_remove [steamid64] [group]`** - Remove an admin; **(`#css/admin` group is required for use)**
- **`css_vm_reload`** - Reloads the configuration; **(`#css/admin` group is required for use)**
- **`css_vm_status`** - Check your admins roles time left; **(`#css/vip` group is required for use)**
- **`css_vm_test`** - Claim a vip test for "X" minutes;
