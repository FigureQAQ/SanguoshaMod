using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using sanguosha.Cards;
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
        SanguoshaCardPortraitLoader.RegisterAll();
        SanguoshaCardUiTextureLoader.RegisterAll();
        SanguoshaCharacterSkills.Register();
        RegisterSanguoshaStarterCompatibility();
        Logger.Info("Sanguosha mod initialized.");
    }

    private static void RegisterSanguoshaStarterCompatibility()
    {
        RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<
            ShaCard,
            MegaCrit.Sts2.Core.Models.Cards.UltimateStrike>(ModId);
        RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<
            ShanCard,
            MegaCrit.Sts2.Core.Models.Cards.UltimateDefend>(ModId);
    }
}
