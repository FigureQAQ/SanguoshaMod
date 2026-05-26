# SanguoshaMod

Slay the Spire 2 三国杀主题模组，包含三国杀风格卡牌、装备槽、能力牌、角色技能、本地化与装备图标资源。

## 角色命名

当前角色本地化已改为三国风格姓名，括号内仅保留内部角色 ID 方便排查代码：

- 赤壁猛将（Ironclad）：偏浴血强攻、力量成长和杀连击。
- 锦囊夜行（Silent）：偏锦囊连段、机巧过牌和负面状态。
- 机关卧龙（Defect）：偏机关雷势、星辉积累和群体爆发。
- 青囊魂医（Necrobinder）：偏治疗、魂值、闪避与续航。
- 汉室仁主（Regent）：偏号令、星辉和团队攻防。

## Build

1. Copy `local.props.example` to `local.props`.
2. Update `Sts2Dir` to your local Slay the Spire 2 install path.
3. Build with:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build .\SanguoshaMod.csproj -c Release --no-restore
```

The build target copies the mod DLL, manifest, localization files, and power icon assets into `mods\sanguosha` under the configured game directory.
