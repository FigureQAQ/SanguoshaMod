using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 火攻 — Debuff种类越多伤害越高的攻击牌。
/// 费1, 攻击, 罕见, 消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class HuoGongCard : ModCardTemplate
{
    public HuoGongCard()
    {
        Name = "火攻";
        Cost = 1;
        Type = CardType.Attack;
        Rarity = CardRarity.Uncommon;
        Exhaust = true;
    }

    public override void OnPlay(AbstractCreature target)
    {
        int baseDamage = Upgraded ? 10 : 7;
        // 每种Debuff +3伤害（上限+9）
        int debuffKinds = target.Powers.Where(p => p.IsDebuff)
            .Select(p => p.GetType()).Distinct().Count();
        int bonus = System.Math.Min(9, debuffKinds * 3);

        // 没有Debuff → 施加易伤
        if (debuffKinds == 0)
            PowerCmd.Apply<VulnerablePower>(Owner, target, 1, null!);

        // 有雷引 → 先触发半额
        var leYin = target.Powers.OfType<LeYinBuff>().FirstOrDefault();
        if (leYin != null && !leYin.HalfTriggered)
        {
            leYin.HalfTriggered = true;
            PowerCmd.DealDamage(Owner, target, leYin.Amount / 2, DamageType.Lightning);
        }

        PowerCmd.DealDamage(Owner, target, baseDamage + bonus, DamageType.Normal);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("HuoGongCard");
}
