using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 无懈可击 — 保留，打出的下一张主动消耗牌返回手牌(0费+回收锁+虚无)。
/// 费1, 技能, 普通, 保留
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class WuXieCard : ModCardTemplate
{
    public WuXieCard()
    {
        Name = "无懈可击";
        Cost = 1;
        Type = CardType.Skill;
        Rarity = CardRarity.Common;
        SelfRetain = true;
        Exhaust = false;
        BaseBlock = Upgraded ? 15 : 11;
    }

    public override void OnPlay(AbstractCreature target)
    {
        // 获得格挡
        BlockCmd.AddBlock(Owner, BaseBlock);

        // 标记：下一张主动消耗牌 → 返回手牌
        PowerCmd.Apply<WuXieNextExhaustBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("WuXieCard");
}

/// <summary>
/// 无懈可击标记 — 下一张主动消耗牌返回手牌（0费+回收锁+虚无）
/// </summary>
[RegisterPower]
public class WuXieNextExhaustBuff : ModPowerTemplate
{
    public WuXieNextExhaustBuff() { IsDebuff = false; }

    public override void OnExhaust(Card card)
    {
        if (card != null && card.Exhaust)
        {
            card.Exhaust = false;
            card.Cost = 0;
            PowerCmd.Apply<RecycleBuff>(Owner, Owner, 1, null!);
            AbstractDungeon.Player.Hand.Add(card);
            PowerCmd.RemovePower(Owner, this);
        }
    }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "无懈", Description = "下一张主动消耗牌返回手牌(0费+虚无)。" };
}
