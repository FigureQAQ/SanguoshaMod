using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ChiFengCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new DynamicVar("LowHpDamage", 10m)
    ];

    public ChiFengCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        var damage = player.Creature.CurrentHp * 2 <= player.Creature.MaxHp
            ? DynamicVars["LowHpDamage"].BaseValue
            : DynamicVars.Damage.BaseValue;
        return SanguoshaCardFx.Attack(choiceContext, cardPlay, damage);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["LowHpDamage"].UpgradeValueBy(4);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class XueShouCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
        new DynamicVar("BonusBlock", 3m)
    ];

    public XueShouCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var block = DynamicVars.Block.BaseValue;
        if (SanguoshaCharacterSkills.LostHpThisTurn(cardPlay.Card.Owner))
        {
            block += DynamicVars["BonusBlock"].BaseValue;
        }

        return SanguoshaCardFx.Block(cardPlay, block);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
        DynamicVars["BonusBlock"].UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class DuRenCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new DynamicVar("Poison", 2m)
    ];

    public DuRenCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        await SanguoshaCardFx.Poison(choiceContext, cardPlay, cardPlay.Target!, DynamicVars["Poison"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["Poison"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class QianXingCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];

    public QianXingCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        if (SanguoshaCardFx.AliveEnemies(cardPlay).Any(enemy => enemy.GetPower<PoisonPower>()?.Amount > 0))
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class LeiYinStarterCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("Thunder", 1m)
    ];

    public LeiYinStarterCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        await SanguoshaCardFx.Thunder(choiceContext, cardPlay, DynamicVars["Thunder"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class JiJiaCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
        new DynamicVar("BonusBlock", 3m)
    ];

    public JiJiaCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var block = DynamicVars.Block.BaseValue;
        if (SanguoshaCharacterSkills.HasThunder(cardPlay.Card.Owner))
        {
            block += DynamicVars["BonusBlock"].BaseValue;
        }

        return SanguoshaCardFx.Block(cardPlay, block);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
        DynamicVars["BonusBlock"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class YaoRenCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("BonusDamage", 4m)
    ];

    public YaoRenCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var damage = DynamicVars.Damage.BaseValue;
        if (SanguoshaCharacterSkills.HealedThisTurn(cardPlay.Card.Owner))
        {
            damage += DynamicVars["BonusDamage"].BaseValue;
        }

        return SanguoshaCardFx.Attack(choiceContext, cardPlay, damage);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["BonusDamage"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class HuHunCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new DynamicVar("Heal", 1m)
    ];

    public HuHunCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
        DynamicVars["Heal"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class HaoLingCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("Stars", 1m)
    ];

    public HaoLingCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        await PlayerCmd.GainStars(DynamicVars["Stars"].IntValue, cardPlay.Card.Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ShouHanCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];

    public ShouHanCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        if (SanguoshaCharacterSkills.LastCardType(cardPlay.Card.Owner) is { } lastType
            && lastType != cardPlay.Card.Type)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class QiangXiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new DynamicVar("HpLoss", 1m)
    ];

    public QiangXiCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCharacterSkills.LoseHp(cardPlay.Card.Owner, DynamicVars["HpLoss"].BaseValue, cardPlay.Card);
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class LieGongCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new DynamicVar("BonusDamage", 4m)
    ];

    public LieGongCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var damage = DynamicVars.Damage.BaseValue;
        if (SanguoshaCardFx.IsDebuffed(cardPlay.Target!))
        {
            damage += DynamicVars["BonusDamage"].BaseValue;
        }

        return SanguoshaCardFx.Attack(choiceContext, cardPlay, damage);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["BonusDamage"].UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class TianYiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    public TianYiCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        var discarded = await SanguoshaCardFx.DiscardFromHand(choiceContext, cardPlay, 1);
        if (discarded.Count > 0 && cardPlay.Target!.IsAlive)
        {
            await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class CaoChuanCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7m, ValueProp.Move)];

    public CaoChuanCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        var player = cardPlay.Card.Owner;
        var sha = player.PlayerCombatState!.DiscardPile.Cards.FirstOrDefault(SanguoshaCharacterSkills.IsShaLike);
        if (sha is not null)
        {
            await CardPileCmd.Add([sha], PileType.Hand, CardPilePosition.Top, cardPlay.Card, false);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class YiYiDaiLaoCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];

    public YiYiDaiLaoCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        SanguoshaCharacterSkills.ScheduleNextTurnDraw(cardPlay.Card.Owner, DynamicVars["Draw"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ZhiJiZhiBiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Draw", 1m)];

    public ZhiJiZhiBiCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        await SanguoshaCardFx.DiscardFromHand(choiceContext, cardPlay, 1, minCards: 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class QuHuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move),
        new EnergyVar(1)
    ];

    public QuHuCard() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hasBuff = cardPlay.Target!.Powers.Any(power => power.Type == PowerType.Buff && power.Amount > 0);
        await SanguoshaCardFx.Attack(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        if (hasBuff)
        {
            SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class HuoShaoLianYingCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("BonusDamage", 3m)
    ];

    public HuoShaoLianYingCard() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in SanguoshaCardFx.AliveEnemies(cardPlay))
        {
            var damage = DynamicVars.Damage.BaseValue
                + (SanguoshaCardFx.IsDebuffed(enemy) ? DynamicVars["BonusDamage"].BaseValue : 0m);
            await SanguoshaCardFx.Damage(choiceContext, cardPlay, enemy, damage);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["BonusDamage"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ShuiYanQiJunCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Weak", 1m)];

    public ShuiYanQiJunCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in SanguoshaCardFx.AliveEnemies(cardPlay))
        {
            if (enemy.Block <= 0)
            {
                continue;
            }

            enemy.LoseBlockInternal(enemy.Block);
            await SanguoshaCardFx.Weak(choiceContext, cardPlay, enemy, DynamicVars["Weak"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Weak"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class FangTianCard : SanguoshaCard
{
    public FangTianCard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateFangTian(cardPlay.Card.Owner);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(1);
    }
}
