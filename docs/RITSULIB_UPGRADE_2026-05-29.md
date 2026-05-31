# 2026-05-29 RitsuLib 升级记录

## 升级内容

- 项目 NuGet 引用从 `STS2.RitsuLib` `0.3.0` 升级到 `0.3.5`。
- 游戏运行时目录 `E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\STS2-RitsuLib` 已覆盖为 v0.3.5 release 包。
- 覆盖前已备份旧运行时到 `E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\STS2-RitsuLib.backup-20260529-183759`。
- 2026-05-29 继续将项目 NuGet 引用升级到 `STS2.RitsuLib` `0.3.6`。
- 游戏运行时目录 `E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\STS2-RitsuLib` 已确认 manifest 版本为 `0.3.6`。
- 2026-05-31 将项目 NuGet 引用升级到 `STS2.RitsuLib` `0.3.8`。
- 发布包内置的 `mods\STS2-RitsuLib` 同步替换为 v0.3.8 `variant-pack`。

## 兼容检查

- 已执行 `dotnet restore`。
- 已执行 Release 构建，结果为 0 警告、0 错误。
- 已检查当前代码没有使用 v0.3.5 标记过时的 string 版 `CardKeyword` / `CardTag` / `CardPile` API；当前卡牌关键词使用的是枚举值，例如 `CardKeyword.Exhaust`。
- v0.3.6 release note 包含 `ModInterop` 类型解析、`AssemblyInterop`、临时 Power 处理、`CardTransform` 异步监听等更新；当前代码不需要立刻改 API，但临时 Power 和互操作修复有助于运行时稳定。
- v0.3.8 release note 包含滚动容器重复报错修复、`ModelID` 查重逻辑优化、RitsuLib 启动阶段耗时审计和死代码清理；当前模组代码不需要改 API。

## 关联收益

- v0.3.5 修复部分时点限制，后续装备 UI、目标状态读取、右键交互和卡牌变形监听更稳。
- 当前模组暂未接入新增的 `CardTransform` 监听和扩展右键交互接口，可作为后续 UI/牌库工具优化点。
- v0.3.6 后续可用于更稳地接入卡牌变形/替换监听，适合处理事件给牌、升级牌和联机牌池同步相关问题。
- v0.3.8 应降低启动注册查询开销，并让后续定位 RitsuLib 启动阶段问题更方便。
