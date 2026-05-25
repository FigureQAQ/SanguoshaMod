using System.Collections.Generic;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Characters;

/// <summary>
/// Necrobinder 三国杀Mod覆盖 — "消耗过牌 + 借刀杀人"
/// 核心资源：魂 — 消耗/击杀让资源回流，不直接强攻
/// HP: 70 · 起始遗物: 残魂灯 · 起始卡组: 2杀+1闪+1酒+1桃+1借刀+1决斗+1过河
/// </summary>
[RegisterCharacter]
public class Necrobinder : ModCharacterTemplate
{
    public override string Name => "Necrobinder";
    public override int MaxHp => 70;
    public override int BaseMaxCharge => 12;

    public override List<string> StartingDeck => new()
    {
        "sanguosha:ShaCard", "sanguosha:ShaCard",
        "sanguosha:ShanCard",
        "sanguosha:JiuCard",
        "sanguosha:TaoCard",
        "sanguosha:JieDaoCard",
        "sanguosha:DuelCard",
        "sanguosha:GuoHeCard"
    };

    public override string StartingRelic => "sanguosha:CanHunLamp";

    public override List<string> GetSkillCards() => new()
    {
        "sanguosha:NecrobinderSoulPower",
        "sanguosha:NecrobinderRebirthPower"
    };
}

// ═══════════════════ 役骨借尸（魂+役魂+借尸） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class NecrobinderSoulPower : ModCardTemplate
{
    public NecrobinderSoulPower()
    {
        Name = "役骨借尸"; Cost = 0; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        var ns = PowerCmd.Apply<NecrobinderSoulEffect>(Owner, Owner, 0, null!);
        if (ns != null) ns.SoulCount = 2; // 初始2魂
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("NecrobinderSoulPower");
}

[RegisterPower]
public class NecrobinderSoulEffect : ModPowerTemplate
{
    public int SoulCount, MaxSoul = 6, TotalSoulGained;
    public bool YiHunUsedThisTurn, JieShiUsedThisTurn, KillThisTurn;

    public NecrobinderSoulEffect() { IsDebuff = false; }

    public override void AtStartOfTurn()
    {
        YiHunUsedThisTurn = false;
        JieShiUsedThisTurn = false;
        KillThisTurn = false;
    }

    public void GainSoul(int amount)
    {
        SoulCount = System.Math.Min(SoulCount + amount, MaxSoul);
        TotalSoulGained += amount;
        NecrobinderRebirthEffect.OnSoulChanged(Owner, amount);
    }

    public bool SpendSoul(int amount)
    {
        if (SoulCount < amount) return false;
        SoulCount -= amount;
        NecrobinderRebirthEffect.OnSoulChanged(Owner, amount);
        return true;
    }

    /// <summary>役魂：首张消耗牌→耗1魂生成魂影（由卡牌系统回调）</summary>
    public static void TryYiHun(AbstractCreature owner)
    {
        var ns = owner.GetPower<NecrobinderSoulEffect>();
        if (ns == null || ns.YiHunUsedThisTurn || ns.SoulCount < 1) return;

        ns.YiHunUsedThisTurn = true;
        ns.SpendSoul(1);

        // 魂影0费技能牌: 获得4+骨盾格挡，有击杀时摸1
        var soulShadow = CardFactory.MakeCard("sanguosha:SoulShadowCard");
        if (soulShadow != null)
        {
            int boneBonus = owner.GetPower<NecrobinderRebirthEffect>()?.BoneShieldBonus ?? 0;
            soulShadow.Misc = boneBonus;
            owner.Hand.AddCard(soulShadow);
        }
    }

    /// <summary>借尸：借刀杀人击杀→回收消耗牌堆非攻击牌0费</summary>
    public static void TryJieShi(AbstractCreature owner)
    {
        var ns = owner.GetPower<NecrobinderSoulEffect>();
        if (ns == null || ns.JieShiUsedThisTurn) return;

        ns.JieShiUsedThisTurn = true;
        var nonAtk = owner.ExhaustPile.Find(c => c.Type != CardType.Attack);
        if (nonAtk != null)
        {
            nonAtk.Cost = 0;
            owner.ExhaustPile.MoveTo(owner.Hand, nonAtk);
        }
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "役骨借尸",
        Description = $"魂: {SoulCount}/{MaxSoul} | 累计获得: {TotalSoulGained}"
    };
}

// ═══════════════════ 还魂夜契（觉醒+替魂+魂契） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class NecrobinderRebirthPower : ModCardTemplate
{
    public NecrobinderRebirthPower()
    {
        Name = "还魂夜契"; Cost = 2; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<NecrobinderRebirthEffect>(Owner, Owner, 0, null!);
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("NecrobinderRebirthPower");
}

[RegisterPower]
public class NecrobinderRebirthEffect : ModPowerTemplate
{
    public bool Awakened, GuardUsed;
    public int GrowthCount, GrowthLevel;
    public int BoneShieldBonus, SoulHealBonus;
    public int NextGrowthAt = 4;

    public NecrobinderRebirthEffect() { IsDebuff = false; }

    public static void OnSoulChanged(AbstractCreature owner, int change)
    {
        var re = owner.GetPower<NecrobinderRebirthEffect>();
        if (re == null) return;

        re.GrowthCount += System.Math.Abs(change);
        re.CheckGrowth();
        re.CheckAwakening(owner);
    }

    void CheckGrowth()
    {
        if (GrowthLevel >= 3) return;
        if (GrowthCount >= NextGrowthAt)
        {
            GrowthLevel++;
            NextGrowthAt += 4;
            if (GrowthLevel % 2 == 1) BoneShieldBonus++;
            else SoulHealBonus++;
        }
    }

    void CheckAwakening(AbstractCreature owner)
    {
        if (Awakened) return;
        var soul = owner.GetPower<NecrobinderSoulEffect>();
        if (soul == null) return;
        if (soul.TotalSoulGained < 8
            && (float)owner.Hp / owner.MaxHp > 0.3f) return;

        Awakened = true;
        int consumed = System.Math.Max(1, soul.SoulCount);
        soul.SpendSoul(consumed);
        owner.Heal(consumed * (2 + SoulHealBonus));

        // 回收最多2张技能牌
        int count = 0;
        foreach (var c in owner.ExhaustPile)
        {
            if (c.Type == CardType.Skill && count < System.Math.Min(consumed, 2))
            {
                c.Cost = 0;
                owner.ExhaustPile.MoveTo(owner.Hand, c);
                count++;
            }
        }
    }

    // ── 替魂护身（险境保底）──
    public override int OnLoseHp(int damageAmount)
    {
        int newHp = Owner.Hp - damageAmount;
        if ((float)newHp / Owner.MaxHp < 0.3f)
        {
            var soul = Owner.GetPower<NecrobinderSoulEffect>();
            if (soul == null) return damageAmount;

            if (GuardUsed) { Owner.GainBlock(5); return damageAmount; }

            if (soul.SoulCount > 0)
            {
                soul.SpendSoul(1);
                Owner.GainBlock(10 + BoneShieldBonus);
            }
            else
            {
                Owner.GainBlock(6 + BoneShieldBonus);
                soul.GainSoul(1);
            }
            GuardUsed = true;
        }
        return damageAmount;
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "还魂夜契",
        Description = (Awakened ? " [已觉醒]" : "")
            + $"魂契{GrowthLevel + 1}档: 骨盾+{BoneShieldBonus} 引魂+{SoulHealBonus}"
    };
}
