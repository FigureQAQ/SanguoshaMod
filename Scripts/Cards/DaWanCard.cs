using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 大宛 — 首次攻击+4/6伤害，若是杀→+4格挡，本回合已获格挡→+2伤。
/// 费1, 能力(-1马), 罕见
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class DaWanCard : ModCardTemplate
{
    public DaWanCard() { Name = "大宛"; Cost = 1; Type = CardType.Power; Rarity = CardRarity.Uncommon; }

    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<DaWanBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("DaWanCard");
}

[RegisterPower]
public class DaWanBuff : EquipmentBuff
{
    public override EquipmentSlot Slot => EquipmentSlot.MountMinus;
    public bool FirstAttackThisTurn;
    public bool BlockGainedThisTurn;

    public override void AtStartOfTurn()
    {
        FirstAttackThisTurn = true;
        BlockGainedThisTurn = false;
    }

    public override void OnGainBlock(int amount)
    {
        if (amount > 0)
            BlockGainedThisTurn = true;
    }

    public override void OnAfterAttack(AbstractCreature target, int damage)
    {
        if (FirstAttackThisTurn)
        {
            FirstAttackThisTurn = false;
            PowerCmd.DealDamage(Owner, target, Upgraded ? 6 : 4, DamageType.Normal);

            // 若已获得格挡 → +2伤
            if (BlockGainedThisTurn)
                PowerCmd.DealDamage(Owner, target, 2, DamageType.Normal);
        }
    }

    public override void OnAfterCardPlayed(Card card)
    {
        if (card is ShaCard)
            BlockCmd.AddBlock(Owner, 4);
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "大宛",
        Description = $"首次攻+{(Upgraded ? 6 : 4)}伤。是杀→+4格挡。已获格挡→+2伤。(【-1马】)"
    };
}
