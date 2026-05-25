using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using sanguosha.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace sanguosha.Cards;

internal static class SanguoshaCardFx
{
    private static readonly Comparison<CardModel> HandCardOrder = (left, right) =>
        left.EnergyCost.GetResolved().CompareTo(right.EnergyCost.GetResolved());

    public static List<Creature> AliveEnemies(CardPlay play)
    {
        return play.Card.CombatState?.HittableEnemies
            .Where(enemy => enemy.IsAlive)
            .ToList() ?? [];
    }

    public static async Task Attack(PlayerChoiceContext context, CardPlay play, decimal amount)
    {
        var target = play.Target!;
        var isSha = SanguoshaCharacterSkills.IsShaLike(play.Card);
        var finalAmount = isSha
            ? amount + SanguoshaCharacterSkills.GetAttackFlatBonus(play.Card.Owner, target)
            : amount;
        PreparePierceBlock(play, target);
        if (isSha)
        {
            finalAmount *= SanguoshaCharacterSkills.GetAttackDamageMultiplier(play.Card.Owner, target);
        }

        await DamageCmd.Attack(finalAmount)
            .FromCard(play.Card)
            .Targeting(target)
            .Execute(context);
        if (isSha)
        {
            await SanguoshaCharacterSkills.ApplyAttackFollowups(context, play, target);
        }

        var splashMultiplier = isSha ? SanguoshaCharacterSkills.GetAttackSplashMultiplier(play.Card.Owner) : 0m;
        if (splashMultiplier <= 0)
        {
            return;
        }

        var splashTargets = AliveEnemies(play)
            .Where(enemy => enemy != target)
            .ToList();
        foreach (var splashTarget in splashTargets)
        {
            var splashAmount = (amount + SanguoshaCharacterSkills.GetAttackFlatBonus(play.Card.Owner, splashTarget))
                * splashMultiplier;
            PreparePierceBlock(play, splashTarget);
            splashAmount *= SanguoshaCharacterSkills.GetAttackDamageMultiplier(play.Card.Owner, splashTarget);
            await Damage(context, play, splashTarget, splashAmount);
            await SanguoshaCharacterSkills.ApplyAttackFollowups(context, play, splashTarget);
        }
    }

    public static async Task AttackAll(PlayerChoiceContext context, CardPlay play, decimal amount)
    {
        var isSha = SanguoshaCharacterSkills.IsShaLike(play.Card);
        var targets = AliveEnemies(play);
        foreach (var target in targets)
        {
            var finalAmount = isSha
                ? amount + SanguoshaCharacterSkills.GetAttackFlatBonus(play.Card.Owner, target)
                : amount;
            PreparePierceBlock(play, target);
            if (isSha)
            {
                finalAmount *= SanguoshaCharacterSkills.GetAttackDamageMultiplier(play.Card.Owner, target);
            }

            await DamageCmd.Attack(finalAmount)
                .FromCard(play.Card)
                .Targeting(target)
                .Execute(context);
            if (isSha)
            {
                await SanguoshaCharacterSkills.ApplyAttackFollowups(context, play, target);
            }
        }
    }

    public static void PreparePierceBlock(CardPlay play, Creature target)
    {
        if (!SanguoshaCharacterSkills.IsShaLike(play.Card)
            || target.Block <= 0
            || !SanguoshaCharacterSkills.ShouldPierceBlock(play.Card.Owner))
        {
            return;
        }

        target.LoseBlockInternal(target.Block);
    }

    public static Task Damage(PlayerChoiceContext context, CardPlay play, Creature target, decimal amount)
    {
        return amount <= 0 || !target.IsAlive
            ? Task.CompletedTask
            : CreatureCmd.Damage(context, target, amount, ValueProp.Move, play.Card.Owner.Creature, play.Card);
    }

    public static Task Block(CardPlay play, decimal amount)
    {
        return amount <= 0
            ? Task.CompletedTask
            : CreatureCmd.GainBlock(play.Card.Owner.Creature, amount, ValueProp.Move, play, false);
    }

    public static Task Draw(PlayerChoiceContext context, CardPlay play, int amount)
    {
        var player = play.Card.Owner;
        var room = CardPile.MaxCardsInHand - player.PlayerCombatState!.Hand.Cards.Count;
        return amount <= 0 || room <= 0
            ? Task.CompletedTask
            : CardPileCmd.Draw(context, Math.Min(amount, room), player, false);
    }

    public static async Task<IReadOnlyList<CardModel>> ExhaustFromHand(
        PlayerChoiceContext context,
        CardPlay play,
        int maxCards,
        Func<CardModel, bool>? filter = null,
        int minCards = 0)
    {
        var player = play.Card.Owner;
        if (maxCards <= 0 || player.PlayerCombatState!.Hand.Cards.Count == 0)
        {
            return [];
        }

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, minCards, maxCards)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
            Comparison = HandCardOrder
        };

        var selected = (await CardSelectCmd.FromHand(
            context,
            player,
            prefs,
            card => card != play.Card && (filter?.Invoke(card) ?? true),
            play.Card)).ToList();

        foreach (var card in selected)
        {
            await CardCmd.Exhaust(context, card, false, false);
        }

        return selected;
    }

    public static async Task<CardModel?> DiscoverToHand(
        PlayerChoiceContext context,
        CardPlay play,
        IReadOnlyList<CardModel> options,
        int reduceCostBy = 0,
        bool makeFree = false)
    {
        if (options.Count == 0)
        {
            return null;
        }

        var player = play.Card.Owner;
        var picked = await CardSelectCmd.FromChooseACardScreen(context, options, player, false);
        if (picked is null)
        {
            return null;
        }

        var result = await CardPileCmd.AddGeneratedCardToCombat(
            picked,
            PileType.Hand,
            player,
            CardPilePosition.Top);

        if (!result.success || result.cardAdded is null)
        {
            return null;
        }

        if (makeFree)
        {
            result.cardAdded.EnergyCost.SetThisTurn(0, true);
        }
        else if (reduceCostBy > 0)
        {
            result.cardAdded.EnergyCost.AddThisTurn(-reduceCostBy, true);
        }

        return result.cardAdded;
    }

    public static async Task<CardModel?> AddFreeShaToHand(CardPlay play, bool exhaustOnPlay = true)
    {
        var player = play.Card.Owner;
        var result = await CardPileCmd.AddGeneratedCardToCombat(
            ModelDb.Card<ShaCard>(),
            PileType.Hand,
            player,
            CardPilePosition.Top);

        if (!result.success || result.cardAdded is null)
        {
            return null;
        }

        result.cardAdded.EnergyCost.SetThisTurn(0, true);
        result.cardAdded.ExhaustOnNextPlay = exhaustOnPlay;
        return result.cardAdded;
    }

    public static void GainEnergy(CardPlay play, int amount)
    {
        if (amount > 0)
        {
            play.Card.Owner.PlayerCombatState!.GainEnergy(amount);
        }
    }

    public static bool IsDebuffed(Creature creature)
    {
        return creature.Powers.Any(power => power.Type == PowerType.Debuff);
    }

    public static async Task<int> ClearDebuffs(Creature creature)
    {
        var debuffs = creature.Powers.Where(power => power.Type == PowerType.Debuff).ToList();
        foreach (var debuff in debuffs)
        {
            await PowerCmd.Remove(debuff);
        }

        return debuffs.Count;
    }

    public static async Task Heal(CardPlay play, decimal amount)
    {
        var creature = play.Card.Owner.Creature;
        var missing = creature.MaxHp - creature.CurrentHp;
        if (amount > 0 && missing > 0)
        {
            await CreatureCmd.Heal(creature, Math.Min(amount, missing), true);
        }
    }

    public static Task Strength(PlayerChoiceContext context, CardPlay play, decimal amount)
    {
        return amount <= 0
            ? Task.CompletedTask
            : PowerCmd.Apply<StrengthPower>(context, play.Card.Owner.Creature, amount, play.Card.Owner.Creature, play.Card, false);
    }

    public static Task Dexterity(PlayerChoiceContext context, CardPlay play, decimal amount)
    {
        return amount <= 0
            ? Task.CompletedTask
            : PowerCmd.Apply<DexterityPower>(context, play.Card.Owner.Creature, amount, play.Card.Owner.Creature, play.Card, false);
    }

    public static Task Weak(PlayerChoiceContext context, CardPlay play, Creature target, decimal amount)
    {
        return amount <= 0 || !target.IsAlive
            ? Task.CompletedTask
            : PowerCmd.Apply<WeakPower>(context, target, amount, play.Card.Owner.Creature, play.Card, false);
    }

    public static Task Vulnerable(PlayerChoiceContext context, CardPlay play, Creature target, decimal amount)
    {
        return amount <= 0 || !target.IsAlive
            ? Task.CompletedTask
            : PowerCmd.Apply<VulnerablePower>(context, target, amount, play.Card.Owner.Creature, play.Card, false);
    }

    public static Task Poison(PlayerChoiceContext context, CardPlay play, Creature target, decimal amount)
    {
        return amount <= 0 || !target.IsAlive
            ? Task.CompletedTask
            : PowerCmd.Apply<PoisonPower>(context, target, amount, play.Card.Owner.Creature, play.Card, false);
    }

    public static Task Slow(PlayerChoiceContext context, CardPlay play, Creature target, decimal amount)
    {
        return amount <= 0 || !target.IsAlive
            ? Task.CompletedTask
            : PowerCmd.Apply<SlowPower>(context, target, amount, play.Card.Owner.Creature, play.Card, false);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class BaiYinCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Dexterity", 1m),
        new DynamicVar("Heal", 5m),
        new DynamicVar("TurnHeal", 2m)
    ];

    public BaiYinCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateBaiYin(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Dexterity(choiceContext, cardPlay, DynamicVars["Dexterity"].BaseValue);
        await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Dexterity"].UpgradeValueBy(1);
        DynamicVars["Heal"].UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class BingLiangCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Weak", 2m),
        new DynamicVar("Slow", 1m),
        new DamageVar(6, ValueProp.Move),
        new BlockVar(9, ValueProp.Move),
        new DynamicVar("Draw", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    public BingLiangCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        await SanguoshaCardFx.Weak(choiceContext, cardPlay, target, DynamicVars["Weak"].BaseValue);
        await SanguoshaCardFx.Slow(choiceContext, cardPlay, target, DynamicVars["Slow"].BaseValue);
        await SanguoshaCardFx.Damage(choiceContext, cardPlay, target, DynamicVars.Damage.BaseValue);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Weak"].UpgradeValueBy(1);
        DynamicVars["Slow"].UpgradeValueBy(1);
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ChiTuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Strength", 2m),
        new EnergyVar(1)
    ];

    public ChiTuCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateChiTu(cardPlay.Card.Owner);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class DaWanCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Strength", 1m),
        new DynamicVar("BonusDamage", 4m)
    ];

    public DaWanCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateDaWan(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1);
        DynamicVars["BonusDamage"].UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class DiLuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Dexterity", 1m),
        new DynamicVar("SkillDraw", 1m)
    ];

    public DiLuCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateDiLu(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Dexterity(choiceContext, cardPlay, DynamicVars["Dexterity"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Dexterity"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class YuXiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new DynamicVar("Draw", 1m),
        new DynamicVar("Strength", 1m)
    ];

    public YuXiCard() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateYuXi(cardPlay.Card.Owner, IsUpgraded);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class MuNiuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("SkillDraws", 1m),
        new DynamicVar("Draw", 1m),
        new EnergyVar(1)
    ];

    public MuNiuCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateMuNiu(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["SkillDraws"].UpgradeValueBy(1);
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class TaiPingCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Dexterity", 1m),
        new DynamicVar("TurnHeal", 2m),
        new EnergyVar(1)
    ];

    public TaiPingCard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateTaiPing(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Dexterity(choiceContext, cardPlay, DynamicVars["Dexterity"].BaseValue);
        await SanguoshaCardFx.Heal(cardPlay, 3);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Dexterity"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class GuanShiCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Strength", 1m),
        new EnergyVar(1),
        new DynamicVar("BonusDamage", 8m)
    ];
    public GuanShiCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateGuanShi(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BonusDamage"].UpgradeValueBy(4);
        DynamicVars["Strength"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class GuDingCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Strength", 1m)
    ];
    public GuDingCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateGuDing(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class GuoHeCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7, ValueProp.Move),
        new DynamicVar("Weak", 1m),
        new DynamicVar("Vulnerable", 1m),
        new EnergyVar(1),
        new DynamicVar("BlockThreshold", 4m)
    ];

    public GuoHeCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var removedBlock = target.Block;
        if (removedBlock > 0)
        {
            await CreatureCmd.LoseBlock(target, removedBlock);
        }

        await SanguoshaCardFx.Damage(choiceContext, cardPlay, target, DynamicVars.Damage.BaseValue);
        await SanguoshaCardFx.Weak(choiceContext, cardPlay, target, DynamicVars["Weak"].BaseValue);
        await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, target, DynamicVars["Vulnerable"].BaseValue);
        if (removedBlock >= DynamicVars["BlockThreshold"].BaseValue)
        {
            SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, 1);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["Vulnerable"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class HanBingCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Weak", 1m),
        new DynamicVar("Draw", 1m)
    ];
    public HanBingCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateHanBing(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Weak"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class HuoGongCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7, ValueProp.Move),
        new DynamicVar("Poison", 4m),
        new DynamicVar("BonusDamage", 5m)
    ];

    public HuoGongCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var isDebuffed = SanguoshaCardFx.IsDebuffed(target);
        var damage = DynamicVars.Damage.BaseValue + (isDebuffed ? DynamicVars["BonusDamage"].BaseValue : 0);

        await SanguoshaCardFx.Poison(choiceContext, cardPlay, target, DynamicVars["Poison"].BaseValue);
        await SanguoshaCardFx.Damage(choiceContext, cardPlay, target, damage);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["Poison"].UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class JieDaoCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(11, ValueProp.Move),
        new DynamicVar("Strength", 1m),
        new DynamicVar("Slow", 1m),
        new DynamicVar("SelfWeak", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    public JieDaoCard() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        await SanguoshaCardFx.Damage(choiceContext, cardPlay, target, DynamicVars.Damage.BaseValue);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
        await SanguoshaCardFx.Slow(choiceContext, cardPlay, target, DynamicVars["Slow"].BaseValue);
        await SanguoshaCardFx.Weak(choiceContext, cardPlay, target, DynamicVars["SelfWeak"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["Strength"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class LeBuCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Slow", 3m),
        new DynamicVar("Vulnerable", 1m),
        new DamageVar(7, ValueProp.Move),
        new BlockVar(13, ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    public LeBuCard() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        await SanguoshaCardFx.Slow(choiceContext, cardPlay, target, DynamicVars["Slow"].BaseValue);
        await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, target, DynamicVars["Vulnerable"].BaseValue);
        await SanguoshaCardFx.Damage(choiceContext, cardPlay, target, DynamicVars.Damage.BaseValue);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Slow"].UpgradeValueBy(1);
        DynamicVars.Block.UpgradeValueBy(4);
        DynamicVars["Vulnerable"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class NanManCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(14, ValueProp.Move),
        new DynamicVar("Weak", 2m),
        new DynamicVar("Heal", 4m)
    ];
    public NanManCard() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var targets = SanguoshaCardFx.AliveEnemies(cardPlay);
        foreach (var target in targets)
        {
            await SanguoshaCardFx.Weak(choiceContext, cardPlay, target, DynamicVars["Weak"].BaseValue);
        }

        await SanguoshaCardFx.AttackAll(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        var killedAny = targets.Any(enemy => !enemy.IsAlive);
        await SanguoshaCardFx.Heal(cardPlay, killedAny ? DynamicVars["Heal"].BaseValue : 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5);
        DynamicVars["Weak"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class QiLinCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Strength", 1m),
        new DynamicVar("Draw", 1m),
        new DynamicVar("BonusDamage", 4m),
        new DynamicVar("Vulnerable", 1m)
    ];
    public QiLinCard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateQiLin(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1);
        DynamicVars["BonusDamage"].UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class QingGangCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1)
    ];
    public QingGangCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SanguoshaCharacterSkills.ActivateQingGang(cardPlay.Card.Owner, IsUpgraded);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ShanDianCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12, ValueProp.Move),
        new DynamicVar("Vulnerable", 2m),
        new DynamicVar("MinDamage", 8m),
        new DynamicVar("MaxDamage", 18m),
        new BlockVar(8, ValueProp.Move)
    ];
    public ShanDianCard() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var rand = new System.Random();
        var min = (int)DynamicVars["MinDamage"].BaseValue;
        var max = (int)DynamicVars["MaxDamage"].BaseValue;
        var actualDamage = rand.Next(min, max + 1);

        await SanguoshaCardFx.Vulnerable(choiceContext, cardPlay, target, DynamicVars["Vulnerable"].BaseValue);
        await SanguoshaCardFx.Damage(choiceContext, cardPlay, target, actualDamage);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MinDamage"].UpgradeValueBy(3);
        DynamicVars["MaxDamage"].UpgradeValueBy(4);
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class TieSuoCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Slow", 2m),
        new BlockVar(8, ValueProp.Move),
        new EnergyVar(1)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [] : [CardKeyword.Exhaust];
    public TieSuoCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        SanguoshaCharacterSkills.ActivateTieSuo(cardPlay.Card.Owner, IsUpgraded);
        await SanguoshaCardFx.Slow(choiceContext, cardPlay, target, DynamicVars["Slow"].BaseValue);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4);
        DynamicVars["Slow"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class ShunShouCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7, ValueProp.Move),
        new BlockVar(8, ValueProp.Move),
        new EnergyVar(1)
    ];
    public ShunShouCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Damage(choiceContext, cardPlay, cardPlay.Target!, DynamicVars.Damage.BaseValue);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, 1);
        if (cardPlay.Target!.Block <= 0)
        {
            SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class TaoYuanCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Heal", 10m),
        new BlockVar(16, ValueProp.Move),
        new EnergyVar(1),
        new DynamicVar("Draw", 1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    public TaoYuanCard() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SanguoshaCardFx.Heal(cardPlay, DynamicVars["Heal"].BaseValue);
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
        await SanguoshaCardFx.Draw(choiceContext, cardPlay, DynamicVars["Draw"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(4);
        DynamicVars.Block.UpgradeValueBy(5);
        DynamicVars["Draw"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class WanJianCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(11, ValueProp.Move),
        new DynamicVar("Strength", 1m),
        new BlockVar(8, ValueProp.Move)
    ];
    public WanJianCard() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var targets = SanguoshaCardFx.AliveEnemies(cardPlay);
        await SanguoshaCardFx.AttackAll(choiceContext, cardPlay, DynamicVars.Damage.BaseValue);
        foreach (var target in targets.Where(enemy => enemy.IsAlive))
        {
            await SanguoshaCardFx.Strength(choiceContext, cardPlay, DynamicVars["Strength"].BaseValue);
        }
        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class WuXieCard : SanguoshaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(11, ValueProp.Move),
        new DynamicVar("CleanseBlock", 6m),
        new EnergyVar(1)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [] : [CardKeyword.Exhaust];
    public WuXieCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cleared = await SanguoshaCardFx.ClearDebuffs(cardPlay.Card.Owner.Creature);
        if (cleared > 0)
        {
            await SanguoshaCardFx.Block(cardPlay, DynamicVars["CleanseBlock"].BaseValue * cleared);
            SanguoshaCardFx.GainEnergy(cardPlay, DynamicVars.Energy.IntValue);
            if (IsUpgraded)
            {
                await SanguoshaCardFx.Draw(choiceContext, cardPlay, 1);
            }
            return;
        }

        await SanguoshaCardFx.Block(cardPlay, DynamicVars.Block.BaseValue);
        if (IsUpgraded)
        {
            await SanguoshaCardFx.Draw(choiceContext, cardPlay, 1);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.SetCustomBaseCost(0);
        DynamicVars.Block.UpgradeValueBy(3);
        DynamicVars["CleanseBlock"].UpgradeValueBy(2);
    }
}

// ========== 兵粮寸断: 虚弱 + 减速 + 伤害（断粮控制） ==========


