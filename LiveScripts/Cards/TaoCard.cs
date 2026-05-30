using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class TaoCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HealVar(6),
        new DynamicVar("LowHpHeal", 3m),
        new BlockVar(2, ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain,
        CardKeyword.Exhaust
    ];
    public TaoCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        var heal = DynamicVars.Heal.BaseValue;
        if (player.Creature.CurrentHp * 2 <= player.Creature.MaxHp)
        {
            heal += DynamicVars["LowHpHeal"].BaseValue;
        }

        await CreatureCmd.Heal(player.Creature, heal, true);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(2);
        DynamicVars["LowHpHeal"].UpgradeValueBy(1);
        DynamicVars.Block.UpgradeValueBy(1);
    }
}


