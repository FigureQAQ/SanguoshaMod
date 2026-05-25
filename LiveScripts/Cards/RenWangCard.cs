using MegaCrit.Sts2.Core.Commands;
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
        new DynamicVar("Dexterity", 2m),
        new EnergyVar(1)
    ];

    public RenWangCard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateRenWang(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Dexterity(choiceContext, cardPlay, DynamicVars["Dexterity"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Dexterity"].UpgradeValueBy(1);
    }
}
