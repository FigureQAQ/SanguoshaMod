# 本地牌面生成工具

这个目录是 `https://sts2custom.shuimu.co.nz/` 的本地轻量工作流版本，重点服务当前三国杀模组：

- 本地网页：手动上传图片、编辑字段、导出单张 PNG/JSON。
- 批处理 CLI：读取 JSON 和图片目录，批量生成牌面 PNG/SVG/manifest。
- PinAI 生图脚本：用 `gpt-image-2` 生成装备插画，并自动同步到牌面输入目录。
- 默认模板：`equipment-cards.json` 已列出当前 20 张装备牌。

## AI 插画生成

先在本机环境变量里设置 `PINAI_API_KEY`，不要把密钥写入仓库或聊天记录。

生成装备牌插画：

```powershell
npm run cardgen:art
```

默认行为：

- 调用 `https://us.pinai-cn.com/v1/images/generations`。
- 使用 `gpt-image-2`、`1536x1024`、`response_format=b64_json`、`stream=true`。
- 图片保存到 `C:\Users\Administrator\Pictures\三国杀mod\image2`。
- 同步复制到 `tools/cardgen/input/equipment-art/<slug>.png`。
- 如果外部目录已有同名中文图片，会直接复用，不重复消耗额度。

只生成或同步某几张：

```powershell
npm run cardgen:art -- -Card zhuge,zhangba
```

强制重新生成：

```powershell
npm run cardgen:art -- -Card chitu -Force
```

### PinAI 流式返回排障

PinAI 的 `gpt-image-2` 生图通常需要几十秒，脚本使用 `stream=true` 按 SSE 流式读取结果。后台如果显示请求已经成功计费，但本地没有生成对应 PNG，优先检查：

- 是否被调用端中途停止，或终端会话被关闭。
- `C:\Users\Administrator\Pictures\三国杀mod\image2\<分类>\.pinai-logs\` 下是否有同名时间戳日志。
- 日志里是否存在 `b64_json`，或返回的是错误、URL、空事件、其他包装结构。

脚本会按完整 SSE 事件块解析 `b64_json`，并递归查找嵌套字段；如果仍找不到图片，会保存原始 SSE 日志后报错，便于对照 PinAI 后台使用记录继续定位。

生成技能牌插画：

```powershell
npm run cardgen:tricks:art
```

技能牌会读取 `trick-cards.json` 里的 `prompt` 和 `externalFileB64`，默认保存到 `C:\Users\Administrator\Pictures\三国杀mod\image2\tricks`，并同步到 `tools/cardgen/input/tricks-art/`。同样支持只生成单张和强制重生成：

```powershell
npm run cardgen:tricks:art -- -Card guanxing
npm run cardgen:tricks:art -- -Card longdan -Force
```

生成基本牌和能力牌插画：

```powershell
npm run cardgen:core:art
```

核心牌会读取 `core-cards.json`，默认保存到 `C:\Users\Administrator\Pictures\三国杀mod\image2\core`，并同步到 `tools/cardgen/input/core-art/`。

## 批量生成牌面

1. 把 AI 生成的装备插画放到 `tools/cardgen/input/equipment-art/`。
2. 文件名使用模板里的 `art` 字段，例如 `zhuge.png`、`zhangba.png`。
3. 运行：

```powershell
npm run cardgen
```

只生成技能牌牌面：

```powershell
npm run cardgen:tricks
```

只生成基本牌和能力牌牌面：

```powershell
npm run cardgen:core
```

重新生成全部已配置牌面：

```powershell
npm run cardgen:all
```

`cardgen:all` 会额外执行一次游戏资产导出，把所有已配置卡牌的 AI 插画裁切为统一头像图，并生成可导入/核对用的 JSON：

- `card_art/<slug>.png`：游戏运行时读取的卡牌头像。
- `card_art/cards.generated.json`：卡牌 id、费用、类型、稀有度、描述、头像路径等元数据。

如果只想重新导出游戏用图片和 JSON，不重新渲染本地牌面：

```powershell
npm run cardgen:export
```

导出的头像路径格式为 `res://mods/sanguosha/card_art/<slug>.png`。C# 侧的 `SanguoshaCard` 会按卡牌类名自动映射到对应 slug，并通过 RitsuLib 0.3.0 的 `CustomPortraitPath` / `CustomBetaPortraitPath` 覆盖游戏内卡牌头像。

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
- `type`：牌面展示分类，统一使用“大类 - 功能”的格式，例如 `基础牌 - 杀`、`技能牌 - 过牌`、`能力牌 - 连段`、`装备牌 - 武器`。
- `rarity`：`Common`、`Uncommon`、`Rare`。
- `description`：牌面描述。
- `art`：插画文件名或相对路径。
- `palette`：可选，覆盖边框颜色。
