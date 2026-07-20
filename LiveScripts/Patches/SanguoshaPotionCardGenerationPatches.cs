using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace sanguosha.Patches;

internal static class SanguoshaPotionCardGeneration
{
    public static bool CanCreateCombatCards(PotionModel potion, CardType? type)
    {
        if (potion.Owner is not { } owner)
        {
            return false;
        }

        var rewardCards = SanguoshaCardCatalog.TryGetRewardCards(owner);
        return rewardCards is { Count: > 0 }
            && (type is null || rewardCards.Any(card => card.Type == type.Value));
    }

    public static bool TryCreateCombatCards(
        PotionModel potion,
        CardType? type,
        int count,
        out List<CardModel> cards)
    {
        cards = [];
        if (potion.Owner is not { } owner || owner.RunState is null)
        {
            return false;
        }

        var rewardCards = SanguoshaCardCatalog.TryGetRewardCards(owner);
        if (rewardCards is null || rewardCards.Count == 0)
        {
            return false;
        }

        var options = type is null
            ? rewardCards
            : rewardCards.Where(card => card.Type == type.Value);

        cards = CardFactory
            .GetDistinctForCombat(
                owner,
                options,
                count,
                owner.RunState.Rng.CombatCardGeneration)
            .ToList();

        if (cards.Count == 0)
        {
            Entry.Logger.Warn($"Sanguosha potion card generation found no cards for {potion.Id.Entry}.");
            return false;
        }

        return true;
    }

    public static async Task ChooseOneFreeCard(PotionModel potion, PlayerChoiceContext choiceContext, CardType? type)
    {
        if (potion.Owner is not { } owner)
        {
            return;
        }

        if (!TryCreateCombatCards(potion, type, 3, out var cards))
        {
            return;
        }

        var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, cards, owner, canSkip: true);
        if (selected is null)
        {
            return;
        }

        selected.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, owner);
    }
}

[HarmonyPatch(typeof(AttackPotion), "OnUse")]
internal static class AttackPotionSanguoshaCardGenerationPatch
{
    private static bool Prefix(AttackPotion __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        if (!SanguoshaPotionCardGeneration.CanCreateCombatCards(__instance, CardType.Attack))
        {
            return true;
        }

        __result = SanguoshaPotionCardGeneration.ChooseOneFreeCard(__instance, choiceContext, CardType.Attack);
        return false;
    }
}

[HarmonyPatch(typeof(SkillPotion), "OnUse")]
internal static class SkillPotionSanguoshaCardGenerationPatch
{
    private static bool Prefix(SkillPotion __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        if (!SanguoshaPotionCardGeneration.CanCreateCombatCards(__instance, CardType.Skill))
        {
            return true;
        }

        __result = SanguoshaPotionCardGeneration.ChooseOneFreeCard(__instance, choiceContext, CardType.Skill);
        return false;
    }
}

[HarmonyPatch(typeof(PowerPotion), "OnUse")]
internal static class PowerPotionSanguoshaCardGenerationPatch
{
    private static bool Prefix(PowerPotion __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        if (!SanguoshaPotionCardGeneration.CanCreateCombatCards(__instance, CardType.Power))
        {
            return true;
        }

        __result = SanguoshaPotionCardGeneration.ChooseOneFreeCard(__instance, choiceContext, CardType.Power);
        return false;
    }
}

[HarmonyPatch(typeof(ColorlessPotion), "OnUse")]
internal static class ColorlessPotionSanguoshaCardGenerationPatch
{
    private static bool Prefix(ColorlessPotion __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        if (!SanguoshaPotionCardGeneration.CanCreateCombatCards(__instance, type: null))
        {
            return true;
        }

        __result = SanguoshaPotionCardGeneration.ChooseOneFreeCard(__instance, choiceContext, type: null);
        return false;
    }
}

[HarmonyPatch(typeof(CosmicConcoction), "OnUse")]
internal static class CosmicConcoctionSanguoshaCardGenerationPatch
{
    private static bool Prefix(CosmicConcoction __instance, ref Task __result)
    {
        if (!SanguoshaPotionCardGeneration.CanCreateCombatCards(__instance, type: null))
        {
            return true;
        }

        __result = AddUpgradedCards(__instance);
        return false;
    }

    private static async Task AddUpgradedCards(CosmicConcoction potion)
    {
        if (potion.Owner is not { } owner)
        {
            return;
        }

        if (!SanguoshaPotionCardGeneration.TryCreateCombatCards(
                potion,
                type: null,
                potion.DynamicVars.Cards.IntValue,
                out var cards))
        {
            return;
        }

        foreach (var card in cards)
        {
            CardCmd.Upgrade(card);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        }
    }
}

[HarmonyPatch(typeof(OrobicAcid), "OnUse")]
internal static class OrobicAcidSanguoshaCardGenerationPatch
{
    private static bool Prefix(OrobicAcid __instance, ref Task __result)
    {
        if (new[] { CardType.Attack, CardType.Skill, CardType.Power }
            .Any(type => !SanguoshaPotionCardGeneration.CanCreateCombatCards(__instance, type)))
        {
            return true;
        }

        __result = AddOneOfEachType(__instance);
        return false;
    }

    private static async Task AddOneOfEachType(OrobicAcid potion)
    {
        if (potion.Owner is not { } owner)
        {
            return;
        }

        var cards = new List<CardModel>();
        foreach (var type in new[] { CardType.Attack, CardType.Skill, CardType.Power })
        {
            if (!SanguoshaPotionCardGeneration.TryCreateCombatCards(potion, type, 1, out var typedCards))
            {
                return;
            }

            cards.AddRange(typedCards);
        }

        foreach (var card in cards)
        {
            card.SetToFreeThisTurn();
        }

        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, owner);
    }
}
