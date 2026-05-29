# SanguoshaMod

三国杀主题的 Slay the Spire 2 玩法模组。当前版本包含三国杀风格基础牌、锦囊牌、能力牌、装备牌、角色技能、初始遗物、卡牌插画、装备栏图标和中文本地化。

## 当前内容

- 55 张已接入卡牌资源。
- 20 张装备牌，分为武器、防具、坐骑、宝物四个槽位；同槽位只能装备一件，新装备会顶掉旧装备并触发旧装备的“失去时”效果。
- 五个角色均有三国化命名、初始遗物和杀附魔机制。
- 卡牌头像来自 `card_art`，装备栏图标来自 `power_icons` 与 `power_icons_big`。
- 依赖 `STS2-RitsuLib`，当前项目引用版本为 `0.3.5`。

## 联机安装

推荐使用发布压缩包 `SanguoshaMod-0.1.0-multiplayer.zip`：

1. 关闭 Slay the Spire 2。
2. 将压缩包解压到 Slay the Spire 2 游戏根目录。
3. 解压后应得到 `mods\sanguosha` 和 `mods\STS2-RitsuLib` 两个文件夹。
4. 如果已经有 `STS2-RitsuLib`，覆盖同名文件夹即可，不要保留两个 `STS2-RitsuLib`，否则游戏会报重复模组 id。
5. 联机玩家需要使用同一份压缩包内容，避免卡池和逻辑不一致。

## 本地构建

1. 复制 `local.props.example` 为 `local.props`。
2. 将 `Sts2Dir` 改为本机 Slay the Spire 2 安装路径。
3. 构建：

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build .\SanguoshaMod.csproj -c Release --no-restore
```

构建目标会把 DLL、manifest、本地化文件、卡牌头像和装备图标复制到配置的 `mods\sanguosha` 目录。

## 卡牌资源生成

```powershell
npm run cardgen:all
```

该命令会重新生成牌面预览、游戏头像和 `card_art/cards.generated.json`。

## 文档

- 装备说明：`docs/EQUIPMENT_CARDS_2026-05-29.md`
- 角色附魔：`docs/CHARACTER_INFUSIONS_2026-05-29.md`
- 平衡记录：`docs/BALANCE_PASS_2026-05-29.md`
- RitsuLib 升级记录：`docs/RITSULIB_UPGRADE_2026-05-29.md`
