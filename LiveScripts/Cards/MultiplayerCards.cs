using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class HongBaoCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("GiftCostBonus", 1m),
        new DynamicVar("GoldPercent", 5m)
    ];

    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public HongBaoCard() : base(-1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = cardPlay.Card.Owner;
        var teammates = SanguoshaCardFx.AliveCombatPlayers(cardPlay)
            .Where(player => player != owner)
            .ToList();
        if (teammates.Count == 0)
        {
            return;
        }

        var x = Math.Max(0, cardPlay.Card.ResolveEnergyXValue());
        var giftCost = x + DynamicVars["GiftCostBonus"].IntValue;
        foreach (var teammate in teammates)
        {
            var result = await CardPileCmd.AddGeneratedCardToCombat(
                ModelDb.Card<HongBaoGiftCard>(),
                PileType.Hand,
                teammate,
                CardPilePosition.Top);
            if (result.success && result.cardAdded is not null)
            {
                result.cardAdded.EnergyCost.SetThisTurn(giftCost, true);
                result.cardAdded.ExhaustOnNextPlay = true;
            }
        }

        TryDistributeGold(owner, teammates, DynamicVars["GoldPercent"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["GiftCostBonus"].UpgradeValueBy(-1);
    }

    private static void TryDistributeGold(Player owner, IReadOnlyList<Player> teammates, int percent)
    {
        if (teammates.Count == 0 || percent <= 0)
        {
            return;
        }

        var source = GetGoldObject(owner);
        var ownerGold = TryGetGold(source);
        if (source is null || ownerGold <= 0)
        {
            return;
        }

        var amount = Math.Max(1, ownerGold * percent / 100);
        if (!TrySetGold(source, ownerGold - amount))
        {
            return;
        }

        var rng = owner.PlayerRng.Rewards;
        for (var i = 0; i < amount; i++)
        {
            var teammate = rng.NextItem(teammates);
            if (teammate is null)
            {
                continue;
            }

            var target = GetGoldObject(teammate);
            var targetGold = TryGetGold(target);
            if (target is not null && targetGold >= 0)
            {
                TrySetGold(target, targetGold + 1);
            }
        }
    }

    private static object? GetGoldObject(Player player)
    {
        return TryGetGold(player) >= 0
            ? player
            : TryGetGold(player.RunState) >= 0
                ? player.RunState
                : null;
    }

    private static int TryGetGold(object? obj)
    {
        if (obj is null)
        {
            return -1;
        }

        foreach (var name in new[] { "Gold", "gold", "Currency", "Money", "Coins" })
        {
            var type = obj.GetType();
            var property = type.GetProperty(name);
            if (property?.CanRead == true && ToInt(property.GetValue(obj), out var propertyValue))
            {
                return propertyValue;
            }

            var field = type.GetField(name);
            if (field is not null && ToInt(field.GetValue(obj), out var fieldValue))
            {
                return fieldValue;
            }
        }

        return -1;
    }

    private static bool TrySetGold(object obj, int amount)
    {
        foreach (var name in new[] { "Gold", "gold", "Currency", "Money", "Coins" })
        {
            var type = obj.GetType();
            var property = type.GetProperty(name);
            if (property?.CanWrite == true && TryConvert(amount, property.PropertyType, out var propertyValue))
            {
                property.SetValue(obj, propertyValue);
                return true;
            }

            var field = type.GetField(name);
            if (field is not null && TryConvert(amount, field.FieldType, out var fieldValue))
            {
                field.SetValue(obj, fieldValue);
                return true;
            }
        }

        return false;
    }

    private static bool ToInt(object? value, out int result)
    {
        try
        {
            result = value switch
            {
                int i => i,
                long l => checked((int)l),
                uint u => checked((int)u),
                ulong ul => checked((int)ul),
                decimal d => (int)d,
                _ => -1
            };
            return result >= 0;
        }
        catch
        {
            result = -1;
            return false;
        }
    }

    private static bool TryConvert(int amount, Type targetType, out object? value)
    {
        try
        {
            value = targetType == typeof(int) ? amount : Convert.ChangeType(amount, targetType);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class HongBaoGiftCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m),
        new DynamicVar("Heal", 4m),
        new DynamicVar("GoodLuck", 1m),
        new EnergyVar(1)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public HongBaoGiftCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, false)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue);
        SanguoshaCharacterSkills.AddGoodLuck(cardPlay.Card.Owner, DynamicVars["GoodLuck"].IntValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
    }
}
