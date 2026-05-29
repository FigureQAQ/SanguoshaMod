using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class CiShaCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7, ValueProp.Move),
        new DynamicVar("BonusDamage", 3m)
    ];

    public CiShaCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var damage = DynamicVars.Damage.BaseValue;
        if (target.Block <= 0)
        {
            damage += DynamicVars["BonusDamage"].BaseValue;
        }

        await SanguoshaCardFx.Attack(choiceContext, cardPlay, damage);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["BonusDamage"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ShouShiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(7, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];

    public ShouShiCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        if (!SanguoshaCharacterSkills.HasPlayedShaThisTurn(cardPlay.Card.Owner))
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class DiaoDuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("MaxCards", 1m),
        new BlockVar(3, ValueProp.Move),
        new EnergyVar(1)
    ];

    public DiaoDuCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var discarded = await SanguoshaCardFx.DiscardFromHand(
            choiceContext,
            cardPlay,
            DynamicVars["MaxCards"].IntValue,
            minCards: 1);
        if (discarded.Count == 0)
        {
            return;
        }

        await SanguoshaCardFx.Draw(choiceContext, cardPlay, discarded.Count);
        if (discarded.Any(SanguoshaCharacterSkills.IsShaLike))
        {
            await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
            if (IsUpgraded)
            {
                SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MaxCards"].UpgradeValueBy(1);
        DynamicVars.Block.UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class RenDeCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Heal", 2m),
        new BlockVar(4, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];

    public RenDeCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var discarded = await SanguoshaCardFx.DiscardFromHand(choiceContext, cardPlay, 1, minCards: 1);
        if (discarded.Count == 0)
        {
            return;
        }

        await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        if (discarded[0].Type == CardType.Skill)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(1);
        DynamicVars.Block.UpgradeValueBy(2);
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class TuXiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m),
        new DynamicVar("StrengthDown", 1m),
        new DynamicVar("Vulnerable", 1m)
    ];

    public TuXiCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        var target = cardPlay.Target!;
        if (target.Powers.Any(power => power is StrengthPower))
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                target,
                -DynamicVars["StrengthDown"].BaseValue,
                cardPlay.Card.Owner.Creature,
                cardPlay.Card,
                false);
        }
        else
        {
            await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, target, DynamicVars["Vulnerable"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class QiXiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Vulnerable", 1m),
        new DynamicVar("Draw", 1m)
    ];

    public QiXiCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var consumed = await SanguoshaCardFx.ExhaustFromHand(
            choiceContext,
            cardPlay,
            1,
            card => card.Type == CardType.Skill,
            IsUpgraded ? 0 : 1);
        if (!IsUpgraded && consumed.Count == 0)
        {
            return;
        }

        var target = cardPlay.Target!;
        if (target.Block > 0)
        {
            target.LoseBlockInternal(target.Block);
        }

        await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, target, DynamicVars["Vulnerable"].BaseValue);
        if (IsUpgraded)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class GuaGuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Heal", 3m),
        new BlockVar(4, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public GuaGuCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cleared = await SanguoshaCardFx.ClearDebuffs(cardPlay.Card.Owner.Creature);
        if (cleared <= 0)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
            return;
        }

        await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue * cleared);
        if (IsUpgraded)
        {
            await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue * cleared);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(1);
    }
}
