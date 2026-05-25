using System.Linq;
using STS2RitsuLib.Scaffolding.Content;
using sanguosha.Characters;

namespace sanguosha.Cards;

/// <summary>
/// 过河拆桥 — 移除格挡并施加Debuff，满足条件时抽牌。
/// 费1, 技能, 普通, 消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class GuoHeCard : ModCardTemplate
{
    public GuoHeCard()
    {
        Name = "过河拆桥";
        Cost = 1;
        Type = CardType.Skill;
        Rarity = CardRarity.Common;
        Exhaust = true;
    }

    public override void OnPlay(AbstractCreature target)
    {
        // 移除目标所有格挡
        var block = target.Block;
        if (block > 0)
            PowerCmd.ReduceBlock(target, block, null!);

        // 根据目标的攻击意图施加Debuff
        if (target is AbstractMonster m && m.GetIntentDmg() > 0)
            PowerCmd.Apply<WeakPower>(null!, target, 1, null!);
        else
            PowerCmd.Apply<VulnerablePower>(null!, target, 1, null!);

        // 移除≥6格挡或目标已有Debuff → 抽1
        bool shouldDraw = block >= 6 || target.Powers.Any(p => p.IsDebuff);
        if (shouldDraw)
            AbstractDungeon.Player.Draw(1);

        // 本回合第一张消耗 → 额外抽1
        if (AbstractDungeon.ActionManager.CardsExhaustedThisTurn.Count == 0)
            AbstractDungeon.Player.Draw(1);

        // 通知Silent计策系统
        SilentStrategyEffect.OnStrategyCard(Owner, "GuoHeCard");
        UpgradeDescription();
    }

    private void UpgradeDescription()
    {
        if (Upgraded)
        { /* 升级：无变化，或可增强Debuff层数 */ }
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("GuoHeCard");
}
