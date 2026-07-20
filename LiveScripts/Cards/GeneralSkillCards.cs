using MegaCrit.Sts2.Core.CardSelection;
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
public sealed class GuanXingCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Look", 4m),
        new DynamicVar("Discard", 2m),
        new BlockVar(2, ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    public GuanXingCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);

        var topCards = player.PlayerCombatState!.DrawPile.Cards
            .Take(DynamicVars["Look"].IntValue)
            .ToList();
        if (topCards.Count > 0)
        {
            var maxDiscard = Math.Min(DynamicVars["Discard"].IntValue, topCards.Count);
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, maxDiscard)
            {
                Cancelable = true,
                RequireManualConfirmation = true
            };

            var discarded = (await CardSelectCmd.FromSimpleGrid(choiceContext, topCards, player, prefs)).ToList();
            if (discarded.Count > 0)
            {
                await CardPileCmd.Add(discarded, PileType.Discard, CardPilePosition.Top, cardPlay.Card, false);
            }
        }

    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
        DynamicVars["Look"].UpgradeValueBy(2);
        DynamicVars["Discard"].UpgradeValueBy(1);
        DynamicVars.Block.UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class KongChengCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Intangible", 1m),
        new EnergyVar(1)
    ];
    public KongChengCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        SanguoshaCharacterSkills.ActivateKongCheng(player, DynamicVars["Intangible"].IntValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class LongDanCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(5, ValueProp.Move),
        new DynamicVar("Choices", 2m),
        new DynamicVar("NextShaDamage", 2m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    public LongDanCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        await SanguoshaCardFx.DiscoverToHand(
            choiceContext,
            cardPlay,
            [ModelDb.Card<ShaCard>(), ModelDb.Card<ShanCard>()],
            makeFree: true,
            exhaustOnPlay: true);
        if (SanguoshaCharacterSkills.HasPlayedShaThisTurn(cardPlay.Card.Owner))
        {
            SanguoshaCharacterSkills.EmpowerNextSha(cardPlay.Card.Owner, DynamicVars["NextShaDamage"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars["NextShaDamage"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ZhiHengCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("MaxCards", 2m),
        new EnergyVar(1)
    ];

    public ZhiHengCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var exchanged = await SanguoshaCardFx.ExhaustFromHand(
            choiceContext,
            cardPlay,
            DynamicVars["MaxCards"].IntValue);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, exchanged.Count);
        if (exchanged.Count > 0)
        {
            SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        }
        SanguoshaCharacterSkills.ActivateZhiHeng(cardPlay.Card.Owner, IsUpgraded);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MaxCards"].UpgradeValueBy(1);
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class WuShuangCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Repeats", 2m),
        new DynamicVar("Strength", 1m)
    ];

    public WuShuangCard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateWuShuang(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(1);
        DynamicVars["Repeats"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class LianYingCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m),
        new DynamicVar("Triggers", 1m),
        new EnergyVar(1)
    ];

    public LianYingCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateLianYing(
            cardPlay.Card.Owner,
            DynamicVars["Draw"].IntValue,
            DynamicVars["Triggers"].IntValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Triggers"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class YiJiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m),
        new DynamicVar("LowHandDraw", 1m),
        new DynamicVar("FreeCards", 1m),
        new EnergyVar(1)
    ];

    public YiJiCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateYiJi(
            cardPlay.Card.Owner,
            DynamicVars["Draw"].IntValue,
            DynamicVars["LowHandDraw"].IntValue,
            DynamicVars["FreeCards"].IntValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class JianXiongCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m),
        new DynamicVar("NextShaDamage", 3m),
        new EnergyVar(1)
    ];

    public JianXiongCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateJianXiong(
            cardPlay.Card.Owner,
            DynamicVars["Draw"].IntValue,
            DynamicVars["NextShaDamage"].IntValue,
            IsUpgraded ? DynamicVars.Energy.IntValue : 0);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["NextShaDamage"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class GuiCaiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(3, ValueProp.Move),
        new DynamicVar("Draw", 1m),
        new DynamicVar("Weak", 1m),
        new EnergyVar(1)
    ];

    public GuiCaiCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateGuiCai(
            cardPlay.Card.Owner,
            DynamicVars.Block.IntValue,
            DynamicVars["Draw"].IntValue,
            DynamicVars["Weak"].IntValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Weak"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class YingZiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m),
        new EnergyVar(1)
    ];

    public YingZiCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateYingZi(cardPlay.Card.Owner, DynamicVars["Draw"].IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class JiZhiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m)
    ];

    public JiZhiCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateJiZhi(cardPlay.Card.Owner, DynamicVars["Draw"].IntValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class LuoYiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("NextShaDamage", 2m)
    ];

    public LuoYiCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateLuoYi(cardPlay.Card.Owner, DynamicVars["NextShaDamage"].IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["NextShaDamage"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class TieQiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Vulnerable", 1m)
    ];

    public TieQiCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateTieQi(cardPlay.Card.Owner, DynamicVars["Vulnerable"].IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Vulnerable"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class QingNangCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Heal", 2m)
    ];

    public QingNangCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateQingNang(cardPlay.Card.Owner, DynamicVars["Heal"].IntValue);
        await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class XiaoJiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m),
        new EnergyVar(1)
    ];

    public XiaoJiCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateXiaoJi(cardPlay.Card.Owner, DynamicVars["Draw"].IntValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
    }
}

