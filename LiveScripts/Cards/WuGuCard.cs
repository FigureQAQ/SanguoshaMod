using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class WuGuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8, ValueProp.Move),
        new EnergyVar(1),
        new DynamicVar("Draw", 2m),
        new DynamicVar("BonusDraw", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    public WuGuCard() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(cardPlay.Card.Owner.Creature, DynamicVars.Block, cardPlay, false);

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
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}


