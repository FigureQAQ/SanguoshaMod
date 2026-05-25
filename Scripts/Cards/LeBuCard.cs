using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 乐不思蜀 — 下回合攻击意图→取消+虚弱+易伤，无攻击→虚弱+抽1。
/// 费2, 技能, 罕见, 消耗, 彩色
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class LeBuCard : ModCardTemplate
{
    public LeBuCard()
    {
        Name = "乐不思蜀";
        Cost = 2;
        Type = CardType.Skill;
        Rarity = CardRarity.Uncommon;
        Exhaust = true;
        IsPrismatic = true;
    }

    public override void OnPlay(AbstractCreature target)
    {
        if (target is AbstractMonster m)
        {
            if (m.GetIntentDmg() > 0)
            {
                // 有攻击意图 → 取消攻击 + 2虚弱 + 1易伤
                // 设置跳过回合
                PowerCmd.Apply<LeBuSkipTurnBuff>(Owner, m, 1, null!);
            }
            else
            {
                // 没有攻击意图 → 1虚弱 + 抽1
                PowerCmd.Apply<WeakPower>(Owner, m, 1, null!);
                AbstractDungeon.Player.Draw(1);
            }
        }
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("LeBuCard");
}

/// <summary>
/// 乐不跳过回合 — 下回合攻击时取消攻击+2虚弱+1易伤
/// </summary>
[RegisterPower]
public class LeBuSkipTurnBuff : ModPowerTemplate
{
    public LeBuSkipTurnBuff() { IsDebuff = true; }

    public override void AtStartOfTurn(AbstractCreature owner)
    {
        if (owner is AbstractMonster m && m.GetIntentDmg() > 0)
        {
            m.SkipTurn = true;
            PowerCmd.Apply<WeakPower>(null!, owner, Upgraded ? 3 : 2, null!);
            PowerCmd.Apply<VulnerablePower>(null!, owner, 1, null!);
        }
        PowerCmd.RemovePower(owner, this);
    }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "乐不", Description = "下回合有攻击→取消+虚弱+易伤。" };
}
