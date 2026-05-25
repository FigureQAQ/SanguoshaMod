using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Relics;

// ═══════════════════ 烈酒葫芦 (Ironclad) ═══════════════════
/// <summary>
/// 桃酒双生：战斗开始随机插入1-2张酒到抽牌堆。
/// 酒意效果翻倍。
/// </summary>
[RegisterRelic]
public class LieJiuGourdRelic : ModRelicTemplate
{
    public override string Name => "烈酒葫芦";
    public override string Description => "桃酒双生: 战斗开始插入1-2张酒到抽牌堆。酒意效果翻倍。";

    public override void AtBattleStart()
    {
        int count = new System.Random().Next(1, 3); // 1 or 2
        for (int i = 0; i < count; i++)
        {
            var jiu = CardFactory.MakeCard("sanguosha:JiuCard");
            if (jiu != null)
                Owner.DrawPile.AddToRandomPosition(jiu);
        }
    }
}

// ═══════════════════ 锦囊袋 (Silent) ═══════════════════
/// <summary>
/// 锦囊妙计: 每回合首张计策牌+1摸牌。
/// </summary>
[RegisterRelic]
public class JinNangPouchRelic : ModRelicTemplate
{
    public override string Name => "锦囊袋";
    public override string Description => "锦囊妙计: 每回合首张计策牌+1摸牌。包含符策计数。";

    // 实际计策逻辑由 SilentStrategyEffect 处理，此遗物标记角色为计策使用者
}

// ═══════════════════ 伏雷核心 (Defect) ═══════════════════
/// <summary>
/// 天罚降世: 战斗开始对攻击最高敌人施加6伤雷引。
/// </summary>
[RegisterRelic]
public class FuLeiCoreRelic : ModRelicTemplate
{
    public override string Name => "伏雷核心";
    public override string Description => "天罚降世: 战斗开始对攻击最高敌人施加6伤雷引。能力牌→装备共鸣。";

    public override void AtBattleStart()
    {
        // 对攻击意图最高的敌人施加6伤害雷引
        AbstractCreature highest = null;
        int highestDmg = 0;
        foreach (var m in AbstractDungeon.GetMonsters().Monsters)
        {
            if (!m.IsDeadOrEscaped && m.GetIntentDamage() > highestDmg)
            {
                highestDmg = m.GetIntentDamage();
                highest = m;
            }
        }
        if (highest != null)
            PowerCmd.Apply<LightningPower>(Owner, highest, 6, Owner);
    }
}

// ═══════════════════ 残魂灯 (Necrobinder) ═══════════════════
/// <summary>
/// 消耗过牌: 战斗开始+2魂。敌人死亡+1魂。
/// </summary>
[RegisterRelic]
public class CanHunLampRelic : ModRelicTemplate
{
    public override string Name => "残魂灯";
    public override string Description => "消耗过牌: 战斗开始+2魂。魂上限6。";

    public override void AtBattleStart()
    {
        // 魂初始化由 NecrobinderSoulPower.OnPlay 处理
    }
}

// ═══════════════════ 星冕诏令 (Regent) ═══════════════════
/// <summary>
/// 星辰蓄力: 战斗开始+1星。装备共鸣+1星。回合未攻击+1星。
/// </summary>
[RegisterRelic]
public class XingMianDecreeRelic : ModRelicTemplate
{
    public override string Name => "星冕诏令";
    public override string Description => "星辰蓄力: 战斗开始+1星。星上限5。";

    public override void AtBattleStart()
    {
        // 星初始化由 RegentStarPower.OnPlay 处理
    }
}
