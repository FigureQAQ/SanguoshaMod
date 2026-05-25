using System.Collections.Generic;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Characters;

/// <summary>
/// Defect 三国杀Mod覆盖 — "天罚降世 + 装备共鸣"
/// 核心资源：雷引 — 布阵列雷，敌回合前引爆
/// HP: 68 · 起始遗物: 伏雷核心 · 起始卡组: 2杀+1闪+1无懈+1火攻+1闪电+1兵粮+1八卦
/// </summary>
[RegisterCharacter]
public class Defect : ModCharacterTemplate
{
    public override string Name => "Defect";
    public override int MaxHp => 68;
    public override int BaseMaxCharge => 12;

    public override List<string> StartingDeck => new()
    {
        "sanguosha:ShaCard", "sanguosha:ShaCard",
        "sanguosha:ShanCard",
        "sanguosha:WuXieCard",
        "sanguosha:HuoGongCard",
        "sanguosha:ShanDianCard",
        "sanguosha:BingLiangCard",
        "sanguosha:BaGuaCard"
    };

    public override string StartingRelic => "sanguosha:FuLeiCore";

    public override List<string> GetSkillCards() => new()
    {
        "sanguosha:DefectLightningPower",
        "sanguosha:DefectAwakeningPower"
    };
}

// ═══════════════════ 天罚阵列（雷引+导雷+过载） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class DefectLightningPower : ModCardTemplate
{
    public DefectLightningPower()
    {
        Name = "天罚阵列"; Cost = 0; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<DefectLightningEffect>(Owner, Owner, 0, null!);
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("DefectLightningPower");
}

[RegisterPower]
public class DefectLightningEffect : ModPowerTemplate
{
    public int AttacksThisTurn;
    public int Threshold => Upgraded ? 3 : 4;
    public int ChargeCount;
    public bool GuideUsedThisTurn;

    public DefectLightningEffect() { IsDebuff = false; }

    public override void AtStartOfTurn()
    {
        AttacksThisTurn = 0;
        GuideUsedThisTurn = false;
    }

    /// <summary>攻击牌打出自动追踪（OnAfterAttack 无需修改卡牌）</summary>
    public override void OnAfterAttack(AbstractCreature target, int damageDealt)
    {
        AttacksThisTurn++;
        if (AttacksThisTurn >= Threshold)
        {
            AttacksThisTurn = 0;
            float mult = 1f;
            var awake = Owner.GetPower<DefectAwakeningEffect>();
            if (awake?.Awakened == true) mult = 1.5f;
            int dmg = (int)((5 + (awake?.LightningBonusDmg ?? 0)) * mult);
            foreach (var m in AbstractDungeon.GetMonsters().Monsters)
                if (!m.IsDeadOrEscaped)
                    PowerCmd.DealDamage(Owner, m, dmg, DamageType.Normal);
            OnLightningExplosion(Owner, dmg);
        }
    }

    /// <summary>导雷：对怪施Debuff时，免费半额引爆</summary>
    public static void TryGuide(AbstractCreature owner, AbstractCreature target)
    {
        var le = owner.GetPower<DefectLightningEffect>();
        if (le == null || le.GuideUsedThisTurn) return;
        if (!target.HasPower("Lightning")) return;

        le.GuideUsedThisTurn = true;
        var lightning = target.GetPower("Lightning");
        if (lightning == null) return;

        int dmg = lightning.Amount / 2;
        PowerCmd.DealDamage(owner, target, dmg, DamageType.Normal);
        le.OnLightningExplosion(owner, dmg);
    }

    void OnLightningExplosion(AbstractCreature owner, int damage)
    {
        ChargeCount++;
        if (ChargeCount >= 4)
        {
            ChargeCount -= 4;
            var skill = owner.DrawPile.Find(c => c.Type == CardType.Skill);
            if (skill != null)
            {
                if (skill.CardId is "ShanDianCard" or "HuoGongCard" or "BingLiangCard")
                    skill.Cost = System.Math.Max(0, skill.Cost - 1);
                owner.DrawPile.MoveTo(owner.Hand, skill);
            }
        }

        DefectAwakeningEffect.OnLightningDamage(owner, damage);
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "天罚阵列",
        Description = $"每{Threshold}次攻击→雷电AOE。充能:{ChargeCount}/4"
    };
}

// ═══════════════════ 天罚校准（觉醒+静电+阵列熟练） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class DefectAwakeningPower : ModCardTemplate
{
    public DefectAwakeningPower()
    {
        Name = "天罚校准"; Cost = 2; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<DefectAwakeningEffect>(Owner, Owner, 0, null!);
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("DefectAwakeningPower");
}

[RegisterPower]
public class DefectAwakeningEffect : ModPowerTemplate
{
    public int TotalLightningDamage;
    public bool Awakened;
    public bool FireAttackEmpowered;
    public int GrowthCount, GrowthLevel;
    public int LightningBonusDmg, StaticBlockBonus;

    public DefectAwakeningEffect() { IsDebuff = false; }

    public static void OnLightningDamage(AbstractCreature owner, int damage)
    {
        var de = owner.GetPower<DefectAwakeningEffect>();
        if (de == null) return;

        de.TotalLightningDamage += damage;
        de.GrowthCount++;
        de.CheckGrowth();
        de.CheckAwakening(owner);
    }

    void CheckGrowth()
    {
        if (GrowthLevel >= 3) return;
        if (GrowthCount >= (GrowthLevel + 1) * 3)
        {
            GrowthLevel++;
            LightningBonusDmg++;
            StaticBlockBonus++;
        }
    }

    void CheckAwakening(AbstractCreature owner)
    {
        if (Awakened || TotalLightningDamage < 40) return;
        Awakened = true;

        foreach (var m in AbstractDungeon.GetMonsters().Monsters)
            if (!m.IsDeadOrEscaped)
                PowerCmd.Apply<LightningPower>(owner, m, 12 + LightningBonusDmg, owner);

        FireAttackEmpowered = true;
    }

    // ── 静电护罩（险境保底）──
    public override int OnLoseHp(int damageAmount)
    {
        int newHp = Owner.Hp - damageAmount;
        if ((float)newHp / Owner.MaxHp < 0.3f)
        {
            int block = 6 + StaticBlockBonus;
            int lc = 0;
            foreach (var m in AbstractDungeon.GetMonsters().Monsters)
                if (!m.IsDeadOrEscaped && m.HasPower("Lightning")) lc++;
            block += System.Math.Min(lc * 2, 6);
            Owner.GainBlock(block);
        }
        return damageAmount;
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "天罚校准",
        Description = $"雷引累计:{TotalLightningDamage}/40" + (Awakened ? " [已觉醒]" : "")
            + $" 阵列{GrowthLevel + 1}档: 聚雷+{LightningBonusDmg} 稳压+{StaticBlockBonus}"
    };
}
