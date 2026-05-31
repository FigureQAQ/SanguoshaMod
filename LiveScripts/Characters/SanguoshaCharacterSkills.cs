using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Cards;
using sanguosha.Patches;
using sanguosha.Relics;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib;

namespace sanguosha.Characters;

internal enum DelayedJudgmentKind
{
    BingLiang,
    LeBu,
    ShanDian
}

internal static class SanguoshaCharacterSkills
{
    private static readonly List<IDisposable> Subscriptions = [];
    private static readonly Dictionary<ulong, CharacterSkillState> States = [];
    private static readonly List<DelayedJudgment> PendingDelayedJudgments = [];
    private static readonly HashSet<uint> ActiveLeBuAttackLocks = [];
    private static readonly HashSet<uint> ActiveBingLiangDebuffLocks = [];
    private static readonly Dictionary<ulong, decimal> DamageTakenLastEnemyTurn = [];
    private static bool HasEnemyDamageWindow;

    private static EquipSlot GetEquipSlot(string cardId)
    {
        return cardId switch
        {
            "ZhuGeCard" or "ZhangBaCard" or "QingGangCard" or "GuanShiCard" or "HanBingCard" or "QiLinCard" or "GuDingCard" or "GanJiangMoYeCard" => EquipSlot.Weapon,
            "BaiYinCard" or "RenWangCard" or "BaGuaCard" or "TengJiaCard" => EquipSlot.Armor,
            "ChiTuCard" or "DaWanCard" or "DiLuCard" or "JueYingCard" => EquipSlot.Mount,
            "YuXiCard" or "MuNiuCard" or "TaiPingCard" or "MengDeXinShuCard" => EquipSlot.Treasure,
            _ => EquipSlot.Weapon
        };
    }

    private static void ReplaceEquipment(Player player, CharacterSkillState state, EquipSlot slot, string newCardId, bool upgraded)
    {
        // 检查同槽是否有旧装备
        if (state.ActiveEquip.TryGetValue(slot, out var oldCardId))
        {
            var wasUpgraded = state.ActiveEquipUpgraded.TryGetValue(slot, out var up) && up;
            _ = RevokeEquipmentStatBonus(player, oldCardId, wasUpgraded);
            if (oldCardId != newCardId)
            {
                RemoveDisplayPower(player, oldCardId);
                _ = DoEquipmentReplaced(player, oldCardId, wasUpgraded);
                ClearEquipmentFlags(state, slot);
            }
        }
        state.ActiveEquip[slot] = newCardId;
        state.ActiveEquipUpgraded[slot] = upgraded;
    }

    private static async Task RevokeEquipmentStatBonus(Player player, string oldCardId, bool wasUpgraded)
    {
        var strength = oldCardId switch
        {
            "ZhuGeCard" or "ChiTuCard" or "DaWanCard" or "GuanShiCard" or "QiLinCard" or "GanJiangMoYeCard" => 1,
            "GuDingCard" => wasUpgraded ? 2 : 1,
            _ => 0
        };
        var dexterity = oldCardId switch
        {
            "DiLuCard" or "TaiPingCard" => wasUpgraded ? 2 : 1,
            "JueYingCard" => 1,
            _ => 0
        };

        await AdjustPower<StrengthPower>(player, player.Creature, -strength, null);
        await AdjustPower<DexterityPower>(player, player.Creature, -dexterity, null);
    }

    private static void ClearEquipmentFlags(CharacterSkillState state, EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.Weapon:
                state.ZhuGeActive = false; state.ZhuGeFreeShaPerTurn = 0;
                state.ZhangBaActive = false; state.ZhangBaUpgraded = false; state.ZhangBaGeneratedThisTurn = false;
                state.QingGangActive = false; state.QingGangBlockedTargetMultiplier = 1m;
                state.GuanShiActive = false; state.GuanShiBonusDamage = 0; state.GuanShiUsedThisTurn = false;
                state.HanBingActive = false; state.HanBingWeak = 0;
                state.QiLinActive = false; state.QiLinBonusDamage = 0; state.QiLinVulnerable = 0;
                state.GuDingActive = false;
                state.GanJiangMoYeActive = false; state.GanJiangMoYeBonusDamage = 0; state.GanJiangMoYeTriggersPerTurn = 0; state.GanJiangMoYeTriggersUsedThisTurn = 0;
                break;
            case EquipSlot.Armor:
                state.BaiYinActive = false; state.BaiYinTurnHeal = 0; state.BaiYinDamageCap = 0;
                state.RenWangActive = false; state.RenWangGuardBlock = 0; state.RenWangDraw = 0; state.RenWangUsedThisTurn = false;
                state.BaGuaActive = false; state.BaGuaUpgraded = false;
                state.TengJiaActive = false; state.TengJiaBlock = 0; state.TengJiaSlow = 0; state.TengJiaSlowCap = 0;
                break;
            case EquipSlot.Mount:
                state.ChiTuActive = false;
                state.DaWanActive = false; state.DaWanBonusDamage = 0;
                state.DiLuActive = false; state.DiLuTrickDrawUsedThisTurn = false;
                state.JueYingActive = false; state.JueYingBlock = 0; state.JueYingUsedThisTurn = false;
                break;
            case EquipSlot.Treasure:
                state.YuXiActive = false; state.YuXiTurnEnergy = 0;
                state.MuNiuActive = false; state.MuNiuTrickDraws = 0; state.MuNiuTrickDrawsUsed = 0;
                state.TaiPingActive = false; state.TaiPingTurnHeal = 0;
                state.MengDeXinShuActive = false; state.MengDeXinShuFreeCards = 0; state.MengDeXinShuUsedThisTurn = false;
                break;
        }
    }

    private static async Task DoEquipmentReplaced(Player player, string oldCardId, bool wasUpgraded)
    {
        switch (oldCardId)
        {
            case "BaiYinCard":
                await HealIfWounded(player, 10);
                break;
            case "RenWangCard":
                await GainBlock(player, wasUpgraded ? 14 : 10, null);
                await Draw(player, wasUpgraded ? 2 : 1);
                break;
            case "BaGuaCard":
                await Draw(player, wasUpgraded ? 2 : 1);
                await GainBlock(player, wasUpgraded ? 8 : 6, null);
                break;
            case "ZhuGeCard":
                var drawCount = Math.Clamp(GetState(player).TurnShaPlayed, 1, 2);
                await Draw(player, drawCount);
                break;
            case "ZhangBaCard":
                player.PlayerCombatState!.GainEnergy(1);
                await EnsureZhangBaSha(player);
                if (wasUpgraded)
                {
                    await Draw(player, 1);
                }
                break;
            case "QingGangCard":
                player.PlayerCombatState!.GainEnergy(wasUpgraded ? 2 : 1);
                break;
            case "GuanShiCard":
                EmpowerNextSha(player, wasUpgraded ? 8 : 5);
                break;
            case "HanBingCard":
                await Draw(player, wasUpgraded ? 2 : 1);
                break;
            case "QiLinCard":
                await Draw(player, wasUpgraded ? 2 : 1);
                break;
            case "GuDingCard":
                break;
            case "GanJiangMoYeCard":
                EmpowerNextSha(player, wasUpgraded ? 6 : 4);
                await Draw(player, 1);
                break;
            case "TengJiaCard":
                await GainBlock(player, wasUpgraded ? 7 : 5, null);
                break;
            case "ChiTuCard":
                player.PlayerCombatState!.GainEnergy(1);
                MakeHandCardsFree(player, wasUpgraded ? 2 : 1, IsShaLike);
                break;
            case "DaWanCard":
                EmpowerNextSha(player, wasUpgraded ? 4 : 3);
                break;
            case "DiLuCard":
                await GainBlock(player, wasUpgraded ? 10 : 6, null);
                break;
            case "JueYingCard":
                await GainBlock(player, wasUpgraded ? 8 : 6, null);
                break;
            case "YuXiCard":
                await Draw(player, wasUpgraded ? 2 : 1);
                break;
            case "MuNiuCard":
                await Draw(player, wasUpgraded ? 2 : 1);
                break;
            case "TaiPingCard":
                await HealIfWounded(player, wasUpgraded ? 10 : 6);
                break;
            case "MengDeXinShuCard":
                player.PlayerCombatState!.GainEnergy(1);
                ReduceHandCardCosts(player, 1, 1, card => IsShaLike(card) || (wasUpgraded && card.Type == CardType.Skill));
                break;
        }

        var state = GetState(player);
        if (state.XiaoJiActive && oldCardId is not null)
        {
            await Draw(player, Math.Max(1, state.XiaoJiDraw));
        }
    }

    public static void ActivateZhuGe(Player player, int freeShaPerTurn)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Weapon, "ZhuGeCard", false);
        state.ZhuGeActive = true;
        state.ZhuGeFreeShaPerTurn = Math.Max(state.ZhuGeFreeShaPerTurn, freeShaPerTurn);
        RefreshDisplayPower<ZhuGeDisplayPower>(player);
    }

    public static void MakeAllShaFreeThisTurn(Player player)
    {
        foreach (var sha in player.PlayerCombatState!.AllCards.OfType<ShaCard>())
        {
            sha.EnergyCost.SetThisTurn(0, true);
        }
    }

    public static void ActivateZhangBa(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Weapon, "ZhangBaCard", upgraded);
        state.ZhangBaActive = true;
        state.ZhangBaUpgraded = upgraded;
        RefreshDisplayPower<ZhangBaDisplayPower>(player);
    }

    public static void ActivateZhiHeng(Player player, bool upgraded)
    {
        var state = GetState(player);
        state.ZhiHengActive = true;
        state.ZhiHengDrawBonus = upgraded ? 2 : 1;
        state.ZhiHengTrickDrawUsedThisTurn = false;
        RefreshDisplayPower<ZhiHengDisplayPower>(player);
    }

    public static void ActivateWuShuang(Player player, bool upgraded)
    {
        var state = GetState(player);
        state.WuShuangActive = true;
        state.WuShuangRepeatsPerTurn = upgraded ? 3 : 2;
        state.WuShuangRepeatsUsedThisTurn = 0;
        RefreshDisplayPower<WuShuangDisplayPower>(player);
    }

    public static void ActivateLianYing(Player player, int draw, int triggersPerTurn)
    {
        var state = GetState(player);
        state.LianYingActive = true;
        state.LianYingDraw = Math.Max(state.LianYingDraw, draw);
        state.LianYingTriggersPerTurn = Math.Max(state.LianYingTriggersPerTurn, triggersPerTurn);
        state.LianYingTriggersUsedThisTurn = 0;
        RefreshDisplayPower<LianYingDisplayPower>(player);
    }

    public static void ActivateYiJi(Player player, int draw, int lowHandDraw, int freeCards)
    {
        var state = GetState(player);
        state.YiJiActive = true;
        state.YiJiDraw = Math.Max(state.YiJiDraw, draw);
        state.YiJiLowHandDraw = Math.Max(state.YiJiLowHandDraw, lowHandDraw);
        state.YiJiFreeCards = Math.Max(state.YiJiFreeCards, freeCards);
        state.YiJiTriggeredThisTurn = false;
        RefreshDisplayPower<YiJiDisplayPower>(player);
    }

    public static void ActivateJianXiong(Player player, int draw, int nextShaDamage, int energy)
    {
        var state = GetState(player);
        state.JianXiongActive = true;
        state.JianXiongDraw = Math.Max(state.JianXiongDraw, draw);
        state.JianXiongNextShaDamage = Math.Max(state.JianXiongNextShaDamage, nextShaDamage);
        state.JianXiongEnergy = Math.Max(state.JianXiongEnergy, energy);
        state.JianXiongTriggeredThisTurn = false;
        RefreshDisplayPower<JianXiongDisplayPower>(player);
    }

    public static void ActivateGuiCai(Player player, int block, int draw, int weak)
    {
        var state = GetState(player);
        state.GuiCaiActive = true;
        state.GuiCaiBlock = Math.Max(state.GuiCaiBlock, block);
        state.GuiCaiDraw = Math.Max(state.GuiCaiDraw, draw);
        state.GuiCaiWeak = Math.Max(state.GuiCaiWeak, weak);
        RefreshDisplayPower<GuiCaiDisplayPower>(player);
    }

    public static void ActivateYingZi(Player player, int draw)
    {
        var state = GetState(player);
        state.YingZiActive = true;
        state.YingZiDraw = Math.Max(state.YingZiDraw, draw);
        RefreshDisplayPower<YingZiDisplayPower>(player);
    }

    public static void ActivateJiZhi(Player player, int draw)
    {
        var state = GetState(player);
        state.JiZhiActive = true;
        state.JiZhiDraw = Math.Max(state.JiZhiDraw, draw);
        state.JiZhiUsedThisTurn = false;
        RefreshDisplayPower<JiZhiDisplayPower>(player);
    }

    public static void ActivateLuoYi(Player player, int nextShaDamage)
    {
        var state = GetState(player);
        state.LuoYiActive = true;
        state.LuoYiNextShaDamage = Math.Max(state.LuoYiNextShaDamage, nextShaDamage);
        RefreshDisplayPower<LuoYiDisplayPower>(player);
    }

    public static void ActivateTieQi(Player player, int vulnerable)
    {
        var state = GetState(player);
        state.TieQiActive = true;
        state.TieQiVulnerable = Math.Max(state.TieQiVulnerable, vulnerable);
        state.TieQiUsedThisTurn = false;
        RefreshDisplayPower<TieQiDisplayPower>(player);
    }

    public static void ActivateQingNang(Player player, int heal)
    {
        var state = GetState(player);
        state.QingNangActive = true;
        state.QingNangHeal = Math.Max(state.QingNangHeal, heal);
        RefreshDisplayPower<QingNangDisplayPower>(player);
    }

    public static void ActivateXiaoJi(Player player, int draw)
    {
        var state = GetState(player);
        state.XiaoJiActive = true;
        state.XiaoJiDraw = Math.Max(state.XiaoJiDraw, draw);
        RefreshDisplayPower<XiaoJiDisplayPower>(player);
    }

    public static void ActivateFenYing(Player player, int block)
    {
        var state = GetState(player);
        state.FenYingActive = true;
        state.FenYingBlock = Math.Max(state.FenYingBlock, block);
        RefreshDisplayPower<FenYingDisplayPower>(player);
    }

    public static void ActivatePoZhu(Player player, int strengthPerVulnerable)
    {
        var state = GetState(player);
        state.PoZhuActive = true;
        state.PoZhuStrengthPerVulnerable = Math.Max(state.PoZhuStrengthPerVulnerable, strengthPerVulnerable);
        RefreshDisplayPower<PoZhuDisplayPower>(player);
    }

    public static bool IsZhangBaUpgraded(Player player)
    {
        var state = GetState(player);
        return state.ZhangBaActive && state.ZhangBaUpgraded;
    }

    public static bool IsShaLike(CardModel card)
    {
        return card is ShaCard or ZhangBaShaCard;
    }

    public static bool HasPlayedShaThisTurn(Player player)
    {
        return GetState(player).TurnShaPlayed > 0;
    }

    public static bool HasPlayedAttackThisTurn(Player player)
    {
        return GetState(player).TurnAttacksPlayed > 0;
    }

    public static async Task EnsureZhangBaSha(Player player)
    {
        var state = GetState(player);
        if (!state.ZhangBaActive)
        {
            return;
        }

        var hand = player.PlayerCombatState!.Hand.Cards;
        if (state.ZhangBaGeneratedThisTurn
            || hand.Any(card => card is ZhangBaShaCard)
            || hand.Count(card => card is not ZhangBaShaCard) < 2)
        {
            return;
        }

        var result = await CardPileCmd.AddGeneratedCardToCombat(
            ModelDb.Card<ZhangBaShaCard>(),
            PileType.Hand,
            player,
            CardPilePosition.Top);
        if (result.success && result.cardAdded is not null)
        {
            state.ZhangBaGeneratedThisTurn = true;
            result.cardAdded.EnergyCost.SetThisTurn(0, true);
            result.cardAdded.ExhaustOnNextPlay = true;
        }
    }

    public static void ActivateBaGua(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Armor, "BaGuaCard", upgraded);
        state.BaGuaActive = true;
        state.BaGuaUpgraded = upgraded;
        RefreshDisplayPower<BaGuaDisplayPower>(player);
    }

    public static void ActivateBaiYin(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Armor, "BaiYinCard", upgraded);
        state.BaiYinActive = true;
        state.BaiYinTurnHeal = 0;
        state.BaiYinDamageCap = upgraded ? 15 : 20;
        RefreshDisplayPower<BaiYinDisplayPower>(player);
    }

    public static void ActivateChiTu(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Mount, "ChiTuCard", upgraded);
        state.ChiTuActive = true;
        RefreshDisplayPower<ChiTuDisplayPower>(player);
    }

    public static void ActivateDaWan(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Mount, "DaWanCard", upgraded);
        state.DaWanActive = true;
        state.DaWanBonusDamage = upgraded ? 3 : 2;
        RefreshDisplayPower<DaWanDisplayPower>(player);
    }

    public static void ActivateDiLu(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Mount, "DiLuCard", upgraded);
        state.DiLuActive = true;
        RefreshDisplayPower<DiLuDisplayPower>(player);
    }

    public static void ActivateGuanShi(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Weapon, "GuanShiCard", upgraded);
        state.GuanShiActive = true;
        state.GuanShiBonusDamage = upgraded ? 6 : 4;
        state.GuanShiUsedThisTurn = false;
        RefreshDisplayPower<GuanShiDisplayPower>(player);
    }

    public static void ActivateHanBing(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Weapon, "HanBingCard", upgraded);
        state.HanBingActive = true;
        state.HanBingWeak = upgraded ? 2 : 1;
        RefreshDisplayPower<HanBingDisplayPower>(player);
    }

    public static void ActivateQiLin(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Weapon, "QiLinCard", upgraded);
        state.QiLinActive = true;
        state.QiLinBonusDamage = upgraded ? 5 : 3;
        state.QiLinVulnerable = 1;
        RefreshDisplayPower<QiLinDisplayPower>(player);
    }

    public static void ActivateRenWang(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Armor, "RenWangCard", upgraded);
        state.RenWangActive = true;
        state.RenWangGuardBlock = upgraded ? 10 : 6;
        state.RenWangDraw = upgraded ? 1 : 0;
        state.RenWangUsedThisTurn = false;
        RefreshDisplayPower<RenWangDisplayPower>(player);
    }

    public static void ActivateQingGang(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Weapon, "QingGangCard", upgraded);
        state.QingGangActive = true;
        state.QingGangBlockedTargetMultiplier = upgraded ? 1.5m : 1m;
        RefreshDisplayPower<QingGangDisplayPower>(player);
    }

    public static void ActivateGuDing(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Weapon, "GuDingCard", upgraded);
        state.GuDingActive = true;
        RefreshDisplayPower<GuDingDisplayPower>(player);
    }

    public static void ActivateGanJiangMoYe(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Weapon, "GanJiangMoYeCard", upgraded);
        state.GanJiangMoYeActive = true;
        state.GanJiangMoYeBonusDamage = upgraded ? 5 : 4;
        state.GanJiangMoYeTriggersPerTurn = upgraded ? 3 : 2;
        state.GanJiangMoYeTriggersUsedThisTurn = 0;
        RefreshDisplayPower<GanJiangMoYeDisplayPower>(player);
    }

    public static void ActivateTengJia(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Armor, "TengJiaCard", upgraded);
        state.TengJiaActive = true;
        state.TengJiaBlock = 10;
        state.TengJiaSlow = 1;
        state.TengJiaSlowCap = upgraded ? 3 : 5;
        RefreshDisplayPower<TengJiaDisplayPower>(player);
    }

    public static void ActivateJueYing(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Mount, "JueYingCard", upgraded);
        state.JueYingActive = true;
        state.JueYingBlock = upgraded ? 8 : 6;
        state.JueYingUsedThisTurn = false;
        RefreshDisplayPower<JueYingDisplayPower>(player);
    }

    public static void ActivateYuXi(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Treasure, "YuXiCard", upgraded);
        state.YuXiActive = true;
        state.YuXiTurnEnergy = 1;
        RefreshDisplayPower<YuXiDisplayPower>(player);
    }

    public static void ActivateMuNiu(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Treasure, "MuNiuCard", upgraded);
        state.MuNiuActive = true;
        state.MuNiuTrickDraws = upgraded ? 2 : 1;
        state.MuNiuTrickDrawsUsed = 0;
        RefreshDisplayPower<MuNiuDisplayPower>(player);
    }

    public static void ActivateTaiPing(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Treasure, "TaiPingCard", upgraded);
        state.TaiPingActive = true;
        state.TaiPingTurnHeal = upgraded ? 2 : 1;
        RefreshDisplayPower<TaiPingDisplayPower>(player);
    }

    public static void ActivateMengDeXinShu(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Treasure, "MengDeXinShuCard", upgraded);
        state.MengDeXinShuActive = true;
        state.MengDeXinShuFreeCards = upgraded ? 2 : 1;
        state.MengDeXinShuUsedThisTurn = false;
        RefreshDisplayPower<MengDeXinShuDisplayPower>(player);
    }

    public static void ActivateKongCheng(Player player, int intangible)
    {
        var state = GetState(player);
        state.KongChengActive = true;
        state.KongChengIntangible = Math.Max(1, intangible);
        state.KongChengPendingNextTurn = true;
        RefreshDisplayPower<KongChengDisplayPower>(player);
    }

    public static bool IsKongChengAttackLocked(Player player)
    {
        return GetState(player).KongChengAttackLockedThisTurn;
    }

    public static void ActivateLongDan(Player player)
    {
        var state = GetState(player);
        var wasActive = state.LongDanActive;
        state.LongDanActive = true;
        if (!wasActive)
        {
            state.LongDanFreeUsedThisTurn = false;
        }

        RefreshLongDanFreeCard(player, state);
        RefreshDisplayPower<LongDanDisplayPower>(player);
    }

    public static void ActivateBathOfBlood(Player player, int hpLoss, int energy)
    {
        var state = GetState(player);
        state.BathOfBloodActive = true;
        state.BathOfBloodHpLoss = Math.Max(state.BathOfBloodHpLoss, hpLoss);
        state.BathOfBloodEnergy = Math.Max(state.BathOfBloodEnergy, energy);
        RefreshDisplayPower<BathOfBloodDisplayPower>(player);
    }

    public static void ActivateNightfallScheme(Player player, int draw, int poison)
    {
        var state = GetState(player);
        state.NightfallSchemeActive = true;
        state.NightfallSchemeDraw = Math.Max(state.NightfallSchemeDraw, draw);
        state.NightfallSchemePoison = Math.Max(state.NightfallSchemePoison, poison);
        RefreshDisplayPower<NightfallSchemeDisplayPower>(player);
    }

    public static void ActivateThunderMandate(Player player, int thunder, int damage)
    {
        var state = GetState(player);
        state.ThunderMandateActive = true;
        state.ThunderMandateThunder = Math.Max(state.ThunderMandateThunder, thunder);
        state.ThunderMandateDamage = Math.Max(state.ThunderMandateDamage, damage);
        RefreshDisplayPower<ThunderMandateDisplayPower>(player);
    }

    public static void ActivateSoulHealerForm(Player player, int heal, int enemyHpLoss)
    {
        var state = GetState(player);
        state.SoulHealerFormActive = true;
        state.SoulHealerFormHeal = Math.Max(state.SoulHealerFormHeal, heal);
        state.SoulHealerFormEnemyHpLoss = Math.Max(state.SoulHealerFormEnemyHpLoss, enemyHpLoss);
        RefreshDisplayPower<SoulHealerFormDisplayPower>(player);
    }

    public static void ActivateImperialEdict(Player player, int energy, int nextShaDamage)
    {
        var state = GetState(player);
        state.ImperialEdictActive = true;
        state.ImperialEdictEnergy = Math.Max(state.ImperialEdictEnergy, energy);
        state.ImperialEdictNextShaDamage = Math.Max(state.ImperialEdictNextShaDamage, nextShaDamage);
        RefreshDisplayPower<ImperialEdictDisplayPower>(player);
    }

    public static void RegisterTemporaryStolenBuff(Player player, ModelId powerId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        GetState(player).TemporaryStolenBuffs.Add(new TemporaryPowerAmount(powerId, amount));
    }

    public static void AddDelayedJudgment(
        Player source,
        Creature target,
        DelayedJudgmentKind kind,
        int value,
        CardModel sourceCard)
    {
        if (!target.IsAlive)
        {
            return;
        }

        PendingDelayedJudgments.RemoveAll(judgment =>
            judgment.TargetCombatId == CreatureKey(target) && judgment.Kind == kind);
        PendingDelayedJudgments.Add(new DelayedJudgment(source, CreatureKey(target), kind, Math.Max(0, value), sourceCard));
    }

    public static bool IsLeBuAttackPrevented(Creature creature)
    {
        return ActiveLeBuAttackLocks.Contains(CreatureKey(creature));
    }

    public static bool ShouldBlockBingLiangDebuff(Creature? giver, Creature target)
    {
        return giver is not null
            && target.IsPlayer
            && ActiveBingLiangDebuffLocks.Contains(CreatureKey(giver));
    }

    public static bool ShouldBlockBingLiangStatusCards(Player player, Creature? source)
    {
        return player.Creature.IsAlive
            && source is { IsPlayer: false }
            && ActiveBingLiangDebuffLocks.Contains(CreatureKey(source));
    }

    internal static void ClampTengJiaSlow(Player player, SlowPower slowPower)
    {
        var state = GetState(player);
        if (!state.TengJiaActive || state.TengJiaSlowCap <= 0)
        {
            return;
        }

        var slowAmount = slowPower.DynamicVars["SlowAmount"];
        if (slowAmount.BaseValue > state.TengJiaSlowCap)
        {
            slowAmount.BaseValue = state.TengJiaSlowCap;
        }
    }

    internal static IReadOnlyList<ExtraIconAmountLabelSlot> GetDisplayPowerCounters(SanguoshaEquipmentDisplayPower power)
    {
        var player = TryGetPowerPlayer(power);
        if (player is null)
        {
            return [];
        }

        var state = GetState(player);

        static int Available(bool used) => used ? 0 : 1;
        static int UsesRemaining(int used, int total) => Math.Max(0, Math.Max(0, total) - used);

        var amount = power switch
        {
            IroncladSkillDisplayPower => state.TurnShaPlayed,
            SilentSkillDisplayPower => Available(state.PoisonTrickBonusGrantedThisTurn),
            DefectSkillDisplayPower => state.ThunderstormCharges > 0 ? 4 : state.Thunder,
            NecrobinderSkillDisplayPower => null,
            RegentSkillDisplayPower => player.PlayerCombatState?.Stars ?? 0,
            LongDanDisplayPower => Available(state.LongDanFreeUsedThisTurn),
            WuShuangDisplayPower => UsesRemaining(state.WuShuangRepeatsUsedThisTurn, state.WuShuangRepeatsPerTurn),
            LianYingDisplayPower => UsesRemaining(state.LianYingTriggersUsedThisTurn, state.LianYingTriggersPerTurn),
            YiJiDisplayPower => Available(state.YiJiTriggeredThisTurn),
            JianXiongDisplayPower => Available(state.JianXiongTriggeredThisTurn),
            YingZiDisplayPower => state.YingZiDraw,
            JiZhiDisplayPower => Available(state.JiZhiUsedThisTurn),
            LuoYiDisplayPower => state.LuoYiNextShaDamage,
            TieQiDisplayPower => Available(state.TieQiUsedThisTurn),
            QingNangDisplayPower => state.QingNangHeal,
            XiaoJiDisplayPower => state.XiaoJiDraw,
            FenYingDisplayPower => state.FenYingBlock,
            PoZhuDisplayPower => state.PoZhuStrengthPerVulnerable,
            BathOfBloodDisplayPower => state.BathOfBloodEnergy,
            NightfallSchemeDisplayPower => state.NightfallSchemePoison,
            ThunderMandateDisplayPower => state.ThunderMandateThunder,
            SoulHealerFormDisplayPower => state.SoulHealerFormHeal,
            ImperialEdictDisplayPower => state.ImperialEdictEnergy,
            GoodLuckDisplayPower => state.GoodLuck,
            ZhiHengDisplayPower => Available(state.ZhiHengTrickDrawUsedThisTurn),
            MengDeXinShuDisplayPower => Available(state.MengDeXinShuUsedThisTurn),
            ZhuGeDisplayPower => Math.Max(1, state.ZhuGeFreeShaPerTurn),
            MuNiuDisplayPower => UsesRemaining(state.MuNiuTrickDrawsUsed, state.MuNiuTrickDraws),
            GuanShiDisplayPower => Available(state.GuanShiUsedThisTurn),
            GanJiangMoYeDisplayPower => UsesRemaining(state.GanJiangMoYeTriggersUsedThisTurn, state.GanJiangMoYeTriggersPerTurn),
            RenWangDisplayPower => Available(state.RenWangUsedThisTurn),
            JueYingDisplayPower => Available(state.JueYingUsedThisTurn),
            _ => (int?)null
        };

        return amount is null
            ? []
            : [ExtraIconAmountLabelSlot.At(ExtraIconAmountLabelCorner.BottomRight, amount.Value.ToString())];
    }

    private static Player? TryGetPowerPlayer(PowerModel power)
    {
        try
        {
            return power.Owner.Player;
        }
        catch
        {
            return null;
        }
    }

    private static void InvalidateDisplayCounters(Player player)
    {
        foreach (var power in player.Creature.Powers.OfType<SanguoshaEquipmentDisplayPower>())
        {
            power.InvalidateExtraIconAmountLabels();
        }
    }

    private static void RemoveSkillDisplayPowers(Player player)
    {
        foreach (var power in player.Creature.Powers.Where(IsSkillDisplayPower).ToList())
        {
            _ = PowerCmd.Remove(power);
        }
    }

    private static bool IsSkillDisplayPower(PowerModel power)
    {
        return power is IroncladSkillDisplayPower
            or SilentSkillDisplayPower
            or DefectSkillDisplayPower
            or NecrobinderSkillDisplayPower
            or RegentSkillDisplayPower;
    }

    private static void RefreshDisplayPower<TPower>(Player player)
        where TPower : ModPowerTemplate
    {
        var creature = player.Creature;
        var existing = creature.Powers.OfType<TPower>().FirstOrDefault();
        if (existing is not null)
        {
            _ = PowerCmd.Remove(existing);
        }

        _ = PowerCmd.Apply<TPower>(NewContext(), creature, 1, creature, null!, false);
    }

    private static void RemoveDisplayPower(Player player, string cardId)
    {
        switch (cardId)
        {
            case "ZhuGeCard":
                RemoveDisplayPower<ZhuGeDisplayPower>(player);
                break;
            case "ZhangBaCard":
                RemoveDisplayPower<ZhangBaDisplayPower>(player);
                break;
            case "QingGangCard":
                RemoveDisplayPower<QingGangDisplayPower>(player);
                break;
            case "GuanShiCard":
                RemoveDisplayPower<GuanShiDisplayPower>(player);
                break;
            case "HanBingCard":
                RemoveDisplayPower<HanBingDisplayPower>(player);
                break;
            case "QiLinCard":
                RemoveDisplayPower<QiLinDisplayPower>(player);
                break;
            case "GuDingCard":
                RemoveDisplayPower<GuDingDisplayPower>(player);
                break;
            case "GanJiangMoYeCard":
                RemoveDisplayPower<GanJiangMoYeDisplayPower>(player);
                break;
            case "BaiYinCard":
                RemoveDisplayPower<BaiYinDisplayPower>(player);
                break;
            case "RenWangCard":
                RemoveDisplayPower<RenWangDisplayPower>(player);
                break;
            case "BaGuaCard":
                RemoveDisplayPower<BaGuaDisplayPower>(player);
                break;
            case "TengJiaCard":
                RemoveDisplayPower<TengJiaDisplayPower>(player);
                break;
            case "ChiTuCard":
                RemoveDisplayPower<ChiTuDisplayPower>(player);
                break;
            case "DaWanCard":
                RemoveDisplayPower<DaWanDisplayPower>(player);
                break;
            case "DiLuCard":
                RemoveDisplayPower<DiLuDisplayPower>(player);
                break;
            case "JueYingCard":
                RemoveDisplayPower<JueYingDisplayPower>(player);
                break;
            case "YuXiCard":
                RemoveDisplayPower<YuXiDisplayPower>(player);
                break;
            case "MuNiuCard":
                RemoveDisplayPower<MuNiuDisplayPower>(player);
                break;
            case "TaiPingCard":
                RemoveDisplayPower<TaiPingDisplayPower>(player);
                break;
            case "MengDeXinShuCard":
                RemoveDisplayPower<MengDeXinShuDisplayPower>(player);
                break;
        }
    }

    private static void RemoveDisplayPower<TPower>(Player player)
        where TPower : PowerModel
    {
        var existing = player.Creature.Powers.OfType<TPower>().FirstOrDefault();
        if (existing is not null)
        {
            _ = PowerCmd.Remove(existing);
        }
    }

    public static void ActivateTieSuo(Player player, bool upgraded)
    {
        var state = GetState(player);
        state.TieSuoThisTurn = true;
        state.TieSuoSplashMultiplier = upgraded ? 0.75m : 0.5m;
    }

    public static void AddGoodLuck(Player player, int amount)
    {
        if (amount <= 0 || !player.Creature.IsAlive)
        {
            return;
        }

        var state = GetState(player);
        state.GoodLuck += amount;
        RefreshDisplayPower<GoodLuckDisplayPower>(player);
        InvalidateDisplayCounters(player);
    }

    public static void TrackPlayerDamageTaken(Player player, decimal amount)
    {
        if (amount <= 0)
        {
            return;
        }

        DamageTakenLastEnemyTurn[player.NetId] =
            DamageTakenLastEnemyTurn.GetValueOrDefault(player.NetId) + amount;
    }

    public static async Task OnCardExhaustedFromHand(Player player, CardModel exhaustedCard, CardModel? source)
    {
        var state = GetState(player);
        if (!state.FenYingActive || !player.Creature.IsAlive)
        {
            return;
        }

        await GainBlock(player, Math.Max(1, state.FenYingBlock), null);
    }

    public static async Task OnVulnerableApplied(Player player, Creature target, decimal amount, CardModel? source)
    {
        var state = GetState(player);
        if (!state.PoZhuActive || amount <= 0 || !player.Creature.IsAlive)
        {
            return;
        }

        var strength = Math.Ceiling(amount) * Math.Max(1, state.PoZhuStrengthPerVulnerable);
        await ApplyPower<StrengthPower>(player, player.Creature, strength, source);
    }

    public static bool ShouldPierceBlock(Player player)
    {
        return GetState(player).QingGangActive;
    }

    public static decimal GetBlockPiercePercent(Player player)
    {
        return GetState(player).QingGangActive ? 1m : 0m;
    }

    public static decimal GetAttackDamageMultiplier(Player player, Creature target, bool targetHadBlock)
    {
        var state = GetState(player);
        var multiplier = 1m;
        if (state.GuDingActive
            && target.MaxHp > 0
            && target.CurrentHp <= Math.Ceiling(target.MaxHp / 2m))
        {
            multiplier *= 1.5m;
        }

        if (state.QingGangActive && targetHadBlock)
        {
            multiplier *= Math.Max(1m, state.QingGangBlockedTargetMultiplier);
        }

        return multiplier;
    }

    public static decimal GetAttackCardDamageMultiplier(CardModel card)
    {
        if (card.Type != CardType.Attack)
        {
            return 1m;
        }

        var state = GetState(card.Owner);
        if (state.JiuDoubledAttackCard is not null
            && !ReferenceEquals(state.JiuDoubledAttackCard, card))
        {
            state.JiuDoubledAttackCard = null;
            state.JiuActiveAttackDamageMultiplier = 1m;
        }

        if (ReferenceEquals(state.JiuDoubledAttackCard, card))
        {
            return Math.Max(1m, state.JiuActiveAttackDamageMultiplier);
        }

        if (state.JiuNextAttackDamageMultiplier <= 1m)
        {
            return 1m;
        }

        state.JiuDoubledAttackCard = card;
        state.JiuActiveAttackDamageMultiplier = Math.Max(1m, state.JiuNextAttackDamageMultiplier);
        state.JiuNextAttackDamageMultiplier = 1m;
        return state.JiuActiveAttackDamageMultiplier;
    }

    public static decimal GetSovereignBladeDamageMultiplier(SovereignBlade card)
    {
        return GetAttackCardDamageMultiplier(card);
    }

    public static decimal GetAttackFlatBonus(Player player, Creature target, bool targetHadBlock)
    {
        var state = GetState(player);
        var bonus = 0m;

        if (state.JiuNextShaBonus > 0)
        {
            bonus += state.JiuNextShaBonus;
        }

        if (state.GuanShiActive && !state.GuanShiUsedThisTurn)
        {
            state.GuanShiUsedThisTurn = true;
            bonus += state.GuanShiBonusDamage;
        }

        if (state.QiLinActive && SanguoshaCardFx.IsDebuffed(target))
        {
            bonus += state.QiLinBonusDamage;
        }

        if (state.DaWanActive)
        {
            bonus += state.DaWanBonusDamage;
        }

        return bonus;
    }

    public static async Task ApplyAttackFollowups(
        PlayerChoiceContext context,
        CardPlay play,
        Creature target,
        decimal shaAmount)
    {
        if (!target.IsAlive)
        {
            return;
        }

        var state = GetState(play.Card.Owner);
        if (IsShaLike(play.Card) && state.JiuNextShaBonus > 0)
        {
            state.JiuNextShaBonus = 0;
        }

        if (state.HanBingActive)
        {
            await ApplyPower<WeakPower>(play.Card.Owner, target, state.HanBingWeak, play.Card);
        }

        if (state.QiLinActive
            && SanguoshaCardFx.IsDebuffed(target)
            && !state.QiLinVulnerableUsedThisTurn)
        {
            state.QiLinVulnerableUsedThisTurn = true;
            await ApplyPower<VulnerablePower>(play.Card.Owner, target, state.QiLinVulnerable, play.Card);
        }

        if (IsShaLike(play.Card))
        {
            await ApplyShaInfusionFollowup(context, play, target, state, shaAmount);
        }

        if (state.WuShuangActive
            && IsShaLike(play.Card)
            && state.WuShuangRepeatsUsedThisTurn < state.WuShuangRepeatsPerTurn)
        {
            state.WuShuangRepeatsUsedThisTurn++;
            await DamageTarget(play.Card.Owner, target, 3, play.Card);
        }

        if (state.GanJiangMoYeActive
            && IsShaLike(play.Card)
            && state.GanJiangMoYeTriggersUsedThisTurn < state.GanJiangMoYeTriggersPerTurn)
        {
            state.GanJiangMoYeTriggersUsedThisTurn++;
            await DamageTarget(play.Card.Owner, target, state.GanJiangMoYeBonusDamage, play.Card);
        }

        if (state.TieQiActive
            && IsShaLike(play.Card)
            && !state.TieQiUsedThisTurn)
        {
            state.TieQiUsedThisTurn = true;
            await ApplyPower<VulnerablePower>(play.Card.Owner, target, Math.Max(1, state.TieQiVulnerable), play.Card);
        }

        InvalidateDisplayCounters(play.Card.Owner);
    }

    private static async Task ApplyShaInfusionFollowup(
        PlayerChoiceContext context,
        CardPlay play,
        Creature target,
        CharacterSkillState state,
        decimal shaAmount)
    {
        var player = play.Card.Owner;
        switch (state.ShaInfusion)
        {
            case ShaInfusion.Fire:
                await DamageTarget(player, target, 2, play.Card);
                break;
            case ShaInfusion.Poison:
                await ApplyPower<PoisonPower>(player, target, 3, play.Card);
                break;
            case ShaInfusion.Thunder:
                await ConsumeThunderstormIfReady(play.Card.CombatState, player, state, play.Card);
                state.Thunder++;
                if (play.Card.CombatState is not null)
                {
                    await DamageTarget(player, target, 2, play.Card);
                    ArmThunderstormIfReady(state);
                }
                break;
            case ShaInfusion.Stored:
                await ForgeRegentSovereignBlade(player, play.Card, shaAmount);
                break;
            case ShaInfusion.Calamity:
                var doomAmount = Math.Max(1, (int)Math.Ceiling(shaAmount / 2m));
                await ApplyPower<DoomPower>(player, target, doomAmount, play.Card);
                break;
        }
    }

    public static decimal GetAttackSplashMultiplier(Player player)
    {
        var state = GetState(player);
        return state.TieSuoThisTurn ? state.TieSuoSplashMultiplier : 0m;
    }

    public static void EmpowerNextSha(Player player, int bonusDamage)
    {
        var state = GetState(player);
        state.JiuNextShaBonus += Math.Max(0, bonusDamage);
    }

    public static void DoubleNextAttackDamage(Player player)
    {
        var state = GetState(player);
        state.JiuNextAttackDamageMultiplier = Math.Max(2m, state.JiuNextAttackDamageMultiplier);
    }

    private static void ClearJiuAttackDamageMultiplier(CharacterSkillState state)
    {
        state.JiuNextAttackDamageMultiplier = 1m;
        state.JiuDoubledAttackCard = null;
        state.JiuActiveAttackDamageMultiplier = 1m;
    }

    public static void ClearAttackCardDamageMultiplier(CardModel card)
    {
        var owner = card.Owner;
        if (owner is null || card.Type != CardType.Attack)
        {
            return;
        }

        var state = GetState(owner);
        if (!ReferenceEquals(state.JiuDoubledAttackCard, card))
        {
            return;
        }

        state.JiuDoubledAttackCard = null;
        state.JiuActiveAttackDamageMultiplier = 1m;
    }

    private static void ClearAttackCardDamageMultiplier(CardPlay play)
    {
        ClearAttackCardDamageMultiplier(play.Card);
    }

    public static void Register()
    {
        if (Subscriptions.Count > 0)
        {
            return;
        }

        Subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(OnCombatStarting));
        Subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(OnSideTurnStarted));
        Subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<CardPlayedEvent>(OnCardPlayed));
        Subscriptions.Add(RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ =>
        {
            States.Clear();
            ClearDelayedJudgmentState();
            DamageTakenLastEnemyTurn.Clear();
            HasEnemyDamageWindow = false;
            BaiYinDamageCapPatch.Clear();
        }));

        Entry.Logger.Info("Sanguosha character skill layer registered.");
    }

    public static void TryApplyDeathGate(Creature creature, ref decimal hpLoss)
    {
        // Removed: death gate resurrection effect
    }

    private static async void OnCombatStarting(CombatStartingEvent evt)
    {
        try
        {
            var combat = evt.CombatState;
            if (combat is null)
            {
                return;
            }

            ClearDelayedJudgmentState();
            HasEnemyDamageWindow = false;
            foreach (var player in combat.Players)
            {
                var state = ResetCombatState(player);
                DamageTakenLastEnemyTurn[player.NetId] = 0;
                RemoveSkillDisplayPowers(player);
                await RunOpeningSkill(player, state);
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Sanguosha combat-start skill failed: {ex}");
        }
    }

    private static async void OnSideTurnStarted(SideTurnStartedEvent evt)
    {
        try
        {
            foreach (var player in evt.CombatState.Players.Where(player => player.Creature.IsAlive))
            {
                await ClearTemporaryStolenBuffs(player);
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Sanguosha temporary buff cleanup failed: {ex}");
        }

        if (evt.Side == CombatSide.Enemy)
        {
            DamageTakenLastEnemyTurn.Clear();
            HasEnemyDamageWindow = true;
            foreach (var player in evt.CombatState.Players)
            {
                DamageTakenLastEnemyTurn[player.NetId] = 0;
            }

            try
            {
                await ResolveDelayedJudgments(evt.CombatState);
            }
            catch (Exception ex)
            {
                Entry.Logger.Warn($"Sanguosha delayed judgment failed: {ex}");
            }

            return;
        }

        ActiveLeBuAttackLocks.Clear();
        ActiveBingLiangDebuffLocks.Clear();

        if (evt.Side != CombatSide.Player)
        {
            return;
        }

        try
        {
            foreach (var player in evt.CombatState.Players.Where(player => player.Creature.IsAlive))
            {
                var state = GetState(player);
                RemoveSkillDisplayPowers(player);
                state.TurnCardsPlayed = 0;
                state.TurnAttacksPlayed = 0;
                state.TurnShaPlayed = 0;
                state.TurnTricksPlayed = 0;
                state.WuShuangRepeatsUsedThisTurn = 0;
                state.GanJiangMoYeTriggersUsedThisTurn = 0;
                state.ZhiHengTrickDrawUsedThisTurn = false;
                state.TieSuoThisTurn = false;
                state.EnergyGrantedThisTurn = false;
                state.IngenuityEnergyGrantedThisTurn = false;
                state.FireVulnerableUsedThisTurn = false;
                state.PoisonTrickBonusGrantedThisTurn = false;
                state.NextPoisonShaBonus = 0;
                state.ThunderDischargedThisTurn = false;
                state.ThunderstormFreshlyArmed = false;
                state.HealedThisTurn = false;
                state.QiLinVulnerableUsedThisTurn = false;
                state.BaGuaEnergyGrantedThisTurn = false;
                state.ChiTuEnergyGrantedThisTurn = false;
                state.JueYingUsedThisTurn = false;
                state.DiLuTrickDrawUsedThisTurn = false;
                state.RenWangUsedThisTurn = false;
                state.GuanShiUsedThisTurn = false;
                state.MuNiuTrickDrawsUsed = 0;
                state.MengDeXinShuUsedThisTurn = false;
                state.KongChengAttackLockedThisTurn = false;
                state.LianYingTriggersUsedThisTurn = 0;
                state.ZhangBaGeneratedThisTurn = false;
                state.LongDanFreeUsedThisTurn = false;
                state.YiJiTriggeredThisTurn = false;
                state.JianXiongTriggeredThisTurn = false;
                state.JiZhiUsedThisTurn = false;
                state.TieQiUsedThisTurn = false;
                ClearJiuAttackDamageMultiplier(state);
                state.LastCardType = null;
                BaiYinDamageCapPatch.ResetDamageThisTurn(player);

                await ResolveGoodLuck(player, state);
                await ApplyKongChengTurnStart(player, state);
                ApplyZhuGeTurnStart(player, state);
                RefreshLongDanFreeCard(player, state);
                await ApplyEquipmentTurnStart(player, state);
                await ApplyGeneralPowerTurnStart(player, state, evt.CombatState);
                await EnsureZhangBaSha(player);
                await ApplyTurnFloor(player, state);
                await RunTurnStartSkill(player, state, evt.CombatState);
                RefreshLongDanFreeCard(player, state);
                InvalidateDisplayCounters(player);
            }

            AwardGoodLuckForLeastDamageTaken(evt.CombatState);
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Sanguosha turn-start skill failed: {ex}");
        }
    }

    private static async void OnCardPlayed(CardPlayedEvent evt)
    {
        CardPlay? cardPlay = null;
        try
        {
            cardPlay = evt.CardPlay;
            var card = cardPlay.Card;
            var player = card.Owner;
            if (player is null || !player.Creature.IsAlive || IsNonPlayableNoise(card))
            {
                return;
            }

            var state = GetState(player);
            state.TurnCardsPlayed++;
            if (card.Type == CardType.Attack)
            {
                state.TurnAttacksPlayed++;
            }

            if (IsShaLike(card))
            {
                state.TurnShaPlayed++;
            }

            if (card.Type == CardType.Skill)
            {
                state.TurnTricksPlayed++;
            }

            if (state.LongDanActive
                && !state.LongDanFreeUsedThisTurn
                && IsLongDanFreeEligible(card))
            {
                state.LongDanFreeUsedThisTurn = true;
            }

            await ApplyLowHpEmergency(player, state, evt.CombatState, card);
            await RunCardPlayedSkill(player, state, evt.CombatState, cardPlay);
            await ApplyLianYingIfEmpty(player, state, card);
            RefreshLongDanFreeCard(player, state);
            InvalidateDisplayCounters(player);

            state.LastCardType = card.Type;
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Sanguosha card-played skill failed: {ex}");
        }
        finally
        {
            if (cardPlay is not null)
            {
                ClearAttackCardDamageMultiplier(cardPlay);
            }
        }
    }

    private static CharacterSkillState ResetCombatState(Player player)
    {
        var state = new CharacterSkillState(ResolveSkill(player));
        States[player.NetId] = state;
        return state;
    }

    private static CharacterSkillState GetState(Player player)
    {
        if (!States.TryGetValue(player.NetId, out var state))
        {
            state = ResetCombatState(player);
        }

        return state;
    }

    private static SanguoshaSkill ResolveSkill(Player player)
    {
        if (HasSkillRelic<IroncladSkillRelic>(player))
        {
            return SanguoshaSkill.Ironclad;
        }

        if (HasSkillRelic<SilentSkillRelic>(player))
        {
            return SanguoshaSkill.Silent;
        }

        if (HasSkillRelic<DefectSkillRelic>(player))
        {
            return SanguoshaSkill.Defect;
        }

        if (HasSkillRelic<NecrobinderSkillRelic>(player))
        {
            return SanguoshaSkill.Necrobinder;
        }

        if (HasSkillRelic<RegentSkillRelic>(player))
        {
            return SanguoshaSkill.Regent;
        }

        return SanguoshaSkill.None;
    }

    private static bool HasSkillRelic<T>(Player player) where T : RelicModel
    {
        return ContainsRelic<T>(player, 0, []);
    }

    private static bool ContainsRelic<T>(object? value, int depth, HashSet<object> visited) where T : RelicModel
    {
        if (value is null || value is string || depth > 4)
        {
            return false;
        }

        if (value is T)
        {
            return true;
        }

        var type = value.GetType();
        if (!type.IsValueType && !visited.Add(value))
        {
            return false;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (ContainsRelic<T>(item, depth + 1, visited))
                {
                    return true;
                }
            }

            return false;
        }

        if (depth >= 3)
        {
            return false;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var property in type.GetProperties(flags))
        {
            if (property.GetIndexParameters().Length > 0 || !LooksLikeRelicCarrier(property.Name, property.PropertyType))
            {
                continue;
            }

            try
            {
                if (ContainsRelic<T>(property.GetValue(value), depth + 1, visited))
                {
                    return true;
                }
            }
            catch
            {
                // Some STS2 properties can be unavailable depending on run state.
            }
        }

        foreach (var field in type.GetFields(flags))
        {
            if (!LooksLikeRelicCarrier(field.Name, field.FieldType))
            {
                continue;
            }

            try
            {
                if (ContainsRelic<T>(field.GetValue(value), depth + 1, visited))
                {
                    return true;
                }
            }
            catch
            {
                // Some STS2 fields can be unavailable depending on run state.
            }
        }

        return false;
    }

    private static bool LooksLikeRelicCarrier(string memberName, Type memberType)
    {
        return memberName.Contains("Relic", StringComparison.OrdinalIgnoreCase)
            || memberType.Name.Contains("Relic", StringComparison.OrdinalIgnoreCase)
            || memberType.FullName?.Contains("Relic", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string GetDisplayName(Player player)
    {
        return player.Character.GetType().Name switch
        {
            "Ironclad" => "赤壁猛将",
            "Silent" => "锦囊夜行",
            "Defect" => "机关卧龙",
            "Necrobinder" => "青囊魂医",
            "Regent" => "汉室仁主",
            var typeName => typeName
        };
    }

    private static async Task RunOpeningSkill(Player player, CharacterSkillState state)
    {
        switch (state.Skill)
        {
            case SanguoshaSkill.Ironclad:
                state.ShaInfusion = ShaInfusion.Fire;
                await ApplyPower<StrengthPower>(player, player.Creature, 1, null);
                break;
            case SanguoshaSkill.Silent:
                state.ShaInfusion = ShaInfusion.Poison;
                await Draw(player, 1);
                break;
            case SanguoshaSkill.Defect:
                state.ShaInfusion = ShaInfusion.Thunder;
                break;
            case SanguoshaSkill.Necrobinder:
                state.ShaInfusion = ShaInfusion.Calamity;
                await HealIfWounded(player, 2);
                break;
            case SanguoshaSkill.Regent:
                state.ShaInfusion = ShaInfusion.Stored;
                await PlayerCmd.GainStars(2, player);
                break;
        }
    }

    private static async Task RunTurnStartSkill(Player player, CharacterSkillState state, ICombatState combat)
    {
        switch (state.Skill)
        {
            case SanguoshaSkill.Ironclad:
                break;
            case SanguoshaSkill.Silent:
                break;
            case SanguoshaSkill.Defect:
                ArmThunderstormIfReady(state);
                state.ThunderstormFreshlyArmed = false;
                break;
            case SanguoshaSkill.Necrobinder:
                await HealIfWounded(player, 1);
                break;
            case SanguoshaSkill.Regent:
                break;
        }
    }

    private static void ArmThunderstormIfReady(CharacterSkillState state)
    {
        if (state.Thunder < 4 || state.ThunderstormCharges > 0)
        {
            return;
        }

        state.Thunder -= 4;
        state.ThunderstormCharges = 1;
        state.ThunderstormFreshlyArmed = true;
    }

    private static async Task ConsumeThunderstormIfReady(
        ICombatState? combat,
        Player player,
        CharacterSkillState state,
        CardModel source)
    {
        if (combat is null
            || state.ThunderstormCharges <= 0
            || state.ThunderstormFreshlyArmed
            || state.ThunderDischargedThisTurn)
        {
            return;
        }

        state.ThunderDischargedThisTurn = true;
        state.ThunderstormCharges--;
        await DamageAll(combat, player, 8, source);
    }

    private static async Task RunCardPlayedSkill(
        Player player,
        CharacterSkillState state,
        ICombatState combat,
        CardPlay cardPlay)
    {
        var card = cardPlay.Card;

        switch (state.Skill)
        {
            case SanguoshaSkill.Ironclad:
                await RunIroncladSkill(player, state, cardPlay);
                break;
            case SanguoshaSkill.Silent:
                await RunSilentSkill(player, state, cardPlay);
                break;
            case SanguoshaSkill.Defect:
                await RunDefectSkill(player, state, combat, cardPlay);
                break;
            case SanguoshaSkill.Necrobinder:
                await RunNecrobinderSkill(player, state, cardPlay);
                break;
            case SanguoshaSkill.Regent:
                await RunRegentSkill(player, state, cardPlay);
                break;
        }

        await RunEquipmentTriggers(player, state, cardPlay);
    }

    private static async Task RunEquipmentTriggers(Player player, CharacterSkillState state, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        var isTrickCard = card.Type == CardType.Skill;
        if (state.MuNiuActive
            && card is not MuNiuCard
            && isTrickCard
            && state.MuNiuTrickDrawsUsed < state.MuNiuTrickDraws)
        {
            state.MuNiuTrickDrawsUsed++;
            await Draw(player, 1);
        }

        if (state.ZhiHengActive
            && card is not ZhiHengCard
            && isTrickCard
            && !state.ZhiHengTrickDrawUsedThisTurn)
        {
            state.ZhiHengTrickDrawUsedThisTurn = true;
            await Draw(player, Math.Max(1, state.ZhiHengDrawBonus));
        }

        if (state.MengDeXinShuActive
            && card is not MengDeXinShuCard
            && isTrickCard
            && !state.MengDeXinShuUsedThisTurn)
        {
            state.MengDeXinShuUsedThisTurn = true;
            player.PlayerCombatState!.GainEnergy(1);
            ReduceHandCardCosts(
                player,
                Math.Max(1, state.MengDeXinShuFreeCards),
                1,
                handCard => IsShaLike(handCard) || handCard.Type == CardType.Skill);
        }

        if (state.JiZhiActive
            && card is not JiZhiCard
            && isTrickCard
            && !state.JiZhiUsedThisTurn)
        {
            state.JiZhiUsedThisTurn = true;
            await Draw(player, Math.Max(1, state.JiZhiDraw));
        }

        await EnsureZhangBaSha(player);
    }

    private static async Task ApplyLianYingIfEmpty(Player player, CharacterSkillState state, CardModel source)
    {
        if (!state.LianYingActive
            || state.LianYingTriggersUsedThisTurn >= state.LianYingTriggersPerTurn
            || player.PlayerCombatState!.Hand.Cards.Count > 0)
        {
            return;
        }

        state.LianYingTriggersUsedThisTurn++;
        await Draw(player, Math.Max(1, state.LianYingDraw));
        ReduceHandCardCosts(player, 1, 1, card => IsShaLike(card) || card.Type == CardType.Skill);
    }

    private static void AwardGoodLuckForLeastDamageTaken(ICombatState combat)
    {
        if (!HasEnemyDamageWindow)
        {
            return;
        }

        HasEnemyDamageWindow = false;
        var candidates = combat.Players
            .Where(player => player.Creature.IsAlive)
            .Select(player => new
            {
                Player = player,
                Damage = DamageTakenLastEnemyTurn.GetValueOrDefault(player.NetId)
            })
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var lowestDamage = candidates.Min(item => item.Damage);
        foreach (var item in candidates.Where(item => item.Damage == lowestDamage))
        {
            AddGoodLuck(item.Player, 1);
        }
    }

    private static async Task ResolveGoodLuck(Player player, CharacterSkillState state)
    {
        if (state.GoodLuck <= 0)
        {
            return;
        }

        var amount = state.GoodLuck;
        state.GoodLuck = 0;
        RemoveDisplayPower<GoodLuckDisplayPower>(player);

        for (var i = 0; i < amount; i++)
        {
            var roll = player.PlayerRng.Rewards.NextInt(4);
            switch (roll)
            {
                case 0:
                    await ApplyPower<StrengthPower>(player, player.Creature, 1, null);
                    break;
                case 1:
                    await ApplyPower<DexterityPower>(player, player.Creature, 1, null);
                    break;
                case 2:
                    await Draw(player, 1);
                    break;
                default:
                    player.PlayerCombatState!.GainEnergy(1);
                    break;
            }
        }
    }

    private static void ApplyZhuGeTurnStart(Player player, CharacterSkillState state)
    {
        if (!state.ZhuGeActive)
        {
            return;
        }

        var freeCount = Math.Max(1, state.ZhuGeFreeShaPerTurn);
        foreach (var sha in player.PlayerCombatState!.Hand.Cards.OfType<ShaCard>().Take(freeCount))
        {
            sha.EnergyCost.SetThisTurn(0, true);
        }
    }

    private static void RefreshLongDanFreeCard(Player player, CharacterSkillState state)
    {
        if (!state.LongDanActive
            || state.LongDanFreeUsedThisTurn
            || player.PlayerCombatState is null)
        {
            return;
        }

        var card = player.PlayerCombatState.Hand.Cards
            .Where(IsLongDanFreeEligible)
            .OrderByDescending(card => card.EnergyCost.GetResolved())
            .FirstOrDefault();
        card?.EnergyCost.SetThisTurn(0, true);
    }

    private static bool IsLongDanFreeEligible(CardModel card)
    {
        return IsShaLike(card) || card is ShanCard;
    }

    private static async Task ApplyEquipmentTurnStart(Player player, CharacterSkillState state)
    {
        if (state.BaiYinActive && state.BaiYinTurnHeal > 0)
        {
            await HealIfWounded(player, state.BaiYinTurnHeal);
        }

        if (state.YuXiActive && state.YuXiTurnEnergy > 0)
        {
            player.PlayerCombatState!.GainEnergy(state.YuXiTurnEnergy);
        }

        if (state.TaiPingActive)
        {
            var removed = await ClearOneDebuff(player.Creature);
            if (removed > 0)
            {
                player.PlayerCombatState!.GainEnergy(1);
            }

            await HealIfWounded(player, state.TaiPingTurnHeal);
        }

        if (state.TengJiaActive)
        {
            await GainBlock(player, Math.Max(1, state.TengJiaBlock), null);
            await ApplyPower<SlowPower>(player, player.Creature, Math.Max(0, state.TengJiaSlow), null);
            if (player.Creature.GetPower<SlowPower>() is { } slowPower)
            {
                ClampTengJiaSlow(player, slowPower);
            }
        }
    }

    private static async Task ApplyGeneralPowerTurnStart(
        Player player,
        CharacterSkillState state,
        ICombatState combat)
    {
        if (state.YingZiActive)
        {
            await Draw(player, Math.Max(1, state.YingZiDraw));
        }

        if (state.LuoYiActive)
        {
            EmpowerNextSha(player, Math.Max(1, state.LuoYiNextShaDamage));
        }

        if (state.QingNangActive)
        {
            await HealIfWounded(player, Math.Max(1, state.QingNangHeal));
        }

        if (state.BathOfBloodActive)
        {
            await LoseHp(player, Math.Max(1, state.BathOfBloodHpLoss), null);
            if (player.Creature.IsAlive)
            {
                player.PlayerCombatState!.GainEnergy(Math.Max(1, state.BathOfBloodEnergy));
            }
        }

        if (state.NightfallSchemeActive)
        {
            await Draw(player, Math.Max(1, state.NightfallSchemeDraw));
            foreach (var enemy in combat.HittableEnemies.Where(enemy => enemy.IsAlive))
            {
                await ApplyPower<PoisonPower>(player, enemy, Math.Max(1, state.NightfallSchemePoison), null);
            }
        }

        if (state.ThunderMandateActive)
        {
            state.Thunder += Math.Max(1, state.ThunderMandateThunder);
            await DamageAll(combat, player, Math.Max(1, state.ThunderMandateDamage), null);
        }

        if (state.SoulHealerFormActive)
        {
            await HealIfWounded(player, Math.Max(1, state.SoulHealerFormHeal));
            await LoseHpAllEnemies(combat, player, Math.Max(1, state.SoulHealerFormEnemyHpLoss), null);
        }

        if (state.ImperialEdictActive)
        {
            player.PlayerCombatState!.GainEnergy(Math.Max(1, state.ImperialEdictEnergy));
            EmpowerNextSha(player, Math.Max(1, state.ImperialEdictNextShaDamage));
        }
    }

    private static Task RunIroncladSkill(Player player, CharacterSkillState state, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    private static async Task RunSilentSkill(Player player, CharacterSkillState state, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card.Type == CardType.Skill
            && card is not ShaCard
            && !state.PoisonTrickBonusGrantedThisTurn)
        {
            state.PoisonTrickBonusGrantedThisTurn = true;
            await Draw(player, 1);
        }
    }

    private static async Task RunDefectSkill(
        Player player,
        CharacterSkillState state,
        ICombatState combat,
        CardPlay cardPlay)
    {
        ArmThunderstormIfReady(state);
        state.ThunderstormFreshlyArmed = false;
        await Task.CompletedTask;
    }

    private static async Task RunNecrobinderSkill(Player player, CharacterSkillState state, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card is ShanCard && card.CombatState is not null)
        {
            await LoseHpAllEnemies(card.CombatState, player, 3, card);
        }
    }

    private static async Task RunRegentSkill(Player player, CharacterSkillState state, CardPlay cardPlay)
    {
        await Task.CompletedTask;
    }

    private static async Task ApplyTurnFloor(Player player, CharacterSkillState state)
    {
        if (!IsLowHp(player, 0.4m))
        {
            return;
        }

        await GainBlock(player, 7, null);
        if (player.PlayerCombatState!.Hand.Cards.Count <= 3)
        {
            await Draw(player, 1);
        }
    }

    private static async Task ApplyLowHpEmergency(
        Player player,
        CharacterSkillState state,
        ICombatState combat,
        CardModel source)
    {
        if (state.LowHpEmergencyUsed || !IsLowHp(player, 0.35m))
        {
            return;
        }

        state.LowHpEmergencyUsed = true;
        await HealIfWounded(player, 4);
        await GainBlock(player, 12, null);
        player.PlayerCombatState!.GainEnergy(1);

        if (state.Skill is SanguoshaSkill.Silent or SanguoshaSkill.Defect)
        {
            foreach (var enemy in combat.HittableEnemies.Where(enemy => enemy.IsAlive))
            {
                await ApplyPower<WeakPower>(player, enemy, 1, source);
            }
        }
    }

    private static Task ApplyKongChengTurnStart(Player player, CharacterSkillState state)
    {
        if (!state.KongChengActive || !state.KongChengPendingNextTurn)
        {
            return Task.CompletedTask;
        }

        state.KongChengPendingNextTurn = false;
        state.KongChengAttackLockedThisTurn = true;
        return ApplyPower<IntangiblePower>(player, player.Creature, state.KongChengIntangible, null);
    }

    private static bool IsLowHp(Player player, decimal threshold)
    {
        return player.Creature.MaxHp > 0
            && player.Creature.CurrentHp <= Math.Ceiling(player.Creature.MaxHp * threshold);
    }

    private static bool IsDebuffed(Creature creature)
    {
        return creature.Powers.Any(power => power.Type == PowerType.Debuff);
    }

    private static async Task<int> ClearOneDebuff(Creature creature)
    {
        var debuff = creature.Powers.FirstOrDefault(power => power.Type == PowerType.Debuff);
        if (debuff is null)
        {
            return 0;
        }

        await PowerCmd.Remove(debuff);
        return 1;
    }

    private static bool IsNonPlayableNoise(CardModel card)
    {
        return card.Type is CardType.Status or CardType.Curse or CardType.Quest
            || card.Rarity is CardRarity.Status or CardRarity.Curse or CardRarity.Quest;
    }

    private static async Task ClearTemporaryStolenBuffs(Player player)
    {
        var state = GetState(player);
        if (state.TemporaryStolenBuffs.Count == 0)
        {
            return;
        }

        var stolenBuffs = state.TemporaryStolenBuffs.ToList();
        state.TemporaryStolenBuffs.Clear();
        foreach (var stolenBuff in stolenBuffs)
        {
            var power = player.Creature.GetPower(stolenBuff.PowerId);
            if (power is not null)
            {
                await PowerCmd.ModifyAmount(NewContext(), power, -stolenBuff.Amount, null, null, true);
            }
        }
    }

    private static async Task ResolveDelayedJudgments(ICombatState combat)
    {
        if (PendingDelayedJudgments.Count == 0)
        {
            return;
        }

        ActiveLeBuAttackLocks.Clear();
        ActiveBingLiangDebuffLocks.Clear();

        var pending = PendingDelayedJudgments.ToList();
        PendingDelayedJudgments.Clear();
        foreach (var judgment in pending)
        {
            var target = combat.HittableEnemies.FirstOrDefault(enemy =>
                enemy.IsAlive && CreatureKey(enemy) == judgment.TargetCombatId);
            if (target is null)
            {
                continue;
            }

            var roll = judgment.Source.PlayerRng.Rewards.NextInt(100);
            if (roll % 2 != 0)
            {
                continue;
            }

            switch (judgment.Kind)
            {
                case DelayedJudgmentKind.BingLiang:
                    ActiveBingLiangDebuffLocks.Add(CreatureKey(target));
                    break;
                case DelayedJudgmentKind.LeBu:
                    ActiveLeBuAttackLocks.Add(CreatureKey(target));
                    break;
                case DelayedJudgmentKind.ShanDian:
                    var damage = Math.Ceiling(target.MaxHp * judgment.Value * 0.08m);
                    await CreatureCmd.Damage(
                        NewContext(),
                        target,
                        damage,
                        ValueProp.Move,
                        judgment.Source.Creature,
                        judgment.SourceCard);
                    break;
            }
        }
    }

    private static void ClearDelayedJudgmentState()
    {
        PendingDelayedJudgments.Clear();
        ActiveLeBuAttackLocks.Clear();
        ActiveBingLiangDebuffLocks.Clear();
    }

    private static uint CreatureKey(Creature creature)
    {
        return creature.CombatId ?? 0;
    }

    private static PlayerChoiceContext NewContext()
    {
        return new ThrowingPlayerChoiceContext();
    }

    private static Task GainBlock(Player player, decimal amount, CardPlay? cardPlay)
    {
        if (amount <= 0)
        {
            return Task.CompletedTask;
        }

        if (cardPlay is null)
        {
            player.Creature.GainBlockInternal(amount);
            return Task.CompletedTask;
        }

        return CreatureCmd.GainBlock(player.Creature, amount, ValueProp.Move, cardPlay, false);
    }

    private static async Task<decimal> HealIfWounded(Player player, decimal amount)
    {
        var missingHp = player.Creature.MaxHp - player.Creature.CurrentHp;
        if (missingHp > 0 && amount > 0)
        {
            var healed = Math.Min(amount, missingHp);
            await CreatureCmd.Heal(player.Creature, healed, true);
            GetState(player).HealedThisTurn = true;
            return healed;
        }

        return 0;
    }

    private static async Task ForgeRegentSovereignBlade(Player player, CardModel source, decimal shaAmount)
    {
        if (player.PlayerCombatState is null || source.CombatState is null)
        {
            return;
        }

        var forgeAmount = Math.Max(1, (int)Math.Ceiling(shaAmount / 2m));
        await ForgeCmd.Forge(forgeAmount, player, source);
        foreach (var blade in player.PlayerCombatState.AllCards.OfType<SovereignBlade>())
        {
            blade.EnergyCost.SetThisCombat(0, true);
            if (!blade.Keywords.Contains(CardKeyword.Exhaust))
            {
                blade.AddKeyword(CardKeyword.Exhaust);
            }
        }
    }

    private static async Task Draw(Player player, int amount)
    {
        if (amount <= 0 || player.PlayerCombatState!.Hand.Cards.Count >= CardPile.MaxCardsInHand)
        {
            return;
        }

        await CardPileCmd.Draw(NewContext(), amount, player, false);
    }

    private static int MakeHandCardsFree(Player player, int count, Func<CardModel, bool>? filter = null)
    {
        var changed = 0;
        foreach (var card in player.PlayerCombatState!.Hand.Cards
                     .Where(card => filter?.Invoke(card) ?? true)
                     .OrderByDescending(card => card.EnergyCost.GetResolved()))
        {
            card.EnergyCost.SetThisTurn(0, true);
            changed++;
            if (changed >= count)
            {
                break;
            }
        }

        return changed;
    }

    private static int ReduceHandCardCosts(Player player, int count, int amount, Func<CardModel, bool>? filter = null)
    {
        if (count <= 0 || amount <= 0)
        {
            return 0;
        }

        var changed = 0;
        foreach (var card in player.PlayerCombatState!.Hand.Cards
                     .Where(card => filter?.Invoke(card) ?? true)
                     .OrderByDescending(card => card.EnergyCost.GetResolved()))
        {
            card.EnergyCost.AddThisTurn(-amount, true);
            changed++;
            if (changed >= count)
            {
                break;
            }
        }

        return changed;
    }

    private static Task ApplyPower<TPower>(Player player, Creature target, decimal amount, CardModel? source)
        where TPower : PowerModel, new()
    {
        return amount <= 0
            ? Task.CompletedTask
            : PowerCmd.Apply<TPower>(NewContext(), target, amount, player.Creature, source!, false);
    }

    private static Task AdjustPower<TPower>(Player player, Creature target, decimal amount, CardModel? source)
        where TPower : PowerModel, new()
    {
        return amount == 0
            ? Task.CompletedTask
            : PowerCmd.Apply<TPower>(NewContext(), target, amount, player.Creature, source!, false);
    }

    internal static async Task TryApplyBaGuaShan(
        PlayerChoiceContext choiceContext,
        Player player,
        decimal incomingDamage,
        ValueProp props,
        CardModel? source)
    {
        if (!States.TryGetValue(player.NetId, out var state)
            || !state.BaGuaActive
            || incomingDamage <= 0
            || props.HasFlag(ValueProp.Unblockable)
            || player.PlayerCombatState is null)
        {
            return;
        }

        var targetBlock = Math.Ceiling(incomingDamage);
        var shanBlock = state.BaGuaUpgraded ? 8m : 6m;
        var maxJudgements = state.BaGuaUpgraded ? 3 : 2;
        for (var judgedCount = 0; judgedCount < maxJudgements && player.Creature.Block < targetBlock; judgedCount++)
        {
            await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
            var revealed = player.PlayerCombatState.DrawPile.Cards.FirstOrDefault();
            if (revealed is null)
            {
                return;
            }

            await CardPileCmd.Add([revealed], PileType.Discard, CardPilePosition.Top, source, false);
            if (revealed is not ShanCard)
            {
                return;
            }

            await CreatureCmd.GainBlock(player.Creature, shanBlock, ValueProp.Move, null, false);
        }
    }

    internal static async Task TryApplyIncomingDamageAbilities(
        PlayerChoiceContext choiceContext,
        Player player,
        Creature dealer,
        decimal incomingDamage,
        ValueProp props,
        CardModel? source)
    {
        if (!States.TryGetValue(player.NetId, out var state)
            || incomingDamage <= 0
            || props.HasFlag(ValueProp.Unblockable)
            || player.PlayerCombatState is null)
        {
            return;
        }

        if (state.GuiCaiActive)
        {
            await ApplyGuiCaiJudgement(choiceContext, player, dealer, state, source);
        }

        if (state.YiJiActive && !state.YiJiTriggeredThisTurn)
        {
            state.YiJiTriggeredThisTurn = true;
            var draw = state.YiJiDraw
                + (player.PlayerCombatState.Hand.Cards.Count <= 3 ? state.YiJiLowHandDraw : 0);
            await Draw(player, Math.Max(1, draw));
            MakeHandCardsFree(player, state.YiJiFreeCards, card => IsShaLike(card) || card.Type == CardType.Skill);
        }

        if (state.JianXiongActive && !state.JianXiongTriggeredThisTurn)
        {
            state.JianXiongTriggeredThisTurn = true;
            await Draw(player, Math.Max(1, state.JianXiongDraw));
            EmpowerNextSha(player, state.JianXiongNextShaDamage);
            if (state.JianXiongEnergy > 0)
            {
                player.PlayerCombatState.GainEnergy(state.JianXiongEnergy);
            }
        }

        if (state.RenWangActive
            && !state.RenWangUsedThisTurn
            && (source is null || IsShaLike(source)))
        {
            state.RenWangUsedThisTurn = true;
            var guardBlock = Math.Ceiling(incomingDamage) + Math.Max(1, state.RenWangGuardBlock);
            await GainBlock(player, guardBlock, null);
            await Draw(player, state.RenWangDraw);
        }

        if (state.JueYingActive && !state.JueYingUsedThisTurn)
        {
            state.JueYingUsedThisTurn = true;
            await GainBlock(player, Math.Max(1, state.JueYingBlock), null);
        }

        InvalidateDisplayCounters(player);
    }

    private static async Task ApplyGuiCaiJudgement(
        PlayerChoiceContext choiceContext,
        Player player,
        Creature dealer,
        CharacterSkillState state,
        CardModel? source)
    {
        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
        var revealed = player.PlayerCombatState!.DrawPile.Cards.FirstOrDefault();
        if (revealed is null)
        {
            return;
        }

        await CardPileCmd.Add([revealed], PileType.Discard, CardPilePosition.Top, source, false);
        if (revealed.Type is CardType.Skill or CardType.Power)
        {
            await CreatureCmd.GainBlock(player.Creature, state.GuiCaiBlock, ValueProp.Move, null, false);
            await Draw(player, Math.Max(1, state.GuiCaiDraw));
            return;
        }

        if (IsShaLike(revealed))
        {
            await ApplyPower<WeakPower>(player, dealer, Math.Max(1, state.GuiCaiWeak), source);
            return;
        }

        MakeHandCardsFree(player, 1, card => IsShaLike(card) || card.Type == CardType.Skill);
    }

    private static Task DamageTarget(Player player, Creature target, decimal amount, CardModel? source)
    {
        return amount <= 0 || !target.IsAlive
            ? Task.CompletedTask
            : CreatureCmd.Damage(NewContext(), target, amount, ValueProp.Move, player.Creature, source!);
    }

    public static Task LoseHp(Player player, decimal amount, CardModel? source)
    {
        return amount <= 0 || !player.Creature.IsAlive
            ? Task.CompletedTask
            : CreatureCmd.Damage(NewContext(), player.Creature, amount, ValueProp.Unblockable, player.Creature, source!);
    }

    private static Task LoseHpAllEnemies(ICombatState combat, Player player, decimal amount, CardModel? source)
    {
        var targets = combat.HittableEnemies.Where(enemy => enemy.IsAlive).ToList();
        return targets.Count == 0 || amount <= 0
            ? Task.CompletedTask
            : CreatureCmd.Damage(NewContext(), targets, amount, ValueProp.Unblockable, player.Creature, source!);
    }

    private static Task DamageAll(ICombatState combat, Player player, decimal amount, CardModel? source)
    {
        var targets = combat.HittableEnemies.Where(enemy => enemy.IsAlive).ToList();
        return targets.Count == 0 || amount <= 0
            ? Task.CompletedTask
            : CreatureCmd.Damage(NewContext(), targets, amount, ValueProp.Move, player.Creature, source!);
    }

    private sealed class CharacterSkillState(SanguoshaSkill skill)
    {
        public SanguoshaSkill Skill { get; } = skill;
        public int TurnCardsPlayed { get; set; }
        public int TurnAttacksPlayed { get; set; }
        public int TurnShaPlayed { get; set; }
        public int TurnTricksPlayed { get; set; }
        public int Strategy { get; set; }
        public int Thunder { get; set; }
        public int ThunderstormCharges { get; set; }
        public int Soul { get; set; }
        public int Ingenuity { get; set; }
        public int IngenuityCap { get; set; } = 5;
        public ShaInfusion ShaInfusion { get; set; }
        public bool LowHpEmergencyUsed { get; set; }
        public bool EnergyGrantedThisTurn { get; set; }
        public int JiuNextShaBonus { get; set; }
        public decimal JiuNextAttackDamageMultiplier { get; set; }
        public CardModel? JiuDoubledAttackCard { get; set; }
        public decimal JiuActiveAttackDamageMultiplier { get; set; }
        public bool IngenuityEnergyGrantedThisTurn { get; set; }
        public bool FireVulnerableUsedThisTurn { get; set; }
        public bool PoisonTrickBonusGrantedThisTurn { get; set; }
        public int NextPoisonShaBonus { get; set; }
        public bool ThunderDischargedThisTurn { get; set; }
        public bool ThunderstormFreshlyArmed { get; set; }
        public bool HealedThisTurn { get; set; }
        public bool QiLinVulnerableUsedThisTurn { get; set; }
        public bool ZhuGeActive { get; set; }
        public int ZhuGeFreeShaPerTurn { get; set; } = 1;
        public bool ZhangBaActive { get; set; }
        public bool ZhangBaUpgraded { get; set; }
        public bool ZhangBaGeneratedThisTurn { get; set; }
        public bool ZhiHengActive { get; set; }
        public int ZhiHengDrawBonus { get; set; } = 1;
        public bool ZhiHengTrickDrawUsedThisTurn { get; set; }
        public bool WuShuangActive { get; set; }
        public int WuShuangRepeatsPerTurn { get; set; } = 1;
        public int WuShuangRepeatsUsedThisTurn { get; set; }
        public bool LianYingActive { get; set; }
        public int LianYingDraw { get; set; }
        public int LianYingTriggersPerTurn { get; set; }
        public int LianYingTriggersUsedThisTurn { get; set; }
        public bool YiJiActive { get; set; }
        public int YiJiDraw { get; set; }
        public int YiJiLowHandDraw { get; set; }
        public int YiJiFreeCards { get; set; }
        public bool YiJiTriggeredThisTurn { get; set; }
        public bool JianXiongActive { get; set; }
        public int JianXiongDraw { get; set; }
        public int JianXiongNextShaDamage { get; set; }
        public int JianXiongEnergy { get; set; }
        public bool JianXiongTriggeredThisTurn { get; set; }
        public bool GuiCaiActive { get; set; }
        public int GuiCaiBlock { get; set; }
        public int GuiCaiDraw { get; set; }
        public int GuiCaiWeak { get; set; }
        public bool YingZiActive { get; set; }
        public int YingZiDraw { get; set; }
        public bool JiZhiActive { get; set; }
        public int JiZhiDraw { get; set; }
        public bool JiZhiUsedThisTurn { get; set; }
        public bool LuoYiActive { get; set; }
        public int LuoYiNextShaDamage { get; set; }
        public bool TieQiActive { get; set; }
        public int TieQiVulnerable { get; set; }
        public bool TieQiUsedThisTurn { get; set; }
        public bool QingNangActive { get; set; }
        public int QingNangHeal { get; set; }
        public bool XiaoJiActive { get; set; }
        public int XiaoJiDraw { get; set; }
        public bool FenYingActive { get; set; }
        public int FenYingBlock { get; set; }
        public bool PoZhuActive { get; set; }
        public int PoZhuStrengthPerVulnerable { get; set; }
        public bool BathOfBloodActive { get; set; }
        public int BathOfBloodHpLoss { get; set; }
        public int BathOfBloodEnergy { get; set; }
        public bool NightfallSchemeActive { get; set; }
        public int NightfallSchemeDraw { get; set; }
        public int NightfallSchemePoison { get; set; }
        public bool ThunderMandateActive { get; set; }
        public int ThunderMandateThunder { get; set; }
        public int ThunderMandateDamage { get; set; }
        public bool SoulHealerFormActive { get; set; }
        public int SoulHealerFormHeal { get; set; }
        public int SoulHealerFormEnemyHpLoss { get; set; }
        public bool ImperialEdictActive { get; set; }
        public int ImperialEdictEnergy { get; set; }
        public int ImperialEdictNextShaDamage { get; set; }
        public int GoodLuck { get; set; }
        public bool BaGuaActive { get; set; }
        public bool BaGuaUpgraded { get; set; }
        public bool BaGuaEnergyGrantedThisTurn { get; set; }
        public bool BaiYinActive { get; set; }
        public int BaiYinTurnHeal { get; set; }
        public int BaiYinDamageCap { get; set; }
        public bool TengJiaActive { get; set; }
        public bool JueYingActive { get; set; }
        public bool ChiTuActive { get; set; }
        public bool ChiTuEnergyGrantedThisTurn { get; set; }
        public bool DaWanActive { get; set; }
        public int DaWanBonusDamage { get; set; }
        public bool DiLuActive { get; set; }
        public bool DiLuTrickDrawUsedThisTurn { get; set; }
        public bool YuXiActive { get; set; }
        public int YuXiTurnEnergy { get; set; }
        public bool MuNiuActive { get; set; }
        public int MuNiuTrickDraws { get; set; }
        public int MuNiuTrickDrawsUsed { get; set; }
        public bool TaiPingActive { get; set; }
        public int TaiPingTurnHeal { get; set; }
        public bool KongChengActive { get; set; }
        public int KongChengIntangible { get; set; }
        public bool KongChengPendingNextTurn { get; set; }
        public bool KongChengAttackLockedThisTurn { get; set; }
        public bool LongDanActive { get; set; }
        public bool LongDanFreeUsedThisTurn { get; set; }
        public bool GuanShiActive { get; set; }
        public int GuanShiBonusDamage { get; set; }
        public bool GuanShiUsedThisTurn { get; set; }
        public bool HanBingActive { get; set; }
        public int HanBingWeak { get; set; }
        public bool QiLinActive { get; set; }
        public int QiLinBonusDamage { get; set; }
        public int QiLinVulnerable { get; set; }
        public bool RenWangActive { get; set; }
        public int RenWangGuardBlock { get; set; }
        public int RenWangDraw { get; set; }
        public bool RenWangUsedThisTurn { get; set; }
        public bool QingGangActive { get; set; }
        public decimal QingGangBlockedTargetMultiplier { get; set; } = 1m;
        public bool GuDingActive { get; set; }
        public bool GanJiangMoYeActive { get; set; }
        public int GanJiangMoYeBonusDamage { get; set; }
        public int GanJiangMoYeTriggersPerTurn { get; set; }
        public int GanJiangMoYeTriggersUsedThisTurn { get; set; }
        public bool TieSuoThisTurn { get; set; }
        public decimal TieSuoSplashMultiplier { get; set; }
        public int TengJiaBlock { get; set; }
        public int TengJiaSlow { get; set; }
        public int TengJiaSlowCap { get; set; }
        public int JueYingBlock { get; set; }
        public bool JueYingUsedThisTurn { get; set; }
        public bool MengDeXinShuActive { get; set; }
        public int MengDeXinShuFreeCards { get; set; }
        public bool MengDeXinShuUsedThisTurn { get; set; }
        public CardType? LastCardType { get; set; }
        public Dictionary<EquipSlot, string> ActiveEquip = new();
        public Dictionary<EquipSlot, bool> ActiveEquipUpgraded = new();
        public List<TemporaryPowerAmount> TemporaryStolenBuffs = new();
    }

    private readonly record struct TemporaryPowerAmount(ModelId PowerId, int Amount);

    private readonly record struct DelayedJudgment(
        Player Source,
        uint TargetCombatId,
        DelayedJudgmentKind Kind,
        int Value,
        CardModel SourceCard);

    private enum SanguoshaSkill
    {
        None,
        Ironclad,
        Silent,
        Defect,
        Necrobinder,
        Regent
    }

    private enum ShaInfusion
    {
        None,
        Fire,
        Poison,
        Thunder,
        Stored,
        Calamity
    }

    private enum EquipSlot
    {
        Weapon,
        Armor,
        Mount,
        Treasure
    }

    internal static bool HasBaiYinActive(Player player)
    {
        return States.TryGetValue(player.NetId, out var state) && state.BaiYinActive;
    }

    internal static int GetBaiYinDamageCap(Player player)
    {
        return States.TryGetValue(player.NetId, out var state) && state.BaiYinActive
            ? Math.Max(1, state.BaiYinDamageCap)
            : 0;
    }
}
