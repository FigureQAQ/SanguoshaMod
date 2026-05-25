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
            "ZhuGeCard" or "ZhangBaCard" or "QingGangCard" or "GuanShiCard" or "HanBingCard" or "QiLinCard" or "GuDingCard" => EquipSlot.Weapon,
            "BaiYinCard" or "RenWangCard" or "BaGuaCard" => EquipSlot.Armor,
            "ChiTuCard" or "DaWanCard" or "DiLuCard" => EquipSlot.Mount,
            "YuXiCard" or "MuNiuCard" or "TaiPingCard" => EquipSlot.Treasure,
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
                state.ZhangBaActive = false; state.ZhangBaUpgraded = false;
                state.QingGangActive = false;
                state.GuanShiActive = false; state.GuanShiBonusDamage = 0;
                state.HanBingActive = false; state.HanBingWeak = 0;
                state.QiLinActive = false; state.QiLinBonusDamage = 0; state.QiLinVulnerable = 0;
                state.GuDingActive = false;
                break;
            case EquipSlot.Armor:
                state.BaiYinActive = false; state.BaiYinTurnHeal = 0;
                state.RenWangActive = false; state.RenWangSkillEnergyGrantedThisTurn = false;
                state.BaGuaActive = false; state.BaGuaUpgraded = false;
                break;
            case EquipSlot.Mount:
                state.ChiTuActive = false;
                state.DaWanActive = false; state.DaWanBonusDamage = 0;
                state.DiLuActive = false; state.DiLuSkillDrawUsedThisTurn = false;
                break;
            case EquipSlot.Treasure:
                state.YuXiActive = false; state.YuXiTurnEnergy = 0;
                state.MuNiuActive = false; state.MuNiuSkillDraws = 0; state.MuNiuSkillDrawsUsed = 0;
                state.TaiPingActive = false; state.TaiPingTurnHeal = 0;
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
                await ApplyPower<DexterityPower>(player, player.Creature, wasUpgraded ? 2 : 1, null);
                break;
            case "BaGuaCard":
                await Draw(player, wasUpgraded ? 2 : 1);
                break;
            case "ZhuGeCard":
                var drawCount = Math.Max(1, GetState(player).TurnAttacksPlayed);
                await Draw(player, drawCount);
                break;
            case "ZhangBaCard":
                player.PlayerCombatState!.GainEnergy(1);
                break;
            case "QingGangCard":
                player.PlayerCombatState!.GainEnergy(wasUpgraded ? 2 : 1);
                break;
            case "GuanShiCard":
                await ApplyPower<StrengthPower>(player, player.Creature, wasUpgraded ? 2 : 1, null);
                break;
            case "HanBingCard":
                await Draw(player, wasUpgraded ? 2 : 1);
                break;
            case "QiLinCard":
                await ApplyPower<StrengthPower>(player, player.Creature, 1, null);
                break;
            case "GuDingCard":
                await ApplyPower<StrengthPower>(player, player.Creature, wasUpgraded ? 2 : 1, null);
                break;
            case "ChiTuCard":
                player.PlayerCombatState!.GainEnergy(1);
                break;
            case "DaWanCard":
                await ApplyPower<StrengthPower>(player, player.Creature, 1, null);
                break;
            case "DiLuCard":
                await ApplyPower<DexterityPower>(player, player.Creature, 1, null);
                break;
            case "YuXiCard":
                player.PlayerCombatState!.GainEnergy(1);
                break;
            case "MuNiuCard":
                await Draw(player, wasUpgraded ? 2 : 1);
                break;
            case "TaiPingCard":
                await HealIfWounded(player, wasUpgraded ? 10 : 6);
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
        state.ZhiHengSkillDrawUsedThisTurn = false;
        RefreshDisplayPower<ZhiHengDisplayPower>(player);
    }

    public static void ActivateWuShuang(Player player, bool upgraded)
    {
        var state = GetState(player);
        state.WuShuangActive = true;
        state.WuShuangRepeatsPerTurn = upgraded ? 2 : 1;
        state.WuShuangRepeatsUsedThisTurn = 0;
        RefreshDisplayPower<WuShuangDisplayPower>(player);
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

    public static async Task EnsureZhangBaSha(Player player)
    {
        var state = GetState(player);
        if (!state.ZhangBaActive)
        {
            return;
        }

        var hand = player.PlayerCombatState!.Hand.Cards;
        if (hand.Any(card => card is ZhangBaShaCard)
            || hand.Count(card => card is not ZhangBaShaCard) < 2
            || hand.Count >= CardPile.MaxCardsInHand)
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
        state.BaiYinTurnHeal = 2;
        RefreshDisplayPower<BaiYinDisplayPower>(player);
    }

    public static void ActivateChiTu(Player player)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Mount, "ChiTuCard", false);
        state.ChiTuActive = true;
        RefreshDisplayPower<ChiTuDisplayPower>(player);
    }

    public static void ActivateDaWan(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Mount, "DaWanCard", upgraded);
        state.DaWanActive = true;
        state.DaWanBonusDamage = upgraded ? 6 : 4;
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
        state.GuanShiBonusDamage = upgraded ? 12 : 8;
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
        state.QiLinBonusDamage = upgraded ? 7 : 4;
        state.QiLinVulnerable = 1;
        RefreshDisplayPower<QiLinDisplayPower>(player);
    }

    public static void ActivateRenWang(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Armor, "RenWangCard", upgraded);
        state.RenWangActive = true;
        RefreshDisplayPower<RenWangDisplayPower>(player);
    }

    public static void ActivateQingGang(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Weapon, "QingGangCard", upgraded);
        state.QingGangActive = true;
        RefreshDisplayPower<QingGangDisplayPower>(player);
    }

    public static void ActivateGuDing(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Weapon, "GuDingCard", upgraded);
        state.GuDingActive = true;
        RefreshDisplayPower<GuDingDisplayPower>(player);
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
        state.MuNiuSkillDraws = upgraded ? 2 : 1;
        state.MuNiuSkillDrawsUsed = 0;
        RefreshDisplayPower<MuNiuDisplayPower>(player);
    }

    public static void ActivateTaiPing(Player player, bool upgraded)
    {
        var state = GetState(player);
        ReplaceEquipment(player, state, EquipSlot.Treasure, "TaiPingCard", upgraded);
        state.TaiPingActive = true;
        state.TaiPingTurnHeal = 2;
        RefreshDisplayPower<TaiPingDisplayPower>(player);
    }

    public static void ActivateKongCheng(Player player, int intangible)
    {
        var state = GetState(player);
        state.KongChengActive = true;
        state.KongChengIntangible = Math.Max(1, intangible);
        state.KongChengTriggeredThisTurn = false;
        RefreshDisplayPower<KongChengDisplayPower>(player);
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
            case "BaiYinCard":
                RemoveDisplayPower<BaiYinDisplayPower>(player);
                break;
            case "RenWangCard":
                RemoveDisplayPower<RenWangDisplayPower>(player);
                break;
            case "BaGuaCard":
                RemoveDisplayPower<BaGuaDisplayPower>(player);
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
            case "YuXiCard":
                RemoveDisplayPower<YuXiDisplayPower>(player);
                break;
            case "MuNiuCard":
                RemoveDisplayPower<MuNiuDisplayPower>(player);
                break;
            case "TaiPingCard":
                RemoveDisplayPower<TaiPingDisplayPower>(player);
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
        state.TieSuoSplashMultiplier = upgraded ? 1m : 0.75m;
    }

    public static bool ShouldPierceBlock(Player player)
    {
        return GetState(player).QingGangActive;
    }

    public static decimal GetAttackDamageMultiplier(Player player, Creature target)
    {
        var state = GetState(player);
        return state.GuDingActive && target.Block <= 0 ? 2m : 1m;
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

        if (state.QiLinActive)
        {
            await ApplyPower<VulnerablePower>(play.Card.Owner, target, state.QiLinVulnerable, play.Card);
        }

        if (state.WuShuangActive
            && IsShaLike(play.Card)
            && state.WuShuangRepeatsUsedThisTurn < state.WuShuangRepeatsPerTurn)
        {
            state.WuShuangRepeatsUsedThisTurn++;
            await DamageTarget(play.Card.Owner, target, 6, play.Card);
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
                state.TurnAttacksPlayed = 0;
                state.TurnSkillsPlayed = 0;
                state.WuShuangRepeatsUsedThisTurn = 0;
                state.ZhiHengSkillDrawUsedThisTurn = false;
                state.TieSuoThisTurn = false;
                state.EnergyGrantedThisTurn = false;
                state.IngenuityEnergyGrantedThisTurn = false;
                state.BaGuaEnergyGrantedThisTurn = false;
                state.ChiTuEnergyGrantedThisTurn = false;
                state.DiLuSkillDrawUsedThisTurn = false;
                state.RenWangSkillEnergyGrantedThisTurn = false;
                state.MuNiuSkillDrawsUsed = 0;
                state.KongChengTriggeredThisTurn = false;
                state.LastCardType = null;
                BaiYinDamageCapPatch.ResetDamageThisTurn(player);

                ApplyZhuGeTurnStart(player, state);
                await ApplyEquipmentTurnStart(player, state);
                await EnsureZhangBaSha(player);
                await ApplyTurnFloor(player, state);
                await RunTurnStartSkill(player, state, evt.CombatState);
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Sanguosha turn-start skill failed: {ex}");
        }
    }

    private static async void OnCardPlayed(CardPlayedEvent evt)
    {
        try
        {
            var cardPlay = evt.CardPlay;
            var card = cardPlay.Card;
            var player = card.Owner;
            if (player is null || !player.Creature.IsAlive || IsNonPlayableNoise(card))
            {
                return;
            }

            var state = GetState(player);
            state.TurnCardsPlayed++;

            await ApplyLowHpEmergency(player, state, evt.CombatState, card);
            await RunCardPlayedSkill(player, state, evt.CombatState, cardPlay);
            await ApplyKongChengIfEmpty(player, state, card);

            state.LastCardType = card.Type;
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Sanguosha card-played skill failed: {ex}");
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
                _ = ApplyPower<StrengthPower>(player, player.Creature, 1, null);
                break;
            case SanguoshaSkill.Silent:
                state.Ingenuity = Math.Max(state.Ingenuity, 2);
                _ = Draw(player, 1);
                break;
            case SanguoshaSkill.Defect:
                state.Thunder = Math.Max(state.Thunder, 1);
                player.PlayerCombatState!.GainStars(2);
                break;
            case SanguoshaSkill.Necrobinder:
                _ = HealIfWounded(player, 3);
                state.Soul = Math.Max(state.Soul, 3);
                break;
            case SanguoshaSkill.Regent:
                state.Command = Math.Max(state.Command, 1);
                player.PlayerCombatState!.GainStars(3);
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
                    await GainBlock(player, 10, null);
                    break;
                }
                await GainBlock(player, 4, null);
                break;
            case SanguoshaSkill.Silent:
                state.Ingenuity = Math.Min(state.IngenuityCap, state.Ingenuity + 2);
                if (player.PlayerCombatState!.Hand.Cards.Count <= 4)
                {
                    await Draw(player, 1);
                }
                await GainBlock(player, 5, null);
                break;
            case SanguoshaSkill.Defect:
                state.Thunder += 2;
                player.PlayerCombatState!.GainStars(1);
                if (state.Thunder >= 3)
                {
                    state.Thunder -= 3;
                    await DamageAll(combat, player, 7, null);
                    await GainBlock(player, 6, null);
                }
                break;
            case SanguoshaSkill.Necrobinder:
                state.Soul = Math.Min(10, state.Soul + 2);
                await HealIfWounded(player, 1);
                await GainBlock(player, Math.Min(10, 4 + state.Soul), null);
                break;
            case SanguoshaSkill.Regent:
                state.Command = Math.Min(8, state.Command + 2);
                player.PlayerCombatState!.GainStars(2);
                await GainBlock(player, 4 + state.Command, null);
                break;
        }
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
        var isSkillLike = card.Type is not CardType.Attack and not CardType.Power;
        if (state.MuNiuActive
            && card is not MuNiuCard
            && isSkillLike
            && state.MuNiuSkillDrawsUsed < state.MuNiuSkillDraws)
        {
            state.MuNiuSkillDrawsUsed++;
            await Draw(player, 1);
        }

        if (state.ZhiHengActive
            && card is not ZhiHengCard
            && isSkillLike
            && !state.ZhiHengSkillDrawUsedThisTurn)
        {
            state.ZhiHengSkillDrawUsedThisTurn = true;
            await Draw(player, Math.Max(1, state.ZhiHengDrawBonus));
        }

        await EnsureZhangBaSha(player);
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
            player.PlayerCombatState!.GainStars(1);
        }

        if (state.Thunder >= 3)
        {
            state.Thunder -= 3;
            await DamageAll(combat, player, 7, card);
            player.PlayerCombatState!.GainStars(1);
        }
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
            await HealIfWounded(player, 3);
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
        }

        if (card is TaoYuanCard)
        {
            player.PlayerCombatState!.GainStars(2);
            await Draw(player, 1);
        }

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

    private static Task ApplyKongChengIfEmpty(Player player, CharacterSkillState state, CardModel source)
    {
        if (!state.KongChengActive
            || state.KongChengTriggeredThisTurn
            || player.PlayerCombatState!.Hand.Cards.Count > 0)
        {
            return Task.CompletedTask;
        }

        state.KongChengTriggeredThisTurn = true;
        return ApplyPower<IntangiblePower>(player, player.Creature, state.KongChengIntangible, source);
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

    private static async Task HealIfWounded(Player player, decimal amount)
    {
        var missingHp = player.Creature.MaxHp - player.Creature.CurrentHp;
        if (missingHp > 0 && amount > 0)
        {
            await CreatureCmd.Heal(player.Creature, Math.Min(amount, missingHp), true);
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

    private static Task ApplyPower<TPower>(Player player, Creature target, decimal amount, CardModel? source)
        where TPower : PowerModel, new()
    {
        return amount <= 0
            ? Task.CompletedTask
            : PowerCmd.Apply<TPower>(NewContext(), target, amount, player.Creature, source!, false);
    }

    internal static async Task TryApplyBaGuaDefense(
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
        while (player.Creature.Block < targetBlock)
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

    private static Task DamageTarget(Player player, Creature target, decimal amount, CardModel? source)
    {
        return amount <= 0 || !target.IsAlive
            ? Task.CompletedTask
            : CreatureCmd.Damage(NewContext(), target, amount, ValueProp.Move, player.Creature, source!);
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
        public int TurnSkillsPlayed { get; set; }
        public int Strategy { get; set; }
        public int Thunder { get; set; }
        public int Soul { get; set; }
        public int Command { get; set; }
        public int Ingenuity { get; set; }
        public int IngenuityCap { get; set; } = 5;
        public bool LowHpEmergencyUsed { get; set; }
        public bool EnergyGrantedThisTurn { get; set; }
        public int JiuNextShaBonus { get; set; }
        public bool IngenuityEnergyGrantedThisTurn { get; set; }
        public bool ZhuGeActive { get; set; }
        public int ZhuGeFreeShaPerTurn { get; set; } = 1;
        public bool ZhangBaActive { get; set; }
        public bool ZhangBaUpgraded { get; set; }
        public bool ZhiHengActive { get; set; }
        public int ZhiHengDrawBonus { get; set; } = 1;
        public bool ZhiHengSkillDrawUsedThisTurn { get; set; }
        public bool WuShuangActive { get; set; }
        public int WuShuangRepeatsPerTurn { get; set; } = 1;
        public int WuShuangRepeatsUsedThisTurn { get; set; }
        public bool BaGuaActive { get; set; }
        public bool BaGuaUpgraded { get; set; }
        public bool BaGuaEnergyGrantedThisTurn { get; set; }
        public bool BaiYinActive { get; set; }
        public int BaiYinTurnHeal { get; set; }
        public bool BaiYinDamageCap { get; set; }
        public bool TengJiaActive { get; set; }
        public bool JueYingActive { get; set; }
        public bool ZhuaHuangActive { get; set; }
        public bool ChiTuActive { get; set; }
        public bool ChiTuEnergyGrantedThisTurn { get; set; }
        public bool DaWanActive { get; set; }
        public int DaWanBonusDamage { get; set; }
        public bool DiLuActive { get; set; }
        public bool DiLuSkillDrawUsedThisTurn { get; set; }
        public bool YuXiActive { get; set; }
        public int YuXiTurnEnergy { get; set; }
        public bool MuNiuActive { get; set; }
        public int MuNiuSkillDraws { get; set; }
        public int MuNiuSkillDrawsUsed { get; set; }
        public bool TaiPingActive { get; set; }
        public int TaiPingTurnHeal { get; set; }
        public bool KongChengActive { get; set; }
        public int KongChengIntangible { get; set; }
        public bool KongChengTriggeredThisTurn { get; set; }
        public bool GuanShiActive { get; set; }
        public int GuanShiBonusDamage { get; set; }
        public bool HanBingActive { get; set; }
        public int HanBingWeak { get; set; }
        public bool QiLinActive { get; set; }
        public int QiLinBonusDamage { get; set; }
        public int QiLinVulnerable { get; set; }
        public bool RenWangActive { get; set; }
        public bool RenWangSkillEnergyGrantedThisTurn { get; set; }
        public bool QingGangActive { get; set; }
        public bool GuDingActive { get; set; }
        public bool TieSuoThisTurn { get; set; }
        public decimal TieSuoSplashMultiplier { get; set; }
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
}
