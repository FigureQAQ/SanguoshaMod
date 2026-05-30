using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using sanguosha.Characters;

namespace sanguosha.Patches;

[HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal))]
internal static class BaiYinDamageCapPatch
{
    private static readonly Dictionary<ulong, decimal> DamageThisTurnByPlayer = new();

    public static void ResetDamageThisTurn(Player player)
    {
        DamageThisTurnByPlayer[player.NetId] = 0;
    }

    public static void Clear()
    {
        DamageThisTurnByPlayer.Clear();
    }

    private static void Prefix(Creature __instance, ref decimal __0)
    {
        if (!__instance.IsPlayer || __instance.Player is null) return;
        var cap = SanguoshaCharacterSkills.GetBaiYinDamageCap(__instance.Player);
        if (cap <= 0) return;
        var damageThisTurn = DamageThisTurnByPlayer.GetValueOrDefault(__instance.Player.NetId);
        var remaining = cap - damageThisTurn;
        if (remaining <= 0)
        {
            __0 = 0;
            return;
        }

        if (__0 > remaining)
        {
            __0 = remaining;
        }
    }

    private static void Postfix(Creature __instance, decimal __0)
    {
        if (!__instance.IsPlayer || __instance.Player is null) return;
        if (SanguoshaCharacterSkills.GetBaiYinDamageCap(__instance.Player) <= 0) return;
        DamageThisTurnByPlayer[__instance.Player.NetId] =
            DamageThisTurnByPlayer.GetValueOrDefault(__instance.Player.NetId) + Math.Max(0, __0);
    }
}
