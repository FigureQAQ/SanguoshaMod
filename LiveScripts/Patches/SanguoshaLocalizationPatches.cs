using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace sanguosha.Patches;

internal static class SanguoshaLocalization
{
    private sealed record Entry(string Table, string Text);

    private static readonly IReadOnlyDictionary<string, string> PowerKeyAliases = new Dictionary<string, string>
    {
        ["SANGUOSHA_ZHU_GE_DISPLAY_POWER"] = "SANGUOSHA_POWER_ZHU_GE_DISPLAY_POWER",
        ["SANGUOSHA_ZHANG_BA_DISPLAY_POWER"] = "SANGUOSHA_POWER_ZHANG_BA_DISPLAY_POWER",
        ["SANGUOSHA_QING_GANG_DISPLAY_POWER"] = "SANGUOSHA_POWER_QING_GANG_DISPLAY_POWER",
        ["SANGUOSHA_GUAN_SHI_DISPLAY_POWER"] = "SANGUOSHA_POWER_GUAN_SHI_DISPLAY_POWER",
        ["SANGUOSHA_HAN_BING_DISPLAY_POWER"] = "SANGUOSHA_POWER_HAN_BING_DISPLAY_POWER",
        ["SANGUOSHA_QI_LIN_DISPLAY_POWER"] = "SANGUOSHA_POWER_QI_LIN_DISPLAY_POWER",
        ["SANGUOSHA_GU_DING_DISPLAY_POWER"] = "SANGUOSHA_POWER_GU_DING_DISPLAY_POWER",
        ["SANGUOSHA_GAN_JIANG_MO_YE_DISPLAY_POWER"] = "SANGUOSHA_POWER_GAN_JIANG_MO_YE_DISPLAY_POWER",
        ["SANGUOSHA_BAI_YIN_DISPLAY_POWER"] = "SANGUOSHA_POWER_BAI_YIN_DISPLAY_POWER",
        ["SANGUOSHA_REN_WANG_DISPLAY_POWER"] = "SANGUOSHA_POWER_REN_WANG_DISPLAY_POWER",
        ["SANGUOSHA_BA_GUA_DISPLAY_POWER"] = "SANGUOSHA_POWER_BA_GUA_DISPLAY_POWER",
        ["SANGUOSHA_TENG_JIA_DISPLAY_POWER"] = "SANGUOSHA_POWER_TENG_JIA_DISPLAY_POWER",
        ["SANGUOSHA_CHI_TU_DISPLAY_POWER"] = "SANGUOSHA_POWER_CHI_TU_DISPLAY_POWER",
        ["SANGUOSHA_DA_WAN_DISPLAY_POWER"] = "SANGUOSHA_POWER_DA_WAN_DISPLAY_POWER",
        ["SANGUOSHA_DI_LU_DISPLAY_POWER"] = "SANGUOSHA_POWER_DI_LU_DISPLAY_POWER",
        ["SANGUOSHA_JUE_YING_DISPLAY_POWER"] = "SANGUOSHA_POWER_JUE_YING_DISPLAY_POWER",
        ["SANGUOSHA_YU_XI_DISPLAY_POWER"] = "SANGUOSHA_POWER_YU_XI_DISPLAY_POWER",
        ["SANGUOSHA_MU_NIU_DISPLAY_POWER"] = "SANGUOSHA_POWER_MU_NIU_DISPLAY_POWER",
        ["SANGUOSHA_TAI_PING_DISPLAY_POWER"] = "SANGUOSHA_POWER_TAI_PING_DISPLAY_POWER",
        ["SANGUOSHA_MENG_DE_XIN_SHU_DISPLAY_POWER"] = "SANGUOSHA_POWER_MENG_DE_XIN_SHU_DISPLAY_POWER",
        ["SANGUOSHA_KONG_CHENG_DISPLAY_POWER"] = "SANGUOSHA_POWER_KONG_CHENG_DISPLAY_POWER",
        ["SANGUOSHA_LONG_DAN_DISPLAY_POWER"] = "SANGUOSHA_POWER_LONG_DAN_DISPLAY_POWER",
        ["SANGUOSHA_ZHI_HENG_DISPLAY_POWER"] = "SANGUOSHA_POWER_ZHI_HENG_DISPLAY_POWER",
        ["SANGUOSHA_WU_SHUANG_DISPLAY_POWER"] = "SANGUOSHA_POWER_WU_SHUANG_DISPLAY_POWER",
        ["SANGUOSHA_LIAN_YING_DISPLAY_POWER"] = "SANGUOSHA_POWER_LIAN_YING_DISPLAY_POWER",
        ["SANGUOSHA_YI_JI_DISPLAY_POWER"] = "SANGUOSHA_POWER_YI_JI_DISPLAY_POWER",
        ["SANGUOSHA_JIAN_XIONG_DISPLAY_POWER"] = "SANGUOSHA_POWER_JIAN_XIONG_DISPLAY_POWER",
        ["SANGUOSHA_GUI_CAI_DISPLAY_POWER"] = "SANGUOSHA_POWER_GUI_CAI_DISPLAY_POWER",
        ["SANGUOSHA_IRONCLAD_SKILL_DISPLAY_POWER"] = "SANGUOSHA_POWER_IRONCLAD_SKILL_DISPLAY_POWER",
        ["SANGUOSHA_SILENT_SKILL_DISPLAY_POWER"] = "SANGUOSHA_POWER_SILENT_SKILL_DISPLAY_POWER",
        ["SANGUOSHA_DEFECT_SKILL_DISPLAY_POWER"] = "SANGUOSHA_POWER_DEFECT_SKILL_DISPLAY_POWER",
        ["SANGUOSHA_NECROBINDER_SKILL_DISPLAY_POWER"] = "SANGUOSHA_POWER_NECROBINDER_SKILL_DISPLAY_POWER",
        ["SANGUOSHA_REGENT_SKILL_DISPLAY_POWER"] = "SANGUOSHA_POWER_REGENT_SKILL_DISPLAY_POWER"
    };

    private static readonly IReadOnlyDictionary<string, Entry> Text = new Dictionary<string, Entry>
    {
        ["CARD_TYPE.SKILL"] = new("gameplay_ui", "技能牌"),
        ["TYPE_SKILL_TIP"] = new("card_library", "技能牌"),
        ["SANGUOSHA_CARD_SHA_CARD.title"] = new("cards", "杀"),
        ["SANGUOSHA_CARD_SHA_CARD.description"] = new("cards", "造成 {Damage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_SHAN_CARD.title"] = new("cards", "闪"),
        ["SANGUOSHA_CARD_SHAN_CARD.description"] = new("cards", "获得 {Block:diff()} 点格挡。"),
        ["SANGUOSHA_CARD_TAO_CARD.title"] = new("cards", "桃"),
        ["SANGUOSHA_CARD_TAO_CARD.description"] = new("cards", "回复 {Heal:diff()} 点生命并获得 {Block:diff()} 点格挡。若生命值不高于一半，额外回复 {LowHpHeal:diff()} 点生命。"),
        ["SANGUOSHA_CARD_JIU_CARD.title"] = new("cards", "酒"),
        ["SANGUOSHA_CARD_JIU_CARD.description"] = new("cards", "你的下一张攻击牌造成的伤害翻倍；该效果不会叠加。"),
        ["STRIKE_DUMMY.title"] = new("relics", "杀木偶"),
        ["STRIKE_DUMMY.description"] = new("relics", "你的[gold]杀[/gold]额外造成[blue]{ExtraDamage}[/blue]点伤害。"),
        ["STRIKE_DUMMY.flavor"] = new("relics", "[red]这个遗物会识别三国杀的杀。[/red]"),
        ["FAKE_STRIKE_DUMMY.title"] = new("relics", "仿制杀木偶"),
        ["FAKE_STRIKE_DUMMY.description"] = new("relics", "你的[gold]杀[/gold]额外造成[blue]{ExtraDamage}[/blue]点伤害。"),
        ["FAKE_STRIKE_DUMMY.flavor"] = new("relics", "[red]这个遗物会识别三国杀的杀。[/red]"),
        ["CLAWS.description"] = new("relics", "拾起时，将你牌组中的所有[gold]杀[/gold][gold]变化[/gold]为对应的重击牌。"),
        ["CLAWS.eventDescription"] = new("relics", "将你牌组中的所有[gold]杀[/gold][gold]变化[/gold]为对应的重击牌。"),
        ["FASTEN.description"] = new("cards", "从[gold]闪[/gold]额外获得 {ExtraBlock:diff()} 点[gold]格挡[/gold]。"),
        ["FASTEN_POWER.description"] = new("powers", "从[gold]闪[/gold]额外获得[blue]4[/blue]点[gold]格挡[/gold]。"),
        ["FASTEN_POWER.smartDescription"] = new("powers", "从[gold]闪[/gold]额外获得[blue]{Amount}[/blue]点[gold]格挡[/gold]。"),
        ["SPIRALING_WHIRLPOOL.pages.INITIAL.options.OBSERVE.description"] = new("events", "为一张基础[gold]杀[/gold]或[gold]闪[/gold][gold]附魔[/gold]：[purple]涡旋[/purple]。"),
        ["AMALGAMATOR.pages.COMBINE_STRIKES.description"] = new("events", "你将两张杀熔合为一张新的杀。"),
        ["AMALGAMATOR.pages.COMBINE_DEFENDS.description"] = new("events", "你将两张闪熔合为一张新的闪。"),
        ["AMALGAMATOR.pages.INITIAL.options.COMBINE_STRIKES.title"] = new("events", "融合杀"),
        ["AMALGAMATOR.pages.INITIAL.options.COMBINE_DEFENDS.title"] = new("events", "融合闪"),
        ["AMALGAMATOR.pages.INITIAL.options.COMBINE_STRIKES.description"] = new("events", "移除[blue]2[/blue]张[gold]杀[/gold]。将[gold]{Card1}[/gold]加入你的[gold]牌组[/gold]。"),
        ["AMALGAMATOR.pages.INITIAL.options.COMBINE_DEFENDS.description"] = new("events", "移除[blue]2[/blue]张[gold]闪[/gold]。将[gold]{Card2}[/gold]加入你的[gold]牌组[/gold]。"),
        ["AMALGAMATOR.options.COMBINE_STRIKES.label"] = new("events", "熔合杀"),
        ["AMALGAMATOR.options.COMBINE_DEFENDS.label"] = new("events", "熔合闪"),
        ["AMALGAMATOR.options.COMBINE_STRIKES.description"] = new("events", "移除 2 张杀，获得 1 张新的杀。"),
        ["AMALGAMATOR.options.COMBINE_DEFENDS.description"] = new("events", "移除 2 张闪，获得 1 张新的闪。"),
        ["GHOST_SEED.description"] = new("relics", "[gold]杀[/gold]和[gold]闪[/gold]获得[gold]虚无[/gold]。"),
        ["LARGE_CAPSULE.description"] = new("relics", "拾起时，获得[blue]{Relics}[/blue]件随机[gold]遗物[/gold]。额外将一张[gold]杀[/gold]和一张[gold]闪[/gold]加入你的[gold]牌组[/gold]。"),
        ["LARGE_CAPSULE.eventDescription"] = new("relics", "获得[blue]{Relics}[/blue]件随机[gold]遗物[/gold]。额外将一张[gold]杀[/gold]和一张[gold]闪[/gold]加入你的[gold]牌组[/gold]。"),
        ["LEAFY_POULTICE.description"] = new("relics", "拾起时，[gold]变化[/gold]你的[blue]1[/blue]张[gold]杀[/gold]和[blue]1[/blue]张[gold]闪[/gold]，然后失去[blue]{MaxHp}[/blue]点最大生命。"),
        ["LEAFY_POULTICE.eventDescription"] = new("relics", "[gold]变化[/gold]你的[blue]1[/blue]张[gold]杀[/gold]和[blue]1[/blue]张[gold]闪[/gold]，然后失去[red]{MaxHp}[/red]点最大生命。"),
        ["NEOWS_TALISMAN.description"] = new("relics", "拾起时，[gold]升级[/gold]你的[blue]1[/blue]张[gold]杀[/gold]和[blue]1[/blue]张[gold]闪[/gold]。"),
        ["NEOWS_TALISMAN.eventDescription"] = new("relics", "[gold]升级[/gold]你的[blue]1[/blue]张[gold]杀[/gold]和[blue]1[/blue]张[gold]闪[/gold]。"),
        ["NUTRITIOUS_SOUP.description"] = new("relics", "拾起时，为你[gold]牌组[/gold]中的所有[gold]杀[/gold][gold]附魔[/gold]：[purple]特兹卡塔拉的余烬[/purple]。"),
        ["NUTRITIOUS_SOUP.eventDescription"] = new("relics", "为你[gold]牌组[/gold]中的所有[gold]杀[/gold][gold]附魔[/gold]：[purple]特兹卡塔拉的余烬[/purple]。"),
        ["PANDORAS_BOX.description"] = new("relics", "[gold]变化[/gold]所有[gold]杀[/gold]和[gold]闪[/gold]。"),
        ["SANGUOSHA_CARD_DUEL_CARD.title"] = new("cards", "决斗"),
        ["SANGUOSHA_CARD_DUEL_CARD.description"] = new("cards", "造成 {Damage:diff()} 点伤害 {BaseHits:diff()} 次。可消耗至多 {MaxSha:diff()} 张手牌中的杀；每消耗 1 张，额外攻击 1 次，并施加 {Vulnerable:diff()} 层易伤。"),
        ["SANGUOSHA_CARD_WU_ZHONG_CARD.title"] = new("cards", "无中生有"),
        ["SANGUOSHA_CARD_WU_ZHONG_CARD.description"] = new("cards", "可消耗手牌中的 1 张牌。抽 {Draw:diff()} 张牌；若成功消耗，获得 {Energy:diff()} 点能量并额外抽 {BonusDraw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_WU_GU_CARD.title"] = new("cards", "五谷丰登"),
        ["SANGUOSHA_CARD_WU_GU_CARD.description"] = new("cards", "获得 {Block:diff()} 点格挡。可消耗手牌中的 1 张牌。所有玩家抽 {Draw:diff()} 张牌；若成功消耗，你获得 {Energy:diff()} 点能量，所有玩家额外抽 {BonusDraw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_ZHU_GE_CARD.title"] = new("cards", "诸葛连弩"),
        ["SANGUOSHA_CARD_ZHU_GE_CARD.description"] = new("cards", "装备：武器。获得 {Strength:diff()} 点力量并生成 1 张免费杀。每回合开始时，手牌中前 {FreeSha:diff()} 张杀本回合变为 0 费；失去时按本回合已打出的杀抽牌，最多 2 张。"),
        ["SANGUOSHA_CARD_ZHANG_BA_CARD.title"] = new("cards", "丈八蛇矛"),
        ["SANGUOSHA_CARD_ZHANG_BA_CARD.description"] = new("cards", "装备：武器。抽 {Draw:diff()} 张牌。每回合一次，若你有至少 2 张其他手牌，生成 1 张 0 费蛇矛杀：可将任意两张牌当杀打出。失去时获得 1 点能量并补 1 张蛇矛杀。"),
        ["SANGUOSHA_CARD_ZHANG_BA_SHA_CARD.title"] = new("cards", "蛇矛杀"),
        ["SANGUOSHA_CARD_ZHANG_BA_SHA_CARD.description"] = new("cards", "消耗任意 2 张其他手牌，视为杀造成 {Damage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_BA_GUA_CARD.title"] = new("cards", "八卦阵"),
        ["SANGUOSHA_CARD_BA_GUA_CARD.description"] = new("cards", "装备：防具。敌人攻击你时，从抽牌堆顶判定；若为闪，获得 {Block:diff()} 点格挡并继续，直到本次攻击被完全格挡、判定次数用尽或判定牌不是闪。失去时抽牌并获得格挡。"),
        ["SANGUOSHA_CARD_REN_WANG_CARD.title"] = new("cards", "仁王盾"),
        ["SANGUOSHA_CARD_REN_WANG_CARD.description"] = new("cards", "装备：防具。获得 {Block:diff()} 点格挡。每回合首次即将受到杀或普通攻击伤害时，获得等同本次伤害量外加 {Guard:diff()} 点的格挡。失去时获得格挡并抽牌。"),
        ["SANGUOSHA_CARD_BAI_YIN_CARD.title"] = new("cards", "白银狮子"),
        ["SANGUOSHA_CARD_BAI_YIN_CARD.description"] = new("cards", "装备：防具。每回合失去不超过 {DamageCap:diff()} 点生命。失去时回复 {LeaveHeal:diff()} 点生命。"),
        ["SANGUOSHA_CARD_BING_LIANG_CARD.title"] = new("cards", "兵粮寸断"),
        ["SANGUOSHA_CARD_BING_LIANG_CARD.description"] = new("cards", "延迟判定：下个敌方回合开始时，若随机数为偶数，目标本回合不能给角色施加负面效果或塞牌。"),
        ["SANGUOSHA_CARD_CHI_TU_CARD.title"] = new("cards", "赤兔"),
        ["SANGUOSHA_CARD_CHI_TU_CARD.description"] = new("cards", "装备：坐骑。获得 {Strength:diff()} 点力量和 {Energy:diff()} 点能量。失去时获得 1 点能量，并使手牌中至多 {FreeSha:diff()} 张杀本回合变为 0 费。"),
        ["SANGUOSHA_CARD_DA_WAN_CARD.title"] = new("cards", "大宛"),
        ["SANGUOSHA_CARD_DA_WAN_CARD.description"] = new("cards", "装备：坐骑。获得 {Strength:diff()} 点力量。你的杀额外造成 {BonusDamage:diff()} 点伤害。失去时强化下一张杀。"),
        ["SANGUOSHA_CARD_DI_LU_CARD.title"] = new("cards", "的卢"),
        ["SANGUOSHA_CARD_DI_LU_CARD.description"] = new("cards", "装备：坐骑。获得 {Dexterity:diff()} 点敏捷。"),
        ["SANGUOSHA_CARD_YU_XI_CARD.title"] = new("cards", "玉玺"),
        ["SANGUOSHA_CARD_YU_XI_CARD.description"] = new("cards", "装备：宝物。获得 {Energy:diff()} 点能量并抽 {Draw:diff()} 张牌。每回合开始时额外获得 1 点能量；失去时抽牌。"),
        ["SANGUOSHA_CARD_MU_NIU_CARD.title"] = new("cards", "木牛流马"),
        ["SANGUOSHA_CARD_MU_NIU_CARD.description"] = new("cards", "装备：宝物。获得 {Energy:diff()} 点能量并抽 {Draw:diff()} 张牌。每回合前 {TrickDraws:diff()} 次打出技能牌后，抽 1 张牌。"),
        ["SANGUOSHA_CARD_TAI_PING_CARD.title"] = new("cards", "太平要术"),
        ["SANGUOSHA_CARD_TAI_PING_CARD.description"] = new("cards", "装备：宝物。获得 {Dexterity:diff()} 点敏捷并回复 3 点生命。每回合开始时净化 1 个负面效果；若成功净化，获得 {Energy:diff()} 点能量。额外回复 {TurnHeal:diff()} 点生命。"),
        ["SANGUOSHA_CARD_GUAN_SHI_CARD.title"] = new("cards", "贯石斧"),
        ["SANGUOSHA_CARD_GUAN_SHI_CARD.description"] = new("cards", "装备：武器。获得 {Strength:diff()} 点力量和 {Energy:diff()} 点能量。每回合首次杀额外造成 {BonusDamage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_GU_DING_CARD.title"] = new("cards", "古锭刀"),
        ["SANGUOSHA_CARD_GU_DING_CARD.description"] = new("cards", "装备：武器。获得 {Strength:diff()} 点力量。杀命中生命不高于一半的敌人时，造成 150% 伤害。"),
        ["SANGUOSHA_CARD_GAN_JIANG_MO_YE_CARD.title"] = new("cards", "干将莫邪"),
        ["SANGUOSHA_CARD_GAN_JIANG_MO_YE_CARD.description"] = new("cards", "装备：武器。获得 {Strength:diff()} 点力量并抽 {Draw:diff()} 张牌。每回合前 {Triggers:diff()} 次杀命中后，追加 {BonusDamage:diff()} 点剑气伤害。失去时强化下一张杀并抽牌。"),
        ["SANGUOSHA_CARD_TENG_JIA_CARD.title"] = new("cards", "藤甲"),
        ["SANGUOSHA_CARD_TENG_JIA_CARD.description"] = new("cards", "装备：防具。下回合开始，每回合获得 {Block:diff()} 点格挡并获得减速；减速上限为 {SlowCap:diff()}%。失去时获得格挡。"),
        ["SANGUOSHA_CARD_JUE_YING_CARD.title"] = new("cards", "绝影"),
        ["SANGUOSHA_CARD_JUE_YING_CARD.description"] = new("cards", "装备：坐骑。获得 {Dexterity:diff()} 点敏捷。每回合首次敌人即将造成伤害时，获得 {Block:diff()} 点格挡。失去时获得格挡。"),
        ["SANGUOSHA_CARD_MENG_DE_XIN_SHU_CARD.title"] = new("cards", "孟德新书"),
        ["SANGUOSHA_CARD_MENG_DE_XIN_SHU_CARD.description"] = new("cards", "装备：宝物。获得 {Energy:diff()} 点能量并抽 {Draw:diff()} 张牌。每回合首次打出技能牌后，获得 1 点能量，并使 1 张杀或技能牌本回合费用降低 1。失去时获得能量并降低 1 张牌费用。"),
        ["SANGUOSHA_CARD_GUO_HE_CARD.title"] = new("cards", "过河拆桥"),
        ["SANGUOSHA_CARD_GUO_HE_CARD.description"] = new("cards", "移除目标这回合所有正面效果。若至少移除 1 个，抽 {Draw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_HAN_BING_CARD.title"] = new("cards", "寒冰剑"),
        ["SANGUOSHA_CARD_HAN_BING_CARD.description"] = new("cards", "装备：武器。抽 {Draw:diff()} 张牌。你的杀命中后施加 {Weak:diff()} 层虚弱。"),
        ["SANGUOSHA_CARD_HUO_GONG_CARD.title"] = new("cards", "火攻"),
        ["SANGUOSHA_CARD_HUO_GONG_CARD.description"] = new("cards", "可弃 1 张手牌。造成 {Damage:diff()} 点伤害并施加 {Poison:diff()} 层中毒；若弃牌，额外造成 {BonusDamage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_JIE_DAO_CARD.title"] = new("cards", "借刀杀人"),
        ["SANGUOSHA_CARD_JIE_DAO_CARD.description"] = new("cards", "可消耗 1 张手牌中的杀。造成 {Damage:diff()} 点伤害；若消耗，额外造成 {BonusDamage:diff()} 点伤害并对所有敌人施加 {Slow:diff()} 层减速。"),
        ["SANGUOSHA_CARD_LE_BU_CARD.title"] = new("cards", "乐不思蜀"),
        ["SANGUOSHA_CARD_LE_BU_CARD.description"] = new("cards", "延迟判定：下个敌方回合开始时，若随机数为偶数，目标本回合不能攻击。"),
        ["SANGUOSHA_CARD_NAN_MAN_CARD.title"] = new("cards", "南蛮入侵"),
        ["SANGUOSHA_CARD_NAN_MAN_CARD.description"] = new("cards", "对所有敌人施加 {Weak:diff()} 层虚弱并造成 {Damage:diff()} 点伤害。若击杀任意敌人，回复 {Heal:diff()} 点生命，否则回复 1 点生命。所有玩家获得 {TeamDexterity:diff()} 点敏捷。"),
        ["SANGUOSHA_CARD_QI_LIN_CARD.title"] = new("cards", "麒麟弓"),
        ["SANGUOSHA_CARD_QI_LIN_CARD.description"] = new("cards", "装备：武器。获得 {Strength:diff()} 点力量并抽 {Draw:diff()} 张牌。杀命中已有负面效果的敌人时额外造成 {BonusDamage:diff()} 点伤害；每回合首次如此命中时施加 {Vulnerable:diff()} 层易伤。"),
        ["SANGUOSHA_CARD_QING_GANG_CARD.title"] = new("cards", "青釭剑"),
        ["SANGUOSHA_CARD_QING_GANG_CARD.description"] = new("cards", "装备：武器。获得 {Energy:diff()} 点能量。你的杀无视格挡；若目标原本有格挡，伤害倍率为 {BlockedMultiplier:diff()}%。"),
        ["SANGUOSHA_CARD_SHAN_DIAN_CARD.title"] = new("cards", "闪电"),
        ["SANGUOSHA_CARD_SHAN_DIAN_CARD.description"] = new("cards", "X 费。延迟判定：下个敌方回合开始时，若随机数为偶数，造成目标最大生命 X*{DamagePercent:diff()}% 的伤害，向上取整。升级后按 X+1 计算。"),
        ["SANGUOSHA_CARD_TIE_SUO_CARD.title"] = new("cards", "铁索连环"),
        ["SANGUOSHA_CARD_TIE_SUO_CARD.description"] = new("cards", "施加 {Slow:diff()} 层减速。本回合你的杀对其他敌人造成 {SplashPercent:diff()}% 溅射伤害。"),
        ["SANGUOSHA_CARD_SHUN_SHOU_CARD.title"] = new("cards", "顺手牵羊"),
        ["SANGUOSHA_CARD_SHUN_SHOU_CARD.description"] = new("cards", "这回合将目标所有正面效果转移给你。若至少转移 1 个，抽 {Draw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_TAO_YUAN_CARD.title"] = new("cards", "桃园结义"),
        ["SANGUOSHA_CARD_TAO_YUAN_CARD.description"] = new("cards", "所有玩家回复 {Heal:diff()} 点生命。你获得 {Block:diff()} 点格挡、{Energy:diff()} 点能量并抽 {Draw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_WAN_JIAN_CARD.title"] = new("cards", "万箭齐发"),
        ["SANGUOSHA_CARD_WAN_JIAN_CARD.description"] = new("cards", "对所有敌人造成 {Damage:diff()} 点伤害。所有玩家获得 {Strength:diff()} 点力量。你获得 {Block:diff()} 点格挡。"),
        ["SANGUOSHA_CARD_WU_XIE_CARD.title"] = new("cards", "无懈可击"),
        ["SANGUOSHA_CARD_WU_XIE_CARD.description"] = new("cards", "移除自身所有负面效果。每移除一种，获得 {CleanseBlock:diff()} 点格挡；若至少移除一种，获得 {Energy:diff()} 点能量。若未移除任何负面效果，获得 {Block:diff()} 点格挡。"),
        ["SANGUOSHA_CARD_GUAN_XING_CARD.title"] = new("cards", "观星"),
        ["SANGUOSHA_CARD_GUAN_XING_CARD.description"] = new("cards", "获得 {Block:diff()} 点格挡。观看抽牌堆顶 {Look:diff()} 张牌，选择其中至多 {Discard:diff()} 张置入弃牌堆，其余保持在抽牌堆顶。"),
        ["SANGUOSHA_CARD_KONG_CHENG_CARD.title"] = new("cards", "空城"),
        ["SANGUOSHA_CARD_KONG_CHENG_CARD.description"] = new("cards", "能力牌。获得 {Block:diff()} 点格挡。下回合开始时获得 {Intangible:diff()} 层无实体，且该回合不能打出攻击牌。"),
        ["SANGUOSHA_CARD_LONG_DAN_CARD.title"] = new("cards", "龙胆"),
        ["SANGUOSHA_CARD_LONG_DAN_CARD.description"] = new("cards", "能力牌。获得 {Block:diff()} 点格挡。此后每回合第一张杀或闪本回合费用变为 0。"),
        ["SANGUOSHA_CARD_ZHI_HENG_CARD.title"] = new("cards", "制衡"),
        ["SANGUOSHA_CARD_ZHI_HENG_CARD.description"] = new("cards", "能力牌。可消耗至多 {MaxCards:diff()} 张手牌，抽等量牌并额外抽 {DrawBonus:diff()} 张。此后每回合首次打出技能牌后抽牌。"),
        ["SANGUOSHA_CARD_WU_SHUANG_CARD.title"] = new("cards", "无双"),
        ["SANGUOSHA_CARD_WU_SHUANG_CARD.description"] = new("cards", "能力牌。获得 {Strength:diff()} 点力量。每回合前 {Repeats:diff()} 次杀命中后，追加 3 点无双伤害；该追加伤害不视为杀命中。"),
        ["SANGUOSHA_CARD_LIAN_YING_CARD.title"] = new("cards", "连营"),
        ["SANGUOSHA_CARD_LIAN_YING_CARD.description"] = new("cards", "能力牌。抽 1 张牌。每回合前 {Triggers:diff()} 次你主动打出牌后没有手牌时，抽 {Draw:diff()} 张牌，并使其中 1 张杀或技能牌本回合费用降低 1。"),
        ["SANGUOSHA_CARD_YI_JI_CARD.title"] = new("cards", "遗计"),
        ["SANGUOSHA_CARD_YI_JI_CARD.description"] = new("cards", "能力牌。抽 1 张牌。此后每回合首次敌人即将对你造成可被格挡的伤害时，抽 {Draw:diff()} 张牌；若触发前手牌不超过 3 张，额外抽 {LowHandDraw:diff()} 张牌。然后使手牌中至多 {FreeCards:diff()} 张杀或技能牌本回合变为 0 费。"),
        ["SANGUOSHA_CARD_JIAN_XIONG_CARD.title"] = new("cards", "奸雄"),
        ["SANGUOSHA_CARD_JIAN_XIONG_CARD.description"] = new("cards", "能力牌。获得 1 点力量。每回合首次敌人即将对你造成伤害时，抽 {Draw:diff()} 张牌，你的下一张杀额外造成 {NextShaDamage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_GUI_CAI_CARD.title"] = new("cards", "鬼才"),
        ["SANGUOSHA_CARD_GUI_CAI_CARD.description"] = new("cards", "能力牌。获得 {Block:diff()} 点格挡。敌人即将对你造成伤害时，判定抽牌堆顶 1 张牌并置入弃牌堆：若为技能牌或能力牌，获得 {Block:diff()} 点格挡并抽 {Draw:diff()} 张牌；若为杀，施加 {Weak:diff()} 层虚弱；否则使 1 张杀或技能牌本回合变为 0 费。"),
        ["SANGUOSHA_CARD_CI_SHA_CARD.title"] = new("cards", "刺杀"),
        ["SANGUOSHA_CARD_CI_SHA_CARD.description"] = new("cards", "造成 {Damage:diff()} 点伤害。若目标已有负面效果，额外造成 {BonusDamage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_SHOU_SHI_CARD.title"] = new("cards", "守势"),
        ["SANGUOSHA_CARD_SHOU_SHI_CARD.description"] = new("cards", "获得 {Block:diff()} 点格挡。若本回合没有打出过攻击牌，本牌本场战斗中格挡值增加 {BlockGrowth:diff()}。"),
        ["SANGUOSHA_CARD_FEN_CHENG_CARD.title"] = new("cards", "焚城"),
        ["SANGUOSHA_CARD_FEN_CHENG_CARD.description"] = new("cards", "X 费。消耗所有其他手牌。每消耗 1 张牌，对所有敌人造成 X*{DamagePerX:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_DIAO_DU_CARD.title"] = new("cards", "调度"),
        ["SANGUOSHA_CARD_DIAO_DU_CARD.description"] = new("cards", "抽 {Draw:diff()} 张牌，然后弃 {Discard:diff()} 张牌。"),
        ["SANGUOSHA_CARD_REN_DE_CARD.title"] = new("cards", "仁德"),
        ["SANGUOSHA_CARD_REN_DE_CARD.description"] = new("cards", "弃 1 张手牌，回复 {Heal:diff()} 点生命并抽 {Draw:diff()} 张牌。若弃杀，下一张杀额外造成 {NextShaDamage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_TU_XI_CARD.title"] = new("cards", "突袭"),
        ["SANGUOSHA_CARD_TU_XI_CARD.description"] = new("cards", "移除目标所有格挡。获得 {Energy:diff()} 点能量，抽 {Draw:diff()} 张牌，并施加 {Vulnerable:diff()} 层易伤。"),
        ["SANGUOSHA_CARD_QI_XI_CARD.title"] = new("cards", "奇袭"),
        ["SANGUOSHA_CARD_QI_XI_CARD.description"] = new("cards", "消耗 1 张技能牌。移除目标所有格挡，施加 {Vulnerable:diff()} 层易伤并抽 {Draw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_GUA_GU_CARD.title"] = new("cards", "刮骨疗毒"),
        ["SANGUOSHA_CARD_GUA_GU_CARD.description"] = new("cards", "消耗。移除自身所有负面效果；每移除 1 个，回复 {Heal:diff()} 点生命。若没有移除负面效果，抽 {Draw:diff()} 张牌。"),

        ["SANGUOSHA_POWER_ZHU_GE_DISPLAY_POWER.title"] = new("powers", "诸葛连弩"),
        ["SANGUOSHA_POWER_ZHU_GE_DISPLAY_POWER.description"] = new("powers", "装备：武器。每回合开始时，手牌中前若干张杀本回合变为 0 费。失去时按本回合已打出的杀抽牌，最多 2 张。"),
        ["SANGUOSHA_POWER_ZHANG_BA_DISPLAY_POWER.title"] = new("powers", "丈八蛇矛"),
        ["SANGUOSHA_POWER_ZHANG_BA_DISPLAY_POWER.description"] = new("powers", "装备：武器。只要你有至少 2 张其他手牌，会补 1 张 0 费蛇矛杀，可将任意两张牌当杀打出。失去时获得能量并补蛇矛杀。"),
        ["SANGUOSHA_POWER_QING_GANG_DISPLAY_POWER.title"] = new("powers", "青釭剑"),
        ["SANGUOSHA_POWER_QING_GANG_DISPLAY_POWER.description"] = new("powers", "装备：武器。你的杀无视格挡；升级的青釭剑会使原本有格挡的目标承受 150% 杀伤害。"),
        ["SANGUOSHA_POWER_GUAN_SHI_DISPLAY_POWER.title"] = new("powers", "贯石斧"),
        ["SANGUOSHA_POWER_GUAN_SHI_DISPLAY_POWER.description"] = new("powers", "装备：武器。每回合首次杀额外造成伤害。失去时强化下一张杀。"),
        ["SANGUOSHA_POWER_HAN_BING_DISPLAY_POWER.title"] = new("powers", "寒冰剑"),
        ["SANGUOSHA_POWER_HAN_BING_DISPLAY_POWER.description"] = new("powers", "装备：武器。你的杀命中后施加虚弱。"),
        ["SANGUOSHA_POWER_QI_LIN_DISPLAY_POWER.title"] = new("powers", "麒麟弓"),
        ["SANGUOSHA_POWER_QI_LIN_DISPLAY_POWER.description"] = new("powers", "装备：武器。你的杀命中负面状态目标时追加伤害；每回合首次如此命中时施加易伤。失去时抽牌。"),
        ["SANGUOSHA_POWER_GU_DING_DISPLAY_POWER.title"] = new("powers", "古锭刀"),
        ["SANGUOSHA_POWER_GU_DING_DISPLAY_POWER.description"] = new("powers", "装备：武器。你的杀命中生命不高于一半的目标时造成 150% 伤害。"),
        ["SANGUOSHA_POWER_GAN_JIANG_MO_YE_DISPLAY_POWER.title"] = new("powers", "干将莫邪"),
        ["SANGUOSHA_POWER_GAN_JIANG_MO_YE_DISPLAY_POWER.description"] = new("powers", "装备：武器。每回合前若干次杀命中后追加剑气伤害。失去时强化下一张杀并抽牌。"),
        ["SANGUOSHA_POWER_BAI_YIN_DISPLAY_POWER.title"] = new("powers", "白银狮子"),
        ["SANGUOSHA_POWER_BAI_YIN_DISPLAY_POWER.description"] = new("powers", "装备：防具。每回合失去生命不超过上限。失去时回复生命。"),
        ["SANGUOSHA_POWER_REN_WANG_DISPLAY_POWER.title"] = new("powers", "仁王盾"),
        ["SANGUOSHA_POWER_REN_WANG_DISPLAY_POWER.description"] = new("powers", "装备：防具。每回合首次即将受到杀或普通攻击伤害时，获得足量格挡。失去时获得格挡并抽牌。"),
        ["SANGUOSHA_POWER_BA_GUA_DISPLAY_POWER.title"] = new("powers", "八卦阵"),
        ["SANGUOSHA_POWER_BA_GUA_DISPLAY_POWER.description"] = new("powers", "装备：防具。敌人攻击你时进行判定；每判定出 1 张闪获得格挡并继续，直到攻击被完全格挡、判定次数用尽或翻出非闪。失去时抽牌并获得格挡。"),
        ["SANGUOSHA_POWER_TENG_JIA_DISPLAY_POWER.title"] = new("powers", "藤甲"),
        ["SANGUOSHA_POWER_TENG_JIA_DISPLAY_POWER.description"] = new("powers", "装备：防具。回合开始时获得 10 点格挡并获得减速；减速有上限。失去时获得格挡。"),
        ["SANGUOSHA_POWER_CHI_TU_DISPLAY_POWER.title"] = new("powers", "赤兔"),
        ["SANGUOSHA_POWER_CHI_TU_DISPLAY_POWER.description"] = new("powers", "装备：坐骑。入场获得力量和能量。失去时获得能量，并使手牌中的杀临时变为 0 费。"),
        ["SANGUOSHA_POWER_DA_WAN_DISPLAY_POWER.title"] = new("powers", "大宛"),
        ["SANGUOSHA_POWER_DA_WAN_DISPLAY_POWER.description"] = new("powers", "装备：坐骑。你的杀额外造成伤害。失去时强化下一张杀。"),
        ["SANGUOSHA_POWER_DI_LU_DISPLAY_POWER.title"] = new("powers", "的卢"),
        ["SANGUOSHA_POWER_DI_LU_DISPLAY_POWER.description"] = new("powers", "装备：坐骑。当前提供敏捷。失去时获得格挡。"),
        ["SANGUOSHA_POWER_JUE_YING_DISPLAY_POWER.title"] = new("powers", "绝影"),
        ["SANGUOSHA_POWER_JUE_YING_DISPLAY_POWER.description"] = new("powers", "装备：坐骑。每回合首次敌人即将造成伤害时获得格挡。失去时获得格挡。"),
        ["SANGUOSHA_POWER_YU_XI_DISPLAY_POWER.title"] = new("powers", "玉玺"),
        ["SANGUOSHA_POWER_YU_XI_DISPLAY_POWER.description"] = new("powers", "装备：宝物。每回合开始额外获得能量。失去时抽牌。"),
        ["SANGUOSHA_POWER_MU_NIU_DISPLAY_POWER.title"] = new("powers", "木牛流马"),
        ["SANGUOSHA_POWER_MU_NIU_DISPLAY_POWER.description"] = new("powers", "装备：宝物。每回合前若干次打出技能牌后抽牌。失去时抽牌。"),
        ["SANGUOSHA_POWER_TAI_PING_DISPLAY_POWER.title"] = new("powers", "太平要术"),
        ["SANGUOSHA_POWER_TAI_PING_DISPLAY_POWER.description"] = new("powers", "装备：宝物。回合开始净化 1 个负面效果；若成功净化，获得能量，额外回复生命。"),
        ["SANGUOSHA_POWER_MENG_DE_XIN_SHU_DISPLAY_POWER.title"] = new("powers", "孟德新书"),
        ["SANGUOSHA_POWER_MENG_DE_XIN_SHU_DISPLAY_POWER.description"] = new("powers", "装备：宝物。每回合首次打出技能牌后获得能量，并使 1 张杀或技能牌本回合费用降低 1。失去时获得能量并临时降费。"),
        ["SANGUOSHA_POWER_KONG_CHENG_DISPLAY_POWER.title"] = new("powers", "空城"),
        ["SANGUOSHA_POWER_KONG_CHENG_DISPLAY_POWER.description"] = new("powers", "下回合开始时获得 1 层无实体，但该回合不能打出攻击牌。"),
        ["SANGUOSHA_POWER_LONG_DAN_DISPLAY_POWER.title"] = new("powers", "龙胆"),
        ["SANGUOSHA_POWER_LONG_DAN_DISPLAY_POWER.description"] = new("powers", "能力牌。每回合第一张杀或闪本回合费用变为 0。"),
        ["SANGUOSHA_POWER_ZHI_HENG_DISPLAY_POWER.title"] = new("powers", "制衡"),
        ["SANGUOSHA_POWER_ZHI_HENG_DISPLAY_POWER.description"] = new("powers", "能力牌。每回合首次打出技能牌后抽牌。"),
        ["SANGUOSHA_POWER_WU_SHUANG_DISPLAY_POWER.title"] = new("powers", "无双"),
        ["SANGUOSHA_POWER_WU_SHUANG_DISPLAY_POWER.description"] = new("powers", "能力牌。每回合前若干次杀命中后追加无双伤害；追加伤害不视为杀命中。"),
        ["SANGUOSHA_POWER_LIAN_YING_DISPLAY_POWER.title"] = new("powers", "连营"),
        ["SANGUOSHA_POWER_LIAN_YING_DISPLAY_POWER.description"] = new("powers", "能力牌。主动打出牌后若没有手牌，抽牌并让 1 张杀或技能牌本回合费用降低 1。"),
        ["SANGUOSHA_POWER_YI_JI_DISPLAY_POWER.title"] = new("powers", "遗计"),
        ["SANGUOSHA_POWER_YI_JI_DISPLAY_POWER.description"] = new("powers", "能力牌。每回合首次敌人即将对你造成可被格挡的伤害时抽牌；若触发前手牌不超过 3 张，额外抽牌。然后使若干张杀或技能牌本回合变为 0 费。"),
        ["SANGUOSHA_POWER_JIAN_XIONG_DISPLAY_POWER.title"] = new("powers", "奸雄"),
        ["SANGUOSHA_POWER_JIAN_XIONG_DISPLAY_POWER.description"] = new("powers", "能力牌。每回合首次敌人即将造成伤害时抽牌，并强化下一张杀。"),
        ["SANGUOSHA_POWER_GUI_CAI_DISPLAY_POWER.title"] = new("powers", "鬼才"),
        ["SANGUOSHA_POWER_GUI_CAI_DISPLAY_POWER.description"] = new("powers", "能力牌。敌人即将造成伤害时判定抽牌堆顶，根据判定牌获得格挡抽牌、施加虚弱或临时减费。"),
        ["SANGUOSHA_POWER_IRONCLAD_SKILL_DISPLAY_POWER.title"] = new("powers", "火杀"),
        ["SANGUOSHA_POWER_IRONCLAD_SKILL_DISPLAY_POWER.description"] = new("powers", "战斗开始获得 1 点力量。杀命中后追加 2 点火焰伤害。"),
        ["SANGUOSHA_POWER_SILENT_SKILL_DISPLAY_POWER.title"] = new("powers", "毒杀"),
        ["SANGUOSHA_POWER_SILENT_SKILL_DISPLAY_POWER.description"] = new("powers", "战斗开始抽 1 张牌。杀命中后施加 3 层中毒；每回合首次打出技能牌时抽 1 张牌。"),
        ["SANGUOSHA_POWER_DEFECT_SKILL_DISPLAY_POWER.title"] = new("powers", "雷杀"),
        ["SANGUOSHA_POWER_DEFECT_SKILL_DISPLAY_POWER.description"] = new("powers", "杀命中后获得 1 点雷势并追加 2 点雷击。雷势达到 4 时消耗 4 点，对所有敌人造成 8 点伤害。"),
        ["SANGUOSHA_POWER_NECROBINDER_SKILL_DISPLAY_POWER.title"] = new("powers", "灾厄杀"),
        ["SANGUOSHA_POWER_NECROBINDER_SKILL_DISPLAY_POWER.description"] = new("powers", "战斗开始回复 2 点生命，回合开始回复 1 点生命。杀命中后施加杀数值一半的灾厄；打出闪时所有敌人失去 3 点生命。"),
        ["SANGUOSHA_POWER_REGENT_SKILL_DISPLAY_POWER.title"] = new("powers", "君王之剑"),
        ["SANGUOSHA_POWER_REGENT_SKILL_DISPLAY_POWER.description"] = new("powers", "战斗开始获得 3 点辉星。杀命中后锻造原版君王之剑，使悬剑伤害提高。"),

        ["IRONCLAD.title"] = new("characters", "赤壁猛将"),
        ["IRONCLAD.titleObject"] = new("characters", "赤壁猛将"),
        ["IRONCLAD.description"] = new("characters", "初始遗物：火杀。战斗开始获得力量，杀命中追加火焰伤害。"),
        ["SILENT.title"] = new("characters", "锦囊夜行"),
        ["SILENT.titleObject"] = new("characters", "锦囊夜行"),
        ["SILENT.description"] = new("characters", "初始遗物：毒杀。战斗开始抽牌，杀命中施加中毒；每回合首次技能牌抽牌。"),
        ["DEFECT.title"] = new("characters", "机关卧龙"),
        ["DEFECT.titleObject"] = new("characters", "机关卧龙"),
        ["DEFECT.description"] = new("characters", "初始遗物：雷杀。杀命中积累雷势并追加雷击；雷势满 4 后释放群体伤害。"),
        ["NECROBINDER.title"] = new("characters", "青囊魂医"),
        ["NECROBINDER.titleObject"] = new("characters", "青囊魂医"),
        ["NECROBINDER.description"] = new("characters", "初始遗物：灾厄杀。杀命中施加灾厄；闪会令所有敌人失去生命。"),
        ["REGENT.title"] = new("characters", "汉室仁主"),
        ["REGENT.titleObject"] = new("characters", "汉室仁主"),
        ["REGENT.description"] = new("characters", "初始遗物：君王之剑。战斗开始获得辉星；杀命中后锻造原版君王之剑，使悬剑伤害提高。"),

        ["SANGUOSHA_RELIC_IRONCLAD_SKILL_RELIC.title"] = new("relics", "火杀"),
        ["SANGUOSHA_RELIC_IRONCLAD_SKILL_RELIC.description"] = new("relics", "战斗开始：获得 1 点力量。杀附魔：火杀，命中后追加 2 点火焰伤害。"),
        ["SANGUOSHA_RELIC_IRONCLAD_SKILL_RELIC.flavor"] = new("relics", "血火铸甲，赤壁开锋。"),
        ["SANGUOSHA_RELIC_SILENT_SKILL_RELIC.title"] = new("relics", "毒杀"),
        ["SANGUOSHA_RELIC_SILENT_SKILL_RELIC.description"] = new("relics", "战斗开始：抽 1 张牌。杀附魔：毒杀，命中后施加 3 层中毒。每回合首次打出技能牌时抽 1 张牌。"),
        ["SANGUOSHA_RELIC_SILENT_SKILL_RELIC.flavor"] = new("relics", "胜负不在手牌里，在别人以为你没有手牌时。"),
        ["SANGUOSHA_RELIC_DEFECT_SKILL_RELIC.title"] = new("relics", "雷杀"),
        ["SANGUOSHA_RELIC_DEFECT_SKILL_RELIC.description"] = new("relics", "杀附魔：雷杀，命中后获得 1 点雷势并追加 2 点雷击。雷势达到 4 时消耗 4 点，对所有敌人造成 8 点伤害。"),
        ["SANGUOSHA_RELIC_DEFECT_SKILL_RELIC.flavor"] = new("relics", "风起之前，星位已经变了。"),
        ["SANGUOSHA_RELIC_NECROBINDER_SKILL_RELIC.title"] = new("relics", "灾厄杀"),
        ["SANGUOSHA_RELIC_NECROBINDER_SKILL_RELIC.description"] = new("relics", "战斗开始：回复 2 点生命；回合开始回复 1 点生命。杀附魔：灾厄杀，命中后施加杀数值一半的灾厄。打出闪时所有敌人失去 3 点生命。"),
        ["SANGUOSHA_RELIC_NECROBINDER_SKILL_RELIC.flavor"] = new("relics", "刀兵之后，总有人要把命从鬼门关边上拉回来。"),
        ["SANGUOSHA_RELIC_REGENT_SKILL_RELIC.title"] = new("relics", "君王之剑"),
        ["SANGUOSHA_RELIC_REGENT_SKILL_RELIC.description"] = new("relics", "战斗开始获得 3 点辉星。杀附魔：杀命中后锻造原版君王之剑，使悬剑伤害提高。"),
        ["SANGUOSHA_RELIC_REGENT_SKILL_RELIC.flavor"] = new("relics", "仁德不是软弱，是让每一次出牌都有人响应。")
    };

    private static readonly IReadOnlyDictionary<string, Entry> TextOverrides = new Dictionary<string, Entry>
    {
        ["SANGUOSHA_CARD_WU_GU_CARD.description"] = new("cards", "获得 {Block:diff()} 点格挡。可消耗手牌中的 1 张牌。所有玩家抽 {Draw:diff()} 张牌；若成功消耗，你获得 {Energy:diff()} 点能量，所有玩家额外抽 {BonusDraw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_NAN_MAN_CARD.description"] = new("cards", "对所有敌人施加 {Weak:diff()} 层虚弱并造成 {Damage:diff()} 点伤害。若击杀任意敌人，回复 {Heal:diff()} 点生命，否则回复 1 点生命。所有玩家获得 {TeamDexterity:diff()} 点敏捷。"),
        ["SANGUOSHA_CARD_TAO_YUAN_CARD.description"] = new("cards", "所有玩家回复 {Heal:diff()} 点生命。你获得 {Block:diff()} 点格挡、{Energy:diff()} 点能量并抽 {Draw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_WAN_JIAN_CARD.description"] = new("cards", "对所有敌人造成 {Damage:diff()} 点伤害。所有玩家获得 {Strength:diff()} 点力量。你获得 {Block:diff()} 点格挡。")
    };

    public static bool TryGet(string key, out string value)
    {
        if (!TryGetEntry(key, out var entry))
        {
            value = string.Empty;
            return false;
        }

        value = entry.Text;
        return true;
    }

    public static bool TryGetTable(string key, out string table)
    {
        if (!TryGetEntry(key, out var entry))
        {
            table = string.Empty;
            return false;
        }

        table = entry.Table;
        return true;
    }

    public static bool Contains(string key)
    {
        return TryGetEntry(key, out _);
    }

    private static bool TryGetEntry(string key, out Entry entry)
    {
        if (TextOverrides.TryGetValue(key, out entry!))
        {
            return true;
        }

        if (Text.TryGetValue(key, out entry!))
        {
            return true;
        }

        foreach (var alias in PowerKeyAliases)
        {
            if (!key.StartsWith(alias.Key, StringComparison.Ordinal))
            {
                continue;
            }

            var normalizedKey = alias.Value + key[alias.Key.Length..];
            if (Text.TryGetValue(normalizedKey, out entry!))
            {
                return true;
            }
        }

        entry = null!;
        return false;
    }
}

[HarmonyPatch(typeof(LocTable), nameof(LocTable.GetRawText))]
internal static class SanguoshaLocTableGetRawTextPatch
{
    private static bool Prefix(string key, ref string __result)
    {
        if (!SanguoshaLocalization.TryGet(key, out var text))
        {
            return true;
        }

        __result = text;
        return false;
    }
}

[HarmonyPatch(typeof(LocTable), nameof(LocTable.HasEntry))]
internal static class SanguoshaLocTableHasEntryPatch
{
    private static bool Prefix(string key, ref bool __result)
    {
        if (!SanguoshaLocalization.Contains(key))
        {
            return true;
        }

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(LocTable), nameof(LocTable.IsLocalKey))]
internal static class SanguoshaLocTableIsLocalKeyPatch
{
    private static bool Prefix(string key, ref bool __result)
    {
        if (!SanguoshaLocalization.Contains(key))
        {
            return true;
        }

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(LocTable), nameof(LocTable.GetLocString))]
internal static class SanguoshaLocTableGetLocStringPatch
{
    private static bool Prefix(string key, ref LocString __result)
    {
        if (!SanguoshaLocalization.TryGetTable(key, out var table))
        {
            return true;
        }

        __result = new LocString(table, key);
        return false;
    }
}
