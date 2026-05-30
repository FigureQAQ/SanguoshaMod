using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Unlocks;

namespace sanguosha.Patches;

[HarmonyPatch(typeof(UnlockState), nameof(UnlockState.Characters), MethodType.Getter)]
internal static class SanguoshaUnlockAllCharactersPatch
{
    private static void Postfix(ref IEnumerable<CharacterModel> __result)
    {
        __result =
        [
            ModelDb.Character<Ironclad>(),
            ModelDb.Character<Silent>(),
            ModelDb.Character<Defect>(),
            ModelDb.Character<Regent>(),
            ModelDb.Character<Necrobinder>()
        ];
    }
}
