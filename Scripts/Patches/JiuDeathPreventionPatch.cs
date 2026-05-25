using HarmonyLib;

namespace sanguosha.Patches;

/// <summary>
/// 酒免死 — 致命伤害时，消耗 JiuBuff 层数保留等量HP。
/// </summary>
[HarmonyPatch]
public static class JiuDeathPreventionPatch
{
    [HarmonyPatch(typeof(AbstractPlayer), nameof(AbstractPlayer.Damage))]
    [HarmonyPrefix]
    public static void Prefix(AbstractPlayer __instance, ref int damageAmount)
    {
        if (damageAmount < __instance.Hp) return;

        var jiuBuff = __instance.Powers.OfType<Cards.JiuBuff>().FirstOrDefault();
        if (jiuBuff != null && jiuBuff.Amount > 0)
        {
            int saved = jiuBuff.Amount;
            damageAmount = Math.Max(0, __instance.Hp - saved);
            if (damageAmount >= __instance.Hp)
                damageAmount = __instance.Hp - 1;
            PowerCmd.ReducePower(__instance, jiuBuff, saved);
            return;
        }

        // 后备：手中未使用的酒牌
        var jiuCard = __instance.Hand.Find(c => c.GetType() == typeof(Cards.JiuCard));
        if (jiuCard != null)
        {
            __instance.ExhaustCard(jiuCard);
            damageAmount = __instance.Hp - 1;
        }
    }
}
