using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class WuZhongCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new DynamicVar("Draw", 2m),
        new DynamicVar("BonusDraw", 2m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [] : [CardKeyword.Exhaust];
    public WuZhongCard() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var consumed = await SanguoshaCardFx.ExhaustFromHand(choiceContext, cardPlay, 1);
        if (consumed.Count > 0)
        {
            cardPlay.Card.Owner.PlayerCombatState!.GainEnergy(DynamicVars.Energy.IntValue);
        }

        var drawCount = DynamicVars["Draw"].IntValue + (consumed.Count > 0 ? DynamicVars["BonusDraw"].IntValue : 0);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, drawCount);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}


