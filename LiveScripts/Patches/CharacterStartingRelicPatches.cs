using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using sanguosha.Relics;

namespace sanguosha.Patches;

[HarmonyPatch]
internal static class CharacterStartingRelicPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(CharacterModel).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(CharacterModel).IsAssignableFrom(type))
            .Select(type => AccessTools.PropertyGetter(type, nameof(CharacterModel.StartingRelics)))
            .Where(method => method is not null)!;
    }

    private static void Postfix(CharacterModel __instance, ref IReadOnlyList<RelicModel> __result)
    {
        __result = __instance.GetType().Name switch
        {
            "Ironclad" => [ModelDb.Relic<IroncladSkillRelic>()],
            "Silent" => [ModelDb.Relic<SilentSkillRelic>()],
            "Defect" => [ModelDb.Relic<DefectSkillRelic>()],
            "Necrobinder" => [ModelDb.Relic<NecrobinderSkillRelic>()],
            "Regent" => [ModelDb.Relic<RegentSkillRelic>()],
            _ => __result
        };
    }
}
