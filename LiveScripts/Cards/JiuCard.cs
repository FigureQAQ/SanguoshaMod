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
public sealed class JiuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Strength", 1m),
        new DynamicVar("NextShaDamage", 6m),
        new DynamicVar("Draw", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    public JiuCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
        SanguoshaCharacterSkills.EmpowerNextSha(cardPlay.Card.Owner, DynamicVars["NextShaDamage"].IntValue);
        var freeSha = SanguoshaCardFx.MakeHandCardsFree(cardPlay, 1, card => card is ShaCard);
        if (freeSha == 0)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["NextShaDamage"].UpgradeValueBy(3);
        EnergyCost.SetCustomBaseCost(0);
    }
}


