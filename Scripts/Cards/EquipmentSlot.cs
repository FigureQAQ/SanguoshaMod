using STS2RitsuLib.Scaffolding.Content;

namespace sanguosha.Cards;

/// <summary>
/// 装备槽位类型 — 1武器+1防具+2马
/// </summary>
public enum EquipmentSlot { Weapon, Armor, MountPlus, MountMinus }

/// <summary>
/// 装备Buff基类 — 同槽自动替换
/// </summary>
public abstract class EquipmentBuff : ModPowerTemplate
{
    public abstract EquipmentSlot Slot { get; }
    public override void OnInitialApplication()
    {
        var old = Owner.Powers.OfType<EquipmentBuff>()
            .FirstOrDefault(p => p != this && p.Slot == Slot);
        if (old != null)
            PowerCmd.RemovePower(Owner, old);
    }
}

/// <summary>
/// 雷引 — 延时Debuff。下回合开始造成闪电伤害(16/21)。
/// 等待期间首次被施加Debuff时先触发半额(8/10)（一次）。
/// 目标死亡→剩余伤害转移给随机敌人(12)。
/// </summary>
[RegisterPower]
public class LeYinBuff : ModPowerTemplate
{
    public bool HalfTriggered { get; set; }
    public int TransferredDamage { get; set; } = 12;
    public LeYinBuff() { IsDebuff = true; }

    public override void AtStartOfTurn()
    {
        PowerCmd.DealDamage(null!, Owner, Amount, DamageType.Lightning);
        PowerCmd.RemovePower(Owner, this);
    }

    public override void OnApplyPower(Power power, AbstractCreature source)
    {
        if (!HalfTriggered && power.IsDebuff && power != this)
        {
            HalfTriggered = true;
            PowerCmd.DealDamage(source, Owner, Amount / 2, DamageType.Lightning);
        }
    }

    /// <summary>
    /// 目标死亡时转移雷引 → 随机敌人受到TransferredDamage闪电伤害
    /// </summary>
    public override void OnDeath()
    {
        var enemies = AbstractDungeon.GetMonsters().Monsters
            .Where(m => m != Owner && !m.IsDead && !m.IsDying).ToList();
        if (enemies.Count > 0)
        {
            var target = enemies[AbstractDungeon.CardRng.Random.Next(enemies.Count)];
            PowerCmd.DealDamage(Owner, target, TransferredDamage, DamageType.Lightning);
        }
    }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "雷引", Description = $"下回合开始受{Amount}闪电伤。施加Debuff时半额先触发{Amount / 2}(一次)。死亡转移12伤。" };
}

/// <summary>
/// 乐不 — 敌方Debuff。下回合攻击意图时：取消攻击+施加2虚弱+1易伤。
/// 没有攻击意图则：施加1虚弱+抽1。
/// </summary>
[RegisterPower]
public class LeBuBuff : ModPowerTemplate
{
    public LeBuBuff() { IsDebuff = true; }
    public bool Triggered { get; set; }

    public override void AtStartOfTurn(AbstractCreature owner)
    {
        if (owner is AbstractMonster monster)
        {
            if (monster.GetIntentDmg() > 0 && !Triggered)
            {
                // 取消攻击
                monster.SkipTurn = true;
                Triggered = true;
                PowerCmd.Apply<VulnerablePower>(null!, owner, 2, null!);
                PowerCmd.Apply<WeakPower>(null!, owner, 1, null!);
            }
            else if (!Triggered)
            {
                // 没有攻击意图
                Triggered = true;
                PowerCmd.Apply<WeakPower>(null!, owner, 1, null!);
                // 抽1张牌
                AbstractDungeon.Player.Draw(1);
            }
        }
        if (Triggered) PowerCmd.RemovePower(owner, this);
    }

    public override PowerStrings GetPowerStrings() => new()
    {
        Name = "乐不",
        Description = "下次攻击意图→取消攻击+2虚弱+1易伤；无攻击→1虚弱+抽1。"
    };
}

/// <summary>
/// 回收锁 — 收回手牌并带有虚无（回合结束未使用则消耗）的标记
/// </summary>
[RegisterPower]
public class RecycleBuff : ModPowerTemplate
{
    public bool IsEthereal { get; set; } = true;
    public RecycleBuff() { IsDebuff = false; }

    public override void AtEndOfTurn()
    {
        if (IsEthereal)
            CardCmd.ExhaustCard(Owner, Owner.Hand.FirstOrDefault(c =>
                c.Powers.Any(p => p is RecycleBuff)));
    }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "回收锁", Description = "虚无：回合结束未使用则消耗。" };
}

/// <summary>
/// 临时能量标记 — 赤兔等临时能量来源
/// </summary>
[RegisterPower]
public class TempEnergyBuff : ModPowerTemplate
{
    public TempEnergyBuff() { IsDebuff = false; }

    public override void AtStartOfTurn()
    {
        AbstractDungeon.Player.GainEnergy(Amount);
        PowerCmd.RemovePower(AbstractDungeon.Player, this);
    }

    public override PowerStrings GetPowerStrings() => new()
    { Name = "临时能量", Description = $"回合开始额外{Amount}临时能量。" };
}
