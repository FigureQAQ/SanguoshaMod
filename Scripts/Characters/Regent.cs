using System.Collections.Generic;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Characters;

/// <summary>
/// Regent 三国杀Mod覆盖 — "装备共鸣 + 星辰蓄力"
/// 核心资源：星 — 前期布装备，后期铸令爆发
/// HP: 68 · 起始遗物: 星冕诏令 · 起始卡组: 2杀+1闪+1无懈+1酒+1顺手+1青釭+1仁王
/// </summary>
[RegisterCharacter]
public class Regent : ModCharacterTemplate
{
    public override string Name => "Regent";
    public override int MaxHp => 68;
    public override int BaseMaxCharge => 12;

    public override List<string> StartingDeck => new()
    {
        "sanguosha:ShaCard", "sanguosha:ShaCard",
        "sanguosha:ShanCard",
        "sanguosha:WuXieCard",
        "sanguosha:JiuCard",
        "sanguosha:ShunShouCard",
        "sanguosha:QingGangCard",
        "sanguosha:RenWangCard"
    };

    public override string StartingRelic => "sanguosha:XingMianDecree";

    public override List<string> GetSkillCards() => new()
    {
        "sanguosha:RegentStarPower",
        "sanguosha:RegentMandatePower"
    };
}

// ═══════════════════ 铸令换印（星+铸令+换印） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class RegentStarPower : ModCardTemplate
{
    public RegentStarPower()
    {
        Name = "铸令换印"; Cost = 0; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        var rs = PowerCmd.Apply<RegentStarEffect>(Owner, Owner, 0, null!);
        if (rs != null) rs.StarCount = 1; // 初始1星
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("RegentStarPower");
}

[RegisterPower]
public class RegentStarEffect : ModPowerTemplate
{
    public int StarCount, MaxStar = 5;
    public bool ZhuLingUsedThisTurn, HuanYinUsedThisTurn;
    public bool DidNotAttack = true;
    public int ResonanceCount;
    public int ZhuLingAtkBonus, ZhuLingSkdBonus;

    public RegentStarEffect() { IsDebuff = false; }

    public override void AtStartOfTurn()
    {
        ZhuLingUsedThisTurn = false;
        HuanYinUsedThisTurn = false;
        DidNotAttack = true;
    }

    public override void AtEndOfTurn()
    {
        // 未打攻击→+1星
        if (DidNotAttack)
            GainStar(1);
    }

    public void GainStar(int amount)
    {
        StarCount = System.Math.Min(StarCount + amount, MaxStar);
    }

    public bool SpendStar(int amount)
    {
        if (StarCount < amount) return false;
        StarCount -= amount;
        return true;
    }

    /// <summary>铸令：首张攻击/技能牌耗星强化（由卡牌系统回调）</summary>
    public static int ApplyZhuLing(CardBase card, ref int dmg, ref int blk)
    {
        var owner = card.Owner;
        var rs = owner.GetPower<RegentStarEffect>();
        if (rs == null || rs.ZhuLingUsedThisTurn) return 0;
        if (card.Type != CardType.Attack && card.Type != CardType.Skill) return 0;

        rs.ZhuLingUsedThisTurn = true;
        int spent = System.Math.Min(2, rs.StarCount);
        if (spent <= 0) return 0;
        rs.SpendStar(spent);

        if (card.Type == CardType.Attack)
        {
            rs.DidNotAttack = false;
            int extra = spent * (2 + rs.ZhuLingAtkBonus);
            // 目标有Debuff→+1每星
            var t = card.GetTarget();
            if (t != null && t.Powers.Exists(p => p.IsDebuff))
                extra += spent;
            return extra; // caller adds to dmg
        }
        else
        {
            blk += spent * (3 + rs.ZhuLingSkdBonus);
            if (card.Exhaust && rs.StarCount > 0)
                owner.Draw(1);
            return 0;
        }
    }

    /// <summary>换印：顶替装备→奖励</summary>
    public static void OnEquipReplace(CardBase oldEquip)
    {
        var owner = oldEquip.Owner;
        var rs = owner.GetPower<RegentStarEffect>();
        if (rs == null || rs.HuanYinUsedThisTurn) return;

        rs.HuanYinUsedThisTurn = true;
        switch (oldEquip.EquipSlot)
        {
            case EquipSlot.Weapon:
                var atk = owner.Hand.Find(c => c.Type == CardType.Attack);
                if (atk != null) atk.Cost = System.Math.Max(0, atk.Cost - 1);
                break;
            case EquipSlot.Armor:
                owner.GainBlock(8);
                break;
            case EquipSlot.Mount:
                owner.GainEnergy(1);
                break;
        }
    }

    /// <summary>装备共鸣：能力/装备牌打出→+1星</summary>
    public static void OnEquipmentResonance(AbstractCreature owner)
    {
        var rs = owner.GetPower<RegentStarEffect>();
        if (rs == null) return;

        rs.ResonanceCount++;
        rs.GainStar(1);
        RegentMandateEffect.OnResonance(owner);
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "铸令换印",
        Description = $"星: {StarCount}/{MaxStar} | 共鸣: {ResonanceCount}次"
    };
}

// ═══════════════════ 天命归一（觉醒+护驾+政令） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class RegentMandatePower : ModCardTemplate
{
    public RegentMandatePower()
    {
        Name = "天命归一"; Cost = 2; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<RegentMandateEffect>(Owner, Owner, 0, null!);
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("RegentMandatePower");
}

[RegisterPower]
public class RegentMandateEffect : ModPowerTemplate
{
    public bool Awakened, GuardUsed;
    public int DecreeType = -1; // 0=武诏 1=守诏 2=策诏
    public int GrowthLevel;
    public int ZhuLingAtkBonus, ZhuLingSkdBonus;

    public RegentMandateEffect() { IsDebuff = false; }

    public static void OnResonance(AbstractCreature owner)
    {
        var rm = owner.GetPower<RegentMandateEffect>();
        if (rm == null) return;

        var rs = owner.GetPower<RegentStarEffect>();
        if (rs == null) return;

        // 政令熟练：每2次共鸣成长
        if (rs.ResonanceCount % 2 == 0 && rm.GrowthLevel < 3)
        {
            rm.GrowthLevel++;
            if (rm.GrowthLevel % 2 == 1) { rm.ZhuLingAtkBonus++; rs.ZhuLingAtkBonus = rm.ZhuLingAtkBonus; }
            else { rm.ZhuLingSkdBonus++; rs.ZhuLingSkdBonus = rm.ZhuLingSkdBonus; }
        }

        rm.CheckAwakening(owner);
    }

    void CheckAwakening(AbstractCreature owner)
    {
        if (Awakened) return;
        var rs = owner.GetPower<RegentStarEffect>();
        if (rs == null) return;
        if (rs.ResonanceCount < 4
            && !(rs.StarCount >= rs.MaxStar && rs.ResonanceCount >= 2)) return;

        Awakened = true;
        rs.GainStar(5);
        DecreeType = 0; // 默认武诏
    }

    /// <summary>天命归一诏令效果</summary>
    public static int ApplyDecree(CardBase card, bool isDamage)
    {
        var owner = card.Owner;
        var rm = owner.GetPower<RegentMandateEffect>();
        if (rm == null || !rm.Awakened) return 0;

        var rs = owner.GetPower<RegentStarEffect>();
        if (rs == null || rs.StarCount <= 0) return 0;

        switch (rm.DecreeType)
        {
            case 0: // 武诏: 攻击+4
                if (isDamage && card.Type == CardType.Attack)
                { rs.SpendStar(1); return 4 + rm.ZhuLingAtkBonus; }
                break;
            case 1: // 守诏: 技能+6格挡
                if (!isDamage && card.Type == CardType.Skill)
                { rs.SpendStar(1); return 6 + rm.ZhuLingSkdBonus; } // caller adds to block
                break;
            case 2: // 策诏: 消耗→摸1
                if (card.Exhaust)
                { rs.SpendStar(1); owner.Draw(1); }
                break;
        }
        return 0;
    }

    // ── 王命护驾（险境保底）──
    public override int OnLoseHp(int damageAmount)
    {
        int newHp = Owner.Hp - damageAmount;
        if ((float)newHp / Owner.MaxHp < 0.3f)
        {
            var rs = Owner.GetPower<RegentStarEffect>();
            if (rs == null) return damageAmount;

            if (GuardUsed) { Owner.GainBlock(5); return damageAmount; }

            if (rs.StarCount > 0)
            {
                rs.SpendStar(1);
                Owner.GainBlock(10 + rs.ZhuLingSkdBonus);
            }
            else
            {
                Owner.GainBlock(6 + rs.ZhuLingSkdBonus);
                rs.GainStar(1);
            }
            if (rs.ResonanceCount > 0) Owner.GainBlock(2);
            GuardUsed = true;
        }
        return damageAmount;
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "天命归一",
        Description = (Awakened ? $"[已觉醒-{(DecreeType==0?"武诏":DecreeType==1?"守诏":"策诏")}]" : "")
            + $" 政令{GrowthLevel + 1}档: 武备+{ZhuLingAtkBonus} 守备+{ZhuLingSkdBonus}"
    };
}
