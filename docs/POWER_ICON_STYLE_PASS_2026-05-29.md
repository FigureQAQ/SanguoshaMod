# 2026-05-29 装备效果图标风格统一

## 问题

装备/能力 Power 图标混用了两种视觉来源：

- 部分图标是简易符号画风。
- 部分图标是写实牌面裁切。

放在角色状态栏时视觉重量不统一。

## 调整

- 新增 `tools/generate_power_icons.py`，从 `card_art` 的牌面插画重新导出 Power 图标。
- 统一导出 `power_icons` 的 64x64 小图标。
- 统一导出 `power_icons_big` 的 256x256 大图标。
- 图标统一采用圆形铜框、暗色背景、轻微暖色调和插画裁切。
- 已覆盖当前所有装备图标，以及复用同套 Power UI 的能力图标。

## 维护方式

更新任意 `card_art/*.png` 后，可运行：

```powershell
python E:\DGodotProjectsTestCSharp\tools\generate_power_icons.py
```

脚本会重新生成状态栏小图标和悬停大图标，避免后续新增装备时再次出现风格混用。
