using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.VarImpl;
using sanguosha.Characters;

namespace sanguosha.Cards;

/// <summary>
/// 酒 — 1费 罕见技能。2层酒意+找攻。被消耗时+1层。消耗。
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class JiuCard : ModCardTemplate
{
    public JiuCard() : base(1, CardType.Skill, CardRarity.Uncommon, CardTargetType.Self, true)
    {
        Exhaust = true;
        CanonicalVars = new CanonicalVar[]
        {
            new(StackVar, ValueProp.Damage) { Base = 2, Current = 2, UpgradeDelta = 1 }
        };
    }
    public const int StackVar = 0;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cp)
    {
        await PowerCmd.Apply<JiuBuff>(ctx, Owner, V<int>(StackVar), Owner);
        // 手牌或弃牌堆有桃→找1张攻击牌
        bool hasPeach = Owner.Hand.Any(c => c is TaoCard)
                     || Owner.DiscardPile.Any(c => c is TaoCard);
        if (hasPeach)
        {
            var atk = Owner.DrawPile.FirstOrDefault(c => c.Type == CardType.Attack)
                   ?? Owner.DiscardPile.FirstOrDefault(c => c.Type == CardType.Attack);
            if (atk != null) PowerCmd.AddCardToHand(Owner, atk);
        }
    }

    // 被消耗触发（由外部系统调用）
    public void OnConsumedFromHand()
    {
        var existing = Owner.GetPower<JiuBuff>();
        int extra = Upgraded ? 2 : 1;
        if (existing != null)
            PowerCmd.IncreasePower(existing, extra);
        else
            PowerCmd.Apply<JiuBuff>(null!, Owner, extra, Owner);
    }

    public override void OnUpgrade() => DynamicVars[StackVar].UpgradeValueBy(1);
}

[RegisterPower]
public class JiuBuff : ModPowerTemplate
{
    public JiuBuff() { IsDebuff = false; CanStack = true; }

    public override void OnAttack(DamageInfo info, int damageAmount, AbstractCreature target)
    {
        if (info.Owner != Owner || target == Owner || Amount <= 0) return;
        int bonus = 4;

        // 桃酒双生：若是Ironclad，酒意伤害翻倍
        if (Owner.GetPower<Characters.IroncladJiuEffect>() != null)
            bonus *= 2;

        if (Amount >= 3 && info.Card is ShaCard) bonus = damageAmount;
        PowerCmd.DealDamage(Owner, target, bonus, DamageType.Normal);
        PowerCmd.ReducePower(Owner, this, 1);
    }

    public override int OnLoseHp(int damageAmount)
    {
        if (damageAmount >= Owner.Hp && Amount > 0)
        {
            int saved = Amount;
            damageAmount = Math.Max(0, Owner.Hp - saved);
            if (damageAmount >= Owner.Hp) damageAmount = Owner.Hp - 1;
            PowerCmd.ReducePower(Owner, this, saved);
        }
        return damageAmount;
    }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "酒意", Description = "攻击+4伤(消耗1层)。≥3层时杀翻倍。致命:1HP/层。" };
}
