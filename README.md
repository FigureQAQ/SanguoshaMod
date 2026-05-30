# SanguoshaMod

三国杀主题的 Slay the Spire 2 玩法模组。模组将“杀、闪、桃、酒”、装备槽位、角色杀附魔和三国式技能牌接入 STS2 的战斗节奏，并为不同角色准备了专属基础牌图、卡牌边框和能量样式。

[最新 Release：v0.1.6-multiplayer](https://github.com/FigureQAQ/SanguoshaMod/releases/tag/v0.1.6-multiplayer)

## 游戏内截图

README 展示图只使用游戏内真实截图或从真实截图裁切出的卡牌，不再使用离线工具生成的完整卡面预览。

![赤壁猛将角色选择](docs/images/real-game/character-select-ironclad.jpg)

![战斗中的三国杀基础牌](docs/images/real-game/combat-hand-ironclad.jpg)

![赤壁猛将牌库中的真实卡牌样式](docs/images/real-game/library-ironclad.jpg)

![骨妹牌库中的真实卡牌样式](docs/images/real-game/library-necrobinder.jpg)

### 升级卡牌样式

| 杀+ | 闪+ | 桃+ | 酒+ | 遗计+ | 诸葛连弩+ |
| --- | --- | --- | --- | --- | --- |
| <img src="docs/images/real-game/cards/sha-upgraded.png" width="150"> | <img src="docs/images/real-game/cards/shan-upgraded.png" width="150"> | <img src="docs/images/real-game/cards/tao-upgraded.png" width="150"> | <img src="docs/images/real-game/cards/jiu-upgraded.png" width="150"> | <img src="docs/images/real-game/cards/yiji-upgraded.png" width="150"> | <img src="docs/images/real-game/cards/zhuge-upgraded.png" width="150"> |

## 玩法内容

- 基础牌包括“杀、闪、桃、酒”，并为五个角色绘制不同风格的角色基础牌。
- 攻击牌、技能牌、能力牌和装备牌已接入游戏卡池。
- 装备牌分为武器、防具、坐骑、宝物四个槽位；同槽位只保留一件，新装备会顶掉旧装备并正确移除旧装备提供的属性。
- 五个角色拥有三国化命名、初始遗物和杀附魔机制。
- 依赖 `STS2-RitsuLib`，当前项目引用版本为 `0.3.6`。

## 角色特色

| 角色 | 杀附魔 | 核心节奏 |
| --- | --- | --- |
| 赤壁猛将 | 火杀 | 追加火焰伤害，低生命时爆发更高。 |
| 锦囊夜行 | 毒杀 | 叠中毒，攻击中毒敌人时获得额外节奏。 |
| 机关卧龙 | 雷杀 | 连锁电击并积累雷力，满层后爆发全体伤害。 |
| 青囊魂医 | 灾厄杀 | 用治疗、魂值和灾厄压制敌人。 |
| 汉室仁主 | 储能杀 | 用号令、星辉和储能把单牌收益转成团队节奏。 |

## 联机安装

推荐所有联机玩家使用同一个 release 标签或同一份发布压缩包，避免卡池、数值和逻辑不一致。

1. 关闭 Slay the Spire 2。
2. 将发布包解压到 Slay the Spire 2 游戏根目录。
3. 解压后应得到 `mods\sanguosha` 和 `mods\STS2-RitsuLib` 两个文件夹。
4. 如果已经有 `STS2-RitsuLib`，覆盖同名文件夹即可，不要保留两个 `STS2-RitsuLib`，否则游戏会报重复模组 id。
5. 联机玩家需要使用同一份压缩包内容。

## 本地构建

1. 复制 `local.props.example` 为 `local.props`。
2. 将 `Sts2Dir` 改为本机 Slay the Spire 2 安装路径。
3. 构建：

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build .\SanguoshaMod.csproj -c Release --no-restore
```

构建目标会把 DLL、manifest、卡牌头像和装备图标复制到配置的 `mods\sanguosha` 目录。运行时中文文本由 DLL 内的本地化补丁提供。

## 卡牌资源生成

离线资源生成命令：

```powershell
npm run cardgen:all
```

常用单项命令：

```powershell
npm run cardgen
npm run cardgen:core
npm run cardgen:tricks
npm run cardgen:role-basic
npm run cardgen:export
```

这些命令生成的是开发用卡图、卡面预览和导出资源，不等同于游戏内真实卡牌渲染。不要直接把工具生成的完整牌面预览作为 GitHub README 的游戏截图。

## 添加真实截图

继续给 README 添加图片时，请把游戏内真实截图放到 `docs/images/real-game/`，再用相对路径引用。

推荐截图内容：

- 战斗中的手牌截图，能看到不同角色边框和能量样式。
- 卡牌奖励或牌库截图，能看到技能牌、能力牌和装备牌分类。
- 角色基础牌截图，能看到杀、闪、桃、酒和属性杀的真实游戏内样式。

## 文档

- 装备说明：`docs/EQUIPMENT_CARDS_2026-05-29.md`
- 角色附魔：`docs/CHARACTER_INFUSIONS_2026-05-29.md`
- 平衡记录：`docs/BALANCE_PASS_2026-05-30.md`
- 杀附魔节奏：`docs/SHA_INFUSION_BALANCE_2026-05-29.md`
- 仁王盾重做：`docs/REN_WANG_REWORK_2026-05-29.md`
- 机关卧龙星辉修正：`docs/DEFECT_THUNDER_STARS_FIX_2026-05-29.md`
- RitsuLib 升级记录：`docs/RITSULIB_UPGRADE_2026-05-29.md`
- 实现审计：`docs/IMPLEMENTATION_AUDIT_2026-05-30.md`
