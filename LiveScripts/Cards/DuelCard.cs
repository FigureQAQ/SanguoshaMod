using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class DuelCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6, ValueProp.Move),
        new DynamicVar("BaseHits", 2m),
        new DynamicVar("MaxSha", 1m),
        new DynamicVar("Vulnerable", 1m)
    ];
    public DuelCard() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var extraSha = await SanguoshaCardFx.ExhaustFromHand(
            choiceContext,
            cardPlay,
            DynamicVars["MaxSha"].IntValue,
            card => card is ShaCard);
        var hits = DynamicVars["BaseHits"].IntValue + extraSha.Count;
        for (var i = 0; i < hits; i++)
        {
            await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        }

        if (extraSha.Count > 0 && cardPlay.Target is { IsAlive: true } target)
        {
            await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, target, DynamicVars["Vulnerable"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["MaxSha"].UpgradeValueBy(1);
    }
}


