using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

public abstract class SanguoshaCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : ModCardTemplate(baseCost, type, rarity, target, showInCardLibrary)
{
    internal static readonly IReadOnlyDictionary<string, string> PortraitSlugs = new Dictionary<string, string>
    {
        ["BaGuaCard"] = "bagua",
        ["BaiYinCard"] = "baiyin",
        ["BingLiangCard"] = "bingliang",
        ["ChiTuCard"] = "chitu",
        ["CiShaCard"] = "cisha",
        ["DaWanCard"] = "dawan",
        ["DiLuCard"] = "dilu",
        ["DiaoDuCard"] = "diaodu",
        ["DuelCard"] = "duel",
        ["GuiCaiCard"] = "guicai",
        ["GuaGuCard"] = "guagu",
        ["GanJiangMoYeCard"] = "ganjiangmoye",
        ["GuanShiCard"] = "guanshi",
        ["GuanXingCard"] = "guanxing",
        ["GuDingCard"] = "guding",
        ["GuoHeCard"] = "guohe",
        ["HanBingCard"] = "hanbing",
        ["HuoGongCard"] = "huogong",
        ["JianXiongCard"] = "jianxiong",
        ["JieDaoCard"] = "jiedao",
        ["JiuCard"] = "jiu",
        ["JueYingCard"] = "jueying",
        ["KongChengCard"] = "kongcheng",
        ["LeBuCard"] = "lebu",
        ["LianYingCard"] = "lianying",
        ["LongDanCard"] = "longdan",
        ["MengDeXinShuCard"] = "mengdexinshu",
        ["MuNiuCard"] = "muniu",
        ["NanManCard"] = "nanman",
        ["QiLinCard"] = "qilin",
        ["QiXiCard"] = "qixi",
        ["QingGangCard"] = "qinggang",
        ["RenDeCard"] = "rende",
        ["RenWangCard"] = "renwang",
        ["ShaCard"] = "sha",
        ["ShanCard"] = "shan",
        ["ShanDianCard"] = "shandian",
        ["ShouShiCard"] = "shoushi",
        ["ShunShouCard"] = "shunshou",
        ["TaiPingCard"] = "taiping",
        ["TaoCard"] = "tao",
        ["TaoYuanCard"] = "taoyuan",
        ["TengJiaCard"] = "tengjia",
        ["TieSuoCard"] = "tiesuo",
        ["TuXiCard"] = "tuxi",
        ["WanJianCard"] = "wanjian",
        ["WangJianShaCard"] = "wangjiansha",
        ["WuGuCard"] = "wugu",
        ["WuShuangCard"] = "wushuang",
        ["WuXieCard"] = "wuxie",
        ["WuZhongCard"] = "wuzhong",
        ["YiJiCard"] = "yiji",
        ["YuXiCard"] = "yuxi",
        ["ZhangBaCard"] = "zhangba",
        ["ZhangBaShaCard"] = "zhangbasha",
        ["ZhiHengCard"] = "zhiheng",
        ["ZhuGeCard"] = "zhuge"
    };

    internal static readonly string[] ShaInfusionPortraitSlugs =
    [
        "sha_fire",
        "sha_poison",
        "sha_thunder",
        "sha_stored",
        "sha_calamity"
    ];

    internal static readonly string[] CharacterBasicPortraitSlugs =
    [
        "shan_ironclad",
        "tao_ironclad",
        "jiu_ironclad",
        "shan_silent",
        "tao_silent",
        "jiu_silent",
        "shan_defect",
        "tao_defect",
        "jiu_defect",
        "shan_necrobinder",
        "tao_necrobinder",
        "jiu_necrobinder",
        "shan_regent",
        "tao_regent",
        "jiu_regent"
    ];

    public override string CustomPortraitPath => GetModPortraitPath();
    public override string CustomBetaPortraitPath => GetModPortraitPath();
    public override string CustomFramePath => GetCharacterUiPath("frame") ?? base.CustomFramePath ?? string.Empty;
    public override string CustomPortraitBorderPath => GetCharacterUiPath("portrait_border") ?? base.CustomPortraitBorderPath ?? string.Empty;
    public override string CustomEnergyIconPath => GetCharacterEnergyIconPath() ?? base.CustomEnergyIconPath ?? string.Empty;

    internal string SanguoshaPortraitSlug => GetPortraitSlug();
    internal string? CharacterFrameAssetName => GetCharacterUiAssetName("frame");
    internal string? CharacterPortraitBorderAssetName => GetCharacterUiAssetName("portrait_border");
    internal string? CharacterEnergyIconAssetName => GetCharacterEnergyIconAssetName();

    private string GetModPortraitPath()
    {
        return SanguoshaCardPortraitLoader.GetResourcePath(SanguoshaPortraitSlug);
    }

    private string? GetCharacterUiPath(string assetPrefix)
    {
        var assetName = GetCharacterUiAssetName(assetPrefix);
        return assetName is null ? null : GetModUiPath(assetName);
    }

    private string? GetCharacterUiAssetName(string assetPrefix)
    {
        var characterKey = GetCharacterSkinKey(this);
        if (characterKey is null)
        {
            return null;
        }

        return $"{assetPrefix}_{characterKey}_{GetCardTypeUiKey()}";
    }

    private string? GetCharacterEnergyIconPath()
    {
        var assetName = GetCharacterEnergyIconAssetName();
        return assetName is null ? null : GetModUiPath(assetName);
    }

    private string? GetCharacterEnergyIconAssetName()
    {
        var characterKey = GetCharacterSkinKey(this);
        return characterKey is null ? null : $"energy_{characterKey}";
    }

    private static string GetModUiPath(string assetName)
    {
        return $"res://mods/{Entry.ModId}/card_art/ui/{assetName}.png";
    }

    private static string? GetCharacterSkinKey(CardModel card)
    {
        var ownerKey = ResolveCharacterSkinKey(card.Owner?.Character.GetType().Name);
        if (ownerKey is not null)
        {
            return ownerKey;
        }

        var poolKey = ResolveCharacterSkinKey(EnergyIconHelper.GetPrefix(card));
        if (poolKey is not null)
        {
            return poolKey;
        }

        return RunManager.Instance.IsInProgress
            ? ResolveCharacterSkinKey(RunManager.Instance.GetLocalCharacterEnergyIconPrefix())
            : null;
    }

    private static string? ResolveCharacterSkinKey(string? key)
    {
        return key?.ToLowerInvariant() switch
        {
            "ironclad" => "ironclad",
            "silent" => "silent",
            "defect" => "defect",
            "necrobinder" => "necrobinder",
            "regent" => "regent",
            _ => null
        };
    }

    private string GetCardTypeUiKey()
    {
        return Type switch
        {
            CardType.Attack => "attack",
            CardType.Power => "power",
            _ => "skill"
        };
    }

    private string GetPortraitSlug()
    {
        var characterName = Owner?.Character.GetType().Name;
        if (characterName is not null
            && GetCharacterBasicPortraitSlug(characterName, GetType().Name) is { } characterSlug)
        {
            return characterSlug;
        }

        return GetPortraitSlug(GetType());
    }

    private static string? GetCharacterBasicPortraitSlug(string characterName, string cardTypeName)
    {
        return (characterName, cardTypeName) switch
        {
            ("Ironclad", "ShaCard") => "sha_fire",
            ("Ironclad", "ShanCard") => "shan_ironclad",
            ("Ironclad", "TaoCard") => "tao_ironclad",
            ("Ironclad", "JiuCard") => "jiu_ironclad",
            ("Silent", "ShaCard") => "sha_poison",
            ("Silent", "ShanCard") => "shan_silent",
            ("Silent", "TaoCard") => "tao_silent",
            ("Silent", "JiuCard") => "jiu_silent",
            ("Defect", "ShaCard") => "sha_thunder",
            ("Defect", "ShanCard") => "shan_defect",
            ("Defect", "TaoCard") => "tao_defect",
            ("Defect", "JiuCard") => "jiu_defect",
            ("Necrobinder", "ShaCard") => "sha_calamity",
            ("Necrobinder", "ShanCard") => "shan_necrobinder",
            ("Necrobinder", "TaoCard") => "tao_necrobinder",
            ("Necrobinder", "JiuCard") => "jiu_necrobinder",
            ("Regent", "ShaCard") => "sha_stored",
            ("Regent", "ShanCard") => "shan_regent",
            ("Regent", "TaoCard") => "tao_regent",
            ("Regent", "JiuCard") => "jiu_regent",
            _ => null
        };
    }

    internal static string GetPortraitSlug(Type type)
    {
        var className = type.Name;
        return PortraitSlugs.TryGetValue(className, out var mappedSlug)
            ? mappedSlug
            : className.EndsWith("Card", StringComparison.Ordinal)
                ? className[..^4].ToLowerInvariant()
                : className.ToLowerInvariant();
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.HasPortrait), MethodType.Getter)]
internal static class SanguoshaCardHasPortraitPatch
{
    private static bool Prefix(CardModel __instance, ref bool __result)
    {
        if (__instance is not SanguoshaCard card)
        {
            return true;
        }

        __result = SanguoshaCardPortraitLoader.HasPortrait(card);
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter)]
internal static class SanguoshaCardPortraitPatch
{
    private static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance is not SanguoshaCard card)
        {
            return true;
        }

        var texture = SanguoshaCardPortraitLoader.Load(card);
        if (texture is null)
        {
            return true;
        }

        __result = texture;
        return false;
    }
}

internal static class SanguoshaCardPortraitLoader
{
    private static readonly Dictionary<string, Texture2D> Cache = [];
    private static bool _registered;

    public static void RegisterAll()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        foreach (var slug in SanguoshaCard.PortraitSlugs.Values
                     .Concat(SanguoshaCard.ShaInfusionPortraitSlugs)
                     .Concat(SanguoshaCard.CharacterBasicPortraitSlugs)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Load(slug, warnIfMissing: false);
        }

        Entry.Logger.Info($"Registered {Cache.Count} Sanguosha card portrait resources.");
    }

    public static string GetResourcePath(string slug)
    {
        return $"res://mods/{Entry.ModId}/card_art/{slug}.png";
    }

    public static bool HasPortrait(SanguoshaCard card)
    {
        return Cache.ContainsKey(card.SanguoshaPortraitSlug)
            || GetPortraitFilePaths(card.SanguoshaPortraitSlug).Any(File.Exists);
    }

    public static Texture2D? Load(SanguoshaCard card)
    {
        return Load(card.SanguoshaPortraitSlug, warnIfMissing: true, card.GetType().Name);
    }

    private static Texture2D? Load(string slug, bool warnIfMissing, string? ownerName = null)
    {
        if (Cache.TryGetValue(slug, out var cached))
        {
            return cached;
        }

        foreach (var path in GetPortraitFilePaths(slug))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var image = Image.LoadFromFile(path);
            if (image is null || image.IsEmpty())
            {
                continue;
            }

            var texture = ImageTexture.CreateFromImage(image);
            texture.TakeOverPath(GetResourcePath(slug));
            Cache[slug] = texture;
            return texture;
        }

        if (warnIfMissing)
        {
            Entry.Logger.Warn($"Card portrait not found for {ownerName ?? slug} ({slug}).");
        }

        return null;
    }

    private static IEnumerable<string> GetPortraitFilePaths(string slug)
    {
        var fileName = $"{slug}.png";

        var modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrWhiteSpace(modDir))
        {
            yield return Path.Combine(modDir, "card_art", fileName);
        }

        var globalizedResPath = ProjectSettings.GlobalizePath($"res://mods/{Entry.ModId}/card_art/{fileName}");
        if (!string.IsNullOrWhiteSpace(globalizedResPath))
        {
            yield return globalizedResPath;
        }

        if (!string.IsNullOrWhiteSpace(AppContext.BaseDirectory))
        {
            yield return Path.Combine(AppContext.BaseDirectory, "card_art", fileName);
            yield return Path.Combine(AppContext.BaseDirectory, "mods", Entry.ModId, "card_art", fileName);
        }

        var currentDir = System.Environment.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(currentDir))
        {
            yield return Path.Combine(currentDir, "card_art", fileName);
            yield return Path.Combine(currentDir, "mods", Entry.ModId, "card_art", fileName);
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Frame), MethodType.Getter)]
internal static class SanguoshaCardFramePatch
{
    private static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance is not SanguoshaCard card)
        {
            return true;
        }

        var texture = SanguoshaCardUiTextureLoader.Load(card.CharacterFrameAssetName);
        if (texture is null)
        {
            return true;
        }

        __result = texture;
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.PortraitBorder), MethodType.Getter)]
internal static class SanguoshaCardPortraitBorderPatch
{
    private static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance is not SanguoshaCard card)
        {
            return true;
        }

        var texture = SanguoshaCardUiTextureLoader.Load(card.CharacterPortraitBorderAssetName);
        if (texture is null)
        {
            return true;
        }

        __result = texture;
        return false;
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.EnergyIcon), MethodType.Getter)]
internal static class SanguoshaCardEnergyIconPatch
{
    private static bool Prefix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance is not SanguoshaCard card)
        {
            return true;
        }

        var texture = SanguoshaCardUiTextureLoader.Load(card.CharacterEnergyIconAssetName);
        if (texture is null)
        {
            return true;
        }

        __result = texture;
        return false;
    }
}

internal static class SanguoshaCardUiTextureLoader
{
    private static readonly Dictionary<string, Texture2D> Cache = [];
    private static bool _registered;

    public static void RegisterAll()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        var characters = new[] { "ironclad", "silent", "defect", "necrobinder", "regent" };
        var cardTypes = new[] { "attack", "skill", "power" };

        foreach (var character in characters)
        {
            Load($"energy_{character}", warnIfMissing: false);
            foreach (var cardType in cardTypes)
            {
                Load($"frame_{character}_{cardType}", warnIfMissing: false);
                Load($"portrait_border_{character}_{cardType}", warnIfMissing: false);
            }
        }

        Entry.Logger.Info($"Registered {Cache.Count} Sanguosha card UI resources.");
    }

    public static Texture2D? Load(string? assetName)
    {
        return string.IsNullOrWhiteSpace(assetName)
            ? null
            : Load(assetName, warnIfMissing: true);
    }

    private static Texture2D? Load(string assetName, bool warnIfMissing)
    {
        if (Cache.TryGetValue(assetName, out var cached))
        {
            return cached;
        }

        foreach (var path in GetUiFilePaths(assetName))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var image = Image.LoadFromFile(path);
            if (image is null || image.IsEmpty())
            {
                continue;
            }

            var texture = ImageTexture.CreateFromImage(image);
            texture.TakeOverPath(GetResourcePath(assetName));
            Cache[assetName] = texture;
            return texture;
        }

        if (warnIfMissing)
        {
            Entry.Logger.Warn($"Card UI texture not found for {assetName}.");
        }

        return null;
    }

    private static string GetResourcePath(string assetName)
    {
        return $"res://mods/{Entry.ModId}/card_art/ui/{assetName}.png";
    }

    private static IEnumerable<string> GetUiFilePaths(string assetName)
    {
        var fileName = $"{assetName}.png";

        var modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrWhiteSpace(modDir))
        {
            yield return Path.Combine(modDir, "card_art", "ui", fileName);
        }

        var globalizedResPath = ProjectSettings.GlobalizePath($"res://mods/{Entry.ModId}/card_art/ui/{fileName}");
        if (!string.IsNullOrWhiteSpace(globalizedResPath))
        {
            yield return globalizedResPath;
        }

        if (!string.IsNullOrWhiteSpace(AppContext.BaseDirectory))
        {
            yield return Path.Combine(AppContext.BaseDirectory, "card_art", "ui", fileName);
            yield return Path.Combine(AppContext.BaseDirectory, "mods", Entry.ModId, "card_art", "ui", fileName);
        }

        var currentDir = System.Environment.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(currentDir))
        {
            yield return Path.Combine(currentDir, "card_art", "ui", fileName);
            yield return Path.Combine(currentDir, "mods", Entry.ModId, "card_art", "ui", fileName);
        }
    }
}
