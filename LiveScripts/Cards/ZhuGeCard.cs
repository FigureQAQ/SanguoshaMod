using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ZhuGeCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("FreeSha", 1m),
        new DynamicVar("Strength", 1m)
    ];
    public ZhuGeCard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int freeSha = (int)DynamicVars["FreeSha"].BaseValue;
        SanguoshaCharacterSkills.ActivateZhuGe(cardPlay.Card.Owner, freeSha);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
        await SanguoshaCardFx.AddFreeShaToHand(cardPlay, false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["FreeSha"].UpgradeValueBy(1);
        EnergyCost.SetCustomBaseCost(1);
    }
}


