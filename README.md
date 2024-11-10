# 红色警戒2 重聚未来 1.5 开发者文档

基于 **CNCNET** 。

## 原理速览

重聚未来（以下简称重聚）的原理非常简单：替换根目录下的文件达成不同**任务**和 **Mod** 。

------

***<u>重聚约定：</u>***

> - **RUMIX.MIX**
>
>   存放需要注入的ini，比如一些修正代码之类。这个MIX不会动。
>
> - **CORE.MIX**
>
>   里面有两个 ini（ rules 和 art ），原版的 **CORE.MIX** 让rules读rules.ini，尤复的 **CORE.MIX** 让rules读**rulesmd.ini**。art 同理。
>
> - **EXPANDMD01.MIX**
>
>   存放 **MOD **文件。
>
> - **MISSION.MIX**
>
>   存放任务地图文件。
>
> - **SKIN.MIX**
>
>   存放皮肤文件。

> - **gamemd.exe**
>
>   官方的gamemd.exe文件，用来给**Ares**配合syringe.exe 传入 **-SPAWN** 启动游戏。
>
> - **gamemd-spawn.exe**
>
>   重聚修改的 gamemd.exe ，传入 **-SPAWN** 以启动 **原生的 不使用扩展的** 游戏。 。
>
> - **gamemd-np.exe**
>
>   修改np的 gamemd.exe ，传入 **-SPAWN **以启动 **NP** 的 MOD 。

例如：

​		同一任务包使用不同 **MOD** 玩，更改 **MOD.MIX** 即可。

​		同一任务包使用不同扩展玩，使用不同的 **gamemd** 启动即可。

## 添加任务&任务包

### Maps\Cp\Battle*.ini

在这里注册战役和任务包，不注册不会在客户端里显示。

[Battles]下注册单个任务，等号左右都不能有重复的。

[MissionPack]下注册任务包，等号左右都不能有重复的。

[任务ID]

Scenario = 使用的任务文件。全大写。可以跟相对路径，不过路径长会读不到。

Description = 任务名称。

LongDescription = 任务简报

BuildOffAlly = 能否在盟友基地旁造东西。

Difficulty = 难度。

DefaultMod = 默认使用的MOD。比如原版任务应该默认用原版玩。

## 添加多人地图&游戏模式

### Maps/Multi/MPMaps*.ini

在这里注册多人地图和游戏模式。

[GameModes]下注册游戏模式。恩...实际上这里一般是工具生成的。

Waypoint* = 路径点位置

Description = 名称

MinPlayers = 最小玩家数

GameModes = 支持的游戏模式

Author = 作者

Size = 实际大小

EnforceMaxPlayers = 是否使用MaxPlayers来限制地图的玩家数量

[MultiMaps]下注册多人地图。



## 添加Mod&AI

### Mod&AI/Mod&AI*.ini

在这里添加Mod或AI。

[Mod]下注册Mod，等号左右都不能有重复的。

[AI]下注册AI，等号左右都不能有重复的。



