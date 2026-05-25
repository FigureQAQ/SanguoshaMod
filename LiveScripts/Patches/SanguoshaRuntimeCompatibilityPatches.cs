using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Cards;
using sanguosha.Characters;

namespace sanguosha.Patches;

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
        await SanguoshaCharacterSkills.TryApplyBaGuaDefense(choiceContext, player, amount, props, cardSource);
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
