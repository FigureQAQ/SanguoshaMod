# 0.1.16 发布检查记录

日期：2026-06-01

## 版本

- 模组版本从 `0.1.15` 提升到 `0.1.16`。
- 推荐联机包为 `SanguoshaMod-0.1.16-multiplayer.zip`。
- 依赖已升级到 `STS2-RitsuLib` `0.4.1`，最低游戏版本维持 `0.106.1`。

## Bug 修复

- 修复 `sanguosha.dll` 初始化失败：
  - 日志中的直接原因是 Harmony 在补丁 `CardPileCmd.Add(CardModel, PileType, CardPilePosition, AbstractModel, bool)` 时找不到 Prefix 参数 `source`。
  - 游戏 `0.106.1` 运行时该参数名为 `clonedBy`，Harmony 按参数名绑定导致 `PatchAll` 抛出 `HarmonyException`。
  - 兵粮寸断状态牌拦截补丁改为使用 `__0`、`__3` 等位置参数绑定，避免运行时参数名变化再次导致初始化失败。
- 收窄 `AddGeneratedCardToCombat` / `AddGeneratedCardsToCombat` 的 Harmony 目标筛选，只匹配第三个参数确实为 `Player` 的重载。
- 保留 0.1.15 中对兵粮寸断、乐不思蜀、闪电延迟判定牌的结算逻辑，不改变卡牌设计。

## 验证

- 从 `ritsulib_self_check_20260531_235529.zip` 与当前 Godot 日志定位到初始化异常堆栈。
- Release 构建通过：0 warning, 0 error。
- 构建产物已复制到本地游戏目录 `mods/sanguosha`。
- 扫描 Harmony Prefix 后，未发现同类 `source` 参数名绑定风险。
