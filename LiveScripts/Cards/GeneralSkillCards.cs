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
        new BlockVar(4, ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [] : [CardKeyword.Exhaust];
    public GuanXingCard() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
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
        DynamicVars["Look"].UpgradeValueBy(2);
        DynamicVars["Discard"].UpgradeValueBy(1);
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class KongChengCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Intangible", 1m),
        new BlockVar(8, ValueProp.Move)
    ];
    public KongChengCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        SanguoshaCharacterSkills.ActivateKongCheng(player, DynamicVars["Intangible"].IntValue);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
        DynamicVars.Block.UpgradeValueBy(4);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class LongDanCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(10, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];
    public LongDanCard() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        var converted = await SanguoshaCardFx.ExhaustFromHand(choiceContext, cardPlay, 1, IsLongDanConvertible);
        if (converted.Count == 0)
        {
            await SanguoshaCardFx.AddFreeShaToHand(cardPlay, false);
            await AddFreeShanToHand(cardPlay);
        }
        else if (converted[0] is ShaCard)
        {
            await AddFreeShanToHand(cardPlay);
        }
        else
        {
            await SanguoshaCardFx.AddFreeShaToHand(cardPlay, false);
        }

        if (IsUpgraded)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
    }

    private static bool IsLongDanConvertible(CardModel card)
    {
        return card is ShaCard or ShanCard;
    }

    private static async Task<CardModel?> AddFreeShanToHand(CardPlay cardPlay)
    {
        var result = await CardPileCmd.AddGeneratedCardToCombat(
            ModelDb.Card<ShanCard>(),
            PileType.Hand,
            cardPlay.Card.Owner,
            CardPilePosition.Top);

        if (!result.success || result.cardAdded is null)
        {
            return null;
        }

        result.cardAdded.EnergyCost.SetThisTurn(0, true);
        return result.cardAdded;
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ZhiHengCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("MaxCards", 3m),
        new DynamicVar("DrawBonus", 1m)
    ];

    public ZhiHengCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var exchanged = await SanguoshaCardFx.ExhaustFromHand(
            choiceContext,
            cardPlay,
            DynamicVars["MaxCards"].IntValue);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, exchanged.Count + DynamicVars["DrawBonus"].IntValue);
        SanguoshaCharacterSkills.ActivateZhiHeng(cardPlay.Card.Owner, IsUpgraded);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MaxCards"].UpgradeValueBy(1);
        DynamicVars["DrawBonus"].UpgradeValueBy(1);
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class WuShuangCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Repeats", 1m),
        new DynamicVar("Strength", 1m)
    ];

    public WuShuangCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateWuShuang(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Repeats"].UpgradeValueBy(1);
        DynamicVars["Strength"].UpgradeValueBy(1);
    }
}


