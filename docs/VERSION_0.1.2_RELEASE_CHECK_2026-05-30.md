# 0.1.2 发布检查记录

日期：2026-05-30

## 版本

- 模组版本从 `0.1.1` 提升到 `0.1.2`。
- 推荐联机包改为 `SanguoshaMod-0.1.2-multiplayer.zip`。

## 卡面图片检查

- 已按 `SanguoshaCardCatalog` 注册列表检查 57 张卡牌。
- 57 / 57 张卡牌均存在 `card_art/*.png` 实体图片。
- 已补充隐藏生成牌 `ZhangBaShaCard` 到 `card_art/cards.generated.json`。
- `WangJianShaCard` 与 `ZhangBaShaCard` 均复用 `sha.png` 作为隐藏生成牌牌面，避免缺图。

## Bug 检查

- 搜索 `ENERGY`、`[ENERGY`、`NOPE/nope` 未发现残留。
- 搜索打击/防御文本残留时，仅发现运行时兼容补丁中引用官方方法名 `CombineStrikes`、`CombineDefends`、`GetStrikeForCharacter`、`GetDefendForCharacter`，这些是必须 patch 的原始 API 名称，不是显示文本。
- 追加修复 `酒` 的运行时文案，移除旧抽牌变量引用，并同步 `cards.json`。
- 补齐 `StrikeDummy`、`FakeStrikeDummy`、`Amalgamator`、`SpiralingWhirlpool`、`FastenPower`、`GhostSeed`、`LeafyPoultice`、`NutritiousSoup`、`PandorasBox`、`NeowsTalisman`、`LargeCapsule` 对 `杀` / `闪` 的行为或显示兼容。
- Release 构建通过，0 警告 / 0 错误。

## 同步文件

- `mod_manifest.json`
- `README.md`
- `docs/MULTIPLAYER_SYNC_FIX_2026-05-29.md`
- `card_art/cards.generated.json`
- `release/SanguoshaMod-0.1.2-multiplayer.zip`
