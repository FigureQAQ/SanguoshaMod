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
        new DynamicVar("Hits", 3m)
    ];
    public DuelCard() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (var i = 0; i < DynamicVars["Hits"].IntValue; i++)
        {
            await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        }

        var nextSha = cardPlay.Card.Owner.PlayerCombatState!.Hand.Cards.OfType<ShaCard>().FirstOrDefault();
        nextSha?.EnergyCost.SetThisTurn(0, true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["Hits"].UpgradeValueBy(1);
    }
}


