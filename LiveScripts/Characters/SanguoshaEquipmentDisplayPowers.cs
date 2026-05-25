using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace sanguosha.Characters;

internal abstract class SanguoshaEquipmentDisplayPower : ModPowerTemplate, IPowerExtraIconAmountLabelsProvider
{
    protected abstract string IconBaseName { get; }

    internal string SanguoshaIconBaseName => IconBaseName;

    protected abstract string LocKey { get; }

    public override LocString Title => new("powers", $"{LocKey}.title");

    public override LocString Description => new("powers", $"{LocKey}.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: ModIconPath("power_icons", IconBaseName),
        BigIconPath: ModIconPath("power_icons_big", IconBaseName));

    public override string CustomIconPath => ModIconPath("power_icons", IconBaseName);

    public override string CustomBigIconPath => ModIconPath("power_icons_big", IconBaseName);

    public virtual IReadOnlyList<ExtraIconAmountLabelSlot> GetPowerExtraIconAmountLabelSlots() => [];

    internal static string ModIconPath(string folder, string iconBaseName)
    {
        return $"res://mods/{Entry.ModId}/{folder}/{iconBaseName}.png";
    }
}

internal static class SanguoshaPowerIconAssets
{
    private const string ProviderKey = $"{Entry.ModId}.power-icons";
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        ExternalAssetOverrideRegistry.RegisterPowerIconPathProvider(
            ProviderKey,
            power => power is SanguoshaEquipmentDisplayPower display
                ? SanguoshaEquipmentDisplayPower.ModIconPath("power_icons", display.SanguoshaIconBaseName)
                : null!);
        ExternalAssetOverrideRegistry.RegisterPowerIconTextureProvider(
            ProviderKey,
            power => LoadTexture(power, "power_icons"));
        ExternalAssetOverrideRegistry.RegisterPowerBigIconTextureProvider(
            ProviderKey,
            power => LoadTexture(power, "power_icons_big"));
    }

    private static Texture2D LoadTexture(PowerModel power, string folder)
    {
        if (power is not SanguoshaEquipmentDisplayPower display)
        {
            return null!;
        }

        var modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrWhiteSpace(modDir))
        {
            return null!;
        }

        var path = Path.Combine(modDir, folder, $"{display.SanguoshaIconBaseName}.png");
        if (!File.Exists(path))
        {
            return null!;
        }

        var image = Image.LoadFromFile(path);
        return image is null || image.IsEmpty()
            ? null!
            : ImageTexture.CreateFromImage(image);
    }
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.DumbHoverTip), MethodType.Getter)]
internal static class SanguoshaEquipmentDisplayPowerHoverTipPatch
{
    private static bool Prefix(PowerModel __instance, ref HoverTip __result)
    {
        if (__instance is not SanguoshaEquipmentDisplayPower power)
        {
            return true;
        }

        __result = new HoverTip(power.Title, power.Description, power.Icon);
        return false;
    }
}

[RegisterPower]
internal sealed class ZhuGeDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "zhuge";

    protected override string LocKey => "SANGUOSHA_POWER_ZHU_GE_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class ZhangBaDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "zhangba";

    protected override string LocKey => "SANGUOSHA_POWER_ZHANG_BA_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class QingGangDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "qinggang";

    protected override string LocKey => "SANGUOSHA_POWER_QING_GANG_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class GuanShiDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "guanshi";

    protected override string LocKey => "SANGUOSHA_POWER_GUAN_SHI_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class HanBingDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "hanbing";

    protected override string LocKey => "SANGUOSHA_POWER_HAN_BING_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class QiLinDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "qilin";

    protected override string LocKey => "SANGUOSHA_POWER_QI_LIN_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class GuDingDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "guding";

    protected override string LocKey => "SANGUOSHA_POWER_GU_DING_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class BaiYinDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "baiyin";

    protected override string LocKey => "SANGUOSHA_POWER_BAI_YIN_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class RenWangDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "renwang";

    protected override string LocKey => "SANGUOSHA_POWER_REN_WANG_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class BaGuaDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "bagua";

    protected override string LocKey => "SANGUOSHA_POWER_BA_GUA_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class ChiTuDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "chitu";

    protected override string LocKey => "SANGUOSHA_POWER_CHI_TU_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class DaWanDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "dawan";

    protected override string LocKey => "SANGUOSHA_POWER_DA_WAN_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class DiLuDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "dilu";

    protected override string LocKey => "SANGUOSHA_POWER_DI_LU_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class YuXiDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "yuxi";

    protected override string LocKey => "SANGUOSHA_POWER_YU_XI_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class MuNiuDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "muniu";

    protected override string LocKey => "SANGUOSHA_POWER_MU_NIU_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class TaiPingDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "taiping";

    protected override string LocKey => "SANGUOSHA_POWER_TAI_PING_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class KongChengDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "kongcheng";

    protected override string LocKey => "SANGUOSHA_POWER_KONG_CHENG_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class ZhiHengDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "yuxi";

    protected override string LocKey => "SANGUOSHA_POWER_ZHI_HENG_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class WuShuangDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "zhangba";

    protected override string LocKey => "SANGUOSHA_POWER_WU_SHUANG_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class LianYingDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "muniu";

    protected override string LocKey => "SANGUOSHA_POWER_LIAN_YING_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class YiJiDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "taiping";

    protected override string LocKey => "SANGUOSHA_POWER_YI_JI_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class JianXiongDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "guding";

    protected override string LocKey => "SANGUOSHA_POWER_JIAN_XIONG_DISPLAY_POWER";

}

[RegisterPower]
internal sealed class GuiCaiDisplayPower : SanguoshaEquipmentDisplayPower
{
    protected override string IconBaseName => "bagua";

    protected override string LocKey => "SANGUOSHA_POWER_GUI_CAI_DISPLAY_POWER";

}
