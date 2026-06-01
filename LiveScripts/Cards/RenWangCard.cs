using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class RenWangCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new DynamicVar("Guard", 6m),
        new DynamicVar("Draw", 0m)
    ];

    public RenWangCard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateRenWang(cardPlay.Card.Owner, IsUpgraded);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(1);
        DynamicVars["Guard"].UpgradeValueBy(4);
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}
