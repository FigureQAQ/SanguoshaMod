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
public sealed class RenWangCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8, ValueProp.Move),
        new DynamicVar("Guard", 6m),
        new DynamicVar("Draw", 0m)
    ];

    public RenWangCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateRenWang(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
        DynamicVars["Guard"].UpgradeValueBy(4);
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}
