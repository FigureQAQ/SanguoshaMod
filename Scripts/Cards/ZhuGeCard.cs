using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 诸葛连弩 — 前3张攻击递增伤害，第3张后抽牌，抽到攻击牌-1费。
/// 费2, 能力(武器), 罕见
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class ZhuGeCard : ModCardTemplate
{
    public ZhuGeCard() { Name = "诸葛连弩"; Cost = 2; Type = CardType.Power; Rarity = CardRarity.Uncommon; }

    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<ZhuGeBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("ZhuGeCard");
}

[RegisterPower]
public class ZhuGeBuff : EquipmentBuff
{
    public override EquipmentSlot Slot => EquipmentSlot.Weapon;
    public int AttacksThisTurn;

    public override void OnAfterAttack(AbstractCreature target, int damage)
    {
        AttacksThisTurn++;
        if (AttacksThisTurn == 1)
            PowerCmd.DealDamage(Owner, target, Upgraded ? 2 : 1, DamageType.Normal);
        else if (AttacksThisTurn == 2)
            PowerCmd.DealDamage(Owner, target, Upgraded ? 4 : 3, DamageType.Normal);
        else if (AttacksThisTurn == 3)
        {
            PowerCmd.DealDamage(Owner, target, Upgraded ? 6 : 5, DamageType.Normal);
            // 第3张后抽1
            AbstractDungeon.Player.Draw(1);
        }
    }

    public override void OnDraw(Card card)
    {
        if (card.Type == CardType.Attack)
            card.Cost = System.Math.Max(0, card.Cost - 1);
    }

    public override void AtStartOfTurn() { AttacksThisTurn = 0; }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "诸葛连弩",
        Description = $"前3张攻击+{(Upgraded ? "2/4/6" : "1/3/5")}伤。第3张后抽1。抽到攻击牌-1费。(【武器】)"
    };
}
