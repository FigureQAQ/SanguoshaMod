# 0.1.15 发布检查记录

日期：2026-05-31

## 版本

- 模组版本从 `0.1.14` 提升到 `0.1.15`。
- 推荐联机包为 `SanguoshaMod-0.1.15-multiplayer.zip`。
- 依赖 `STS2-RitsuLib` 同步为 `0.3.9`，最低游戏版本要求同步到 `0.106.1`。

## 内容变更

- 扩容普通卡池，新增稳定回费、烧牌、易伤转力量和多人互动牌。
- 新增每个角色的 Boss 专属能力牌，只在 Boss 奖励池按角色出现。
- 卡牌奖励和商店过滤掉 `杀`、`闪`、`桃`、`酒` 基础牌；保留这些牌在初始牌组、特殊生成和角色机制中的用途。
- 新增多人互动收益：桃园结义、五谷丰登、发红包和好运显示能力。
- 补齐新增卡面图并导出运行时 `card_art` 资源；本轮未使用 PinAI。
- 统一并压缩游戏内卡牌文案，控制抽牌、回费、消耗和增伤数量。

## Bug 检查

- `酒` 的下一张攻击牌增伤已能作用于储君生成的 `王者之剑`。
- `王者之剑` 维持 0 费、保留、消耗，且使用后清理酒的伤害倍率。
- `兵粮寸断`、`乐不思蜀`、`闪电` 延迟判定均保留回合开始结算路径。
- 修正 `兵粮寸断` 的状态牌拦截范围：只拦截被兵粮命中的敌人向玩家塞入的状态/诅咒/任务牌，避免多人模式中误伤其他玩家或其他敌人的正常生成牌流程。
- Boss 专属能力牌不会进入普通奖励或商店；普通奖励池仍保留多张稳定回费牌。

## 资源检查

- `npm run cardgen:export` 输出 `Exported 95 card portrait(s) to card_art`。
- 导出过程没有出现 placeholder portrait 警告。
- 关键新增图面已存在：`hongbao`、`kurou`、`jixing`、`fenying`、`pozhu`、`bathofblood`、`nightfallscheme`、`thundermandate`、`soulhealerform`、`imperialedict`。

## 验证

- JSON 校验通过：
  - `sanguosha/localization/zhs/cards.json`
  - `sanguosha/localization/zhs/powers.json`
  - `sanguosha/localization/zhs/relics.json`
  - `sanguosha/localization/zhs/characters.json`
  - `tools/cardgen/core-cards.json`
  - `tools/cardgen/equipment-cards.json`
  - `tools/cardgen/trick-cards.json`
  - `card_art/cards.generated.json`
- Release 构建通过：0 warning, 0 error。

