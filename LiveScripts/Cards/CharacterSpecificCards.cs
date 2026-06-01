using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class CrimsonRaidCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("HpLoss", 3m),
        new EnergyVar(2),
        new DynamicVar("NextShaDamage", 5m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public CrimsonRaidCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCharacterSkills.LoseHp(cardPlay.Card.Owner, DynamicVars["HpLoss"].BaseValue, cardPlay.Card);
        if (!cardPlay.Card.Owner.Creature.IsAlive)
        {
            return;
        }

        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        SanguoshaCharacterSkills.EmpowerNextSha(cardPlay.Card.Owner, DynamicVars["NextShaDamage"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HpLoss"].UpgradeValueBy(-1);
        DynamicVars["NextShaDamage"].UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class VenomAmbushCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Poison", 5m),
        new DynamicVar("Vulnerable", 1m),
        new EnergyVar(1)
    ];

    public VenomAmbushCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        await SanguoshaCardFx.Poison(choiceContext, cardPlay, target, DynamicVars["Poison"].BaseValue);
        await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, target, DynamicVars["Vulnerable"].BaseValue);
        if (target.HasPower<PoisonPower>())
        {
            SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Poison"].UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ThunderRelayCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Thunder", 2m),
        new DamageVar(6, ValueProp.Move),
        new EnergyVar(1)
    ];

    public ThunderRelayCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Thunder(choiceContext, cardPlay, DynamicVars["Thunder"].BaseValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        foreach (var enemy in SanguoshaCardFx.AliveEnemies(cardPlay))
        {
            await SanguoshaCardFx.Damage(choiceContext, cardPlay, enemy, DynamicVars.Damage.BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Thunder"].UpgradeValueBy(1);
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class SoulRansomCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Exhaust", 2m),
        new DynamicVar("Heal", 2m),
        new DynamicVar("HpLoss", 4m)
    ];

    public SoulRansomCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var exhausted = await SanguoshaCardFx.ExhaustFromHand(
            choiceContext,
            cardPlay,
            DynamicVars["Exhaust"].IntValue);
        var heal = exhausted.Count * DynamicVars["Heal"].BaseValue;
        if (heal > 0)
        {
            await SanguoshaCardFx.Heal(cardPlay, heal);
        }

        foreach (var enemy in SanguoshaCardFx.AliveEnemies(cardPlay))
        {
            await SanguoshaCardFx.Damage(choiceContext, cardPlay, enemy, DynamicVars["HpLoss"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(1);
        DynamicVars["HpLoss"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class EdictReserveCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Cards", 2m),
        new DynamicVar("CostReduce", 1m),
        new EnergyVar(1)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain
    ];

    public EdictReserveCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCardFx.ReduceHandCardCosts(
            cardPlay,
            DynamicVars["Cards"].IntValue,
            DynamicVars["CostReduce"].IntValue,
            card => SanguoshaCharacterSkills.IsShaLike(card) || card.Type == CardType.Skill);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Cards"].UpgradeValueBy(1);
    }
}
