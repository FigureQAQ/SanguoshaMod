using MegaCrit.Sts2.Core.Entities.Cards;
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
}

