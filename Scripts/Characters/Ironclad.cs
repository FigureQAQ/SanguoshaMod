using System.Collections.Generic;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Characters;

/// <summary>
/// Ironclad 三国杀Mod覆盖 — "杀意纵横 + 桃酒双生"
/// 核心资源：士气（越受伤越强）+ 酒意（输出驱动）
/// HP: 80 · 起始遗物: 烈酒葫芦 · 起始卡组: 3杀+2闪+1酒+1桃+1无中生有
/// </summary>
[RegisterCharacter]
public class Ironclad : ModCharacterTemplate
{
    public override string Name => "Ironclad";
    public override int MaxHp => 80;
    public override int BaseMaxCharge => 12;

    public override List<string> StartingDeck => new()
    {
        "sanguosha:ShaCard", "sanguosha:ShaCard", "sanguosha:ShaCard",
        "sanguosha:ShanCard", "sanguosha:ShanCard",
        "sanguosha:JiuCard",
        "sanguosha:TaoCard",
        "sanguosha:WuZhongCard"
    };

    public override string StartingRelic => "sanguosha:LieJiuGourd";

    public override List<string> GetSkillCards() => new()
    {
        "sanguosha:IroncladMoralePower",
        "sanguosha:IroncladJiuPower",
        "sanguosha:IroncladBerserkPower"
    };
}

// ═══════════════════ 杀意纵横（士气被动） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class IroncladMoralePower : ModCardTemplate
{
    public IroncladMoralePower()
    {
        Name = "杀意纵横"; Cost = 0; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<IroncladMoraleEffect>(Owner, Owner, 0, null!);
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("IroncladMoralePower");
}

[RegisterPower]
public class IroncladMoraleEffect : ModPowerTemplate
{
    public int Morale;
    public float LastHpFraction = 1f;
    public int MaxMorale => Upgraded ? 8 : 6;

    public IroncladMoraleEffect() { IsDebuff = false; }

    public override void AtStartOfTurn()
    {
        // 每失去10%HP → +2士气
        var cur = (float)Owner.Hp / Owner.MaxHp;
        var lost = LastHpFraction - cur;
        if (lost >= 0.1f)
        {
            int gain = (int)(lost * 10f) * 2;
            Morale = System.Math.Min(Morale + gain, MaxMorale);
        }
        LastHpFraction = cur;

        // 同步到觉醒系统
        IroncladBerserkEffect.TryAwaken(Owner, Morale, cur);
    }

    // 士气提供伤害加成
    public override float AtDamageModify(float damage, AbstractCreature target)
        => damage + Morale;

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "杀意纵横",
        Description = $"每层士气+1伤害。当前：{Morale}/{MaxMorale}"
    };
}

// ═══════════════════ 桃酒双生（酒意翻倍） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class IroncladJiuPower : ModCardTemplate
{
    public IroncladJiuPower()
    {
        Name = "桃酒双生"; Cost = 0; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<IroncladJiuEffect>(Owner, Owner, 0, null!);
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("IroncladJiuPower");
}

[RegisterPower]
public class IroncladJiuEffect : ModPowerTemplate
{
    public IroncladJiuEffect() { IsDebuff = false; }

    // 拦截JiuBuff施加→翻倍（在JiuBuff的OnAttack中生效）
    // 注意：实际由JiuBuff检查此Power
    public bool IsJiuDoubled() => true;

    public override PowerStrings GetPowerStrings() => new()
    { Name = "桃酒双生", Description = "酒意效果翻倍。" };
}

// ═══════════════════ 酒神之怒（觉醒+保底+成长） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class IroncladBerserkPower : ModCardTemplate
{
    public IroncladBerserkPower()
    {
        Name = "酒神之怒"; Cost = 2; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<IroncladBerserkEffect>(Owner, Owner, 0, null!);
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("IroncladBerserkPower");
}

[RegisterPower]
public class IroncladBerserkEffect : ModPowerTemplate
{
    public bool Awakened;
    public bool DangerUsedThisCombat;
    public int GrowthCount;
    public int GrowthLevel;           // 0-3, each level = WushenDmg +25% or TiebiBlock +3
    public int WushenDmgBonusPercent; // 武神伤害%
    public int TiebiBlockBonus;       // 铁壁格挡

    // ── 觉醒触发（由 MoraleEffect 每回合调用）──
    public static void TryAwaken(AbstractCreature owner, int morale, float hpFraction)
    {
        var berserk = owner.GetPower<IroncladBerserkEffect>();
        if (berserk == null || berserk.Awakened) return;

        if (hpFraction < 0.4f || morale >= 5)
        {
            berserk.Awakened = true;
            // 全体AOE: 10+士气*2
            int dmg = 10 + morale * 2;
            foreach (var m in AbstractDungeon.GetMonsters().Monsters)
                if (!m.IsDeadOrEscaped)
                    PowerCmd.DealDamage(owner, m, dmg, DamageType.Normal);
        }
    }

    // ── 伤害增益 ──
    public override float AtDamageModify(float damage, AbstractCreature target)
    {
        float mult = Awakened ? 1.5f : (1f + WushenDmgBonusPercent / 100f);
        return damage * mult;
    }

    // ── 背水一战（险境保底）──
    public override int OnLoseHp(int damageAmount)
    {
        int newHp = Owner.Hp - damageAmount;
        float threshold = (float)newHp / Owner.MaxHp;

        if (threshold < 0.3f && !DangerUsedThisCombat)
        {
            DangerUsedThisCombat = true;
            AbstractDungeon.Player.GainEnergy(1);           // +1费
            PowerCmd.Apply<StrengthPower>(Owner, Owner, 2, Owner); // +2力量
            CheckGrowth();
        }
        return damageAmount;
    }

    // ── 磨砺成锋（成长）──
    public void CheckGrowth()
    {
        if (GrowthLevel >= 3) return;
        GrowthCount++;
        if (GrowthCount >= (GrowthLevel + 1) * 3)
        {
            GrowthLevel++;
            if (GrowthLevel % 2 == 1) WushenDmgBonusPercent += 25; // 武神
            else TiebiBlockBonus += 3;                               // 铁壁
        }
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "酒神之怒",
        Description = (Awakened ? "[觉醒] 伤害+50%" : "")
            + $" 磨砺{GrowthLevel}/3档: 武神+{WushenDmgBonusPercent}% 铁壁+{TiebiBlockBonus}"
    };
}
