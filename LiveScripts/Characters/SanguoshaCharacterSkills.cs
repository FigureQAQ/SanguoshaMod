using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Cards;
using sanguosha.Patches;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib;

namespace sanguosha.Characters;

internal static class SanguoshaCharacterSkills
{
    private static readonly List<IDisposable> Subscriptions = [];
    private static readonly Dictionary<ulong, CharacterSkillState> States = [];

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
        if (state.ActiveEquip.TryGetValue(slot, out var oldCardId) && oldCardId != newCardId)
        {
            var wasUpgraded = state.ActiveEquipUpgraded.TryGetValue(slot, out var up) && up;
            RemoveDisplayPower(player, oldCardId);
            _ = DoEquipmentReplaced(player, oldCardId, wasUpgraded);
            ClearEquipmentFlags(state, slot);
        }
        state.ActiveEquip[slot] = newCardId;
        state.ActiveEquipUpgraded[slot] = upgraded;
    }

    private static void ClearEquipmentFlags(CharacterSkillState state, EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.Weapon:
                state.ZhuGeActive = false; state.ZhuGeFreeShaPerTurn = 0;
                state.ZhangBaActive = false; state.ZhangBaUpgraded = false; state.ZhangBaGeneratedThisTurn = false;
                state.QingGangActive = false; state.QingGangBonusDamage = 0;
                state.GuanShiActive = false; state.GuanShiBonusDamage = 0;
                state.HanBingActive = false; state.HanBingWeak = 0;
                state.QiLinActive = false; state.QiLinBonusDamage = 0; state.QiLinVulnerable = 0;
                state.GuDingActive = false;
                state.GanJiangMoYeActive = false; state.GanJiangMoYeBonusDamage = 0; state.GanJiangMoYeTriggersPerTurn = 0; state.GanJiangMoYeTriggersUsedThisTurn = 0;
                break;
            case EquipSlot.Armor:
                state.BaiYinActive = false; state.BaiYinTurnHeal = 0; state.BaiYinDamageCap = 0;
                state.RenWangActive = false; state.RenWangGuardBlock = 0; state.RenWangDraw = 0; state.RenWangUsedThisTurn = false;
                state.BaGuaActive = false; state.BaGuaUpgraded = false;
                state.TengJiaActive = false; state.TengJiaBlock = 0; state.TengJiaHpLoss = 0;
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
                await HealIfWounded(player, wasUpgraded ? 12 : 8);
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
                await ApplyPower<StrengthPower>(player, player.Creature, wasUpgraded ? 2 : 1, null);
                EmpowerNextSha(player, wasUpgraded ? 8 : 5);
                break;
            case "HanBingCard":
                await Draw(player, wasUpgraded ? 2 : 1);
                break;
            case "QiLinCard":
                await ApplyPower<StrengthPower>(player, player.Creature, 1, null);
                await Draw(player, wasUpgraded ? 2 : 1);
                break;
            case "GuDingCard":
                await ApplyPower<StrengthPower>(player, player.Creature, wasUpgraded ? 2 : 1, null);
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
                await ApplyPower<StrengthPower>(player, player.Creature, 1, null);
                EmpowerNextSha(player, wasUpgraded ? 4 : 3);
                break;
            case "DiLuCard":
                await ApplyPower<DexterityPower>(player, player.Creature, 1, null);
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

    public static bool IsZhangBaUpgraded(Player player)
    {
        var state = GetState(player);
        return state.ZhangBaActive && state.ZhangBaUpgraded;
    }

    public static bool IsShaLike(CardModel card)
    {
        return card is ShaCard or ZhangBaShaCard or WangJianShaCard;
    }

    public static int GetRegentCommand(Player player)
    {
        return Math.Max(0, GetState(player).Command);
    }

    public static bool HasPlayedShaThisTurn(Player player)
    {
        return GetState(player).TurnShaPlayed > 0;
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
        state.BaiYinTurnHeal = upgraded ? 2 : 1;
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
        state.GuanShiBonusDamage = upgraded ? 9 : 6;
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
        state.QingGangBonusDamage = upgraded ? 3 : 2;
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
        state.TengJiaHpLoss = 1;
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
        state.MengDeXinShuFreeCards = 1;
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

    public static bool ShouldPierceBlock(Player player)
    {
        return GetState(player).QingGangActive;
    }

    public static decimal GetBlockPiercePercent(Player player)
    {
        return GetState(player).QingGangActive ? 0.5m : 0m;
    }

    public static decimal GetAttackDamageMultiplier(Player player, Creature target)
    {
        var state = GetState(player);
        return state.GuDingActive && target.Block <= 0 ? 1.5m : 1m;
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

    public static decimal GetAttackFlatBonus(Player player, Creature target)
    {
        var state = GetState(player);
        var bonus = 0m;

        if (state.JiuNextShaBonus > 0)
        {
            bonus += state.JiuNextShaBonus;
        }

        if (state.GuanShiActive && target.Block > 0)
        {
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

        if (state.QingGangActive && target.Block > 0)
        {
            bonus += state.QingGangBonusDamage;
        }

        return bonus;
    }

    public static async Task ApplyAttackFollowups(PlayerChoiceContext context, CardPlay play, Creature target)
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
            await ApplyShaInfusionFollowup(context, play, target, state);
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
    }

    private static async Task ApplyShaInfusionFollowup(
        PlayerChoiceContext context,
        CardPlay play,
        Creature target,
        CharacterSkillState state)
    {
        var player = play.Card.Owner;
        switch (state.ShaInfusion)
        {
            case ShaInfusion.Fire:
                var isLowHp = IsLowHp(player, 0.5m);
                var fireDamage = isLowHp ? 3 : 2;
                if (isLowHp && !state.FireVulnerableUsedThisTurn)
                {
                    fireDamage += 1;
                }

                await DamageTarget(player, target, fireDamage, play.Card);
                if (isLowHp && !state.FireVulnerableUsedThisTurn && target.IsAlive)
                {
                    state.FireVulnerableUsedThisTurn = true;
                    await ApplyPower<VulnerablePower>(player, target, 1, play.Card);
                }
                break;
            case ShaInfusion.Poison:
                var wasPoisoned = target.Powers.Any(power => power is PoisonPower);
                var poison = 2 + (wasPoisoned ? 0 : 1) + state.NextPoisonShaBonus;
                state.NextPoisonShaBonus = 0;
                await ApplyPower<PoisonPower>(player, target, poison, play.Card);
                if (wasPoisoned && !state.IngenuityEnergyGrantedThisTurn)
                {
                    state.IngenuityEnergyGrantedThisTurn = true;
                    player.PlayerCombatState!.GainEnergy(1);
                }
                break;
            case ShaInfusion.Thunder:
                state.Thunder++;
                if (play.Card.CombatState is not null)
                {
                    foreach (var enemy in play.Card.CombatState.HittableEnemies.Where(enemy => enemy.IsAlive))
                    {
                        await DamageTarget(player, enemy, enemy == target ? 1 : 2, play.Card);
                    }

                    await DischargeThunderIfReady(play.Card.CombatState, player, state, play.Card);
                }
                break;
            case ShaInfusion.Stored:
                if (play.Card is WangJianShaCard)
                {
                    break;
                }

                state.StoredShaCharge++;
                await ForgeWangJianIfReady(player, state);
                break;
            case ShaInfusion.Calamity:
                state.Soul = Math.Min(10, state.Soul + 1);
                await ApplyPower<MegaCrit.Sts2.Core.Models.Powers.CalamityPower>(player, target, 1, play.Card);
                if (state.HealedThisTurn)
                {
                    await DamageTarget(player, target, 2, play.Card);
                }

                if (state.Soul >= 4)
                {
                    state.Soul -= 4;
                    await HealIfWounded(player, 2);
                    await DamageTarget(player, target, 4, play.Card);
                }
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
        state.JiuNextAttackDamageMultiplier = Math.Max(1m, state.JiuNextAttackDamageMultiplier) * 2m;
    }

    private static void ClearJiuAttackDamageMultiplier(CharacterSkillState state)
    {
        state.JiuNextAttackDamageMultiplier = 1m;
        state.JiuDoubledAttackCard = null;
        state.JiuActiveAttackDamageMultiplier = 1m;
    }

    private static void ClearAttackCardDamageMultiplier(CardPlay play)
    {
        var owner = play.Card.Owner;
        if (owner is null || play.Card.Type != CardType.Attack)
        {
            return;
        }

        var state = GetState(owner);
        if (!ReferenceEquals(state.JiuDoubledAttackCard, play.Card))
        {
            return;
        }

        state.JiuDoubledAttackCard = null;
        state.JiuActiveAttackDamageMultiplier = 1m;
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

            foreach (var player in combat.Players)
            {
                var state = ResetCombatState(player);
                RunOpeningSkill(player, state);
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Sanguosha combat-start skill failed: {ex}");
        }
    }

    private static async void OnSideTurnStarted(SideTurnStartedEvent evt)
    {
        if (evt.Side != CombatSide.Player)
        {
            return;
        }

        try
        {
            foreach (var player in evt.CombatState.Players.Where(player => player.Creature.IsAlive))
            {
                var state = GetState(player);
                state.TurnCardsPlayed = 0;
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
                state.StoredShaDrawGrantedThisTurn = false;
                state.RegentDecreeUsedThisTurn = false;
                state.HealedThisTurn = false;
                state.QiLinVulnerableUsedThisTurn = false;
                state.BaGuaEnergyGrantedThisTurn = false;
                state.ChiTuEnergyGrantedThisTurn = false;
                state.JueYingUsedThisTurn = false;
                state.DiLuTrickDrawUsedThisTurn = false;
                state.RenWangUsedThisTurn = false;
                state.MuNiuTrickDrawsUsed = 0;
                state.MengDeXinShuUsedThisTurn = false;
                state.KongChengAttackLockedThisTurn = false;
                state.LianYingTriggersUsedThisTurn = 0;
                state.ZhangBaGeneratedThisTurn = false;
                state.LongDanFreeUsedThisTurn = false;
                state.YiJiTriggeredThisTurn = false;
                state.JianXiongTriggeredThisTurn = false;
                ClearJiuAttackDamageMultiplier(state);
                state.LastCardType = null;
                BaiYinDamageCapPatch.ResetDamageThisTurn(player);

                await ApplyKongChengTurnStart(player, state);
                ApplyZhuGeTurnStart(player, state);
                RefreshLongDanFreeCard(player, state);
                await ApplyEquipmentTurnStart(player, state);
                await EnsureZhangBaSha(player);
                await ApplyTurnFloor(player, state);
                await RunTurnStartSkill(player, state, evt.CombatState);
                RefreshLongDanFreeCard(player, state);
            }
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
        return player.Character.GetType().Name switch
        {
            "Ironclad" => SanguoshaSkill.Ironclad,
            "Silent" => SanguoshaSkill.Silent,
            "Defect" => SanguoshaSkill.Defect,
            "Necrobinder" => SanguoshaSkill.Necrobinder,
            "Regent" => SanguoshaSkill.Regent,
            _ => SanguoshaSkill.None
        };
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

    private static void RunOpeningSkill(Player player, CharacterSkillState state)
    {
        switch (state.Skill)
        {
            case SanguoshaSkill.Ironclad:
                state.ShaInfusion = ShaInfusion.Fire;
                _ = ApplyPower<StrengthPower>(player, player.Creature, 1, null);
                break;
            case SanguoshaSkill.Silent:
                state.ShaInfusion = ShaInfusion.Poison;
                state.Ingenuity = Math.Max(state.Ingenuity, 2);
                _ = Draw(player, 1);
                break;
            case SanguoshaSkill.Defect:
                state.ShaInfusion = ShaInfusion.Thunder;
                state.Thunder = Math.Max(state.Thunder, 1);
                break;
            case SanguoshaSkill.Necrobinder:
                state.ShaInfusion = ShaInfusion.Calamity;
                _ = HealIfWounded(player, 3);
                state.Soul = Math.Max(state.Soul, 3);
                break;
            case SanguoshaSkill.Regent:
                state.ShaInfusion = ShaInfusion.Stored;
                state.Command = Math.Max(state.Command, 1);
                player.PlayerCombatState!.GainStars(1);
                break;
        }
    }

    private static async Task RunTurnStartSkill(Player player, CharacterSkillState state, ICombatState combat)
    {
        switch (state.Skill)
        {
            case SanguoshaSkill.Ironclad:
                if (IsLowHp(player, 0.6m))
                {
                    await ApplyPower<StrengthPower>(player, player.Creature, 1, null);
                }
                break;
            case SanguoshaSkill.Silent:
                state.Ingenuity = Math.Min(state.IngenuityCap, state.Ingenuity + 2);
                if (player.PlayerCombatState!.Hand.Cards.Count <= 4)
                {
                    await Draw(player, 1);
                }
                break;
            case SanguoshaSkill.Defect:
                state.Thunder += 1;
                await DischargeThunderIfReady(combat, player, state, null);
                break;
            case SanguoshaSkill.Necrobinder:
                state.Soul = Math.Min(10, state.Soul + 1);
                await HealIfWounded(player, 1);
                break;
            case SanguoshaSkill.Regent:
                player.PlayerCombatState!.GainStars(1);
                await ResolveRegentStars(player, state);
                break;
        }
    }

    private static async Task DischargeThunderIfReady(
        ICombatState combat,
        Player player,
        CharacterSkillState state,
        CardModel? source)
    {
        if (state.Thunder < 4)
        {
            return;
        }

        if (state.ThunderDischargedThisTurn)
        {
            state.Thunder = 4;
            return;
        }

        state.ThunderDischargedThisTurn = true;
        state.Thunder -= 4;
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
        if (state.BaiYinActive)
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
            await LoseHp(player, Math.Max(0, state.TengJiaHpLoss), null);
        }
    }

    private static async Task RunIroncladSkill(Player player, CharacterSkillState state, CardPlay cardPlay)
    {
        var card = cardPlay.Card;

        if (card is JiuCard && !state.EnergyGrantedThisTurn)
        {
            state.EnergyGrantedThisTurn = true;
            player.PlayerCombatState!.GainEnergy(1);
        }

        if (card is DuelCard)
        {
            await ApplyPower<StrengthPower>(player, player.Creature, 1, card);
        }

        if (card is NanManCard)
        {
            await ApplyPower<StrengthPower>(player, player.Creature, 1, card);
        }

        if (card is WanJianCard)
        {
            await ApplyPower<StrengthPower>(player, player.Creature, 1, card);
            await Draw(player, 1);
        }
    }

    private static async Task RunSilentSkill(Player player, CharacterSkillState state, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card is WuXieCard)
        {
            state.Ingenuity = Math.Min(state.IngenuityCap, state.Ingenuity + 1);
        }

        if (card.Type == CardType.Skill
            && card is not ShaCard
            && !state.PoisonTrickBonusGrantedThisTurn)
        {
            state.PoisonTrickBonusGrantedThisTurn = true;
            state.NextPoisonShaBonus += 1;
        }

        if (card is HuoGongCard && cardPlay.Target is { IsAlive: true } poisonTarget)
        {
            await ApplyPower<PoisonPower>(player, poisonTarget, 1, card);
        }

        if (card is ShunShouCard)
        {
            await Draw(player, 1);
            await ApplyPower<DexterityPower>(player, player.Creature, 1, card);
        }

        if (card is LeBuCard)
        {
            state.Strategy++;
            await ApplyPower<WeakPower>(player, player.Creature, 1, card);
        }

    }

    private static async Task RunDefectSkill(
        Player player,
        CharacterSkillState state,
        ICombatState combat,
        CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card is ShanDianCard)
        {
            state.Thunder += 3;
        }

        if (card is TieSuoCard)
        {
            state.Thunder += 1;
        }

        await DischargeThunderIfReady(combat, player, state, card);
    }

    private static async Task RunNecrobinderSkill(Player player, CharacterSkillState state, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        state.Soul = Math.Min(10, state.Soul + 1);

        if (card is TaoCard)
        {
            state.Soul = Math.Min(10, state.Soul + 3);
            await HealIfWounded(player, 2);
        }

        if (card is JieDaoCard)
        {
            state.Soul = Math.Min(10, state.Soul + 1);
        }

        if (card is TaoYuanCard)
        {
            state.Soul = Math.Min(10, state.Soul + 3);
        }

        if (state.Soul >= 3)
        {
            state.Soul -= 3;
            var healed = await HealIfWounded(player, 3);
            var overflow = Math.Max(0, 3 - healed);
            if (overflow > 0 && card.CombatState is not null)
            {
                var overflowTarget = card.CombatState.HittableEnemies
                    .Where(enemy => enemy.IsAlive)
                    .OrderBy(enemy => enemy.CurrentHp)
                    .FirstOrDefault();
                if (overflowTarget is not null)
                {
                    await DamageTarget(player, overflowTarget, Math.Min(4, overflow), card);
                }
            }

            await Draw(player, 1);
        }
    }

    private static async Task RunRegentSkill(Player player, CharacterSkillState state, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (state.LastCardType is not null && state.LastCardType != card.Type && !state.EnergyGrantedThisTurn)
        {
            state.EnergyGrantedThisTurn = true;
            player.PlayerCombatState!.GainEnergy(1);
        }

        if (card is LeBuCard)
        {
            state.Command = Math.Min(8, state.Command + 2);
        }

        if (card is WuZhongCard)
        {
            player.PlayerCombatState!.GainStars(1);
            await Draw(player, 1);
            await ResolveRegentStars(player, state);
        }

        if (card is TaoYuanCard)
        {
            player.PlayerCombatState!.GainStars(2);
            await Draw(player, 1);
            await ResolveRegentStars(player, state);
        }

    }

    private static async Task ResolveRegentStars(Player player, CharacterSkillState state)
    {
        var playerState = player.PlayerCombatState!;
        if (playerState.Stars < 3)
        {
            return;
        }

        if (state.RegentDecreeUsedThisTurn)
        {
            return;
        }

        state.RegentDecreeUsedThisTurn = true;
        playerState.LoseStars(3);
        playerState.GainEnergy(1);
        await Draw(player, 1);
        state.Command = Math.Min(8, state.Command + 1);
        state.StoredShaCharge++;
        await ForgeWangJianIfReady(player, state);
        MakeHandCardsFree(player, 1, card => IsShaLike(card) || card.Type == CardType.Skill);
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

    private static async Task ForgeWangJianIfReady(Player player, CharacterSkillState state)
    {
        if (state.StoredShaCharge < 3)
        {
            return;
        }

        state.StoredShaCharge -= 3;
        var result = await CardPileCmd.AddGeneratedCardToCombat(
            ModelDb.Card<WangJianShaCard>(),
            PileType.Hand,
            player,
            CardPilePosition.Top);
        if (result.success && result.cardAdded is not null)
        {
            result.cardAdded.EnergyCost.SetThisTurn(0, true);
            result.cardAdded.ExhaustOnNextPlay = true;
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

    private static Task LoseHp(Player player, decimal amount, CardModel? source)
    {
        return amount <= 0 || !player.Creature.IsAlive
            ? Task.CompletedTask
            : CreatureCmd.Damage(NewContext(), player.Creature, amount, ValueProp.Unblockable, player.Creature, source!);
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
        public int TurnShaPlayed { get; set; }
        public int TurnTricksPlayed { get; set; }
        public int Strategy { get; set; }
        public int Thunder { get; set; }
        public int Soul { get; set; }
        public int Command { get; set; }
        public int Ingenuity { get; set; }
        public int IngenuityCap { get; set; } = 5;
        public ShaInfusion ShaInfusion { get; set; }
        public int StoredShaCharge { get; set; }
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
        public bool StoredShaDrawGrantedThisTurn { get; set; }
        public bool RegentDecreeUsedThisTurn { get; set; }
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
        public int QingGangBonusDamage { get; set; }
        public bool GuDingActive { get; set; }
        public bool GanJiangMoYeActive { get; set; }
        public int GanJiangMoYeBonusDamage { get; set; }
        public int GanJiangMoYeTriggersPerTurn { get; set; }
        public int GanJiangMoYeTriggersUsedThisTurn { get; set; }
        public bool TieSuoThisTurn { get; set; }
        public decimal TieSuoSplashMultiplier { get; set; }
        public int TengJiaBlock { get; set; }
        public int TengJiaHpLoss { get; set; }
        public int JueYingBlock { get; set; }
        public bool JueYingUsedThisTurn { get; set; }
        public bool MengDeXinShuActive { get; set; }
        public int MengDeXinShuFreeCards { get; set; }
        public bool MengDeXinShuUsedThisTurn { get; set; }
        public CardType? LastCardType { get; set; }
        public Dictionary<EquipSlot, string> ActiveEquip = new();
        public Dictionary<EquipSlot, bool> ActiveEquipUpgraded = new();
    }

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
