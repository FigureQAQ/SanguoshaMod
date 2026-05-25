using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 赤兔 — 回合开始+1临时能量，用该能打牌→攻击+3伤/技能抽1。
/// 费1, 能力(-1马), 稀有
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class ChiTuCard : ModCardTemplate
{
    public ChiTuCard() { Name = "赤兔"; Cost = 1; Type = CardType.Power; Rarity = CardRarity.Rare; }

    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<ChiTuBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("ChiTuCard");
}

[RegisterPower]
public class ChiTuBuff : EquipmentBuff
{
    public override EquipmentSlot Slot => EquipmentSlot.MountMinus;
    public bool TempEnergyUsed;
    public int TempEnergyBonus = Upgraded ? 2 : 1;

    public override void AtStartOfTurn()
    {
        // 回合开始 +1临时能量
        PowerCmd.Apply<TempEnergyBuff>(Owner, Owner, TempEnergyBonus, null!);
        TempEnergyUsed = false;
    }

    public override void OnAfterCardPlayed(Card card)
    {
        if (!TempEnergyUsed)
        {
            TempEnergyUsed = true;
            if (card.Type == CardType.Attack)
                // 用该能打攻击牌 → +3/+5伤（在afterPlay中加一次伤害）
                PowerCmd.DealDamage(Owner, AbstractDungeon.AllEnemies[0],
                    Upgraded ? 5 : 3, DamageType.Normal);
            else if (card.Type == CardType.Skill)
                AbstractDungeon.Player.Draw(Upgraded ? 2 : 1);
        }
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "赤兔",
        Description = $"回合开始+{TempEnergyBonus}临时能。用该能→攻击+{(Upgraded ? 5 : 3)}伤/技能抽{(Upgraded ? 2 : 1)}。(【-1马】)"
    };
}
