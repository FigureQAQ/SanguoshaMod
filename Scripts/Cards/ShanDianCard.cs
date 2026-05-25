using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 闪电 — 对目标施加单次雷引(16/21伤)，目标死亡后转移12伤。
/// 费2, 技能, 罕见, 消耗
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class ShanDianCard : ModCardTemplate
{
    public ShanDianCard()
    {
        Name = "闪电";
        Cost = 2;
        Type = CardType.Skill;
        Rarity = CardRarity.Uncommon;
        Exhaust = true;
    }

    public override void OnPlay(AbstractCreature target)
    {
        int leyinAmount = Upgraded ? 21 : 16;
        // 对目标施加雷引
        var leyin = new LeYinBuff { Amount = leyinAmount };
        PowerCmd.ApplyPower(Owner, target, leyin);

        // 提前触发半额(8/10)
        int preTrigger = Upgraded ? 10 : 8;
        PowerCmd.DealDamage(Owner, target, preTrigger, DamageType.Lightning);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("ShanDianCard");
}
