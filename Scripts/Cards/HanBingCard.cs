using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 寒冰剑 — 首攻上2虚弱，对虚弱+3伤，本回合获得过格挡→+5。
/// 费1, 能力(武器), 罕见
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class HanBingCard : ModCardTemplate
{
    public HanBingCard() { Name = "寒冰剑"; Cost = 1; Type = CardType.Power; Rarity = CardRarity.Uncommon; }

    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<HanBingBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("HanBingCard");
}

[RegisterPower]
public class HanBingBuff : EquipmentBuff
{
    public override EquipmentSlot Slot => EquipmentSlot.Weapon;
    public bool FirstAttackDone;
    public bool BlockGainedThisTurn;

    public override void OnGainBlock(int amount)
    {
        BlockGainedThisTurn = true;
    }

    public override void OnBeforeAttack(AbstractCreature target)
    {
        if (!FirstAttackDone)
        {
            // 首攻 → 2虚弱
            PowerCmd.Apply<WeakPower>(Owner, target, 2, null!);
            FirstAttackDone = true;
        }
    }

    public override void OnAfterAttack(AbstractCreature target, int damage)
    {
        // 对虚弱 → +3伤害（在BeforeAttack中已加虚弱，此处加伤）
        if (target.Powers.Any(p => p is WeakPower))
            PowerCmd.DealDamage(Owner, target, Upgraded ? 4 : 3, DamageType.Normal);

        // 本回合获得过格挡 → +5
        if (BlockGainedThisTurn)
            PowerCmd.DealDamage(Owner, target, 5, DamageType.Normal);
    }

    public override void AtStartOfTurn() { FirstAttackDone = false; BlockGainedThisTurn = false; }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "寒冰剑", Description = $"首攻上2虚弱。对虚弱+{(Upgraded ? 4 : 3)}伤。获格挡→+5。(【武器】)" };
}
