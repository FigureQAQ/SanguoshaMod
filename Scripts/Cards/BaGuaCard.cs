using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 八卦阵 — 荆棘+回合开始格挡+回合末≥2种格挡来源→格挡30%转为伤害。
/// 费2, 能力(防具), 稀有
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class BaGuaCard : ModCardTemplate
{
    public BaGuaCard() { Name = "八卦阵"; Cost = 2; Type = CardType.Power; Rarity = CardRarity.Rare; }

    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<BaGuaBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("BaGuaCard");
}

[RegisterPower]
public class BaGuaBuff : EquipmentBuff
{
    public override EquipmentSlot Slot => EquipmentSlot.Armor;
    public int Thorn = Upgraded ? 5 : 3;
    public HashSet<Type> BlockSourcesThisTurn = new();

    public override void AtStartOfTurn()
    {
        // 回合开始 +4格挡
        BlockCmd.AddBlock(Owner, Upgraded ? 6 : 4);
        BlockSourcesThisTurn.Clear();
    }

    public override int OnThornDamage(AbstractCreature attacker)
    {
        return Thorn;
    }

    public override void OnGainBlock(int amount)
    {
        // 记录格挡来源
        if (amount > 0)
            BlockSourcesThisTurn.Add(typeof(BaGuaBuff));
    }

    public override void AtEndOfTurn()
    {
        // ≥2种不同牌获得了格挡 → 格挡的30%转为随机伤害
        if (BlockSourcesThisTurn.Count >= 2)
        {
            int block = Owner.Block;
            int damage = (int)(block * 0.3f);
            if (damage > 0)
            {
                var enemies = AbstractDungeon.GetMonsters().Monsters
                    .Where(m => !m.IsDead && !m.IsDying).ToList();
                if (enemies.Count > 0)
                {
                    var target = enemies[AbstractDungeon.CardRng.Random.Next(enemies.Count)];
                    PowerCmd.DealDamage(Owner, target, damage, DamageType.Normal);
                }
            }
        }
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "八卦阵",
        Description = $"{(Upgraded ? 5 : 3)}荆棘。回合开始+{(Upgraded ? 6 : 4)}格挡。≥2种格挡来源→格挡30%转为伤。(【防具】)"
    };
}
