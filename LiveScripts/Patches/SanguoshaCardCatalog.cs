using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using sanguosha.Cards;

namespace sanguosha.Patches;

internal static class SanguoshaCardCatalog
{
    public static IReadOnlyList<CardModel>? TryGetCards(bool includeGeneratedOnly = false)
    {
        try
        {
            var cards = new List<CardModel>
            {
                ModelDb.Card<BaGuaCard>(),
                ModelDb.Card<BaiYinCard>(),
                ModelDb.Card<BingLiangCard>(),
                ModelDb.Card<ChiTuCard>(),
                ModelDb.Card<DaWanCard>(),
                ModelDb.Card<DiLuCard>(),
                ModelDb.Card<YuXiCard>(),
                ModelDb.Card<MuNiuCard>(),
                ModelDb.Card<TaiPingCard>(),
                ModelDb.Card<ShaCard>(),
                ModelDb.Card<ShanCard>(),
                ModelDb.Card<WuZhongCard>(),
                ModelDb.Card<TaoCard>(),
                ModelDb.Card<JiuCard>(),
                ModelDb.Card<CiShaCard>(),
                ModelDb.Card<ShouShiCard>(),
                ModelDb.Card<DiaoDuCard>(),
                ModelDb.Card<FenChengCard>(),
                ModelDb.Card<RenDeCard>(),
                ModelDb.Card<TuXiCard>(),
                ModelDb.Card<QiXiCard>(),
                ModelDb.Card<GuaGuCard>(),
                ModelDb.Card<DuelCard>(),
                ModelDb.Card<GuDingCard>(),
                ModelDb.Card<GanJiangMoYeCard>(),
                ModelDb.Card<GuanShiCard>(),
                ModelDb.Card<GuoHeCard>(),
                ModelDb.Card<HanBingCard>(),
                ModelDb.Card<HuoGongCard>(),
                ModelDb.Card<JieDaoCard>(),
                ModelDb.Card<JueYingCard>(),
                ModelDb.Card<LeBuCard>(),
                ModelDb.Card<MengDeXinShuCard>(),
                ModelDb.Card<NanManCard>(),
                ModelDb.Card<QiLinCard>(),
                ModelDb.Card<QingGangCard>(),
                ModelDb.Card<RenWangCard>(),
                ModelDb.Card<ShanDianCard>(),
                ModelDb.Card<ShunShouCard>(),
                ModelDb.Card<TaoYuanCard>(),
                ModelDb.Card<TengJiaCard>(),
                ModelDb.Card<TieSuoCard>(),
                ModelDb.Card<WanJianCard>(),
                ModelDb.Card<WuGuCard>(),
                ModelDb.Card<WuXieCard>(),
                ModelDb.Card<GuanXingCard>(),
                ModelDb.Card<KongChengCard>(),
                ModelDb.Card<LongDanCard>(),
                ModelDb.Card<ZhiHengCard>(),
                ModelDb.Card<WuShuangCard>(),
                ModelDb.Card<LianYingCard>(),
                ModelDb.Card<YiJiCard>(),
                ModelDb.Card<JianXiongCard>(),
                ModelDb.Card<GuiCaiCard>(),
                ModelDb.Card<ZhangBaCard>(),
                ModelDb.Card<ZhangBaShaCard>(),
                ModelDb.Card<WangJianShaCard>(),
                ModelDb.Card<ZhuGeCard>()
            };

            return includeGeneratedOnly
                ? cards.OrderBy(StableCardKey, StringComparer.Ordinal).ToList()
                : cards.Where(card => card is not ZhangBaShaCard and not WangJianShaCard)
                    .OrderBy(StableCardKey, StringComparer.Ordinal)
                    .ToList();
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Sanguosha card catalog is not ready yet: {ex.Message}");
            return null;
        }
    }

    public static IEnumerable<CardModel> ReplaceGeneratedCards(
        IEnumerable<CardModel> original,
        Func<CardModel, bool>? originalFilter = null)
    {
        var originalList = TrySnapshot(original);
        if (originalList is null)
        {
            return original;
        }

        if (originalList.All(IsSanguoshaCard) || originalList.All(IsNonCollectible))
        {
            return originalList.OrderBy(StableCardKey, StringComparer.Ordinal).ToList();
        }

        var cards = TryGetCards();
        if (cards is null || cards.Count == 0)
        {
            return originalList;
        }

        var filtered = originalFilter is null
            ? cards.ToList()
            : cards.Where(card => SafeMatches(originalFilter, card)).ToList();

        if (filtered.Count == 0)
        {
            Entry.Logger.Warn("Sanguosha card replacement ignored an incompatible original card filter and used the full Sanguosha pool.");
            filtered = cards.ToList();
        }

        if (originalList.Count == 0)
        {
            return filtered.OrderBy(StableCardKey, StringComparer.Ordinal).ToList();
        }

        var shaped = KeepOriginalCollectibleShape(originalList, filtered);
        if (shaped.Count == 0)
        {
            Entry.Logger.Warn("Sanguosha card replacement kept the filtered Sanguosha pool because the original pool shape was incompatible.");
            shaped = filtered;
        }

        return shaped.OrderBy(StableCardKey, StringComparer.Ordinal).ToList();
    }

    public static IEnumerable<CardModel> ReplaceAllCards(IEnumerable<CardModel> original)
    {
        var originalList = TrySnapshot(original);
        if (originalList is null)
        {
            return original;
        }

        if (originalList.Count == 0 || originalList.All(IsSanguoshaCard))
        {
            return originalList.OrderBy(StableCardKey, StringComparer.Ordinal).ToList();
        }

        var cards = TryGetCards(includeGeneratedOnly: true);
        return cards is null || cards.Count == 0
            ? originalList
            : cards;
    }

    public static IEnumerable<CardModel> ReplaceStartingDeck(IEnumerable<CardModel> original)
    {
        var originalList = TrySnapshot(original);
        if (originalList is null)
        {
            return original;
        }

        if (originalList.Count == 0 || originalList.All(IsSanguoshaCard))
        {
            return originalList;
        }

        var cards = TryGetCards();
        if (cards is null || cards.Count == 0)
        {
            return originalList;
        }

        var replacedAttackWithTao = false;
        var replacedSkillWithJiu = false;
        return originalList.Select(card =>
            ReplaceStartingCard(
                card,
                cards,
                ref replacedAttackWithTao,
                ref replacedSkillWithJiu)).ToList();
    }

    private static CardModel ReplaceStartingCard(
        CardModel card,
        IReadOnlyList<CardModel> cards,
        ref bool replacedAttackWithTao,
        ref bool replacedSkillWithJiu)
    {
        if (IsSanguoshaCard(card) || IsNonCollectible(card))
        {
            return card;
        }

        if (card.Type == CardType.Attack)
        {
            if (!replacedAttackWithTao)
            {
                replacedAttackWithTao = true;
                return cards.OfType<TaoCard>().First();
            }

            return cards.OfType<ShaCard>().First();
        }

        if (card.Type == CardType.Skill || card.GainsBlock)
        {
            if (!replacedSkillWithJiu)
            {
                replacedSkillWithJiu = true;
                return cards.OfType<JiuCard>().First();
            }

            return cards.OfType<ShanCard>().First();
        }

        return cards.OfType<WuZhongCard>().First();
    }

    public static bool IsSanguoshaBasicCard(CardModel card)
    {
        return card is ShaCard or ShanCard or TaoCard or JiuCard;
    }

    private static bool IsSanguoshaCard(CardModel card)
    {
        return card is SanguoshaCard;
    }

    private static bool IsNonCollectible(CardModel card)
    {
        return card.Rarity is CardRarity.Status or CardRarity.Curse or CardRarity.Quest
            || card.Type is CardType.Status or CardType.Curse or CardType.Quest;
    }

    private static bool IsReplaceableCollectible(CardModel card)
    {
        return !IsNonCollectible(card)
            && card.Type is CardType.Attack or CardType.Skill or CardType.Power;
    }

    private static IReadOnlyList<CardModel> KeepOriginalCollectibleShape(
        IReadOnlyList<CardModel> original,
        IReadOnlyList<CardModel> replacement)
    {
        var collectibleOriginal = original.Where(IsReplaceableCollectible).ToList();
        if (collectibleOriginal.Count == 0)
        {
            return [];
        }

        var originalTypes = collectibleOriginal.Select(card => card.Type).Distinct().ToList();
        if (originalTypes.Any(type => replacement.All(card => card.Type != type)))
        {
            return [];
        }

        var typed = replacement.Where(card => originalTypes.Contains(card.Type)).ToList();
        var originalRarities = collectibleOriginal.Select(card => card.Rarity).Distinct().ToList();
        var rarified = typed.Where(card => originalRarities.Contains(card.Rarity)).ToList();

        var shaped = rarified.Count > 0 ? rarified : typed;
        return shaped.OrderBy(StableCardKey, StringComparer.Ordinal).ToList();
    }

    private static bool SafeMatches(Func<CardModel, bool> filter, CardModel card)
    {
        try
        {
            return filter(card);
        }
        catch
        {
            return false;
        }
    }

    private static List<CardModel>? TrySnapshot(IEnumerable<CardModel> cards)
    {
        try
        {
            return cards.ToList();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("You monster", StringComparison.OrdinalIgnoreCase))
        {
            Entry.Logger.Warn("Skipped Sanguosha card replacement for an internal mock card pool.");
            return null;
        }
    }

    private static string StableCardKey(CardModel card)
    {
        return $"{(int)card.Type:D2}:{(int)card.Rarity:D2}:{card.GetType().FullName}";
    }
}
