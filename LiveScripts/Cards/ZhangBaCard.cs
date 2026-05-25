using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ZhangBaCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m)
    ];
    public ZhangBaCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateZhangBa(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCharacterSkills.EnsureZhangBaSha(cardPlay.Card.Owner);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        await SanguoshaCharacterSkills.EnsureZhangBaSha(cardPlay.Card.Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ZhangBaShaCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(9, ValueProp.Move),
        new DynamicVar("UpgradeDamage", 3m),
        new DynamicVar("Draw", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public ZhangBaShaCard() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, false)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await SanguoshaCardFx.ExhaustFromHand(
            choiceContext,
            cardPlay,
            maxCards: 2,
            minCards: 2);
        if (selected.Count < 2)
        {
            return;
        }

        var upgraded = SanguoshaCharacterSkills.IsZhangBaUpgraded(cardPlay.Card.Owner);
        var damage = DynamicVars.Damage.BaseValue + (upgraded ? DynamicVars["UpgradeDamage"].BaseValue : 0);
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, damage);
        if (upgraded)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
    }
}


