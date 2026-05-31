using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class HongBaoCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m),
        new DynamicVar("Heal", 3m),
        new DynamicVar("GoodLuck", 1m),
        new EnergyVar(1)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public HongBaoCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = cardPlay.Card.Owner;
        foreach (var player in SanguoshaCardFx.AliveCombatPlayers(cardPlay).Where(player => player != owner))
        {
            await SanguoshaCardFx.Draw(choiceContext, player, DynamicVars["Draw"].IntValue);
            await SanguoshaCardFx.Heal(player, DynamicVars["Heal"].BaseValue);
            SanguoshaCharacterSkills.AddGoodLuck(player, DynamicVars["GoodLuck"].IntValue);
        }

        owner.PlayerCombatState!.GainEnergy(DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}
