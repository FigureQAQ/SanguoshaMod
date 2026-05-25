using System.Linq;
using STS2RitsuLib.Scaffolding.Content;
using sanguosha.Characters;

namespace sanguosha.Cards;

/// <summary>
/// 顺手牵羊 — 偷取格挡(≤8)，无格挡保底，打过过河拆桥→0费。
/// 费1, 技能, 普通, 消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class ShunShouCard : ModCardTemplate
{
    public ShunShouCard()
    {
        Name = "顺手牵羊";
        Cost = 1;
        Type = CardType.Skill;
        Rarity = CardRarity.Common;
        Exhaust = true;
    }

    public override void OnPlay(AbstractCreature target)
    {
        // 本回合打过过河拆桥 → 0费
        bool playedGuoHe = AbstractDungeon.ActionManager.CardsPlayedThisTurn
            .Any(c => c is GuoHeCard);
        if (playedGuoHe)
            this.Cost = 0;

        int stealAmount = Upgraded ? 12 : 8;
        int targetBlock = target.Block;

        if (targetBlock > 0)
        {
            int stolen = System.Math.Min(stealAmount, targetBlock);
            PowerCmd.ReduceBlock(target, stolen, null!);
            BlockCmd.AddBlock(Owner, stolen);
        }
        else
        {
            // 目标无格挡 → 获得6格挡 + 抽1
            BlockCmd.AddBlock(Owner, 6);
            AbstractDungeon.Player.Draw(1);
        }

        // 通知Silent计策系统
        SilentStrategyEffect.OnStrategyCard(Owner, "ShunShouCard");
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("ShunShouCard");
}
