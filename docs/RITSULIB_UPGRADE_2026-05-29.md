# 2026-05-29 RitsuLib 0.3.5 升级记录

## 升级内容

- 项目 NuGet 引用从 `STS2.RitsuLib` `0.3.0` 升级到 `0.3.5`。
- 游戏运行时目录 `E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\STS2-RitsuLib` 已覆盖为 v0.3.5 release 包。
- 覆盖前已备份旧运行时到 `E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\STS2-RitsuLib.backup-20260529-183759`。

## 兼容检查

- 已执行 `dotnet restore`。
- 已执行 Release 构建，结果为 0 警告、0 错误。
- 已检查当前代码没有使用 v0.3.5 标记过时的 string 版 `CardKeyword` / `CardTag` / `CardPile` API；当前卡牌关键词使用的是枚举值，例如 `CardKeyword.Exhaust`。

## 关联收益

- v0.3.5 修复部分时点限制，后续装备 UI、目标状态读取、右键交互和卡牌变形监听更稳。
- 当前模组暂未接入新增的 `CardTransform` 监听和扩展右键交互接口，可作为后续 UI/牌库工具优化点。
