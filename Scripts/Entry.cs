using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Models;
using sanguosha.Cards;
using sanguosha.Relics;

namespace sanguosha;

/// <summary>
/// 三国杀Mod入口 — 替换 Ironclad/Silent/Defect/Necrobinder/Regent 五位角色机制。
/// 30张三国杀卡牌 = 五角色共享Mod牌池。
///
/// Ironclad    — 士气/酒意
/// Silent      — 计策
/// Defect      — 雷引
/// Necrobinder — 魂
/// Regent      — 星
/// </summary>
public static class Entry
{
    [ModInitializer]
    public static void Init()
    {
        RitsuLibFramework.EnsureGodotScriptsRegistered();
        ModTypeDiscoveryHub.RegisterModAssembly(typeof(Entry).Assembly);
    }
}
