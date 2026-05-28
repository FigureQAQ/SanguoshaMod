# 本地牌面生成工具

这个目录是 `https://sts2custom.shuimu.co.nz/` 的本地轻量工作流版本，重点服务当前模组：

- 本地网页：手动上传图片、编辑字段、导出单张 PNG/JSON。
- 批处理 CLI：读取 JSON 和图片目录，批量生成牌面 PNG/SVG/manifest。
- 默认模板：`equipment-cards.json` 已列出当前 16 张装备牌。

## 批量生成

1. 把 AI 生成的装备插画放到 `tools/cardgen/input/equipment-art/`。
2. 文件名使用模板里的 `art` 字段，例如 `zhuge.png`、`zhangba.png`。
3. 运行：

```powershell
npm run cardgen
```

输出在 `tools/cardgen/output/`：

- `png/*.png`：牌面 PNG。
- `svg/*.svg`：可继续编辑的 SVG。
- `manifest.json`：生成结果清单，方便后续接入模组资源。

## 本地网页

```powershell
npm run cardgen:serve
```

然后打开终端显示的本地地址。网页不依赖线上服务，图片只在本机浏览器里处理。

## JSON 字段

- `id`：卡牌代码名。
- `slug`：输出文件名。
- `title`：牌名。
- `cost`：费用，可写数字或 `X`。
- `type`：卡牌类型，例如 `装备 - 武器`。
- `rarity`：`Common`、`Uncommon`、`Rare`。
- `description`：牌面描述。
- `art`：插画文件名或相对路径。
- `palette`：可选，覆盖边框颜色。
