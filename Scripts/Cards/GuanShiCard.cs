using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 贯石斧 — 每回合第一次弃/消耗手牌→下张攻击+7伤，若消耗的是攻击牌→抽1。
/// 费1, 能力(武器), 罕见
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class GuanShiCard : ModCardTemplate
{
    public GuanShiCard() { Name = "贯石斧"; Cost = 1; Type = CardType.Power; Rarity = CardRarity.Uncommon; }

    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<GuanShiBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("GuanShiCard");
}

[RegisterPower]
public class GuanShiBuff : EquipmentBuff
{
    public override EquipmentSlot Slot => EquipmentSlot.Weapon;
    public bool TriggeredThisTurn;
    public bool NextAttackHasBonus;

    public override void OnDiscard(Card card)
    {
        if (!TriggeredThisTurn && card != null)
        {
            TriggeredThisTurn = true;
            NextAttackHasBonus = true;
        }
    }

    public override void OnExhaust(Card card)
    {
        if (!TriggeredThisTurn && card != null)
        {
            TriggeredThisTurn = true;
            NextAttackHasBonus = true;
            // 若消耗的是攻击牌 → 抽1
            if (card.Type == CardType.Attack)
                AbstractDungeon.Player.Draw(1);
        }
    }

    public override void OnBeforeAttack(AbstractCreature target)
    {
        if (NextAttackHasBonus)
        {
            NextAttackHasBonus = false;
            PowerCmd.DealDamage(Owner, target, Upgraded ? 9 : 7, DamageType.Normal);
        }
    }

    public override void AtStartOfTurn() { TriggeredThisTurn = false; NextAttackHasBonus = false; }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "贯石斧", Description = $"首次弃/消耗手牌→下张攻击+{(Upgraded ? 9 : 7)}伤。消耗攻击牌→抽1。(【武器】)" };
}
