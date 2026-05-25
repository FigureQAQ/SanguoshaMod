using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using sanguosha.Characters;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace sanguosha;

[ModInitializer(nameof(Init))]
public static class Entry
{
    public const string ModId = "sanguosha";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        new Harmony($"{ModId}.global-card-replacement").PatchAll(assembly);
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        SanguoshaPowerIconAssets.Register();
        SanguoshaCharacterSkills.Register();
        Logger.Info("Sanguosha mod initialized.");
    }
}
