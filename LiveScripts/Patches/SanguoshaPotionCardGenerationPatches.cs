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

        var catalog = SanguoshaCardCatalog.TryGetCards();
        if (catalog is null || catalog.Count == 0)
        {
            return false;
        }

        var options = type is null
            ? catalog
            : catalog.Where(card => card.Type == type.Value);

        cards = CardFactory
            .GetDistinctForCombat(
                potion.Owner,
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
        if (!TryCreateCombatCards(potion, type, 3, out var cards))
        {
            return;
        }

        var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, cards, potion.Owner, canSkip: true);
        if (selected is null)
        {
            return;
        }

        selected.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, potion.Owner);
    }
}

[HarmonyPatch(typeof(AttackPotion), "OnUse")]
internal static class AttackPotionSanguoshaCardGenerationPatch
{
    private static bool Prefix(AttackPotion __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        __result = SanguoshaPotionCardGeneration.ChooseOneFreeCard(__instance, choiceContext, CardType.Attack);
        return false;
    }
}

[HarmonyPatch(typeof(SkillPotion), "OnUse")]
internal static class SkillPotionSanguoshaCardGenerationPatch
{
    private static bool Prefix(SkillPotion __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        __result = SanguoshaPotionCardGeneration.ChooseOneFreeCard(__instance, choiceContext, CardType.Skill);
        return false;
    }
}

[HarmonyPatch(typeof(PowerPotion), "OnUse")]
internal static class PowerPotionSanguoshaCardGenerationPatch
{
    private static bool Prefix(PowerPotion __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        __result = SanguoshaPotionCardGeneration.ChooseOneFreeCard(__instance, choiceContext, CardType.Power);
        return false;
    }
}

[HarmonyPatch(typeof(ColorlessPotion), "OnUse")]
internal static class ColorlessPotionSanguoshaCardGenerationPatch
{
    private static bool Prefix(ColorlessPotion __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        __result = SanguoshaPotionCardGeneration.ChooseOneFreeCard(__instance, choiceContext, type: null);
        return false;
    }
}

[HarmonyPatch(typeof(CosmicConcoction), "OnUse")]
internal static class CosmicConcoctionSanguoshaCardGenerationPatch
{
    private static bool Prefix(CosmicConcoction __instance, ref Task __result)
    {
        __result = AddUpgradedCards(__instance);
        return false;
    }

    private static async Task AddUpgradedCards(CosmicConcoction potion)
    {
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
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, potion.Owner);
        }
    }
}

[HarmonyPatch(typeof(OrobicAcid), "OnUse")]
internal static class OrobicAcidSanguoshaCardGenerationPatch
{
    private static bool Prefix(OrobicAcid __instance, ref Task __result)
    {
        __result = AddOneOfEachType(__instance);
        return false;
    }

    private static async Task AddOneOfEachType(OrobicAcid potion)
    {
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

        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, potion.Owner);
    }
}
