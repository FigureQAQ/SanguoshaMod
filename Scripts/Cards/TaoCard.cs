using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.VarImpl;

namespace sanguosha.Cards;

/// <summary>
/// 桃 — 2费 稀有技能。保留+6回血+酒意消耗换回血抽牌。HP<30%→费用1。
/// </summary>
[RegisterCard(typeof(CardPoolsColorless))]
public class TaoCard : ModCardTemplate
{
    public TaoCard() : base(2, CardType.Skill, CardRarity.Rare, CardTargetType.Self, true)
    {
        SelfRetain = true;
        Exhaust = true;
        CanonicalVars = new CanonicalVar[]
        {
            new(HealVar, ValueProp.Heal) { Base = 6, Current = 6, UpgradeDelta = 2 }
        };
    }
    public const int HealVar = 0;

    public override bool CanUse(PlayerChoiceContext ctx) => Owner.Hp < Owner.MaxHp;

    public override int GetCost()
    {
        int baseCost = base.GetCost();
        float hpRatio = (float)Owner.Hp / Owner.MaxHp;
        float threshold = Upgraded ? 0.5f : 0.3f;
        return hpRatio < threshold ? Math.Max(1, baseCost - 1) : baseCost;
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cp)
    {
        int heal = V<int>(HealVar);
        var jiu = Owner.GetPower<JiuBuff>();
        if (jiu != null && jiu.Amount > 0)
        {
            PowerCmd.ReducePower(Owner, jiu, 1);
            heal += 4;
            await DrawCmd.DrawCards(Owner, 1).Build().Execute(null!);
        }
        await HealCmd.Heal(Owner, heal).FromCard(this).Execute(ctx);
    }

    public override void OnUpgrade() => DynamicVars[HealVar].UpgradeValueBy(2);
}
