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
        ["SANGUOSHA_BAI_YIN_DISPLAY_POWER"] = "SANGUOSHA_POWER_BAI_YIN_DISPLAY_POWER",
        ["SANGUOSHA_REN_WANG_DISPLAY_POWER"] = "SANGUOSHA_POWER_REN_WANG_DISPLAY_POWER",
        ["SANGUOSHA_BA_GUA_DISPLAY_POWER"] = "SANGUOSHA_POWER_BA_GUA_DISPLAY_POWER",
        ["SANGUOSHA_CHI_TU_DISPLAY_POWER"] = "SANGUOSHA_POWER_CHI_TU_DISPLAY_POWER",
        ["SANGUOSHA_DA_WAN_DISPLAY_POWER"] = "SANGUOSHA_POWER_DA_WAN_DISPLAY_POWER",
        ["SANGUOSHA_DI_LU_DISPLAY_POWER"] = "SANGUOSHA_POWER_DI_LU_DISPLAY_POWER",
        ["SANGUOSHA_YU_XI_DISPLAY_POWER"] = "SANGUOSHA_POWER_YU_XI_DISPLAY_POWER",
        ["SANGUOSHA_MU_NIU_DISPLAY_POWER"] = "SANGUOSHA_POWER_MU_NIU_DISPLAY_POWER",
        ["SANGUOSHA_TAI_PING_DISPLAY_POWER"] = "SANGUOSHA_POWER_TAI_PING_DISPLAY_POWER",
        ["SANGUOSHA_KONG_CHENG_DISPLAY_POWER"] = "SANGUOSHA_POWER_KONG_CHENG_DISPLAY_POWER",
        ["SANGUOSHA_ZHI_HENG_DISPLAY_POWER"] = "SANGUOSHA_POWER_ZHI_HENG_DISPLAY_POWER",
        ["SANGUOSHA_WU_SHUANG_DISPLAY_POWER"] = "SANGUOSHA_POWER_WU_SHUANG_DISPLAY_POWER"
    };

    private static readonly IReadOnlyDictionary<string, Entry> Text = new Dictionary<string, Entry>
    {
        ["SANGUOSHA_CARD_SHA_CARD.title"] = new("cards", "杀"),
        ["SANGUOSHA_CARD_SHA_CARD.description"] = new("cards", "造成 {Damage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_SHAN_CARD.title"] = new("cards", "闪"),
        ["SANGUOSHA_CARD_SHAN_CARD.description"] = new("cards", "获得 {Block:diff()} 点格挡。"),
        ["SANGUOSHA_CARD_TAO_CARD.title"] = new("cards", "桃"),
        ["SANGUOSHA_CARD_TAO_CARD.description"] = new("cards", "回复 {Heal:diff()} 点生命并获得 {Block:diff()} 点格挡。若生命值不高于一半，额外回复 {LowHpHeal:diff()} 点生命。获得 {Energy:diff()} 点能量。"),
        ["SANGUOSHA_CARD_JIU_CARD.title"] = new("cards", "酒"),
        ["SANGUOSHA_CARD_JIU_CARD.description"] = new("cards", "获得 {Strength:diff()} 点力量，并对所有敌人造成 {Damage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_DUEL_CARD.title"] = new("cards", "决斗"),
        ["SANGUOSHA_CARD_DUEL_CARD.description"] = new("cards", "造成 {Damage:diff()} 点伤害 {Hits:diff()} 次。将手牌中的 1 张杀本回合变为 0 费。"),
        ["SANGUOSHA_CARD_WU_ZHONG_CARD.title"] = new("cards", "无中生有"),
        ["SANGUOSHA_CARD_WU_ZHONG_CARD.description"] = new("cards", "可消耗手牌中的 1 张牌。抽 {Draw:diff()} 张牌；若成功消耗，获得 {Energy:diff()} 点能量并额外抽 {BonusDraw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_WU_GU_CARD.title"] = new("cards", "五谷丰登"),
        ["SANGUOSHA_CARD_WU_GU_CARD.description"] = new("cards", "获得 {Block:diff()} 点格挡。可消耗手牌中的 1 张牌。抽 {Draw:diff()} 张牌；若成功消耗，获得 {Energy:diff()} 点能量并额外抽 {BonusDraw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_ZHU_GE_CARD.title"] = new("cards", "诸葛连弩"),
        ["SANGUOSHA_CARD_ZHU_GE_CARD.description"] = new("cards", "武器。获得 {Strength:diff()} 点力量并生成 1 张免费杀。每回合开始时，手牌中前 {FreeSha:diff()} 张杀本回合变为 0 费。"),
        ["SANGUOSHA_CARD_ZHANG_BA_CARD.title"] = new("cards", "丈八蛇矛"),
        ["SANGUOSHA_CARD_ZHANG_BA_CARD.description"] = new("cards", "武器。抽 {Draw:diff()} 张牌。只要你有至少 2 张其他手牌，生成 1 张 0 费蛇矛杀：可将任意两张牌当杀打出。升级后蛇矛杀伤害提高并摸牌。"),
        ["SANGUOSHA_CARD_ZHANG_BA_SHA_CARD.title"] = new("cards", "蛇矛杀"),
        ["SANGUOSHA_CARD_ZHANG_BA_SHA_CARD.description"] = new("cards", "消耗任意 2 张其他手牌，视为杀造成 {Damage:diff()} 点伤害。若丈八蛇矛已升级，额外造成 {UpgradeDamage:diff()} 点伤害并抽 {Draw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_BA_GUA_CARD.title"] = new("cards", "八卦阵"),
        ["SANGUOSHA_CARD_BA_GUA_CARD.description"] = new("cards", "防具。敌人攻击你时，从抽牌堆顶依次打出牌：若为闪，获得 6 点格挡并继续；直到不是闪或本次攻击已被完全格挡。升级后 0 费，闪提供 8 点格挡。"),
        ["SANGUOSHA_CARD_REN_WANG_CARD.title"] = new("cards", "仁王盾"),
        ["SANGUOSHA_CARD_REN_WANG_CARD.description"] = new("cards", "防具。获得 {Dexterity:diff()} 点敏捷。"),
        ["SANGUOSHA_CARD_BAI_YIN_CARD.title"] = new("cards", "白银狮子"),
        ["SANGUOSHA_CARD_BAI_YIN_CARD.description"] = new("cards", "防具。获得 {Dexterity:diff()} 点敏捷并回复 {Heal:diff()} 点生命。每回合开始时回复 {TurnHeal:diff()} 点生命。每回合最多受到 15 点单次伤害。"),
        ["SANGUOSHA_CARD_BING_LIANG_CARD.title"] = new("cards", "兵粮寸断"),
        ["SANGUOSHA_CARD_BING_LIANG_CARD.description"] = new("cards", "施加 {Weak:diff()} 层虚弱和 {Slow:diff()} 层减速，造成 {Damage:diff()} 点伤害，获得 {Block:diff()} 点格挡并抽 {Draw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_CHI_TU_CARD.title"] = new("cards", "赤兔"),
        ["SANGUOSHA_CARD_CHI_TU_CARD.description"] = new("cards", "坐骑。获得 {Strength:diff()} 点力量和 {Energy:diff()} 点能量。"),
        ["SANGUOSHA_CARD_DA_WAN_CARD.title"] = new("cards", "大宛"),
        ["SANGUOSHA_CARD_DA_WAN_CARD.description"] = new("cards", "坐骑。获得 {Strength:diff()} 点力量。"),
        ["SANGUOSHA_CARD_DI_LU_CARD.title"] = new("cards", "的卢"),
        ["SANGUOSHA_CARD_DI_LU_CARD.description"] = new("cards", "坐骑。获得 {Dexterity:diff()} 点敏捷。"),
        ["SANGUOSHA_CARD_YU_XI_CARD.title"] = new("cards", "玉玺"),
        ["SANGUOSHA_CARD_YU_XI_CARD.description"] = new("cards", "宝物。获得 {Energy:diff()} 点能量、{Strength:diff()} 点力量并抽 {Draw:diff()} 张牌。每回合开始时额外获得 1 点能量。"),
        ["SANGUOSHA_CARD_MU_NIU_CARD.title"] = new("cards", "木牛流马"),
        ["SANGUOSHA_CARD_MU_NIU_CARD.description"] = new("cards", "宝物。获得 {Energy:diff()} 点能量并抽 {Draw:diff()} 张牌。每回合前 {SkillDraws:diff()} 次打出锦囊牌后，抽 1 张牌。"),
        ["SANGUOSHA_CARD_TAI_PING_CARD.title"] = new("cards", "太平要术"),
        ["SANGUOSHA_CARD_TAI_PING_CARD.description"] = new("cards", "宝物。获得 {Dexterity:diff()} 点敏捷并回复 3 点生命。每回合开始时净化 1 个负面效果；若成功净化，获得 {Energy:diff()} 点能量。额外回复 {TurnHeal:diff()} 点生命。"),
        ["SANGUOSHA_CARD_GUAN_SHI_CARD.title"] = new("cards", "贯石斧"),
        ["SANGUOSHA_CARD_GUAN_SHI_CARD.description"] = new("cards", "武器。获得 {Strength:diff()} 点力量和 {Energy:diff()} 点能量。杀命中有格挡的敌人时额外造成 {BonusDamage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_GU_DING_CARD.title"] = new("cards", "古锭刀"),
        ["SANGUOSHA_CARD_GU_DING_CARD.description"] = new("cards", "武器。获得 {Strength:diff()} 点力量。杀命中没有格挡的敌人时，伤害翻倍。"),
        ["SANGUOSHA_CARD_GUO_HE_CARD.title"] = new("cards", "过河拆桥"),
        ["SANGUOSHA_CARD_GUO_HE_CARD.description"] = new("cards", "移除目标所有格挡。造成 {Damage:diff()} 点伤害，施加 {Weak:diff()} 层虚弱和 {Vulnerable:diff()} 层易伤。若移除了至少 {BlockThreshold:diff()} 点格挡，获得 {Energy:diff()} 点能量并抽 1 张牌。"),
        ["SANGUOSHA_CARD_HAN_BING_CARD.title"] = new("cards", "寒冰剑"),
        ["SANGUOSHA_CARD_HAN_BING_CARD.description"] = new("cards", "武器。抽 {Draw:diff()} 张牌。你的杀命中后施加 {Weak:diff()} 层虚弱。"),
        ["SANGUOSHA_CARD_HUO_GONG_CARD.title"] = new("cards", "火攻"),
        ["SANGUOSHA_CARD_HUO_GONG_CARD.description"] = new("cards", "施加 {Poison:diff()} 层中毒。造成 {Damage:diff()} 点伤害。若目标已有负面效果，额外造成 {BonusDamage:diff()} 点伤害。"),
        ["SANGUOSHA_CARD_JIE_DAO_CARD.title"] = new("cards", "借刀杀人"),
        ["SANGUOSHA_CARD_JIE_DAO_CARD.description"] = new("cards", "造成 {Damage:diff()} 点伤害，获得 {Strength:diff()} 点力量，并施加 {Slow:diff()} 层减速和 {SelfWeak:diff()} 层虚弱。"),
        ["SANGUOSHA_CARD_LE_BU_CARD.title"] = new("cards", "乐不思蜀"),
        ["SANGUOSHA_CARD_LE_BU_CARD.description"] = new("cards", "施加 {Slow:diff()} 层减速和 {Vulnerable:diff()} 层易伤，造成 {Damage:diff()} 点伤害并获得 {Block:diff()} 点格挡。"),
        ["SANGUOSHA_CARD_NAN_MAN_CARD.title"] = new("cards", "南蛮入侵"),
        ["SANGUOSHA_CARD_NAN_MAN_CARD.description"] = new("cards", "对所有敌人施加 {Weak:diff()} 层虚弱并造成 {Damage:diff()} 点伤害。若击杀任意敌人，回复 {Heal:diff()} 点生命，否则回复 1 点生命。"),
        ["SANGUOSHA_CARD_QI_LIN_CARD.title"] = new("cards", "麒麟弓"),
        ["SANGUOSHA_CARD_QI_LIN_CARD.description"] = new("cards", "武器。获得 {Strength:diff()} 点力量并抽 {Draw:diff()} 张牌。杀命中已有负面效果的敌人时额外造成 {BonusDamage:diff()} 点伤害，并施加 {Vulnerable:diff()} 层易伤。"),
        ["SANGUOSHA_CARD_QING_GANG_CARD.title"] = new("cards", "青釭剑"),
        ["SANGUOSHA_CARD_QING_GANG_CARD.description"] = new("cards", "武器。获得 {Energy:diff()} 点能量。你的杀无视敌人格挡。"),
        ["SANGUOSHA_CARD_SHAN_DIAN_CARD.title"] = new("cards", "闪电"),
        ["SANGUOSHA_CARD_SHAN_DIAN_CARD.description"] = new("cards", "施加 {Vulnerable:diff()} 层易伤。造成 {MinDamage:diff()}-{MaxDamage:diff()} 点随机伤害，并获得 {Block:diff()} 点格挡。"),
        ["SANGUOSHA_CARD_TIE_SUO_CARD.title"] = new("cards", "铁索连环"),
        ["SANGUOSHA_CARD_TIE_SUO_CARD.description"] = new("cards", "施加 {Slow:diff()} 层减速，获得 {Block:diff()} 点格挡和 {Energy:diff()} 点能量。本回合你的杀会对其他敌人造成 75% 伤害；升级后为 100%。"),
        ["SANGUOSHA_CARD_SHUN_SHOU_CARD.title"] = new("cards", "顺手牵羊"),
        ["SANGUOSHA_CARD_SHUN_SHOU_CARD.description"] = new("cards", "造成 {Damage:diff()} 点伤害，获得 {Block:diff()} 点格挡并抽 1 张牌。若目标没有格挡，获得 {Energy:diff()} 点能量。"),
        ["SANGUOSHA_CARD_TAO_YUAN_CARD.title"] = new("cards", "桃园结义"),
        ["SANGUOSHA_CARD_TAO_YUAN_CARD.description"] = new("cards", "回复 {Heal:diff()} 点生命，获得 {Block:diff()} 点格挡和 {Energy:diff()} 点能量，并抽 {Draw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_WAN_JIAN_CARD.title"] = new("cards", "万箭齐发"),
        ["SANGUOSHA_CARD_WAN_JIAN_CARD.description"] = new("cards", "对所有敌人造成 {Damage:diff()} 点伤害。每命中一个仍存活的敌人，获得 {Strength:diff()} 点力量。获得 {Block:diff()} 点格挡。"),
        ["SANGUOSHA_CARD_WU_XIE_CARD.title"] = new("cards", "无懈可击"),
        ["SANGUOSHA_CARD_WU_XIE_CARD.description"] = new("cards", "移除自身所有负面效果。每移除一种，获得 {CleanseBlock:diff()} 点格挡；若至少移除一种，获得 {Energy:diff()} 点能量。若未移除任何负面效果，获得 {Block:diff()} 点格挡。升级后抽 1 张牌。"),
        ["SANGUOSHA_CARD_GUAN_XING_CARD.title"] = new("cards", "观星"),
        ["SANGUOSHA_CARD_GUAN_XING_CARD.description"] = new("cards", "获得 {Block:diff()} 点格挡。观看抽牌堆顶 {Look:diff()} 张牌，选择其中至多 {Discard:diff()} 张置入弃牌堆，其余保持在抽牌堆顶。"),
        ["SANGUOSHA_CARD_KONG_CHENG_CARD.title"] = new("cards", "空城"),
        ["SANGUOSHA_CARD_KONG_CHENG_CARD.description"] = new("cards", "能力。获得 {Block:diff()} 点格挡。每回合首次在你打出牌结算后没有手牌时，获得 {Intangible:diff()} 层无实体。升级后变为 0 费。"),
        ["SANGUOSHA_CARD_LONG_DAN_CARD.title"] = new("cards", "龙胆"),
        ["SANGUOSHA_CARD_LONG_DAN_CARD.description"] = new("cards", "获得 {Block:diff()} 点格挡。可消耗手牌中的 1 张杀或闪：杀视为闪，生成 1 张本回合 0 费闪；闪视为杀，生成 1 张本回合 0 费杀。若未消耗，获得 1 张免费杀和 1 张免费闪。升级后每次使用摸 {Draw:diff()} 张牌。"),
        ["SANGUOSHA_CARD_ZHI_HENG_CARD.title"] = new("cards", "制衡"),
        ["SANGUOSHA_CARD_ZHI_HENG_CARD.description"] = new("cards", "能力。可消耗至多 {MaxCards:diff()} 张手牌，抽等量牌并额外抽 {DrawBonus:diff()} 张。此后每回合首次打出锦囊牌后抽牌；升级后变为 0 费并提高制衡上限。"),
        ["SANGUOSHA_CARD_WU_SHUANG_CARD.title"] = new("cards", "无双"),
        ["SANGUOSHA_CARD_WU_SHUANG_CARD.description"] = new("cards", "能力。获得 {Strength:diff()} 点力量。每回合前 {Repeats:diff()} 次杀命中后追加一次无双伤害；升级后次数和力量提高。"),

        ["SANGUOSHA_POWER_ZHU_GE_DISPLAY_POWER.title"] = new("powers", "诸葛连弩"),
        ["SANGUOSHA_POWER_ZHU_GE_DISPLAY_POWER.description"] = new("powers", "装备：武器。每回合开始时，手牌中前 2 张杀本回合变为 0 费；升级后为 3 张。被替换时按本回合已打出的杀抽牌。"),
        ["SANGUOSHA_POWER_ZHANG_BA_DISPLAY_POWER.title"] = new("powers", "丈八蛇矛"),
        ["SANGUOSHA_POWER_ZHANG_BA_DISPLAY_POWER.description"] = new("powers", "装备：武器。只要你有至少 2 张其他手牌，会补充 1 张 0 费蛇矛杀，可将任意两张牌当杀打出。"),
        ["SANGUOSHA_POWER_QING_GANG_DISPLAY_POWER.title"] = new("powers", "青釭剑"),
        ["SANGUOSHA_POWER_QING_GANG_DISPLAY_POWER.description"] = new("powers", "装备：武器。你的杀无视敌人格挡。"),
        ["SANGUOSHA_POWER_GUAN_SHI_DISPLAY_POWER.title"] = new("powers", "贯石斧"),
        ["SANGUOSHA_POWER_GUAN_SHI_DISPLAY_POWER.description"] = new("powers", "装备：武器。你的杀命中有格挡的敌人时额外造成伤害。"),
        ["SANGUOSHA_POWER_HAN_BING_DISPLAY_POWER.title"] = new("powers", "寒冰剑"),
        ["SANGUOSHA_POWER_HAN_BING_DISPLAY_POWER.description"] = new("powers", "装备：武器。你的杀命中后施加虚弱。"),
        ["SANGUOSHA_POWER_QI_LIN_DISPLAY_POWER.title"] = new("powers", "麒麟弓"),
        ["SANGUOSHA_POWER_QI_LIN_DISPLAY_POWER.description"] = new("powers", "装备：武器。你的杀命中负面状态目标时追加伤害并施加易伤。"),
        ["SANGUOSHA_POWER_GU_DING_DISPLAY_POWER.title"] = new("powers", "古锭刀"),
        ["SANGUOSHA_POWER_GU_DING_DISPLAY_POWER.description"] = new("powers", "装备：武器。你的杀命中无格挡目标时伤害翻倍。"),
        ["SANGUOSHA_POWER_BAI_YIN_DISPLAY_POWER.title"] = new("powers", "白银狮子"),
        ["SANGUOSHA_POWER_BAI_YIN_DISPLAY_POWER.description"] = new("powers", "装备：防具。回合开始回复生命；每回合单次受伤最多 15。"),
        ["SANGUOSHA_POWER_REN_WANG_DISPLAY_POWER.title"] = new("powers", "仁王盾"),
        ["SANGUOSHA_POWER_REN_WANG_DISPLAY_POWER.description"] = new("powers", "装备：防具。当前提供敏捷。"),
        ["SANGUOSHA_POWER_BA_GUA_DISPLAY_POWER.title"] = new("powers", "八卦阵"),
        ["SANGUOSHA_POWER_BA_GUA_DISPLAY_POWER.description"] = new("powers", "装备：防具。敌人攻击你时，从抽牌堆顶判定；每打出 1 张闪获得格挡，并持续判定到攻击被完全格挡或翻出非闪。"),
        ["SANGUOSHA_POWER_CHI_TU_DISPLAY_POWER.title"] = new("powers", "赤兔"),
        ["SANGUOSHA_POWER_CHI_TU_DISPLAY_POWER.description"] = new("powers", "装备：坐骑。当前提供力量和能量。"),
        ["SANGUOSHA_POWER_DA_WAN_DISPLAY_POWER.title"] = new("powers", "大宛"),
        ["SANGUOSHA_POWER_DA_WAN_DISPLAY_POWER.description"] = new("powers", "装备：坐骑。当前提供力量。"),
        ["SANGUOSHA_POWER_DI_LU_DISPLAY_POWER.title"] = new("powers", "的卢"),
        ["SANGUOSHA_POWER_DI_LU_DISPLAY_POWER.description"] = new("powers", "装备：坐骑。当前提供敏捷。"),
        ["SANGUOSHA_POWER_YU_XI_DISPLAY_POWER.title"] = new("powers", "玉玺"),
        ["SANGUOSHA_POWER_YU_XI_DISPLAY_POWER.description"] = new("powers", "装备：宝物。每回合开始额外获得能量。"),
        ["SANGUOSHA_POWER_MU_NIU_DISPLAY_POWER.title"] = new("powers", "木牛流马"),
        ["SANGUOSHA_POWER_MU_NIU_DISPLAY_POWER.description"] = new("powers", "装备：宝物。每回合前若干次打出锦囊牌后抽牌；升级后次数提高。"),
        ["SANGUOSHA_POWER_TAI_PING_DISPLAY_POWER.title"] = new("powers", "太平要术"),
        ["SANGUOSHA_POWER_TAI_PING_DISPLAY_POWER.description"] = new("powers", "装备：宝物。回合开始净化 1 个负面效果；若成功净化，获得能量，额外回复生命。"),
        ["SANGUOSHA_POWER_KONG_CHENG_DISPLAY_POWER.title"] = new("powers", "空城"),
        ["SANGUOSHA_POWER_KONG_CHENG_DISPLAY_POWER.description"] = new("powers", "每回合首次在你打出牌结算后没有手牌时，获得 1 层无实体。"),
        ["SANGUOSHA_POWER_ZHI_HENG_DISPLAY_POWER.title"] = new("powers", "制衡"),
        ["SANGUOSHA_POWER_ZHI_HENG_DISPLAY_POWER.description"] = new("powers", "能力。每回合首次打出锦囊牌后抽牌；升级后抽牌更多。"),
        ["SANGUOSHA_POWER_WU_SHUANG_DISPLAY_POWER.title"] = new("powers", "无双"),
        ["SANGUOSHA_POWER_WU_SHUANG_DISPLAY_POWER.description"] = new("powers", "能力。每回合前若干次杀命中后追加无双伤害；升级后次数提高。"),

        ["IRONCLAD.title"] = new("characters", "赤壁猛将"),
        ["IRONCLAD.titleObject"] = new("characters", "赤壁猛将"),
        ["IRONCLAD.description"] = new("characters", "赤壁猛将承袭铁甲战士的浴血强攻：以杀的节奏、力量成长和残血爆发压制敌人。"),
        ["SILENT.title"] = new("characters", "锦囊夜行"),
        ["SILENT.titleObject"] = new("characters", "锦囊夜行"),
        ["SILENT.description"] = new("characters", "锦囊夜行承袭猎人的潜伏谋略：以技能连段、机巧资源、抽牌和负面效果掌控战局。"),
        ["DEFECT.title"] = new("characters", "机关卧龙"),
        ["DEFECT.titleObject"] = new("characters", "机关卧龙"),
        ["DEFECT.description"] = new("characters", "机关卧龙承袭故障的机关雷格：积累雷势和星辉，周期性打出全体爆发。"),
        ["NECROBINDER.title"] = new("characters", "青囊魂医"),
        ["NECROBINDER.titleObject"] = new("characters", "青囊魂医"),
        ["NECROBINDER.description"] = new("characters", "青囊魂医承袭亡缚者的魂术急救：以治疗、魂值和闪避续航拖出优势。"),
        ["REGENT.title"] = new("characters", "汉宫仁主"),
        ["REGENT.titleObject"] = new("characters", "汉宫仁主"),
        ["REGENT.description"] = new("characters", "汉宫仁主承袭储君的王道统御：以号令、星辉和交替出牌扩大团队攻防。"),

        ["SANGUOSHA_RELIC_IRONCLAD_SKILL_RELIC.title"] = new("relics", "武魂：无双"),
        ["SANGUOSHA_RELIC_IRONCLAD_SKILL_RELIC.description"] = new("relics", "赤壁猛将的武魂。战斗开始获得力量；酒、决斗、南蛮入侵、万箭齐发会推动力量与抽牌节奏。"),
        ["SANGUOSHA_RELIC_IRONCLAD_SKILL_RELIC.flavor"] = new("relics", "血火铸甲，赤壁开锋。"),
        ["SANGUOSHA_RELIC_SILENT_SKILL_RELIC.title"] = new("relics", "武魂：鬼手"),
        ["SANGUOSHA_RELIC_SILENT_SKILL_RELIC.description"] = new("relics", "锦囊夜行的武魂。战斗开始获得机巧并抽牌；锦囊按具体牌名触发抽牌、敏捷或负面效果联动。"),
        ["SANGUOSHA_RELIC_SILENT_SKILL_RELIC.flavor"] = new("relics", "胜负不在手牌里，在别人以为你没有手牌时。"),
        ["SANGUOSHA_RELIC_DEFECT_SKILL_RELIC.title"] = new("relics", "武魂：观星雷击"),
        ["SANGUOSHA_RELIC_DEFECT_SKILL_RELIC.description"] = new("relics", "机关卧龙的武魂。积累雷势和星辉，雷势满时对所有敌人造成伤害并获得星辉。"),
        ["SANGUOSHA_RELIC_DEFECT_SKILL_RELIC.flavor"] = new("relics", "风起之前，星位已经变了。"),
        ["SANGUOSHA_RELIC_NECROBINDER_SKILL_RELIC.title"] = new("relics", "武魂：青囊急救"),
        ["SANGUOSHA_RELIC_NECROBINDER_SKILL_RELIC.description"] = new("relics", "青囊魂医的武魂。治疗与部分锦囊积累魂值，魂值满时回复生命并补充手牌。"),
        ["SANGUOSHA_RELIC_NECROBINDER_SKILL_RELIC.flavor"] = new("relics", "刀兵之后，总有人要把命从鬼门关边上拉回来。"),
        ["SANGUOSHA_RELIC_REGENT_SKILL_RELIC.title"] = new("relics", "武魂：仁德激将"),
        ["SANGUOSHA_RELIC_REGENT_SKILL_RELIC.description"] = new("relics", "汉宫仁主的武魂。战斗开始获得号令和星辉；交替出牌获得能量，部分锦囊会补充星辉或手牌。"),
        ["SANGUOSHA_RELIC_REGENT_SKILL_RELIC.flavor"] = new("relics", "仁德不是软弱，是让每一次出牌都有人响应。")
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
