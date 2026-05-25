using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 白银狮子 — 回合开始+1回血，首次回血→+1酒意+4格挡，格挡≥20→+4回血(每战一次)。
/// 费1, 能力(防具), 稀有
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class BaiYinCard : ModCardTemplate
{
    public BaiYinCard() { Name = "白银狮子"; Cost = 1; Type = CardType.Power; Rarity = CardRarity.Rare; }

    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<BaiYinBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("BaiYinCard");
}

[RegisterPower]
public class BaiYinBuff : EquipmentBuff
{
    public override EquipmentSlot Slot => EquipmentSlot.Armor;
    public bool FirstHealTriggered;
    public bool HighBlockTriggered;

    public override void AtStartOfTurn()
    {
        // 回合开始 +1回血
        AbstractDungeon.Player.Heal(1);
    }

    public override int OnHeal(int amount)
    {
        if (!FirstHealTriggered && amount > 0)
        {
            FirstHealTriggered = true;
            // 首次回血 → +1酒意 + 4格挡
            PowerCmd.Apply<JiuBuff>(Owner, Owner, 1, null!);
            BlockCmd.AddBlock(Owner, Upgraded ? 6 : 4);
        }
        return amount;
    }

    public override void AtEndOfTurn()
    {
        // 格挡≥20 → +4回血（每战一次）
        if (!HighBlockTriggered && Owner.Block >= 20)
        {
            HighBlockTriggered = true;
            AbstractDungeon.Player.Heal(Upgraded ? 6 : 4);
        }
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "白银狮子",
        Description = $"回合开始+1回血。首次回血→+1酒意+{(Upgraded ? 6 : 4)}格挡。格挡≥20→+{(Upgraded ? 6 : 4)}回血(每战一次)。(【防具】)"
    };
}
