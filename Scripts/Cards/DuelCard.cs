using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.VarImpl;

namespace sanguosha.Cards;

/// <summary>
/// 决斗 — 1费 普通攻击。9伤+3反伤。第2+攻→取消反伤，翻顶攻打出并消耗。
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class DuelCard : ModCardTemplate
{
    public DuelCard() : base(1, CardType.Attack, CardRarity.Common, CardTargetType.Enemy, true)
    {
        CanonicalVars = new CanonicalVar[]
        {
            new(DamageVar, ValueProp.Damage) { Base = 9, Current = 9, UpgradeDelta = 3 }
        };
    }
    public const int DamageVar = 0;
    private const int Recoil = 3;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cp)
    {
        int atkOrdinal = AbstractDungeon.ActionManager.CardsPlayedThisTurn
            .Count(c => c.Type == CardType.Attack) + 1;

        await DamageCmd.Attack(V<int>(DamageVar)).FromCard(this).Targeting(cp.Target!).Execute(ctx);

        if (atkOrdinal >= 2)
        {
            // 翻抽牌堆顶攻击牌打出并消耗
            var topAtk = Owner.DrawPile.FirstOrDefault(c => c.Type == CardType.Attack);
            if (topAtk != null)
            {
                topAtk.Exhaust = true;
                await topAtk.OnPlay(ctx, cp); // 打出
            }
            else
                await DrawCmd.DrawCards(Owner, 1).Build().Execute(null!);
        }
        else
            await DamageCmd.Attack(Recoil).FromCard(this).Targeting(Owner).Execute(ctx);
    }

    public override void OnUpgrade() => DynamicVars[DamageVar].UpgradeValueBy(3);
}
