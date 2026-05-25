using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.VarImpl;

namespace sanguosha.Cards;

/// <summary>
/// 杀 — 1费 基础攻击。8伤(+3 vs格挡/Debuff)。第3攻时→+6伤+3格挡。
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class ShaCard : ModCardTemplate
{
    public ShaCard() : base(1, CardType.Attack, CardRarity.Basic, CardTargetType.Enemy, true)
    {
        CanonicalVars = new CanonicalVar[]
        {
            new(DamageVar, ValueProp.Damage) { Base = 8, Current = 8, UpgradeDelta = 3 }
        };
    }
    public const int DamageVar = 0;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cp)
    {
        var t = cp.Target!;
        int atkOrdinal = GetAttackOrdinal(); // 本回合第几张攻击
        int dmg = V<int>(DamageVar);

        bool hasCondition = t.Block > 0 || t.Powers.Any(p => p.IsDebuff);
        int bonus = hasCondition ? (atkOrdinal >= 3 ? 6 : 3) : 0;

        if (atkOrdinal >= 3 && bonus > 0)
            await BlockCmd.AddBlock(ctx, 3, Owner).Build();

        await DamageCmd.Attack(dmg + bonus).FromCard(this).Targeting(t).Execute(ctx);
    }

    private int GetAttackOrdinal()
        => AbstractDungeon.ActionManager.CardsPlayedThisTurn.Count(c => c.Type == CardType.Attack) + 1;

    public override void OnUpgrade() => DynamicVars[DamageVar].UpgradeValueBy(3);
}
