using System.Linq;
using STS2RitsuLib.Scaffolding.Content;
using sanguosha.Characters;

namespace sanguosha.Cards;

/// <summary>
/// 借刀杀人 — 指定1名敌人对另一名敌人造成伤害，Debuff种类越多伤害越高。
/// 费1, 技能, 稀有, 消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class JieDaoCard : ModCardTemplate
{
    public JieDaoCard()
    {
        Name = "借刀杀人";
        Cost = 1;
        Type = CardType.Skill;
        Rarity = CardRarity.Rare;
        Exhaust = true;
    }

    public override void OnPlay(AbstractCreature target)
    {
        var monsters = AbstractDungeon.GetMonsters().Monsters
            .Where(m => !m.IsDead && !m.IsDying).ToList();

        if (monsters.Count == 1)
        {
            // 只有一个敌人 → 直接造成伤害
            int baseDamage = 11;
            int debuffCount = target.Powers.Count(p => p.IsDebuff);
            int bonus = System.Math.Min(6, debuffCount * 2);
            PowerCmd.DealDamage(Owner, target, baseDamage + bonus, DamageType.Normal);
        }
        else if (monsters.Count >= 2)
        {
            // 指定target攻击另一个随机敌人
            var others = monsters.Where(m => m != target).ToList();
            var victim = others[AbstractDungeon.CardRng.Random.Next(others.Count)];

            int baseDamage = 11;
            int debuffCount = target.Powers.Count(p => p.IsDebuff);
            int bonus = System.Math.Min(6, debuffCount * 2);

            PowerCmd.DealDamage(target, victim, baseDamage + bonus, DamageType.Normal);
        }
        // 通知Silent计策系统 + Necrobinder役魂(消耗牌)
        SilentStrategyEffect.OnStrategyCard(Owner, "JieDaoCard");
        if (Exhaust) NecrobinderSoulEffect.TryYiHun(Owner);
    }

    public override void OnExhaustedFromHand()
    {
        // Necrobinder役魂通知
        NecrobinderSoulEffect.TryYiHun(Owner);
        
        // 从手牌消耗 → 对所有敌人造成60%伤害
        float ratio = Upgraded ? 0.7f : 0.6f;
        int damage = (int)(11 * ratio);
        foreach (var m in AbstractDungeon.GetMonsters().Monsters.Where(m => !m.IsDead && !m.IsDying))
            PowerCmd.DealDamage(Owner, m, damage, DamageType.Normal);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("JieDaoCard");
}
