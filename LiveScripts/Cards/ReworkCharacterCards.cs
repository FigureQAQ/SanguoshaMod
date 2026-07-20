using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class PaoXiaoCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("Vulnerable", 1m)
    ];

    public PaoXiaoCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        if (player.Creature.CurrentHp * 2 <= player.Creature.MaxHp && cardPlay.Target!.IsAlive)
        {
            await SanguoshaCardFx.Vulnerable(
                choiceContext,
                cardPlay,
                cardPlay.Target,
                DynamicVars["Vulnerable"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class WuShengCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Exhaust", 1m)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public WuShengCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var exhausted = await SanguoshaCardFx.ExhaustFromHand(
            choiceContext,
            cardPlay,
            DynamicVars["Exhaust"].IntValue,
            card => card.Type != CardType.Attack,
            1);
        if (exhausted.Count == 0)
        {
            return;
        }

        var sha = await SanguoshaCardFx.AddFreeShaToHand(cardPlay, false);
        if (sha is not null && IsUpgraded)
        {
            CardCmd.Upgrade(sha);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class GangLieCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    public GangLieCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateGangLie(cardPlay.Card.Owner, DynamicVars.Energy.IntValue);
        if (IsUpgraded)
        {
            SanguoshaCardFx.GainEnergy(cardPlay, 1);
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class LiJianCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new DynamicVar("Poison", 3m)
    ];

    public LiJianCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        await SanguoshaCardFx.Poison(
            choiceContext,
            cardPlay,
            cardPlay.Target!,
            DynamicVars["Poison"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["Poison"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class QiCeCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Discard", 2m),
        new DynamicVar("Poison", 3m)
    ];

    public QiCeCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var discarded = await SanguoshaCardFx.DiscardFromHand(
            choiceContext,
            cardPlay,
            DynamicVars["Discard"].IntValue);
        if (discarded.Count > 0)
        {
            await SanguoshaCardFx.Poison(
                choiceContext,
                cardPlay,
                cardPlay.Target!,
                discarded.Count * DynamicVars["Poison"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Poison"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class BiYueCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Draw", 2m)];

    public BiYueCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateBiYue(cardPlay.Card.Owner, DynamicVars["Draw"].IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class LeiJiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move)];

    public LeiJiCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        if (cardPlay.Target!.IsAlive && SanguoshaCharacterSkills.ConsumeThunder(cardPlay.Card.Owner, 1) > 0)
        {
            await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class KanPoCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Thunder", 2m),
        new BlockVar(6m, ValueProp.Move)
    ];

    public KanPoCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var consumed = SanguoshaCharacterSkills.ConsumeThunder(
            cardPlay.Card.Owner,
            DynamicVars["Thunder"].IntValue);
        return SanguoshaCardFx.Block(cardPlay, consumed * DynamicVars.Block.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class KuangFengCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("NextShaDamage", 5m)];

    public KuangFengCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateKuangFeng(
            cardPlay.Card.Owner,
            DynamicVars["NextShaDamage"].IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["NextShaDamage"].UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class JiJiuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Heal", 3m),
        new BlockVar(5m, ValueProp.Move)
    ];

    public JiJiuCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(1);
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class HuoMoCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Exhaust", 1m),
        new DynamicVar("Heal", 3m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public HuoMoCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var exhausted = await SanguoshaCardFx.ExhaustFromHand(
            choiceContext,
            cardPlay,
            DynamicVars["Exhaust"].IntValue,
            minCards: 1);
        if (exhausted.Count > 0)
        {
            await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class DuanChangCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Vulnerable", 1m),
        new BlockVar(0m, ValueProp.Move)
    ];

    public DuanChangCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateDuanChang(
            cardPlay.Card.Owner,
            DynamicVars["Vulnerable"].IntValue,
            DynamicVars.Block.IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class JiJiangCard : SanguoshaCard
{
    public JiJiangCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        var sha = player.PlayerCombatState!.DrawPile.Cards.FirstOrDefault(SanguoshaCharacterSkills.IsShaLike)
            ?? player.PlayerCombatState.DiscardPile.Cards.FirstOrDefault(SanguoshaCharacterSkills.IsShaLike);
        if (sha is null)
        {
            return;
        }

        await CardPileCmd.Add([sha], PileType.Hand, CardPilePosition.Top, cardPlay.Card, false);
        sha.EnergyCost.SetThisTurn(0, true);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(sha);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class QianChongCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Draw", 2m),
        new DynamicVar("Stars", 1m)
    ];

    public QianChongCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        if (SanguoshaCharacterSkills.LastCardType(cardPlay.Card.Owner) is { } lastType
            && lastType != cardPlay.Card.Type)
        {
            await PlayerCmd.GainStars(DynamicVars["Stars"].IntValue, cardPlay.Card.Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class SongWeiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Triggers", 1m),
        new DynamicVar("Stars", 2m)
    ];

    public SongWeiCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateSongWei(
            cardPlay.Card.Owner,
            DynamicVars["Triggers"].IntValue,
            DynamicVars["Stars"].IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Triggers"].UpgradeValueBy(1);
    }
}
