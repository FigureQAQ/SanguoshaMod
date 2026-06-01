using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
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
                ModelDb.Card<YangGongCard>(),
                ModelDb.Card<XuShiCard>(),
                ModelDb.Card<PoZhenCard>(),
                ModelDb.Card<BuFangCard>(),
                ModelDb.Card<FenChengCard>(),
                ModelDb.Card<FenYingCard>(),
                ModelDb.Card<JiXingCard>(),
                ModelDb.Card<KuRouCard>(),
                ModelDb.Card<RenDeCard>(),
                ModelDb.Card<PoZhuCard>(),
                ModelDb.Card<TuXiCard>(),
                ModelDb.Card<QiXiCard>(),
                ModelDb.Card<GuaGuCard>(),
                ModelDb.Card<HongBaoCard>(),
                ModelDb.Card<HongBaoGiftCard>(),
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
                ModelDb.Card<YingZiCard>(),
                ModelDb.Card<JiZhiCard>(),
                ModelDb.Card<LuoYiCard>(),
                ModelDb.Card<TieQiCard>(),
                ModelDb.Card<QingNangCard>(),
                ModelDb.Card<XiaoJiCard>(),
                ModelDb.Card<BathOfBloodCard>(),
                ModelDb.Card<NightfallSchemeCard>(),
                ModelDb.Card<ThunderMandateCard>(),
                ModelDb.Card<SoulHealerFormCard>(),
                ModelDb.Card<ImperialEdictCard>(),
                ModelDb.Card<CrimsonRaidCard>(),
                ModelDb.Card<VenomAmbushCard>(),
                ModelDb.Card<ThunderRelayCard>(),
                ModelDb.Card<SoulRansomCard>(),
                ModelDb.Card<EdictReserveCard>(),
                ModelDb.Card<ZhangBaCard>(),
                ModelDb.Card<ZhangBaShaCard>(),
                ModelDb.Card<ZhuGeCard>()
            };

            return includeGeneratedOnly
                ? cards.OrderBy(StableCardKey, StringComparer.Ordinal).ToList()
                : cards.Where(card => !IsGeneratedOnlyCard(card))
                    .OrderBy(StableCardKey, StringComparer.Ordinal)
                    .ToList();
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Sanguosha card catalog is not ready yet: {ex.Message}");
            return null;
        }
    }

    public static IReadOnlyList<CardModel>? TryGetRewardCards(Player? player = null)
    {
        var cards = TryGetCards();
        return cards?
            .Where(card => IsRewardEligible(card, player))
            .OrderBy(StableCardKey, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<CardModel>? TryGetBossRewardCards(Player player)
    {
        var cards = TryGetCards();
        if (cards is null)
        {
            return null;
        }

        var bossCards = cards
            .Where(card => IsBossRewardCardForCharacter(card, player))
            .OrderBy(StableCardKey, StringComparer.Ordinal)
            .ToList();
        if (bossCards.Count == 0)
        {
            return bossCards;
        }

        var rareRewardCards = cards
            .Where(card => IsRewardEligible(card, player))
            .Where(card => card.Rarity == CardRarity.Rare)
            .OrderBy(StableCardKey, StringComparer.Ordinal)
            .ToList();
        var targetCount = Math.Max(3, bossCards.Count);
        foreach (var card in rareRewardCards)
        {
            if (bossCards.Count >= targetCount)
            {
                break;
            }

            bossCards.Add(card);
        }

        if (bossCards.Count >= targetCount)
        {
            return bossCards;
        }

        var regularRewardCards = cards
            .Where(card => IsRewardEligible(card, player))
            .OrderBy(StableCardKey, StringComparer.Ordinal)
            .ToList();
        foreach (var card in regularRewardCards)
        {
            if (bossCards.Count >= targetCount)
            {
                break;
            }

            if (!bossCards.Contains(card))
            {
                bossCards.Add(card);
            }
        }

        return bossCards;
    }

    public static IEnumerable<CardModel> ReplaceGeneratedCards(
        IEnumerable<CardModel> original,
        Player player,
        CardCreationOptions options)
    {
        var originalList = TrySnapshot(original);
        if (originalList is null)
        {
            return original;
        }

        if (originalList.All(IsNonCollectible))
        {
            return originalList.OrderBy(StableCardKey, StringComparer.Ordinal).ToList();
        }

        if (IsBossEncounterReward(options))
        {
            var bossCards = TryGetBossRewardCards(player);
            if (bossCards is { Count: > 0 })
            {
                return bossCards.OrderBy(StableCardKey, StringComparer.Ordinal).ToList();
            }
        }

        if (originalList.All(IsSanguoshaCard))
        {
            var rewardOriginal = originalList
                .Where(card => IsRewardEligible(card, player))
                .ToList();
            if (rewardOriginal.Count > 0)
            {
                return rewardOriginal.OrderBy(StableCardKey, StringComparer.Ordinal).ToList();
            }

            var rewardCards = TryGetRewardCards(player);
            if (rewardCards is { Count: > 0 })
            {
                var shapedRewardCards = KeepOriginalCollectibleShape(originalList, rewardCards);
                return shapedRewardCards.Count > 0 ? shapedRewardCards : rewardCards;
            }

            return originalList.OrderBy(StableCardKey, StringComparer.Ordinal).ToList();
        }

        var cards = IsBossEncounterReward(options)
            ? TryGetBossRewardCards(player)
            : TryGetRewardCards(player);
        if (cards is null || cards.Count == 0)
        {
            return originalList;
        }

        var filtered = options.CardPoolFilter is null
            ? cards.ToList()
            : cards.Where(card => SafeMatches(options.CardPoolFilter, card)).ToList();

        if (filtered.Count == 0)
        {
            Entry.Logger.Warn("Sanguosha card replacement ignored an incompatible original card filter and used the reward Sanguosha pool.");
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

    public static bool IsBossRewardOnlyCard(CardModel card)
    {
        return card is BathOfBloodCard
            or NightfallSchemeCard
            or ThunderMandateCard
            or SoulHealerFormCard
            or ImperialEdictCard;
    }

    public static IReadOnlyList<CardModel> KeepRewardEligibleCards(Player player, IReadOnlyList<CardModel> cards)
    {
        var filtered = cards
            .Where(card => IsRewardEligible(card, player))
            .ToList();
        if (filtered.Count > 0)
        {
            return filtered.OrderBy(StableCardKey, StringComparer.Ordinal).ToList();
        }

        var rewardCards = TryGetRewardCards(player);
        if (rewardCards is not { Count: > 0 })
        {
            return cards;
        }

        var shapedRewardCards = KeepOriginalCollectibleShape(cards, rewardCards);
        return shapedRewardCards.Count > 0 ? shapedRewardCards : rewardCards;
    }

    private static bool IsSanguoshaCard(CardModel card)
    {
        return card is SanguoshaCard;
    }

    private static bool IsGeneratedOnlyCard(CardModel card)
    {
        return card is ZhangBaShaCard or HongBaoGiftCard;
    }

    private static bool IsRewardEligible(CardModel card, Player? player)
    {
        if (IsSanguoshaBasicCard(card)
            || IsGeneratedOnlyCard(card)
            || IsBossRewardOnlyCard(card))
        {
            return false;
        }

        if (player is not null)
        {
            if (IsCharacterSpecificCard(card) && !IsCharacterSpecificCardForPlayer(card, player))
            {
                return false;
            }

            if (IsMultiplayerOnlyCard(card) && !IsMultiplayerRun(player))
            {
                return false;
            }

            if (IsEquipmentCard(card) && OwnsSameEquipment(player, card))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsMultiplayerOnlyCard(CardModel card)
    {
        return card is HongBaoCard or TaoYuanCard or WuGuCard;
    }

    private static bool IsMultiplayerRun(Player player)
    {
        return player.RunState.Players.Count(runPlayer => runPlayer.PlayerCombatState is not null || runPlayer.Deck is not null) > 1;
    }

    private static bool IsEquipmentCard(CardModel card)
    {
        return card is ZhuGeCard or ZhangBaCard or QingGangCard or GuanShiCard or HanBingCard
            or QiLinCard or GuDingCard or GanJiangMoYeCard
            or BaiYinCard or RenWangCard or BaGuaCard or TengJiaCard
            or ChiTuCard or DaWanCard or DiLuCard or JueYingCard
            or YuXiCard or MuNiuCard or TaiPingCard or MengDeXinShuCard;
    }

    private static bool OwnsSameEquipment(Player player, CardModel card)
    {
        var cardType = card.GetType();
        return player.Deck.Cards.Any(deckCard => deckCard.GetType() == cardType);
    }

    private static bool IsCharacterSpecificCard(CardModel card)
    {
        return card is CrimsonRaidCard
            or VenomAmbushCard
            or ThunderRelayCard
            or SoulRansomCard
            or EdictReserveCard;
    }

    private static bool IsCharacterSpecificCardForPlayer(CardModel card, Player player)
    {
        return player.Character.GetType().Name switch
        {
            "Ironclad" => card is CrimsonRaidCard,
            "Silent" => card is VenomAmbushCard,
            "Defect" => card is ThunderRelayCard,
            "Necrobinder" => card is SoulRansomCard,
            "Regent" => card is EdictReserveCard,
            _ => false
        };
    }

    private static bool IsBossEncounterReward(CardCreationOptions options)
    {
        return options.RarityOdds == CardRarityOddsType.BossEncounter
            || options.Source.ToString().Contains("Boss", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBossRewardCardForCharacter(CardModel card, Player player)
    {
        return player.Character.GetType().Name switch
        {
            "Ironclad" => card is BathOfBloodCard,
            "Silent" => card is NightfallSchemeCard,
            "Defect" => card is ThunderMandateCard,
            "Necrobinder" => card is SoulHealerFormCard,
            "Regent" => card is ImperialEdictCard,
            _ => false
        };
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
