using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 桃园结义 — X费，回复X*3生命，消耗酒意→抽牌+格挡，X=0→格挡+保留。
/// X费, 技能, 稀有, 非消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class TaoYuanCard : ModCardTemplate
{
    public TaoYuanCard()
    {
        Name = "桃园结义";
        Cost = -1; // X-cost
        Type = CardType.Skill;
        Rarity = CardRarity.Rare;
        Exhaust = false;
    }

    public override void OnPlay(AbstractCreature target)
    {
        int x = EnergyOnUse;
        int healAmount = System.Math.Min(x * 3, 5 * 3); // 上限5X

        if (x > 0)
        {
            // 消耗酒意 → 每层抽1+4格挡
            var jiuBuff = Owner.Powers.OfType<JiuBuff>().FirstOrDefault();
            if (jiuBuff != null)
            {
                int jiuStacks = jiuBuff.Amount;
                AbstractDungeon.Player.Draw(jiuStacks);
                BlockCmd.AddBlock(Owner, jiuStacks * 4);
                PowerCmd.RemovePower(Owner, jiuBuff);
            }

            AbstractDungeon.Player.Heal(healAmount);
        }
        else
        {
            // X=0 → 3格挡 + 保留 + 不消耗
            BlockCmd.AddBlock(Owner, 3);
            this.SelfRetain = true;
            this.Exhaust = false;
        }
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("TaoYuanCard");
}
