using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class BaGuaCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new BlockVar(4, ValueProp.Move)
    ];

    public BaGuaCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateBaGua(cardPlay.Card.Owner, IsUpgraded);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
        DynamicVars.Block.UpgradeValueBy(1);
    }
}
