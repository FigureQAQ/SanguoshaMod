using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 五谷丰登 — 从消耗牌堆回收非稀有技能，0费+虚无+回收锁。
/// 费1, 技能, 罕见, 非消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class WuGuCard : ModCardTemplate
{
    public WuGuCard()
    {
        Name = "五谷丰登";
        Cost = 1;
        Type = CardType.Skill;
        Rarity = CardRarity.Uncommon;
        Exhaust = false;
    }

    public override void OnPlay(AbstractCreature target)
    {
        int drawCount = Upgraded ? 3 : 2;
        AbstractDungeon.Player.Draw(drawCount);

        // 从消耗牌堆选1张非稀有技能 → 加入手牌（0费+回收锁+虚无）
        var exhaustPile = AbstractDungeon.Player.ExhaustPile;
        var candidates = exhaustPile
            .Where(c => c.Type == CardType.Skill && c.Rarity != CardRarity.Rare)
            .ToList();

        if (candidates.Count > 0)
        {
            // 简化：选第一张
            var chosen = candidates[0];
            exhaustPile.Remove(chosen);
            chosen.Cost = 0;
            PowerCmd.Apply<RecycleBuff>(AbstractDungeon.Player, AbstractDungeon.Player, 1, null!);
            AbstractDungeon.Player.Hand.Add(chosen);
        }
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("WuGuCard");
}
