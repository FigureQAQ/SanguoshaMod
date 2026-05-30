# 联机卡牌同步与事件兼容修复 2026-05-29

## 问题

联机游玩时出现“卡牌数据不同步”，并且部分原版遗物/事件仍按原版牌池、打击、防御来处理。典型表现包括：

- `巨大卷轴` 等“添加卡牌”事件候选为空或无法正常加入三国杀卡牌。
- `Amalgamator` 仍尝试合并原版 `Strike` / `Defend`，没有识别三国杀的 `杀` / `闪`。
- 牌面图片在部署后可能因为 DLL 临时加载路径不同而找不到 `card_art` 资源。

## 修复内容

- `ModelDb.AllCards` 改为稳定的完整三国杀卡牌数据池，包含隐藏生成牌 `ZhangBaShaCard`。
- 奖励、商店、发现等可获得卡牌池只保留可收集牌，排除 `ZhangBaShaCard` 这类临时生成牌。
- 所有卡牌池替换结果按类型、稀有度、类名稳定排序，降低联机双方枚举顺序漂移导致的同步风险。
- `CardCreationOptions.GetPossibleCards` 增加空候选兜底：如果原版事件的过滤器把候选筛空，会回退到三国杀可收集牌池，避免同类“添加卡牌”逻辑再次失效。
- 修复卡牌头像加载路径：除 DLL 所在目录外，同时尝试 `res://mods/sanguosha/card_art`、游戏根目录和进程目录下的模组路径。

## 事件与遗物适配

- `巨大卷轴`：改为从三国杀可收集牌池生成 3 张候选，选择 1 张加入牌组并显示预览。
- `奥术卷轴`：改为添加 1 张已升级的三国杀稀有牌。
- `铅镇纸`：改为从 2 张三国杀牌中选择 1 张加入牌组。
- `海玻璃`：改为展示三国杀普通、罕见、稀有候选，最多选择 3 张加入牌组。
- `脑蛭` 的分享知识、`感染机器人` 的研究/触碰核心、`奶酪房` 的大快朵颐、`无尽传送带` 的炸鳗鱼：均改为使用三国杀牌池生成和加入卡牌。
- `Amalgamator`：出现条件和合成逻辑改为识别 `杀` / `闪`。合并 2 张 `杀` 会得到新的 `杀`，合并 2 张 `闪` 会得到新的 `闪`，不再依赖原版 `Strike` / `Defend` 标签。
- `LargeCapsule`：固定添加基础牌时改为添加 `杀` / `闪`，不再添加原版打击/防御。

## 2026-05-30 追加修复

- `酒`：运行时本地化与 `cards.json` 统一为当前效果，不再引用旧的抽牌变量；效果改为下一张攻击牌伤害翻倍，升级后移除消耗。
- `StrikeDummy` / `FakeStrikeDummy`：行为和文案改为识别 `杀`，并补齐遗物 flavor，避免界面残留“打击木偶”。
- `Amalgamator`：补上真实选项 key `AMALGAMATOR.pages.INITIAL.options.*`，事件初始选项不再显示“融合打击/防御”。
- `SpiralingWhirlpool`：观察选项改为从基础 `杀` / `闪` 中选择并附魔，不再要求原版 `Strike` / `Defend`。
- `FastenPower`、`GhostSeed`、`LeafyPoultice`、`NutritiousSoup`、`PandorasBox`、`NeowsTalisman`、`LargeCapsule`：补齐杀/闪行为或显示兼容，清理原版打击/防御文案残留。
- Release 构建验证：`dotnet build -c Release --no-restore` 通过，0 警告 / 0 错误。

## 联机安装要求

- 双方必须使用同一个 Release zip。
- 安装前建议删除旧目录：
- `mods/sanguosha`
- `mods/STS2-RitsuLib`
- 再把新版 zip 解压到游戏根目录，确保只存在一份 `STS2-RitsuLib`。

当前模组版本：`0.1.2`。
