using System.Collections.Generic;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Characters;

/// <summary>
/// Silent 三国杀Mod覆盖 — "锦囊妙计 + 控场压制"
/// 核心资源：计策 — 层层递进，暗中消耗手牌获取资源
/// HP: 72 · 起始遗物: 锦囊袋 · 起始卡组: 2杀+2闪+1无懈+1过河+1顺手+1决斗
/// </summary>
[RegisterCharacter]
public class Silent : ModCharacterTemplate
{
    public override string Name => "Silent";
    public override int MaxHp => 72;
    public override int BaseMaxCharge => 12;

    public override List<string> StartingDeck => new()
    {
        "sanguosha:ShaCard", "sanguosha:ShaCard",
        "sanguosha:ShanCard", "sanguosha:ShanCard",
        "sanguosha:WuXieCard",
        "sanguosha:GuoHeCard",
        "sanguosha:ShunShouCard",
        "sanguosha:DuelCard"
    };

    public override string StartingRelic => "sanguosha:JinNangPouch";

    public override List<string> GetSkillCards() => new()
    {
        "sanguosha:SilentStrategyPower",
        "sanguosha:SilentAmbushPower"
    };
}

// ═══════════════════ 锦囊叠影（计策+暗渡） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class SilentStrategyPower : ModCardTemplate
{
    public SilentStrategyPower()
    {
        Name = "锦囊叠影"; Cost = 0; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<SilentStrategyEffect>(Owner, Owner, 0, null!);
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("SilentStrategyPower");
}

[RegisterPower]
public class SilentStrategyEffect : ModPowerTemplate
{
    public int StrategyThisTurn;
    public bool ShadowUsed, ChainUsed, ShadeDrawUsed;

    // 计策牌ID
    public static readonly HashSet<string> StrategyIds = new()
    { "GuoHeCard", "ShunShouCard", "WuZhongCard", "WuGuCard",
      "JieDaoCard", "LeBuCard", "TaoYuanCard" };

    public SilentStrategyEffect() { IsDebuff = false; }

    public override void AtStartOfTurn()
    {
        StrategyThisTurn = 0;
        ShadowUsed = false;
        ChainUsed = false;
        ShadeDrawUsed = false;
    }

    /// <summary>计策牌打出时调用（由相应卡牌OnPlay中回调）</summary>
    public static void OnStrategyCard(AbstractCreature owner, string cardId)
    {
        var se = owner.GetPower<SilentStrategyEffect>();
        if (se == null || !StrategyIds.Contains(cardId)) return;

        se.StrategyThisTurn++;

        // 锦囊影：第1张→摸1
        if (se.StrategyThisTurn == 1 && !se.ShadowUsed)
        {
            se.ShadowUsed = true;
            owner.Draw(1);
            se.StrategyThisTurn--; // 锦囊影不占计数
            return;
        }

        // 连环计：第2张→摸1+易伤
        if (se.StrategyThisTurn == 2 && !se.ChainUsed)
        {
            se.ChainUsed = true;
            owner.Draw(1);
            var t = AbstractDungeon.GetRandomMonster();
            if (t != null)
            {
                PowerCmd.Apply<VulnerablePower>(owner, t, 1, owner);
                if (cardId is "GuoHeCard" or "ShunShouCard" or "JieDaoCard")
                    PowerCmd.Apply<VulnerablePower>(owner, t, 1, owner);
            }
        }

        SilentAmbushEffect.OnStrategy(owner);
    }

    /// <summary>暗渡：手牌被消耗→+1格挡；技能牌消耗→摸1</summary>
    public void OnHandExhaust(CardBase card)
    {
        Owner.GainBlock(1);
        if (card.Type == CardType.Skill && !ShadeDrawUsed)
        {
            ShadeDrawUsed = true;
            Owner.Draw(1);
        }
    }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "锦囊叠影", Description = "每回合首张计策→摸1。手牌消耗→+1格挡；技能消耗→摸1。" };
}

// ═══════════════════ 十面埋伏（觉醒+烟幕+谍报） ═══════════════════
[RegisterCard(typeof(CardPoolsColorless))]
public class SilentAmbushPower : ModCardTemplate
{
    public SilentAmbushPower()
    {
        Name = "十面埋伏"; Cost = 2; Type = CardType.Power; Rarity = CardRarity.Starter;
    }
    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<SilentAmbushEffect>(Owner, Owner, 0, null!);
    }
    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("SilentAmbushPower");
}

[RegisterPower]
public class SilentAmbushEffect : ModPowerTemplate
{
    public int TotalStrategy;
    public bool Awakened;
    public bool SmokeUsedThisTurn;
    public int GrowthCount, GrowthLevel;
    public int SmokeBlockBonus, StrangleWeakBonus;

    public SilentAmbushEffect() { IsDebuff = false; }

    public static void OnStrategy(AbstractCreature owner)
    {
        var ambush = owner.GetPower<SilentAmbushEffect>();
        if (ambush == null) return;

        ambush.TotalStrategy++;
        ambush.GrowthCount++;
        ambush.CheckGrowth();
        ambush.CheckAwakening(owner);
    }

    public void CheckAwakening(AbstractCreature owner)
    {
        if (Awakened || TotalStrategy < 6) return;
        Awakened = true;

        foreach (var m in AbstractDungeon.GetMonsters().Monsters)
        {
            if (!m.IsDeadOrEscaped)
            {
                PowerCmd.Apply<WeakPower>(owner, m, 2 + StrangleWeakBonus, owner);
                PowerCmd.Apply<VulnerablePower>(owner, m, 2, owner);
            }
        }
    }

    void CheckGrowth()
    {
        if (GrowthLevel >= 3) return;
        if (GrowthCount >= (GrowthLevel + 1) * 3)
        {
            GrowthLevel++;
            if (GrowthLevel % 2 == 1) SmokeBlockBonus += 2;
            else StrangleWeakBonus += 1;
        }
    }

    // ── 烟幕撤步（险境保底）──
    public override int OnLoseHp(int damageAmount)
    {
        int newHp = Owner.Hp - damageAmount;
        if ((float)newHp / Owner.MaxHp < 0.3f && !SmokeUsedThisTurn)
        {
            SmokeUsedThisTurn = true;
            var skill = Owner.DrawPile.Find(c => c.Type == CardType.Skill);
            if (skill != null)
            {
                skill.Cost = System.Math.Max(0, skill.Cost - 1);
                Owner.DrawPile.MoveTo(Owner.Hand, skill);
            }
            Owner.GainBlock(4 + SmokeBlockBonus);
        }
        return damageAmount;
    }

    public override void AtStartOfTurn() { SmokeUsedThisTurn = false; }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "十面埋伏",
        Description = $"计策累计:{TotalStrategy}/6" + (Awakened ? " [已觉醒]" : "")
            + $" 谍报{GrowthLevel + 1}档: 烟幕+{SmokeBlockBonus} 锁喉+{StrangleWeakBonus}"
    };
}
