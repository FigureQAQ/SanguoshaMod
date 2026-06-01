using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Relics;

public abstract class SanguoshaSkillRelic(string iconBaseName) : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool IsAllowedInShops => false;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: ImageHelper.GetImagePath($"atlases/relic_atlas.sprites/{iconBaseName}.tres"),
        IconOutlinePath: ImageHelper.GetImagePath($"atlases/relic_outline_atlas.sprites/{iconBaseName}.tres"),
        BigIconPath: ImageHelper.GetImagePath($"relics/{iconBaseName}.png"));
}

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class IroncladSkillRelic() : SanguoshaSkillRelic("burning_blood");

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class SilentSkillRelic() : SanguoshaSkillRelic("ring_of_the_snake");

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class DefectSkillRelic() : SanguoshaSkillRelic("cracked_core");

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class NecrobinderSkillRelic() : SanguoshaSkillRelic("bound_phylactery");

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class RegentSkillRelic() : SanguoshaSkillRelic("divine_right");
