using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 丈八蛇矛 — 打出技能后生成0费5伤杀(消耗)，第3张攻击牌→抽1。
/// 费2, 能力(武器), 稀有
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class ZhangBaCard : ModCardTemplate
{
    public ZhangBaCard() { Name = "丈八蛇矛"; Cost = 2; Type = CardType.Power; Rarity = CardRarity.Rare; }

    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<ZhangBaBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("ZhangBaCard");
}

[RegisterPower]
public class ZhangBaBuff : EquipmentBuff
{
    public override EquipmentSlot Slot => EquipmentSlot.Weapon;
    public int AttacksThisTurn;

    public override void OnAfterCardPlayed(Card card)
    {
        if (card.Type == CardType.Skill)
        {
            // 生成0费5伤杀(消耗)
            var sha = new ShaCard { Cost = 0, Exhaust = true };
            AbstractDungeon.Player.Hand.Add(sha);
        }
    }

    public override void OnAfterAttack(AbstractCreature target, int damage)
    {
        AttacksThisTurn++;
        if (AttacksThisTurn == 3)
            AbstractDungeon.Player.Draw(1);
    }

    public override void AtStartOfTurn() { AttacksThisTurn = 0; }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "丈八蛇矛", Description = "技能后→生成0费5伤杀(消耗)。第3张攻击→抽1。(【武器】)" };
}
