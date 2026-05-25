using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 南蛮入侵 — AOE+虚弱，击杀回血，命中≥2名→下张杀0费。
/// 费2, 攻击, 稀有, 消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class NanManCard : ModCardTemplate
{
    public NanManCard()
    {
        Name = "南蛮入侵";
        Cost = 2;
        Type = CardType.Attack;
        Rarity = CardRarity.Rare;
        Exhaust = true;
    }

    public override void OnPlay(AbstractCreature target)
    {
        int baseDamage = Upgraded ? 10 : 7;
        int kills = 0;
        int hits = 0;

        foreach (var m in AbstractDungeon.GetMonsters().Monsters.Where(m => !m.IsDead && !m.IsDying))
        {
            // 对有攻击意图的敌人先上虚弱
            if (m is AbstractMonster mon && mon.GetIntentDmg() > 0)
                PowerCmd.Apply<WeakPower>(Owner, m, 1, null!);

            PowerCmd.DealDamage(Owner, m, baseDamage, DamageType.Normal);
            hits++;

            // 击杀 → 回2血（上限6）
            if (m.IsDead || m.CurrentHealth <= 0)
            {
                kills++;
            }
        }

        int healAmount = System.Math.Min(6, kills * 2);
        if (healAmount > 0)
            AbstractDungeon.Player.Heal(healAmount);

        // 命中≥2名 → 下张杀0费
        if (hits >= 2)
        {
            var sha = AbstractDungeon.Player.Hand
                .FirstOrDefault(c => c is ShaCard);
            if (sha != null)
                sha.Cost = 0;
            else
            {
                var newSha = new ShaCard { Cost = 0 };
                AbstractDungeon.Player.Hand.Add(newSha);
            }
        }
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("NanManCard");
}
