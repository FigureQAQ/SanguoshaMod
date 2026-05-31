using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class CiShaCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7, ValueProp.Move),
        new DynamicVar("BonusDamage", 3m)
    ];

    public CiShaCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var damage = DynamicVars.Damage.BaseValue;
        if (SanguoshaCardFx.IsDebuffed(target))
        {
            damage += DynamicVars["BonusDamage"].BaseValue;
        }

        await SanguoshaCardFx.Attack(choiceContext, cardPlay, damage);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["BonusDamage"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ShouShiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(5, ValueProp.Move),
        new DynamicVar("BlockGrowth", 5m)
    ];

    public ShouShiCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        if (!SanguoshaCharacterSkills.HasPlayedAttackThisTurn(cardPlay.Card.Owner))
        {
            DynamicVars.Block.BaseValue += DynamicVars["BlockGrowth"].BaseValue;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class FenChengCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("DamagePerX", 2m)
    ];

    protected override bool HasEnergyCostX => true;

    public FenChengCard() : base(-1, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var x = Math.Max(0, cardPlay.Card.ResolveEnergyXValue());
        var damage = x * DynamicVars["DamagePerX"].BaseValue;
        if (damage <= 0)
        {
            return;
        }

        var hand = cardPlay.Card.Owner.PlayerCombatState!.Hand.Cards
            .Where(card => card != cardPlay.Card)
            .ToList();
        foreach (var card in hand)
        {
            await CardCmd.Exhaust(choiceContext, card, false, false);
            await SanguoshaCharacterSkills.OnCardExhaustedFromHand(cardPlay.Card.Owner, card, cardPlay.Card);
        }

        foreach (var _ in hand)
        {
            await SanguoshaCardFx.AttackAll(choiceContext, cardPlay, damage);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DamagePerX"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class DiaoDuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m),
        new DynamicVar("Discard", 1m)
    ];

    public DiaoDuCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = DynamicVars["Draw"].IntValue;
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, amount);
        await SanguoshaCardFx.DiscardFromHand(
            choiceContext,
            cardPlay,
            Math.Min(DynamicVars["Discard"].IntValue, cardPlay.Card.Owner.PlayerCombatState!.Hand.Cards.Count),
            minCards: Math.Min(DynamicVars["Discard"].IntValue, cardPlay.Card.Owner.PlayerCombatState!.Hand.Cards.Count));
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
        DynamicVars["Discard"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class YangGongCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];

    public YangGongCard() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        if (SanguoshaCardFx.IsDebuffed(target))
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class XuShiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("NextShaDamage", 3m),
        new DynamicVar("Draw", 1m)
    ];

    public XuShiCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        var hasSha = player.PlayerCombatState!.Hand.Cards
            .Any(card => card != cardPlay.Card && SanguoshaCharacterSkills.IsShaLike(card));
        SanguoshaCharacterSkills.EmpowerNextSha(player, DynamicVars["NextShaDamage"].IntValue);
        if (!hasSha)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["NextShaDamage"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class PoZhenCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Vulnerable", 1m),
        new DynamicVar("Draw", 1m)
    ];

    public PoZhenCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var brokeBlock = target.Block > 0;
        if (brokeBlock)
        {
            target.LoseBlockInternal(target.Block);
        }

        await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, target, DynamicVars["Vulnerable"].BaseValue);
        if (brokeBlock)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Vulnerable"].UpgradeValueBy(1);
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class BuFangCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(7, ValueProp.Move),
        new DynamicVar("CostReduce", 1m)
    ];

    public BuFangCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        if (!SanguoshaCharacterSkills.HasPlayedAttackThisTurn(cardPlay.Card.Owner))
        {
            SanguoshaCardFx.ReduceHandCardCosts(
                cardPlay,
                1,
                DynamicVars["CostReduce"].IntValue,
                card => SanguoshaCharacterSkills.IsShaLike(card) || card.Type == CardType.Skill);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class RenDeCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Heal", 2m),
        new EnergyVar(1),
        new DynamicVar("NextShaDamage", 3m)
    ];

    public RenDeCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var discarded = await SanguoshaCardFx.DiscardFromHand(choiceContext, cardPlay, 1, minCards: 1);
        if (discarded.Count == 0)
        {
            return;
        }

        var card = discarded[0];
        await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        if (SanguoshaCharacterSkills.IsShaLike(card))
        {
            SanguoshaCharacterSkills.EmpowerNextSha(cardPlay.Card.Owner, DynamicVars["NextShaDamage"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(1);
        DynamicVars["NextShaDamage"].UpgradeValueBy(1);
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class TuXiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Draw", 1m),
        new DynamicVar("Vulnerable", 1m)
    ];

    public TuXiCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target.Block > 0)
        {
            target.LoseBlockInternal(target.Block);
        }

        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, target, DynamicVars["Vulnerable"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Vulnerable"].UpgradeValueBy(1);
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class QiXiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Vulnerable", 1m),
        new DynamicVar("Draw", 1m)
    ];

    public QiXiCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var consumed = await SanguoshaCardFx.ExhaustFromHand(
            choiceContext,
            cardPlay,
            1,
            card => card.Type == CardType.Skill,
            IsUpgraded ? 0 : 1);
        if (!IsUpgraded && consumed.Count == 0)
        {
            return;
        }

        var target = cardPlay.Target!;
        if (target.Block > 0)
        {
            target.LoseBlockInternal(target.Block);
        }

        await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, target, DynamicVars["Vulnerable"].BaseValue);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Vulnerable"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class GuaGuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Heal", 3m),
        new BlockVar(3, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public GuaGuCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cleared = await SanguoshaCardFx.ClearDebuffs(cardPlay.Card.Owner.Creature);
        if (cleared <= 0)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
            return;
        }

        await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue * cleared);
        if (IsUpgraded)
        {
            await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue * cleared);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
        DynamicVars["Heal"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class KuRouCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("HpLoss", 3m),
        new EnergyVar(2)
    ];

    public KuRouCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCharacterSkills.LoseHp(cardPlay.Card.Owner, DynamicVars["HpLoss"].BaseValue, cardPlay.Card);
        if (cardPlay.Card.Owner.Creature.IsAlive)
        {
            SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HpLoss"].UpgradeValueBy(-1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class JiXingCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(2),
        new DynamicVar("Draw", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public JiXingCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        if (IsUpgraded)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class FenYingCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(4, ValueProp.Move),
        new DynamicVar("ExhaustBlock", 3m)
    ];

    public FenYingCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateFenYing(cardPlay.Card.Owner, DynamicVars["ExhaustBlock"].IntValue);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class PoZhuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Vulnerable", 2m),
        new DynamicVar("Strength", 1m)
    ];

    public PoZhuCard() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, cardPlay.Target!, DynamicVars["Vulnerable"].BaseValue);
        SanguoshaCharacterSkills.ActivatePoZhu(cardPlay.Card.Owner, DynamicVars["Strength"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1);
    }
}
