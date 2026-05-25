using System.Linq;
using STS2RitsuLib.Scaffolding.Content;
using sanguosha.Characters;

namespace sanguosha.Cards;

/// <summary>
/// 无中生有 — 从虚空中创造卡牌。消耗事件加成，抽到的技能减费。
/// 费0, 技能, 罕见, 消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class WuZhongCard : ModCardTemplate
{
    public WuZhongCard()
    {
        Name = "无中生有";
        Cost = 0;
        Type = CardType.Skill;
        Rarity = CardRarity.Uncommon;
        Exhaust = true;
    }

    public override void OnPlay(AbstractCreature target)
    {
        int drawCount = Upgraded ? 3 : 2;

        // 本回合已有牌被消耗 → +1抽
        if (AbstractDungeon.ActionManager.CardsExhaustedThisTurn.Count > 0)
            drawCount++;

        AbstractDungeon.Player.Draw(drawCount);

        // 抽到的第一张技能牌费用-1（升级-2）
        // 简化：对抽到的技能牌集合做处理
        var drawn = AbstractDungeon.Player.Hand.Group
            .Take(drawCount)
            .Where(c => c.Type == CardType.Skill);
        int discount = Upgraded ? 2 : 1;
        foreach (var card in drawn)
            card.Cost = System.Math.Max(0, card.Cost - discount);

        // 通知Silent计策系统 + Necrobinder役魂(消耗牌)
        SilentStrategyEffect.OnStrategyCard(Owner, "WuZhongCard");
        if (Exhaust) NecrobinderSoulEffect.TryYiHun(Owner);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("WuZhongCard");
}
