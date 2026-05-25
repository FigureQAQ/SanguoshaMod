using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class JiuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Strength", 2m),
        new DamageVar(4, ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    public JiuCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
        foreach (var enemy in SanguoshaCardFx.AliveEnemies(cardPlay))
        {
            await SanguoshaCardFx.Damage(choiceContext, cardPlay, enemy, DynamicVars.Damage.BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1);
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}


