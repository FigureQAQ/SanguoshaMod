using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Cards;
using sanguosha.Characters;

namespace sanguosha.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlayTargeting))]
internal static class KongChengAttackLockCanPlayTargetingPatch
{
    private static bool Prefix(CardModel __instance, ref bool __result)
    {
        if (__instance.Type == CardType.Attack
            && __instance.Owner is { } owner
            && SanguoshaCharacterSkills.IsKongChengAttackLocked(owner))
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
internal static class DelayedLeBuModifyDamagePatch
{
    private static void Postfix(Creature? target, Creature? dealer, ref decimal __result)
    {
        if (target is { IsPlayer: true }
            && dealer is { IsPlayer: false }
            && SanguoshaCharacterSkills.IsLeBuAttackPrevented(dealer))
        {
            __result = 0;
        }
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyPowerAmountGiven))]
internal static class DelayedBingLiangPowerAmountPatch
{
    private static void Postfix(PowerModel power, Creature giver, Creature? target, ref decimal __result)
    {
        if (__result > 0
            && power.Type == PowerType.Debuff
            && target is not null
            && SanguoshaCharacterSkills.ShouldBlockBingLiangDebuff(giver, target))
        {
            __result = 0;
        }
    }
}

internal static class DelayedBingLiangStatusCardPatchHelper
{
    public static bool PrefixSingle(CardModel card, ref Task<CardPileAddResult> __result)
    {
        if (!IsBlockedStatusCard(card))
        {
            return true;
        }

        __result = Task.FromResult(new CardPileAddResult { cardAdded = card, success = false });
        return false;
    }

    public static bool PrefixMany(ref IEnumerable<CardModel> cards, ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        var cardList = cards.ToList();
        if (cardList.Count == 0)
        {
            return true;
        }

        var blocked = cardList
            .Where(card => IsBlockedStatusCard(card))
            .ToList();
        if (blocked.Count == 0)
        {
            return true;
        }

        var allowed = cardList.Except(blocked).ToList();
        if (allowed.Count > 0)
        {
            cards = allowed;
            return true;
        }

        __result = Task.FromResult<IReadOnlyList<CardPileAddResult>>(
            blocked
                .Select(card => new CardPileAddResult { cardAdded = card, success = false })
                .ToList());
        return false;
    }

    public static bool IsBlockedStatusCard(CardModel card)
    {
        return card.Owner is { } owner
            && SanguoshaCharacterSkills.ShouldBlockBingLiangStatusCards(owner)
            && (card.Type is CardType.Status or CardType.Curse or CardType.Quest
                || card.Rarity is CardRarity.Status or CardRarity.Curse or CardRarity.Quest);
    }
}

[HarmonyPatch(
    typeof(CardPileCmd),
    nameof(CardPileCmd.Add),
    [
        typeof(CardModel),
        typeof(PileType),
        typeof(CardPilePosition),
        typeof(AbstractModel),
        typeof(bool)
    ])]
internal static class DelayedBingLiangStatusCardPileTypeSinglePatch
{
    private static bool Prefix(CardModel card, ref Task<CardPileAddResult> __result)
    {
        return DelayedBingLiangStatusCardPatchHelper.PrefixSingle(card, ref __result);
    }
}

[HarmonyPatch(
    typeof(CardPileCmd),
    nameof(CardPileCmd.Add),
    [
        typeof(CardModel),
        typeof(CardPile),
        typeof(CardPilePosition),
        typeof(AbstractModel),
        typeof(bool)
    ])]
internal static class DelayedBingLiangStatusCardPileSinglePatch
{
    private static bool Prefix(CardModel card, ref Task<CardPileAddResult> __result)
    {
        return DelayedBingLiangStatusCardPatchHelper.PrefixSingle(card, ref __result);
    }
}

[HarmonyPatch(
    typeof(CardPileCmd),
    nameof(CardPileCmd.Add),
    [
        typeof(IEnumerable<CardModel>),
        typeof(PileType),
        typeof(CardPilePosition),
        typeof(AbstractModel),
        typeof(bool)
    ])]
internal static class DelayedBingLiangStatusCardsPileTypePatch
{
    private static bool Prefix(ref IEnumerable<CardModel> cards, ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        return DelayedBingLiangStatusCardPatchHelper.PrefixMany(ref cards, ref __result);
    }
}

[HarmonyPatch(
    typeof(CardPileCmd),
    nameof(CardPileCmd.Add),
    [
        typeof(IEnumerable<CardModel>),
        typeof(CardPile),
        typeof(CardPilePosition),
        typeof(AbstractModel),
        typeof(bool)
    ])]
internal static class DelayedBingLiangStatusCardsPilePatch
{
    private static bool Prefix(ref IEnumerable<CardModel> cards, ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        return DelayedBingLiangStatusCardPatchHelper.PrefixMany(ref cards, ref __result);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeDamageReceived))]
internal static class BaGuaBeforeDamageReceivedPatch
{
    private static void Postfix(
        PlayerChoiceContext choiceContext,
        IRunState runState,
        ICombatState? combatState,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref Task __result)
    {
        if (!target.IsPlayer
            || target.Player is not { } player
            || dealer is null
            || dealer.IsPlayer)
        {
            return;
        }

        __result = ApplyAfterOriginal(__result, choiceContext, player, dealer, amount, props, cardSource);
    }

    private static async Task ApplyAfterOriginal(
        Task original,
        PlayerChoiceContext choiceContext,
        Player player,
        Creature dealer,
        decimal amount,
        ValueProp props,
        CardModel? cardSource)
    {
        await original;
        await SanguoshaCharacterSkills.TryApplyIncomingDamageAbilities(
            choiceContext,
            player,
            dealer,
            amount,
            props,
            cardSource);
        await SanguoshaCharacterSkills.TryApplyBaGuaShan(choiceContext, player, amount, props, cardSource);
    }
}

[HarmonyPatch(typeof(TuningFork), nameof(TuningFork.AfterCardPlayed))]
internal static class TuningForkSanguoshaSkillPatch
{
    private const int SkillsThreshold = 10;
    private const decimal BlockAmount = 7m;

    private static bool Prefix(TuningFork __instance, CardPlay cardPlay, ref Task __result)
    {
        if (cardPlay.Card is not SanguoshaCard
            || cardPlay.Card.Type != CardType.Skill
            || cardPlay.Card.Owner != __instance.Owner)
        {
            return true;
        }

        __result = ApplySanguoshaSkillCount(__instance);
        return false;
    }

    private static async Task ApplySanguoshaSkillCount(TuningFork relic)
    {
        relic.NotifySkillPlayed();
        if (relic.SkillsPlayed < SkillsThreshold)
        {
            return;
        }

        relic.Flash();
        await CreatureCmd.GainBlock(relic.Owner.Creature, BlockAmount, ValueProp.Unpowered, null);
        AccessTools.PropertySetter(typeof(TuningFork), nameof(TuningFork.SkillsPlayed))
            ?.Invoke(relic, [relic.SkillsPlayed - SkillsThreshold]);
    }
}

[HarmonyPatch(typeof(StrikeDummy), nameof(StrikeDummy.ModifyDamageAdditive))]
internal static class StrikeDummySanguoshaShaPatch
{
    private static bool Prefix(StrikeDummy __instance, CardModel? cardSource, ref decimal __result)
    {
        if (cardSource is null || !SanguoshaRuntimeCardEventHelpers.IsShaLike(cardSource))
        {
            return true;
        }

        __result = __instance.DynamicVars["ExtraDamage"].BaseValue;
        return false;
    }
}

[HarmonyPatch(typeof(FakeStrikeDummy), nameof(FakeStrikeDummy.ModifyDamageAdditive))]
internal static class FakeStrikeDummySanguoshaShaPatch
{
    private static bool Prefix(FakeStrikeDummy __instance, CardModel? cardSource, ref decimal __result)
    {
        if (cardSource is null || !SanguoshaRuntimeCardEventHelpers.IsShaLike(cardSource))
        {
            return true;
        }

        __result = __instance.DynamicVars["ExtraDamage"].BaseValue;
        return false;
    }
}

[HarmonyPatch(typeof(FastenPower), nameof(FastenPower.ModifyBlockAdditive))]
internal static class FastenPowerSanguoshaShanBlockPatch
{
    private static bool Prefix(FastenPower __instance, CardModel cardSource, ref decimal __result)
    {
        if (!SanguoshaRuntimeCardEventHelpers.IsShanLike(cardSource))
        {
            return true;
        }

        __result = __instance.Amount;
        return false;
    }
}

[HarmonyPatch(typeof(FastenPower), nameof(FastenPower.AfterModifyingBlockAmount))]
internal static class FastenPowerSanguoshaShanFlashPatch
{
    private static bool Prefix(CardModel cardSource, ref Task __result)
    {
        if (!SanguoshaRuntimeCardEventHelpers.IsShanLike(cardSource))
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }
}

[HarmonyPatch(typeof(Claws), nameof(Claws.AfterObtained))]
internal static class ClawsSanguoshaShaPatch
{
    private static bool Prefix(Claws __instance, ref Task __result)
    {
        var shaCards = __instance.Owner!.Deck.Cards
            .Where(SanguoshaRuntimeCardEventHelpers.IsShaLike)
            .ToList();
        if (shaCards.Count == 0)
        {
            return true;
        }

        __result = TransformShaCards(__instance, shaCards);
        return false;
    }

    private static Task TransformShaCards(Claws relic, IReadOnlyList<CardModel> shaCards)
    {
        var transformations = shaCards
            .Select(card => new CardTransformation(card, CreateMaulFromOriginal(relic, card)))
            .ToList();

        return CardCmd.Transform(transformations, relic.Owner!.PlayerRng.Rewards, CardPreviewStyle.HorizontalLayout);
    }

    private static CardModel CreateMaulFromOriginal(Claws relic, CardModel original)
    {
        return (CardModel)AccessTools
            .Method(typeof(Claws), "CreateMaulFromOriginal")!
            .Invoke(relic, [original, false])!;
    }
}

[HarmonyPatch(typeof(SpiralingWhirlpool), nameof(SpiralingWhirlpool.IsAllowed))]
internal static class SpiralingWhirlpoolSanguoshaAllowedPatch
{
    private static bool Prefix(IRunState runState, ref bool __result)
    {
        __result = runState.Players.All(player =>
            player.Deck.Cards.Any(SanguoshaRuntimeCardEventHelpers.IsShaOrShanLike));
        return false;
    }
}

[HarmonyPatch(typeof(SpiralingWhirlpool), "ObserveTheSpiral")]
internal static class SpiralingWhirlpoolSanguoshaObservePatch
{
    private static bool Prefix(SpiralingWhirlpool __instance, ref Task __result)
    {
        __result = EnchantShaOrShan(__instance);
        return false;
    }

    private static async Task EnchantShaOrShan(SpiralingWhirlpool eventModel)
    {
        var selectedCards = await CardSelectCmd.FromDeckForEnchantment(
            eventModel.Owner!,
            ModelDb.Enchantment<Spiral>(),
            1,
            card => card is not null && SanguoshaRuntimeCardEventHelpers.IsShaOrShanLike(card),
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1));

        foreach (var card in selectedCards)
        {
            CardCmd.Enchant<Spiral>(card, 1);
        }

        SanguoshaRuntimeCardEventHelpers.FinishEvent(
            eventModel,
            "SPIRALING_WHIRLPOOL.pages.OBSERVE.description");
    }
}

[HarmonyPatch(typeof(Amalgamator), nameof(Amalgamator.IsAllowed))]
internal static class AmalgamatorSanguoshaAllowedPatch
{
    private static bool Prefix(IRunState runState, ref bool __result)
    {
        __result = runState.Players.All(player =>
            player.Deck.Cards.Count(SanguoshaRuntimeCardEventHelpers.IsShaLike) >= 2
            || player.Deck.Cards.Count(SanguoshaRuntimeCardEventHelpers.IsShanLike) >= 2);
        return false;
    }
}

[HarmonyPatch(typeof(Amalgamator), "GenerateInitialOptions")]
internal static class AmalgamatorSanguoshaOptionsPatch
{
    private static void Postfix(Amalgamator __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (__instance.Owner!.Deck.Cards.Count(SanguoshaRuntimeCardEventHelpers.IsShaLike) < 2)
        {
            __result = __result.Skip(1).ToList();
        }

        if (__instance.Owner!.Deck.Cards.Count(SanguoshaRuntimeCardEventHelpers.IsShanLike) < 2)
        {
            __result = __result.Take(1).ToList();
        }
    }
}

[HarmonyPatch(typeof(Amalgamator), "CombineStrikes")]
internal static class AmalgamatorSanguoshaShaPatch
{
    private static bool Prefix(Amalgamator __instance, ref Task __result)
    {
        __result = SanguoshaRuntimeCardEventHelpers.CombineBasicCards<ShaCard>(
            __instance,
            SanguoshaRuntimeCardEventHelpers.IsShaLike,
            "AMALGAMATOR.pages.COMBINE_STRIKES.description");
        return false;
    }
}

[HarmonyPatch(typeof(Amalgamator), "CombineDefends")]
internal static class AmalgamatorSanguoshaShanPatch
{
    private static bool Prefix(Amalgamator __instance, ref Task __result)
    {
        __result = SanguoshaRuntimeCardEventHelpers.CombineBasicCards<ShanCard>(
            __instance,
            SanguoshaRuntimeCardEventHelpers.IsShanLike,
            "AMALGAMATOR.pages.COMBINE_DEFENDS.description");
        return false;
    }
}

[HarmonyPatch(typeof(MassiveScroll), nameof(MassiveScroll.AfterObtained))]
internal static class MassiveScrollSanguoshaCardPatch
{
    private const int Options = 3;

    private static bool Prefix(MassiveScroll __instance, ref Task __result)
    {
        __result = AddSanguoshaCard(__instance);
        return false;
    }

    private static async Task AddSanguoshaCard(MassiveScroll relic)
    {
        await SanguoshaRuntimeCardEventHelpers.ChooseOneCardAndAddToDeck(relic.Owner, Options);
    }
}

[HarmonyPatch(typeof(ArcaneScroll), nameof(ArcaneScroll.AfterObtained))]
internal static class ArcaneScrollSanguoshaCardPatch
{
    private static bool Prefix(ArcaneScroll __instance, ref Task __result)
    {
        __result = SanguoshaRuntimeCardEventHelpers.AddOneCardToDeck(
            __instance.Owner!,
            card => card.Rarity == CardRarity.Rare && card is not ZhangBaShaCard,
            upgrade: true,
            exactRarity: true);
        return false;
    }
}

[HarmonyPatch(typeof(NeowsTalisman), nameof(NeowsTalisman.AfterObtained))]
internal static class NeowsTalismanSanguoshaBasicPatch
{
    private static bool Prefix(NeowsTalisman __instance, ref Task __result)
    {
        __result = UpgradeShaAndShan(__instance);
        return false;
    }

    private static Task UpgradeShaAndShan(NeowsTalisman relic)
    {
        var deck = PileType.Deck.GetPile(relic.Owner).Cards;
        var sha = deck.LastOrDefault(SanguoshaRuntimeCardEventHelpers.IsShaLike);
        var shan = deck.LastOrDefault(SanguoshaRuntimeCardEventHelpers.IsShanLike);

        if (sha is not null)
        {
            CardCmd.Upgrade(sha);
        }

        if (shan is not null && shan != sha)
        {
            CardCmd.Upgrade(shan);
        }

        return Task.CompletedTask;
    }
}

[HarmonyPatch(typeof(GhostSeed), nameof(GhostSeed.CanAffect))]
internal static class GhostSeedSanguoshaBasicPatch
{
    private static bool Prefix(CardModel card, ref bool __result)
    {
        if (!SanguoshaRuntimeCardEventHelpers.IsShaOrShanLike(card))
        {
            return true;
        }

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(LeafyPoultice), nameof(LeafyPoultice.AfterObtained))]
internal static class LeafyPoulticeSanguoshaBasicPatch
{
    private static bool Prefix(LeafyPoultice __instance, ref Task __result)
    {
        var cards = new[]
            {
                __instance.Owner!.Deck.Cards.FirstOrDefault(card =>
                    SanguoshaRuntimeCardEventHelpers.IsShaLike(card) && card.IsTransformable),
                __instance.Owner!.Deck.Cards.FirstOrDefault(card =>
                    SanguoshaRuntimeCardEventHelpers.IsShanLike(card) && card.IsTransformable)
            }
            .Where(card => card is not null)
            .Cast<CardModel>()
            .ToList();
        if (cards.Count == 0)
        {
            return true;
        }

        __result = TransformAndLoseMaxHp(__instance, cards);
        return false;
    }

    private static async Task TransformAndLoseMaxHp(LeafyPoultice relic, IReadOnlyList<CardModel> cards)
    {
        await SanguoshaRuntimeCardEventHelpers.TransformToSanguoshaCards(
            relic.Owner!,
            cards,
            CardPreviewStyle.HorizontalLayout);
        await CreatureCmd.LoseMaxHp(
            new BlockingPlayerChoiceContext(),
            relic.Owner!.Creature,
            relic.DynamicVars["MaxHp"].BaseValue,
            false);
    }
}

[HarmonyPatch(typeof(NutritiousSoup), nameof(NutritiousSoup.AfterObtained))]
internal static class NutritiousSoupSanguoshaShaPatch
{
    private static bool Prefix(NutritiousSoup __instance, ref Task __result)
    {
        var shaCards = __instance.Owner!.Deck.Cards
            .Where(SanguoshaRuntimeCardEventHelpers.IsShaLike)
            .ToList();
        if (shaCards.Count == 0)
        {
            return true;
        }

        __result = EnchantShaCards(shaCards);
        return false;
    }

    private static Task EnchantShaCards(IReadOnlyList<CardModel> shaCards)
    {
        foreach (var card in shaCards)
        {
            CardCmd.Enchant<TezcatarasEmber>(card, 1);
        }

        CardCmd.Preview(shaCards, 1.2f, CardPreviewStyle.HorizontalLayout);
        return Task.CompletedTask;
    }
}

[HarmonyPatch(typeof(PandorasBox), nameof(PandorasBox.AfterObtained))]
internal static class PandorasBoxSanguoshaBasicPatch
{
    private static bool Prefix(PandorasBox __instance, ref Task __result)
    {
        var cards = __instance.Owner!.Deck.Cards
            .Where(card => SanguoshaRuntimeCardEventHelpers.IsShaOrShanLike(card) && card.IsTransformable)
            .ToList();
        if (cards.Count == 0)
        {
            return true;
        }

        __result = SanguoshaRuntimeCardEventHelpers.TransformToSanguoshaCards(
            __instance.Owner!,
            cards,
            CardPreviewStyle.HorizontalLayout);
        return false;
    }
}

[HarmonyPatch(typeof(LargeCapsule), "GetStrikeForCharacter")]
internal static class LargeCapsuleSanguoshaShaPatch
{
    private static void Postfix(ref CardModel __result)
    {
        __result = ModelDb.Card<ShaCard>();
    }
}

[HarmonyPatch(typeof(LargeCapsule), "GetDefendForCharacter")]
internal static class LargeCapsuleSanguoshaShanPatch
{
    private static void Postfix(ref CardModel __result)
    {
        __result = ModelDb.Card<ShanCard>();
    }
}

[HarmonyPatch(typeof(LeadPaperweight), nameof(LeadPaperweight.AfterObtained))]
internal static class LeadPaperweightSanguoshaCardPatch
{
    private static bool Prefix(LeadPaperweight __instance, ref Task __result)
    {
        __result = SanguoshaRuntimeCardEventHelpers.ChooseOneCardAndAddToDeck(__instance.Owner, 2);
        return false;
    }
}

[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.AfterObtained))]
internal static class CircletSanguoshaClonePatch
{
    private static bool Prefix(RelicModel __instance, ref Task __result)
    {
        if (__instance is not Circlet)
        {
            return true;
        }

        __result = EnchantOneCard(__instance.Owner);
        return false;
    }

    private static async Task EnchantOneCard(Player player)
    {
        var eligibleCards = player.Deck.Cards
            .Where(card => card is not null)
            .ToList();
        if (eligibleCards.Count == 0)
        {
            return;
        }

        IEnumerable<CardModel> selectedCards;
        try
        {
            selectedCards = await CardSelectCmd.FromDeckForEnchantment(
                player,
                ModelDb.Enchantment<Clone>(),
                1,
                card => card is not null,
                new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot wait for remote choice", StringComparison.Ordinal))
        {
            Entry.Logger.Warn("Circlet clone selection was unavailable during run setup; enchanting the first eligible deck card instead.");
            selectedCards = eligibleCards.Take(1);
        }

        foreach (var card in selectedCards)
        {
            CardCmd.Enchant<Clone>(card, 2);
        }
    }
}

[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
internal static class CloneEnchantmentRestSiteOptionPatch
{
    private static void Postfix(Player player, ref List<RestSiteOption> __result)
    {
        if (!player.Deck.Cards.Any(SanguoshaCloneRestSiteHelpers.HasCloneEnchantment))
        {
            return;
        }

        if (__result.Any(option => option.OptionId == "CLONE"))
        {
            return;
        }

        __result.Add(new CloneRestSiteOption(player));
    }
}

[HarmonyPatch(typeof(CloneRestSiteOption), nameof(CloneRestSiteOption.OnSelect))]
internal static class CloneRestSiteOptionSelectionPatch
{
    private static bool Prefix(CloneRestSiteOption __instance, ref Task<bool> __result)
    {
        var owner = Traverse.Create(__instance).Property("Owner").GetValue<Player>();
        __result = SanguoshaCloneRestSiteHelpers.CloneCards(owner);
        return false;
    }
}

internal static class SanguoshaCloneRestSiteHelpers
{
    public static bool HasCloneEnchantment(CardModel card)
    {
        var enchantment = card.Enchantment;
        return enchantment is Clone
            || enchantment?.Id == ModelDb.Enchantment<Clone>().Id;
    }

    public static async Task<bool> CloneCards(Player player)
    {
        var cloneCards = player.Deck.Cards
            .Where(HasCloneEnchantment)
            .ToList();
        if (cloneCards.Count == 0)
        {
            Entry.Logger.Warn("Clone rest site option was selected, but no clone-enchanted cards were found.");
            return false;
        }

        var results = new List<CardPileAddResult>(cloneCards.Count);
        foreach (var sourceCard in cloneCards)
        {
            var card = player.RunState.CloneCard(sourceCard);
            var result = await CardPileCmd.Add(
                card,
                PileType.Deck,
                CardPilePosition.Bottom,
                sourceCard.Enchantment,
                skipVisuals: false);
            if (result.success)
            {
                results.Add(result);
            }
        }

        if (results.Count > 0)
        {
            CardCmd.PreviewCardPileAdd(results, 1.2f, CardPreviewStyle.MessyLayout);
        }

        return results.Count > 0;
    }
}

[HarmonyPatch(typeof(ScrollBoxes), nameof(ScrollBoxes.IsAllowedAtNeow))]
internal static class ScrollBoxesSanguoshaAllowedPatch
{
    private static bool Prefix(Player player, ref bool __result)
    {
        __result = SanguoshaRuntimeCardEventHelpers.CanCreateSanguoshaBundles();
        return false;
    }
}

[HarmonyPatch(typeof(ScrollBoxes), nameof(ScrollBoxes.AfterObtained))]
internal static class ScrollBoxesSanguoshaBundlePatch
{
    private static bool Prefix(ScrollBoxes __instance, ref Task __result)
    {
        __result = ChooseSanguoshaBundle(__instance);
        return false;
    }

    private static async Task ChooseSanguoshaBundle(ScrollBoxes relic)
    {
        var bundles = SanguoshaRuntimeCardEventHelpers.CreateSanguoshaBundles(relic.Owner);
        if (bundles.Count == 0)
        {
            return;
        }

        var chosenCards = await CardSelectCmd.FromChooseABundleScreen(relic.Owner, bundles);
        foreach (var card in chosenCards)
        {
            await CardPileCmd.Add(card, PileType.Deck);
        }
    }
}

[HarmonyPatch(typeof(NinjaScroll), nameof(NinjaScroll.BeforeHandDraw))]
internal static class NinjaScrollSanguoshaShaPatch
{
    private static bool Prefix(
        NinjaScroll __instance,
        Player player,
        ICombatState combatState,
        ref Task __result)
    {
        if (player != __instance.Owner
            || __instance.Owner.PlayerCombatState is not { TurnNumber: <= 1 })
        {
            return true;
        }

        __result = AddTemporarySha(__instance, combatState);
        return false;
    }

    private static async Task AddTemporarySha(NinjaScroll relic, ICombatState combatState)
    {
        relic.Flash();
        var cards = new List<CardModel>();
        for (var index = 0; index < relic.DynamicVars["Shivs"].IntValue; index++)
        {
            var card = combatState.CreateCard<ShaCard>(relic.Owner);
            card.EnergyCost.SetThisTurn(0, true);
            card.ExhaustOnNextPlay = true;
            cards.Add(card);
        }

        await CardPileCmd.Add(cards, PileType.Hand, CardPilePosition.Top);
    }
}

[HarmonyPatch(typeof(SeaGlass), nameof(SeaGlass.AfterObtained))]
internal static class SeaGlassSanguoshaCardPatch
{
    private static bool Prefix(SeaGlass __instance, ref Task __result)
    {
        __result = SanguoshaRuntimeCardEventHelpers.ChooseCardsAndAddToDeck(
            __instance.Owner!,
            new List<List<CardCreationResult>>
            {
                SanguoshaRuntimeCardEventHelpers.CreateRewardOptions(__instance.Owner, 1, card => card.Rarity == CardRarity.Common),
                SanguoshaRuntimeCardEventHelpers.CreateRewardOptions(__instance.Owner, 1, card => card.Rarity == CardRarity.Uncommon),
                SanguoshaRuntimeCardEventHelpers.CreateRewardOptions(__instance.Owner, 1, card => card.Rarity == CardRarity.Rare)
            }.SelectMany(options => options).ToList(),
            0,
            3,
            CardPreviewStyle.HorizontalLayout);
        return false;
    }
}

[HarmonyPatch(typeof(BrainLeech), "ShareKnowledge")]
internal static class BrainLeechSanguoshaCardPatch
{
    private static bool Prefix(BrainLeech __instance, ref Task __result)
    {
        var optionCount = Math.Max(1, __instance.DynamicVars["FromCardChoiceCount"].IntValue);
        __result = SanguoshaRuntimeCardEventHelpers.ChooseOneCardAndAddToDeck(
            __instance.Owner!,
            optionCount,
            finishEvent: (__instance, "BRAIN_LEECH.pages.SHARE_KNOWLEDGE.description"),
            selectionPrompt: "BRAIN_LEECH.pages.SHARE_KNOWLEDGE.selectionScreenPrompt");
        return false;
    }
}

[HarmonyPatch(typeof(InfestedAutomaton), "Study")]
internal static class InfestedAutomatonStudySanguoshaCardPatch
{
    private static bool Prefix(InfestedAutomaton __instance, ref Task __result)
    {
        __result = SanguoshaRuntimeCardEventHelpers.AddOneCardToDeck(
            __instance.Owner!,
            finishEvent: (__instance, "INFESTED_AUTOMATON.pages.STUDY.description"));
        return false;
    }
}

[HarmonyPatch(typeof(InfestedAutomaton), "TouchCore")]
internal static class InfestedAutomatonTouchCoreSanguoshaCardPatch
{
    private static bool Prefix(InfestedAutomaton __instance, ref Task __result)
    {
        __result = SanguoshaRuntimeCardEventHelpers.AddOneCardToDeck(
            __instance.Owner!,
            upgrade: true,
            finishEvent: (__instance, "INFESTED_AUTOMATON.pages.TOUCH_CORE.description"));
        return false;
    }
}

[HarmonyPatch(typeof(RoomFullOfCheese), "Gorge")]
internal static class RoomFullOfCheeseGorgeSanguoshaCardPatch
{
    private static bool Prefix(RoomFullOfCheese __instance, ref Task __result)
    {
        var rewardOptions = SanguoshaRuntimeCardEventHelpers.CreateRewardOptions(
            __instance.Owner!,
            8,
            card => card.Rarity == CardRarity.Common,
            exactRarity: true);
        __result = SanguoshaRuntimeCardEventHelpers.ChooseCardsAndAddToDeck(
            __instance.Owner!,
            rewardOptions,
            2,
            2,
            CardPreviewStyle.HorizontalLayout,
            (__instance, "ROOM_FULL_OF_CHEESE.pages.GORGE.description"),
            "ROOM_FULL_OF_CHEESE.pages.GORGE.selectionScreenPrompt");
        return false;
    }
}

[HarmonyPatch(typeof(EndlessConveyor), "FriedEel")]
internal static class EndlessConveyorFriedEelSanguoshaCardPatch
{
    private static bool Prefix(EndlessConveyor __instance, ref Task __result)
    {
        __result = SanguoshaRuntimeCardEventHelpers.AddOneCardToDeck(__instance.Owner!);
        return false;
    }
}

internal static class SanguoshaRuntimeCardEventHelpers
{
    public static bool IsShaLike(CardModel card)
    {
        return card is ShaCard;
    }

    public static bool IsShanLike(CardModel card)
    {
        return card is ShanCard;
    }

    public static bool IsShaOrShanLike(CardModel card)
    {
        return IsShaLike(card) || IsShanLike(card);
    }

    public static Task TransformToSanguoshaCards(
        Player player,
        IReadOnlyList<CardModel> cards,
        CardPreviewStyle previewStyle)
    {
        var replacements = CreateSanguoshaTransformationOptions();
        if (replacements.Count == 0)
        {
            return Task.CompletedTask;
        }

        var transformations = cards
            .Select(card => new CardTransformation(card, replacements))
            .ToList();
        return CardCmd.Transform(transformations, player.PlayerRng.Rewards, previewStyle);
    }

    public static async Task CombineBasicCards<TCard>(
        EventModel eventModel,
        Func<CardModel, bool> filter,
        string finishedDescriptionKey)
        where TCard : SanguoshaCard
    {
        var selectedCards = (await CardSelectCmd.FromDeckForRemoval(
            eventModel.Owner!,
            new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2),
            card => filter(card) && card.IsRemovable)).ToList();
        if (selectedCards.Count == 0)
        {
            FinishEvent(eventModel, finishedDescriptionKey);
            return;
        }

        await CardPileCmd.RemoveFromDeck(selectedCards, false);
        var owner = eventModel.Owner!;
        var newCard = owner.RunState.CreateCard<TCard>(owner);
        var result = await CardPileCmd.Add(newCard, PileType.Deck, CardPilePosition.Bottom, null, false);
        CardCmd.PreviewCardPileAdd(result, 1.2f, CardPreviewStyle.HorizontalLayout);
        FinishEvent(eventModel, finishedDescriptionKey);
    }

    public static bool CanCreateSanguoshaBundles()
    {
        var cards = SanguoshaCardCatalog.TryGetCards();
        if (cards is null)
        {
            return false;
        }

        return cards.Count(card => card.Rarity == CardRarity.Common) >= 2
            && cards.Any(card => card.Rarity == CardRarity.Uncommon);
    }

    public static List<IReadOnlyList<CardModel>> CreateSanguoshaBundles(Player player)
    {
        var cards = SanguoshaCardCatalog.TryGetCards();
        if (cards is null || cards.Count == 0)
        {
            return [];
        }

        var common = cards.Where(card => card.Rarity == CardRarity.Common).ToList();
        var uncommon = cards.Where(card => card.Rarity == CardRarity.Uncommon).ToList();
        if (common.Count == 0 || uncommon.Count == 0)
        {
            return [];
        }

        var rewards = player.PlayerRng.Rewards;
        var usedCardIds = new HashSet<ModelId>();
        var bundles = new List<IReadOnlyList<CardModel>>();
        for (var bundleIndex = 0; bundleIndex < 2; bundleIndex++)
        {
            var bundle = new List<CardModel>();
            for (var commonIndex = 0; commonIndex < 2; commonIndex++)
            {
                var card = PickUnusedCard(common, usedCardIds, rewards);
                if (card is not null)
                {
                    bundle.Add(player.RunState.CreateCard(card, player));
                }
            }

            var uncommonCard = PickUnusedCard(uncommon, usedCardIds, rewards);
            if (uncommonCard is not null)
            {
                bundle.Add(player.RunState.CreateCard(uncommonCard, player));
            }

            if (bundle.Count > 0)
            {
                bundles.Add(bundle);
            }
        }

        return bundles;
    }

    public static List<CardCreationResult> CreateRewardOptions(
        Player player,
        int count,
        Func<CardModel, bool>? filter = null,
        bool upgrade = false,
        bool exactRarity = false)
    {
        var cards = SanguoshaCardCatalog.TryGetCards();
        if (cards is null || cards.Count == 0)
        {
            return [];
        }

        var candidates = filter is null ? cards : cards.Where(filter).ToList();
        if (candidates.Count == 0)
        {
            candidates = cards.ToList();
        }

        var creationOptions = new CardCreationOptions(
            candidates,
            CardCreationSource.Other,
            exactRarity ? CardRarityOddsType.Uniform : CardRarityOddsType.RegularEncounter);
        var results = CardFactory.CreateForReward(player, count, creationOptions).ToList();
        if (results.Count == 0 && exactRarity)
        {
            creationOptions = new CardCreationOptions(
                candidates,
                CardCreationSource.Other,
                CardRarityOddsType.RegularEncounter);
            results = CardFactory.CreateForReward(player, count, creationOptions).ToList();
        }

        if (upgrade)
        {
            foreach (var result in results)
            {
                CardCmd.Upgrade(result.Card, CardPreviewStyle.None);
            }
        }

        return results;
    }

    public static async Task AddOneCardToDeck(
        Player player,
        Func<CardModel, bool>? filter = null,
        bool upgrade = false,
        (EventModel Event, string DescriptionKey)? finishEvent = null,
        bool exactRarity = false)
    {
        var card = CreateRewardOptions(player, 1, filter, upgrade, exactRarity).FirstOrDefault()?.Card;
        if (card is not null)
        {
            await AddCardToDeck(card, CardPreviewStyle.HorizontalLayout);
        }

        if (finishEvent is { } finished)
        {
            FinishEvent(finished.Event, finished.DescriptionKey);
        }
    }

    public static async Task ChooseOneCardAndAddToDeck(
        Player player,
        int optionCount,
        Func<CardModel, bool>? filter = null,
        (EventModel Event, string DescriptionKey)? finishEvent = null,
        string? selectionPrompt = null)
    {
        var options = CreateRewardOptions(player, optionCount, filter);
        var cards = options.Select(result => result.Card).ToList();
        if (cards.Count == 0)
        {
            if (finishEvent is { } emptyFinished)
            {
                FinishEvent(emptyFinished.Event, emptyFinished.DescriptionKey);
            }

            return;
        }

        var chosenCard = selectionPrompt is null
            ? await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), cards, player, true)
            : (await CardSelectCmd.FromSimpleGridForRewards(
                new BlockingPlayerChoiceContext(),
                options,
                player,
                new CardSelectorPrefs(L10NLookup(finishEvent?.Event, selectionPrompt), 1) { Cancelable = false }))
                .FirstOrDefault();
        if (chosenCard is not null)
        {
            await AddCardToDeck(chosenCard, CardPreviewStyle.HorizontalLayout);
        }

        RecordUnchosenCardOptions(player, cards, chosenCard);

        if (finishEvent is { } finished)
        {
            FinishEvent(finished.Event, finished.DescriptionKey);
        }
    }

    public static async Task ChooseCardsAndAddToDeck(
        Player player,
        List<CardCreationResult> options,
        int minCards,
        int maxCards,
        CardPreviewStyle previewStyle,
        (EventModel Event, string DescriptionKey)? finishEvent = null,
        string? selectionPrompt = null)
    {
        if (options.Count == 0)
        {
            if (finishEvent is { } emptyFinished)
            {
                FinishEvent(emptyFinished.Event, emptyFinished.DescriptionKey);
            }

            return;
        }

        var chosenCards = (await CardSelectCmd.FromSimpleGridForRewards(
            new BlockingPlayerChoiceContext(),
            options,
            player,
            new CardSelectorPrefs(L10NLookup(finishEvent?.Event, selectionPrompt), minCards, maxCards)
            {
                Cancelable = minCards == 0
            })).ToList();
        foreach (var card in chosenCards)
        {
            await AddCardToDeck(card, previewStyle);
        }

        if (finishEvent is { } finished)
        {
            FinishEvent(finished.Event, finished.DescriptionKey);
        }
    }

    private static async Task AddCardToDeck(CardModel card, CardPreviewStyle previewStyle)
    {
        var result = await CardPileCmd.Add(card, PileType.Deck, CardPilePosition.Bottom, null, false);
        CardCmd.PreviewCardPileAdd(result, 1.2f, previewStyle);
    }

    private static CardModel? PickUnusedCard(
        IReadOnlyList<CardModel> source,
        HashSet<ModelId> usedCardIds,
        MegaCrit.Sts2.Core.Random.Rng rewards)
    {
        var candidates = source.Where(card => !usedCardIds.Contains(card.Id)).ToList();
        if (candidates.Count == 0)
        {
            candidates = source.ToList();
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        var card = rewards.NextItem(candidates);
        if (card is null)
        {
            return null;
        }

        usedCardIds.Add(card.Id);
        return card;
    }

    private static void RecordUnchosenCardOptions(Player player, IReadOnlyList<CardModel> options, CardModel? chosenCard)
    {
        var history = player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId);
        if (history is null)
        {
            return;
        }

        foreach (var option in options.Where(option => option != chosenCard))
        {
            history.CardChoices.Add(new CardChoiceHistoryEntry(option, false));
        }
    }

    public static void FinishEvent(EventModel eventModel, string descriptionKey)
    {
        var description = L10NLookup(eventModel, descriptionKey);
        AccessTools.Method(typeof(EventModel), "SetEventFinished")?.Invoke(eventModel, [description]);
    }

    private static List<CardModel> CreateSanguoshaTransformationOptions()
    {
        var cards = SanguoshaCardCatalog.TryGetCards();
        if (cards is null)
        {
            return [];
        }

        var options = cards
            .Where(card => card is not ShaCard
                && card is not ShanCard
                && card is not ZhangBaShaCard
                && card is not WangJianShaCard)
            .ToList();
        return options.Count > 0 ? options : cards.ToList();
    }

    private static MegaCrit.Sts2.Core.Localization.LocString L10NLookup(EventModel? eventModel, string? key)
    {
        if (eventModel is not null && key is not null)
        {
            return (MegaCrit.Sts2.Core.Localization.LocString)AccessTools
                .Method(typeof(EventModel), "L10NLookup")!
                .Invoke(eventModel, [key])!;
        }

        return CardSelectorPrefs.RemoveSelectionPrompt;
    }
}
