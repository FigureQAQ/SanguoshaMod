using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 兵粮寸断 — 削减力量+Debuff协同增强，雷引半额先触发。
/// 费1, 技能, 罕见, 消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class BingLiangCard : ModCardTemplate
{
    public BingLiangCard()
    {
        Name = "兵粮寸断";
        Cost = 1;
        Type = CardType.Skill;
        Rarity = CardRarity.Uncommon;
        Exhaust = true;
    }

    public override void OnPlay(AbstractCreature target)
    {
        int strengthLoss = Upgraded ? 5 : 4;
        // 基础：-4力量 + 1虚弱
        PowerCmd.ReduceStrength(target, strengthLoss);
        PowerCmd.Apply<WeakPower>(Owner, target, 1, null!);

        // 已有易伤或乐不 → 下回合再-2力量
        bool hasVulnOrLeBu = target.Powers.Any(p =>
            p is VulnerablePower || p is LeBuSkipTurnBuff);
        if (hasVulnOrLeBu)
            PowerCmd.Apply<BingLiangNextTurnStrengthLossBuff>(Owner, target, 1, null!);

        // 有雷引 → 先触发半额
        var leYin = target.Powers.OfType<LeYinBuff>().FirstOrDefault();
        if (leYin != null && !leYin.HalfTriggered)
        {
            leYin.HalfTriggered = true;
            PowerCmd.DealDamage(Owner, target, leYin.Amount / 2, DamageType.Lightning);
        }
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("BingLiangCard");
}

/// <summary>
/// 兵粮寸断下回合减力量 — 下回合开始-2力量
/// </summary>
[RegisterPower]
public class BingLiangNextTurnStrengthLossBuff : ModPowerTemplate
{
    public BingLiangNextTurnStrengthLossBuff() { IsDebuff = true; }

    public override void AtStartOfTurn(AbstractCreature owner)
    {
        PowerCmd.ReduceStrength(owner, 2);
        PowerCmd.RemovePower(owner, this);
    }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "兵粮·延", Description = "下回合开始-2力量。" };
}
