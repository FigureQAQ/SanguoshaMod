# 2026-05-30 杀牌插画重绘

本轮将不同类型的「杀」按玩法身份拆分出独立插画资源，避免所有生成杀继续复用同一张基础杀图。

## 已接入游戏读取

- `ShaCard`：基础杀，使用 `card_art/sha.png`。
- `ZhangBaShaCard`：蛇矛杀，使用 `card_art/zhangbasha.png`。
- `WangJianShaCard`：王剑杀，使用 `card_art/wangjiansha.png`，与储能杀使用不同图。

## 属性杀动态牌面

- 赤壁猛将手中的 `ShaCard`：火杀，使用 `card_art/sha_fire.png`。
- 锦囊夜行手中的 `ShaCard`：毒杀，使用 `card_art/sha_poison.png`。
- 机关卧龙手中的 `ShaCard`：雷杀，使用 `card_art/sha_thunder.png`。
- 汉室仁主手中的 `ShaCard`：储能杀，使用 `card_art/sha_stored.png`，当前采用用户指定的金色王剑插画。
- 青囊魂医手中的 `ShaCard`：灾厄杀，使用 `card_art/sha_calamity.png`。

属性杀仍然是同一个 `ShaCard` 实体，运行时根据持有者角色动态选择牌面；没有持有者或卡牌图鉴中仍回退为基础杀图。

## 资源同步

- 同步更新 `LiveScripts/Cards/SanguoshaCard.cs`，让蛇矛杀与王剑杀读取专属 slug。
- 同步更新 `LiveScripts/Cards/SanguoshaCard.cs`，让基础杀根据当前持有者角色读取属性杀图片。
- 同步更新 `card_art/cards.generated.json`，让隐藏生成牌的元数据指向专属图片。
- 同步复制源图到 `tools/cardgen/input/core-art/`，避免后续批量生成牌面时丢失素材。
- `sha_stored.png` 已按用户指定替换为金色王剑图；`wangjiansha.png` 使用另一张金色号令斩图，避免两者重复。
- 已校验 `sha.png`、`sha_fire.png`、`sha_poison.png`、`sha_thunder.png`、`sha_stored.png`、`sha_calamity.png`、`zhangbasha.png`、`wangjiansha.png` 的 SHA256 哈希均不重复。
