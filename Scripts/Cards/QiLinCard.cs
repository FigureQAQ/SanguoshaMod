using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 麒麟弓 — 击杀→7伤扩散+1易伤，有雷引→+4伤+转移8伤雷引。
/// 费2, 能力(武器), 稀有
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class QiLinCard : ModCardTemplate
{
    public QiLinCard() { Name = "麒麟弓"; Cost = 2; Type = CardType.Power; Rarity = CardRarity.Rare; }

    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<QiLinBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("QiLinCard");
}

[RegisterPower]
public class QiLinBuff : EquipmentBuff
{
    public override EquipmentSlot Slot => EquipmentSlot.Weapon;
    public bool TriggeredThisCombat;

    public override void OnKill(AbstractCreature victim)
    {
        if (TriggeredThisCombat) return;
        TriggeredThisCombat = true;

        int splashDamage = Upgraded ? 9 : 7;
        var others = AbstractDungeon.GetMonsters().Monsters
            .Where(m => m != victim && !m.IsDead && !m.IsDying).ToList();

        foreach (var m in others)
        {
            PowerCmd.DealDamage(Owner, m, splashDamage, DamageType.Normal);
            PowerCmd.Apply<VulnerablePower>(Owner, m, 1, null!);
        }

        // 有雷引 → +4伤 + 转移8伤雷引
        var leYin = Owner.Powers.OfType<LeYinBuff>().FirstOrDefault();
        if (leYin != null)
        {
            if (others.Count > 0)
                PowerCmd.DealDamage(Owner, others[0], 4, DamageType.Lightning);

            PowerCmd.RemovePower(Owner, leYin);
            if (others.Count > 0)
            {
                var transferred = new LeYinBuff { Amount = 8 };
                PowerCmd.ApplyPower(Owner, others[0], transferred);
            }
        }
    }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "麒麟弓", Description = $"击杀→{(Upgraded ? 9 : 7)}伤扩散+易伤。有雷引→+4伤+转移8雷引。(一次/战)(【武器】)" };
}
