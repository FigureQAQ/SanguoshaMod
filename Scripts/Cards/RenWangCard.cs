using System.Linq;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 仁王盾 — 回合末+格挡，打过闪→双倍，首次未格挡受伤害→消耗1技能+10格挡（每战一次）。
/// 费2, 能力(防具), 罕见
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class RenWangCard : ModCardTemplate
{
    public RenWangCard() { Name = "仁王盾"; Cost = 2; Type = CardType.Power; Rarity = CardRarity.Uncommon; }

    public override void OnPlay(AbstractCreature target)
    {
        PowerCmd.Apply<RenWangBuff>(Owner, Owner, 1, null!);
    }

    public override CustomCardStrings GetCardStrings() => CustomCardStrings.FromLocalization("RenWangCard");
}

[RegisterPower]
public class RenWangBuff : EquipmentBuff
{
    public override EquipmentSlot Slot => EquipmentSlot.Armor;
    public bool PlayedShanThisTurn;
    public bool EmergencyUsed;

    public override void OnAfterCardPlayed(Card card)
    {
        if (card is ShanCard)
            PlayedShanThisTurn = true;
    }

    public override void AtEndOfTurn()
    {
        int baseBlock = Upgraded ? 10 : 7;
        if (PlayedShanThisTurn)
            baseBlock *= 2; // 打过闪 → 双倍(14/20)
        BlockCmd.AddBlock(Owner, baseBlock);
        PlayedShanThisTurn = false;
    }

    public override int OnAttacked(AbstractCreature attacker, int damage)
    {
        if (!EmergencyUsed && Owner.Block < damage)
        {
            // 首次未格挡受伤害 → 消耗手牌中1张技能 → +10格挡（每战一次）
            var skillInHand = Owner.Hand.FirstOrDefault(c => c.Type == CardType.Skill);
            if (skillInHand != null)
            {
                EmergencyUsed = true;
                SkillExhaustedThisCombat = true;
                CardCmd.ExhaustCard(Owner, skillInHand);
                BlockCmd.AddBlock(Owner, Upgraded ? 15 : 10);
            }
        }
        return damage;
    }

    public bool SkillExhaustedThisCombat;

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "仁王盾",
        Description = $"回合末+{(Upgraded ? 10 : 7)}格挡。打过闪→双倍{(Upgraded ? 20 : 14)}。首次未格挡受伤害→消耗技能+{(Upgraded ? 15 : 10)}格挡(每战一次)。(【防具】)"
    };
}
