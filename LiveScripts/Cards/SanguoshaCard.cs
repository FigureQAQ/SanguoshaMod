using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
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
        ["WuGuCard"] = "wugu",
        ["WuShuangCard"] = "wushuang",
        ["WuXieCard"] = "wuxie",
        ["WuZhongCard"] = "wuzhong",
        ["YiJiCard"] = "yiji",
        ["YuXiCard"] = "yuxi",
        ["ZhangBaCard"] = "zhangba",
        ["ZhangBaShaCard"] = "sha",
        ["ZhiHengCard"] = "zhiheng",
        ["ZhuGeCard"] = "zhuge"
    };

    public override string CustomPortraitPath => GetModPortraitPath();
    public override string CustomBetaPortraitPath => GetModPortraitPath();

    internal string SanguoshaPortraitSlug => GetPortraitSlug(GetType());

    private string GetModPortraitPath()
    {
        return $"res://mods/{Entry.ModId}/card_art/{SanguoshaPortraitSlug}.png";
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

    public static bool HasPortrait(SanguoshaCard card)
    {
        return File.Exists(GetPortraitFilePath(card));
    }

    public static Texture2D? Load(SanguoshaCard card)
    {
        var slug = card.SanguoshaPortraitSlug;
        if (Cache.TryGetValue(slug, out var cached))
        {
            return cached;
        }

        var path = GetPortraitFilePath(card);
        if (!File.Exists(path))
        {
            return null;
        }

        var image = Image.LoadFromFile(path);
        if (image is null || image.IsEmpty())
        {
            return null;
        }

        var texture = ImageTexture.CreateFromImage(image);
        Cache[slug] = texture;
        return texture;
    }

    private static string GetPortraitFilePath(SanguoshaCard card)
    {
        var modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return string.IsNullOrWhiteSpace(modDir)
            ? string.Empty
            : Path.Combine(modDir, "card_art", $"{card.SanguoshaPortraitSlug}.png");
    }
}
