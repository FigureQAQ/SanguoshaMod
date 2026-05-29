#!/usr/bin/env node
import { mkdir, readFile, writeFile, access } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import sharp from 'sharp';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '../..');

const CARD_W = 512;
const CARD_H = 768;
const ART = { x: 50, y: 112, w: 412, h: 302 };
const TEXT = { x: 56, y: 494, w: 400, h: 180 };

const palettes = {
  attack: { a: '#5b1013', b: '#b93a27', c: '#f0bd73', glow: '#ff6b3d' },
  skill: { a: '#2d355d', b: '#526ea7', c: '#d8e5ff', glow: '#78a6ff' },
  power: { a: '#293c2b', b: '#6d8d45', c: '#e9f0bb', glow: '#b4e367' },
  equipment: { a: '#3b2418', b: '#b27a35', c: '#f6d18a', glow: '#f0a742' },
  treasure: { a: '#3e263f', b: '#9b6aa9', c: '#f1d5ff', glow: '#e3a8ff' },
  common: { a: '#353841', b: '#6a7080', c: '#e3e5ec', glow: '#b9c0d6' }
};

function parseArgs(argv) {
  const args = {
    input: path.join(__dirname, 'equipment-cards.json'),
    artDir: path.join(__dirname, 'input', 'equipment-art'),
    out: path.join(__dirname, 'output'),
    card: null,
    svgOnly: false,
    pngOnly: false
  };
  for (let i = 2; i < argv.length; i++) {
    const key = argv[i];
    const next = argv[i + 1];
    if (key === '--input') args.input = path.resolve(next), i++;
    else if (key === '--art-dir') args.artDir = path.resolve(next), i++;
    else if (key === '--out') args.out = path.resolve(next), i++;
    else if (key === '--card') args.card = next, i++;
    else if (key === '--svg-only') args.svgOnly = true;
    else if (key === '--png-only') args.pngOnly = true;
    else if (key === '--help' || key === '-h') {
      printHelp();
      process.exit(0);
    }
  }
  return args;
}

function printHelp() {
  console.log(`Usage:
  node tools/cardgen/render-cards.mjs --input tools/cardgen/equipment-cards.json --out tools/cardgen/output

Options:
  --input <file>    Card batch JSON.
  --art-dir <dir>   Directory used for relative art paths.
  --out <dir>       Output directory.
  --card <slug>     Render one card by slug or id.
  --svg-only        Only write SVG files.
  --png-only        Only write PNG files.
`);
}

async function exists(file) {
  try {
    await access(file);
    return true;
  } catch {
    return false;
  }
}

function esc(value) {
  return String(value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function textWidthUnits(text) {
  let units = 0;
  for (const ch of text) units += /[\x00-\x7f]/.test(ch) ? 0.55 : 1;
  return units;
}

function wrapText(text, maxUnits) {
  const normalized = String(text ?? '').replace(/\s+/g, ' ').trim();
  const tokens = [];
  let ascii = '';
  for (const ch of normalized) {
    if (/[\x00-\x7f]/.test(ch) && ch !== ' ') {
      ascii += ch;
    } else {
      if (ascii) tokens.push(ascii), ascii = '';
      if (ch === ' ') tokens.push(' ');
      else tokens.push(ch);
    }
  }
  if (ascii) tokens.push(ascii);

  const lines = [];
  let line = '';
  const closingPunctuation = new Set(['。', '，', '；', '：', '！', '？', '、', '）', '》', '」', '』']);
  for (const token of tokens) {
    const candidate = token === ' ' && !line ? line : line + token;
    if (textWidthUnits(candidate) > maxUnits && line) {
      if (closingPunctuation.has(token)) {
        line = candidate;
        continue;
      }
      lines.push(line.trimEnd());
      line = token.trimStart();
    } else {
      line = candidate;
    }
  }
  if (line.trim()) lines.push(line.trimEnd());
  return lines;
}

function rarityLabel(rarity) {
  return { Common: '普通', Uncommon: '非凡', Rare: '稀有' }[rarity] ?? rarity ?? '';
}

function paletteFor(card) {
  if (card.palette && palettes[card.palette]) return palettes[card.palette];
  const type = String(card.type ?? '').toLowerCase();
  if (type.includes('武器') || type.includes('防具') || type.includes('坐骑')) return palettes.equipment;
  if (type.includes('宝物')) return palettes.treasure;
  return palettes.common;
}

async function imageDataUrl(file) {
  const bytes = await readFile(file);
  const ext = path.extname(file).toLowerCase();
  const mime = ext === '.jpg' || ext === '.jpeg' ? 'image/jpeg' : ext === '.webp' ? 'image/webp' : 'image/png';
  return `data:${mime};base64,${bytes.toString('base64')}`;
}

function placeholderDataUrl(card, palette) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${ART.w}" height="${ART.h}" viewBox="0 0 ${ART.w} ${ART.h}">
    <defs>
      <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="${palette.b}"/>
        <stop offset="0.55" stop-color="#16110e"/>
        <stop offset="1" stop-color="${palette.a}"/>
      </linearGradient>
      <radialGradient id="r" cx="50%" cy="42%" r="60%">
        <stop offset="0" stop-color="${palette.glow}" stop-opacity=".55"/>
        <stop offset="1" stop-color="#000" stop-opacity="0"/>
      </radialGradient>
    </defs>
    <rect width="100%" height="100%" fill="url(#g)"/>
    <rect width="100%" height="100%" fill="url(#r)"/>
    <path d="M40 232 C120 120 260 105 372 58" fill="none" stroke="${palette.c}" stroke-width="14" stroke-linecap="round" opacity=".65"/>
    <circle cx="206" cy="151" r="58" fill="none" stroke="${palette.c}" stroke-width="8" opacity=".5"/>
    <text x="206" y="164" text-anchor="middle" font-size="34" font-weight="800" fill="${palette.c}" font-family="Microsoft YaHei, SimHei, sans-serif">${esc(card.title)}</text>
  </svg>`;
  return `data:image/svg+xml;base64,${Buffer.from(svg).toString('base64')}`;
}

async function resolveArt(card, inputFile, artDir) {
  const candidates = [];
  if (card.art) {
    candidates.push(path.resolve(artDir, card.art));
    candidates.push(path.resolve(path.dirname(inputFile), card.art));
  }
  candidates.push(path.resolve(root, 'power_icons_big', `${card.slug}.png`));
  candidates.push(path.resolve(root, 'power_icons', `${card.slug}.png`));
  for (const candidate of candidates) {
    if (await exists(candidate)) return candidate;
  }
  return null;
}

async function renderSvg(card, inputFile, artDir) {
  const palette = paletteFor(card);
  const artFile = await resolveArt(card, inputFile, artDir);
  const artHref = artFile ? await imageDataUrl(artFile) : placeholderDataUrl(card, palette);
  const titleLines = wrapText(card.title, 8).slice(0, 2);
  const descriptionLines = wrapText(card.description, 15).slice(0, 8);
  const typeText = `${card.type ?? '卡牌'} · ${rarityLabel(card.rarity)}`;
  const cost = card.cost ?? 1;

  const descFont = descriptionLines.length > 6 ? 24 : 26;
  const descLineHeight = descriptionLines.length > 6 ? 31 : 35;
  const titleFont = titleLines.length > 1 ? 34 : 40;

  return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="${CARD_W}" height="${CARD_H}" viewBox="0 0 ${CARD_W} ${CARD_H}">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="${palette.a}"/>
      <stop offset=".55" stop-color="#17100d"/>
      <stop offset="1" stop-color="#050403"/>
    </linearGradient>
    <linearGradient id="rim" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="${palette.c}"/>
      <stop offset=".45" stop-color="${palette.b}"/>
      <stop offset="1" stop-color="${palette.a}"/>
    </linearGradient>
    <radialGradient id="shine" cx="50%" cy="16%" r="75%">
      <stop offset="0" stop-color="${palette.glow}" stop-opacity=".85"/>
      <stop offset=".35" stop-color="${palette.glow}" stop-opacity=".18"/>
      <stop offset="1" stop-color="#000" stop-opacity="0"/>
    </radialGradient>
    <clipPath id="artClip">
      <rect x="${ART.x}" y="${ART.y}" width="${ART.w}" height="${ART.h}" rx="22"/>
    </clipPath>
    <filter id="shadow" x="-20%" y="-20%" width="140%" height="140%">
      <feDropShadow dx="0" dy="8" stdDeviation="8" flood-color="#000" flood-opacity=".65"/>
    </filter>
  </defs>
  <rect width="100%" height="100%" fill="#050403"/>
  <rect x="18" y="18" width="476" height="732" rx="42" fill="url(#bg)" stroke="#140b07" stroke-width="14"/>
  <rect x="28" y="28" width="456" height="712" rx="34" fill="none" stroke="url(#rim)" stroke-width="8"/>
  <rect x="36" y="36" width="440" height="696" rx="28" fill="url(#shine)" opacity=".75"/>

  <circle cx="70" cy="72" r="42" fill="#130b08" stroke="url(#rim)" stroke-width="7" filter="url(#shadow)"/>
  <circle cx="70" cy="72" r="31" fill="${palette.b}" stroke="${palette.c}" stroke-width="3"/>
  <text x="70" y="86" text-anchor="middle" font-size="38" font-weight="900" fill="#fff8df" font-family="Georgia, Microsoft YaHei, serif">${esc(cost)}</text>

  <g font-family="Microsoft YaHei, SimHei, Noto Sans CJK SC, sans-serif" font-weight="900" fill="#fff4d4" stroke="#1b0b04" stroke-width="5" paint-order="stroke fill">
    ${titleLines.map((line, i) => `<text x="256" y="${70 + i * 38}" text-anchor="middle" font-size="${titleFont}">${esc(line)}</text>`).join('\n    ')}
  </g>

  <rect x="${ART.x - 6}" y="${ART.y - 6}" width="${ART.w + 12}" height="${ART.h + 12}" rx="26" fill="#130b08" stroke="${palette.c}" stroke-width="4"/>
  <image href="${artHref}" x="${ART.x}" y="${ART.y}" width="${ART.w}" height="${ART.h}" preserveAspectRatio="xMidYMid slice" clip-path="url(#artClip)"/>
  <rect x="${ART.x}" y="${ART.y}" width="${ART.w}" height="${ART.h}" rx="22" fill="none" stroke="#fff1b8" stroke-opacity=".35" stroke-width="2"/>

  <path d="M62 438 H450 Q462 438 462 450 V468 Q462 480 450 480 H62 Q50 480 50 468 V450 Q50 438 62 438 Z" fill="#1b100b" stroke="${palette.c}" stroke-width="3"/>
  <text x="256" y="468" text-anchor="middle" font-size="25" font-weight="800" fill="${palette.c}" font-family="Microsoft YaHei, SimHei, sans-serif">${esc(typeText)}</text>

  <rect x="44" y="490" width="424" height="214" rx="20" fill="#efe0bd" stroke="#3a1d10" stroke-width="5" opacity=".96"/>
  <g font-family="Microsoft YaHei, SimHei, Noto Sans CJK SC, sans-serif" font-size="${descFont}" font-weight="700" fill="#24160e">
    ${descriptionLines.map((line, i) => `<text x="${TEXT.x}" y="${TEXT.y + 38 + i * descLineHeight}">${esc(line)}</text>`).join('\n    ')}
  </g>

  <path d="M95 724 C160 704 352 704 417 724" fill="none" stroke="${palette.c}" stroke-width="5" opacity=".7"/>
</svg>`;
}

async function main() {
  const args = parseArgs(process.argv);
  const raw = JSON.parse(await readFile(args.input, 'utf8'));
  const defaults = raw.defaults ?? {};
  const cards = (raw.cards ?? []).map(card => ({ ...defaults, ...card }));
  const selected = args.card
    ? cards.filter(card => card.slug === args.card || card.id === args.card || card.title === args.card)
    : cards;

  if (selected.length === 0) {
    throw new Error(`No cards matched ${args.card}`);
  }

  const svgDir = path.join(args.out, 'svg');
  const pngDir = path.join(args.out, 'png');
  if (!args.pngOnly) await mkdir(svgDir, { recursive: true });
  if (!args.svgOnly) await mkdir(pngDir, { recursive: true });

  const manifest = [];
  for (const card of selected) {
    const slug = card.slug || card.id || card.title;
    const svg = await renderSvg(card, args.input, args.artDir);
    const svgPath = path.join(svgDir, `${slug}.svg`);
    const pngPath = path.join(pngDir, `${slug}.png`);
    if (!args.pngOnly) await writeFile(svgPath, svg, 'utf8');
    if (!args.svgOnly) await sharp(Buffer.from(svg)).png().toFile(pngPath);
    manifest.push({
      id: card.id,
      slug,
      title: card.title,
      svg: args.pngOnly ? null : path.relative(args.out, svgPath).replace(/\\/g, '/'),
      png: args.svgOnly ? null : path.relative(args.out, pngPath).replace(/\\/g, '/')
    });
    console.log(`rendered ${card.title} -> ${slug}`);
  }

  await writeFile(
    path.join(args.out, 'manifest.json'),
    `${JSON.stringify({ generatedAt: new Date().toISOString(), cards: manifest }, null, 2)}\n`,
    'utf8'
  );
}

main().catch(error => {
  console.error(error);
  process.exit(1);
});
