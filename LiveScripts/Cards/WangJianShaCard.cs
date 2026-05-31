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
public sealed class WangJianShaCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(8, ValueProp.Move),
        new DynamicVar("ExhaustBonus", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public WangJianShaCard() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, false)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!SanguoshaCharacterSkills.TrySpendWangJianShaStar(cardPlay.Card.Owner))
        {
            return;
        }

        var exhaustCount = SanguoshaCharacterSkills.GetExhaustPileCount(cardPlay.Card.Owner);
        var damage = DynamicVars.Damage.BaseValue + exhaustCount * DynamicVars["ExhaustBonus"].BaseValue;
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, damage);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}
