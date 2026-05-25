using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using sanguosha.Cards;

namespace sanguosha.Patches;

internal static class SanguoshaCardCatalog
{
    public static IReadOnlyList<CardModel>? TryGetCards()
    {
        try
        {
            return [
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
                ModelDb.Card<DuelCard>(),
                ModelDb.Card<GuDingCard>(),
                ModelDb.Card<GuanShiCard>(),
                ModelDb.Card<GuoHeCard>(),
                ModelDb.Card<HanBingCard>(),
                ModelDb.Card<HuoGongCard>(),
                ModelDb.Card<JieDaoCard>(),
                ModelDb.Card<LeBuCard>(),
                ModelDb.Card<NanManCard>(),
                ModelDb.Card<QiLinCard>(),
                ModelDb.Card<QingGangCard>(),
                ModelDb.Card<RenWangCard>(),
                ModelDb.Card<ShanDianCard>(),
                ModelDb.Card<ShunShouCard>(),
                ModelDb.Card<TaoYuanCard>(),
                ModelDb.Card<TieSuoCard>(),
                ModelDb.Card<WanJianCard>(),
                ModelDb.Card<WuGuCard>(),
                ModelDb.Card<WuXieCard>(),
                ModelDb.Card<GuanXingCard>(),
                ModelDb.Card<KongChengCard>(),
                ModelDb.Card<LongDanCard>(),
                ModelDb.Card<ZhiHengCard>(),
                ModelDb.Card<WuShuangCard>(),
                ModelDb.Card<ZhangBaCard>(),
                ModelDb.Card<ZhuGeCard>()
            ];
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
        var originalList = original.ToList();
        if (originalList.Count == 0 || originalList.All(IsSanguoshaCard) || originalList.All(IsNonCollectible))
        {
            return originalList;
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
            Entry.Logger.Warn("Sanguosha card replacement kept original cards because no Sanguosha card matched the original filter.");
            return originalList;
        }

        var shaped = KeepOriginalCollectibleShape(originalList, filtered);
        if (shaped.Count == 0)
        {
            Entry.Logger.Warn("Sanguosha card replacement kept original cards because replacement candidates did not cover the original card types.");
            return originalList;
        }

        return shaped;
    }

    public static IEnumerable<CardModel> ReplaceStartingDeck(IEnumerable<CardModel> original)
    {
        var originalList = original.ToList();
        if (originalList.Count == 0 || originalList.All(IsSanguoshaCard))
        {
            return originalList;
        }

        var cards = TryGetCards();
        if (cards is null || cards.Count == 0)
        {
            return originalList;
        }

        return originalList.Select(card => ReplaceStartingCard(card, cards)).ToList();
    }

    private static CardModel ReplaceStartingCard(CardModel card, IReadOnlyList<CardModel> cards)
    {
        if (IsSanguoshaCard(card) || IsNonCollectible(card))
        {
            return card;
        }

        if (card.Type == CardType.Attack)
        {
            return cards.OfType<ShaCard>().First();
        }

        if (card.Type == CardType.Skill || card.GainsBlock)
        {
            return cards.OfType<ShanCard>().First();
        }

        return cards.OfType<WuZhongCard>().First();
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

        return rarified.Count > 0 ? rarified : typed;
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
}
