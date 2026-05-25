# SanguoshaMod

Slay the Spire 2 三国杀主题模组，包含三国杀风格卡牌、装备槽、能力牌、角色技能、本地化与装备图标资源。

## Build

1. Copy `local.props.example` to `local.props`.
2. Update `Sts2Dir` to your local Slay the Spire 2 install path.
3. Build with:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build .\SanguoshaMod.csproj -c Release --no-restore
```

The build target copies the mod DLL, manifest, localization files, and power icon assets into `mods\sanguosha` under the configured game directory.
