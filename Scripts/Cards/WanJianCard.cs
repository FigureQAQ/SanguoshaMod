using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 万箭齐发 — AOE伤害，对已有Debuff加成，命中≥2名敌人获得0费杀。
/// 费2, 攻击, 普通, 消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class WanJianCard : ModCardTemplate
{
    public WanJianCard()
    {
        Name = "万箭齐发";
        Cost = 2;
        Type = CardType.Attack;
        Rarity = CardRarity.Common;
        Exhaust = true;
    }

    public override void OnPlay(AbstractCreature target)
    {
        int baseDamage = Upgraded ? 8 : 6;
        int hits = 0;

        foreach (var m in AbstractDungeon.GetMonsters().Monsters.Where(m => !m.IsDead && !m.IsDying))
        {
            int damage = baseDamage;
            // 对已有Debuff的敌人+3伤害
            if (m.Powers.Any(p => p.IsDebuff))
                damage += 3;

            PowerCmd.DealDamage(Owner, m, damage, DamageType.Normal);
            PowerCmd.Apply<VulnerablePower>(Owner, m, 1, null!);
            hits++;
        }

        // 命中≥2名敌人 → 获得1张0费杀
        if (hits >= 2)
        {
            var sha = new ShaCard { Cost = 0 };
            AbstractDungeon.Player.Hand.Add(sha);
        }
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("WanJianCard");
}
