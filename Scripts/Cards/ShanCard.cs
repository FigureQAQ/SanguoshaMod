using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.VarImpl;

namespace sanguosha.Cards;

/// <summary>
/// 闪 — 1费 普通技能。9格挡+保留。若被保留或敌攻击→+5格挡，下次攻击+4伤。
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class ShanCard : ModCardTemplate
{
    public ShanCard() : base(1, CardType.Skill, CardRarity.Common, CardTargetType.Self, true)
    {
        SelfRetain = true;
        CanonicalVars = new CanonicalVar[]
        {
            new(BlockVar, ValueProp.Block) { Base = 9, Current = 9, UpgradeDelta = 3 }
        };
    }
    public const int BlockVar = 0;

    public bool WasRetained; // 外部设置

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cp)
    {
        int block = V<int>(BlockVar);
        var target = AbstractDungeon.GetRandomMonster();
        bool enemyAttacking = target != null && target.GetIntentDmg() > 0;
        bool trigger = WasRetained || enemyAttacking;

        if (trigger) block += 5;
        await BlockCmd.AddBlock(ctx, block, Owner).Build();

        if (trigger)
            await PowerCmd.Apply<ShanNextAttackBuff>(ctx, Owner, Upgraded ? 5 : 4, Owner);
    }

    public override void OnUpgrade() => DynamicVars[BlockVar].UpgradeValueBy(3);
}

[RegisterPower]
public class ShanNextAttackBuff : ModPowerTemplate
{
    public ShanNextAttackBuff() { IsDebuff = false; }
    public override void OnAttack(DamageInfo info, int damageAmount, AbstractCreature target)
    {
        if (info.Owner == Owner && target != Owner)
        {
            PowerCmd.DealDamage(Owner, target, Amount, DamageType.Normal);
            PowerCmd.RemovePower(Owner, this);
        }
    }
    public override PowerStrings GetPowerStrings() => new()
    { Name = "闪·反", Description = $"下次攻击额外+{Amount}伤。" };
}
