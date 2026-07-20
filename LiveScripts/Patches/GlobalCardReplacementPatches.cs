using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace sanguosha.Patches;

[HarmonyPatch(typeof(CardCreationOptions), nameof(CardCreationOptions.GetPossibleCards))]
internal static class CardCreationOptionsGetPossibleCardsPatch
{
    private static void Postfix(CardCreationOptions __instance, Player player, ref IEnumerable<CardModel> __result)
    {
        __result = SanguoshaCardCatalog.ReplaceGeneratedCards(__result, player, __instance);
    }
}

[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards))]
internal static class CardPoolModelGetUnlockedCardsPatch
{
    private static void Postfix(ref IEnumerable<CardModel> __result)
    {
        __result = SanguoshaCardCatalog.ReplaceAllCards(__result);
    }
}

[HarmonyPatch]
internal static class MerchantSanguoshaBasicCardFilterPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var createForMerchant = nameof(CardFactory.CreateForMerchant);
        return [
            AccessTools.Method(
                typeof(CardFactory),
                createForMerchant,
                [typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType)]),
            AccessTools.Method(
                typeof(CardFactory),
                createForMerchant,
                [typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardRarity)])
        ];
    }

    private static void Prefix(Player player, ref IEnumerable<CardModel> options)
    {
        options = SanguoshaCardCatalog.KeepRewardEligibleCards(player, options.ToList());
    }
}

[HarmonyPatch]
internal static class CharacterStartingDeckPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(CharacterModel).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(CharacterModel).IsAssignableFrom(type))
            .Select(type => AccessTools.PropertyGetter(type, nameof(CharacterModel.StartingDeck)))
            .Where(method => method is not null)!;
    }

    private static void Postfix(CharacterModel __instance, ref IEnumerable<CardModel> __result)
    {
        __result = SanguoshaCardCatalog.ReplaceStartingDeck(__result, __instance.GetType().Name);
    }
}
