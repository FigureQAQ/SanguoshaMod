# 2026-05-30 技能与数值收敛记录

参考文档：`C:/Users/Administrator/Desktop/三国杀Mod技能与数值优化建议_2026-05-30.md`

本轮重点不是继续堆强度，而是限制「免费牌、抽牌、回能、追加伤害」在同一条链路里无限叠加，降低多人模式数据不同步和单回合滚雪球风险。

## 已调整内容

- 赤壁猛将：火杀基础追加仍为 2 点；半血以下的 4 点追加和易伤只在每回合首次火杀命中时触发。
- 锦囊夜行：毒杀改为基础 2 层中毒；若目标原本没有中毒，额外 +1 层。每回合首次打出锦囊牌会使下一张毒杀额外 +1 层中毒。
- 机关卧龙：雷势爆发改为达到 4 点时触发，且每回合最多爆发 1 次；移除了旧的 3 点雷势全体伤害逻辑。
- 汉室仁主：星辉诏令每回合最多触发 1 次；诏令不再额外提升号令。储能杀每 2 点储能仍会回能，但每回合只有首次储能转换额外抽牌。
- 铁索连环：溅射伤害不再触发杀命中、武魂附魔、无双、武器追击等后续效果。
- 无双：改为每回合前 2 次杀命中追加 3 点伤害，升级后前 3 次；追加伤害不视为杀命中。
- 连营：触发后不再直接使牌 0 费，改为抽牌后让 1 张杀或锦囊本回合费用降低 1。
- 诸葛连弩：失去时抽牌上限限制为 2 张；升级保留每回合 2 张杀变 0 费，不再额外降本体费用。
- 酒：基础版不再同时提供力量、免费杀和兜底抽牌；现在改为使下一张攻击牌造成的伤害翻倍，升级后移除消耗。
- 玉玺：费用提高到 2；入场移除力量收益，保留能量、抽牌和每回合能量；失去时只抽牌。
- 孟德新书：每回合首次锦囊触发后，改为使 1 张杀或锦囊费用降低 1，不再直接让多张牌免费。
- 稀有度下调：观星、龙胆、奇袭、贯石斧、青釭剑、顺手牵羊、制衡由稀有调整为罕见。

## 同步文件

- `LiveScripts/Characters/SanguoshaCharacterSkills.cs`
- `LiveScripts/Cards/JiuCard.cs`
- `LiveScripts/Cards/ZhuGeCard.cs`
- `LiveScripts/Cards/GeneralSkillCards.cs`
- `LiveScripts/Cards/ExpansionCards.cs`
- `LiveScripts/Cards/MoreSanguoshaCards.cs`
- `sanguosha/localization/zhs/cards.json`
- `sanguosha/localization/zhs/powers.json`
- `sanguosha/localization/zhs/relics.json`
- `tools/cardgen/*.json`
- `card_art/cards.generated.json`

## 验证

- `dotnet build E:/DGodotProjectsTestCSharp/SanguoshaMod.csproj -c Release --no-restore` 通过。
- 当前 Release 构建为 0 个 warning、0 个 error。
