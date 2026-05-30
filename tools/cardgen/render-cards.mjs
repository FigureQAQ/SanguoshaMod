#!/usr/bin/env node
import { mkdir, readFile, writeFile, access } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import sharp from 'sharp';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '../..');
const uiAssetDir = path.join(__dirname, 'input', 'ui-assets');

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

const characterSkins = {
  ironclad: {
    label: 'Ironclad',
    energy: 'energy_ironclad.png',
    palette: { a: '#43100c', b: '#9a2418', c: '#ffc46a', glow: '#ff4b22', dark: '#170706' }
  },
  silent: {
    label: 'Silent',
    energy: 'energy_silent.png',
    palette: { a: '#162015', b: '#327b4a', c: '#b4ffcf', glow: '#42d681', dark: '#080d0a' }
  },
  defect: {
    label: 'Defect',
    energy: 'energy_defect.png',
    palette: { a: '#071d35', b: '#2266b6', c: '#d7f4ff', glow: '#58b9ff', dark: '#04101e' }
  },
  necrobinder: {
    label: 'Necrobinder',
    energy: 'energy_necrobinder.png',
    palette: { a: '#14152b', b: '#4b3179', c: '#9ff7e3', glow: '#6bead1', dark: '#070912' }
  },
  regent: {
    label: 'Regent',
    energy: 'energy_regent.png',
    palette: { a: '#3e2908', b: '#b88725', c: '#fff0ac', glow: '#ffd45a', dark: '#170f03' }
  }
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

function characterKey(card) {
  const raw = String(card.character ?? card.skin ?? card.energyStyle ?? '').toLowerCase();
  return {
    ironclad: 'ironclad',
    clad: 'ironclad',
    silent: 'silent',
    defect: 'defect',
    necrobinder: 'necrobinder',
    necro: 'necrobinder',
    regent: 'regent'
  }[raw] ?? null;
}

function skinFor(card) {
  const key = characterKey(card);
  return key ? { key, ...characterSkins[key] } : null;
}

function renderEnergyOrb(cost, palette, skin, energyHref) {
  if (!skin) {
    return `<circle cx="70" cy="72" r="42" fill="#130b08" stroke="url(#rim)" stroke-width="7" filter="url(#shadow)"/>
  <circle cx="70" cy="72" r="31" fill="${palette.b}" stroke="${palette.c}" stroke-width="3"/>
  <text x="70" y="86" text-anchor="middle" font-size="38" font-weight="900" fill="#fff8df" font-family="Georgia, Microsoft YaHei, serif">${esc(cost)}</text>`;
  }

  const text = `<text x="70" y="86" text-anchor="middle" font-size="38" font-weight="900" fill="#fff8df" stroke="${palette.dark}" stroke-width="2" paint-order="stroke fill" font-family="Georgia, Microsoft YaHei, serif">${esc(cost)}</text>`;

  if (energyHref) {
    return `<g filter="url(#shadow)">
    <circle cx="70" cy="70" r="45" fill="${palette.dark}" opacity=".76"/>
    <image href="${energyHref}" x="26" y="26" width="88" height="88" preserveAspectRatio="xMidYMid meet"/>
    <circle cx="70" cy="70" r="26" fill="${palette.glow}" opacity=".16"/>
    ${text}
  </g>`;
  }

  if (skin.key === 'ironclad') {
    return `<g filter="url(#shadow)">
    <path d="M70 25 C92 42 112 49 106 78 C101 105 79 117 56 110 C32 103 20 78 30 55 C39 36 51 37 70 25 Z" fill="${palette.dark}" stroke="${palette.c}" stroke-width="5"/>
    <path d="M69 38 C83 55 88 66 78 92 C97 79 99 57 83 43 C95 52 101 66 96 83 C91 101 74 109 58 103 C44 98 36 86 37 71 C38 57 50 49 69 38 Z" fill="${palette.b}"/>
    <path d="M58 100 C53 81 61 66 75 53 C72 71 82 78 75 96" fill="${palette.glow}" opacity=".75"/>
    ${text}
  </g>`;
  }

  if (skin.key === 'silent') {
    return `<g filter="url(#shadow)">
    <path d="M70 25 L113 70 L70 115 L27 70 Z" fill="${palette.dark}" stroke="${palette.c}" stroke-width="5"/>
    <circle cx="70" cy="70" r="31" fill="${palette.b}" stroke="#4a235e" stroke-width="5"/>
    <path d="M83 43 C64 50 53 65 55 82 C57 98 69 104 85 101 C71 112 48 105 40 87 C31 66 45 43 68 38 C74 37 79 39 83 43 Z" fill="${palette.glow}" opacity=".72"/>
    <path d="M46 70 C61 58 78 58 94 70" fill="none" stroke="${palette.c}" stroke-width="4" stroke-linecap="round" opacity=".78"/>
    ${text}
  </g>`;
  }

  if (skin.key === 'defect') {
    return `<g filter="url(#shadow)">
    <path d="M70 24 L108 46 L108 91 L70 114 L32 91 L32 46 Z" fill="${palette.dark}" stroke="${palette.c}" stroke-width="5"/>
    <circle cx="70" cy="70" r="30" fill="${palette.b}" stroke="${palette.glow}" stroke-width="4"/>
    <path d="M70 36 V104 M36 70 H104 M49 49 L91 91 M91 49 L49 91" stroke="${palette.c}" stroke-width="3" opacity=".62"/>
    <circle cx="70" cy="70" r="13" fill="${palette.glow}" opacity=".42"/>
    ${text}
  </g>`;
  }

  if (skin.key === 'necrobinder') {
    return `<g filter="url(#shadow)">
    <path d="M70 24 C101 24 113 49 103 78 C97 100 87 114 70 117 C53 114 43 100 37 78 C27 49 39 24 70 24 Z" fill="${palette.dark}" stroke="${palette.c}" stroke-width="5"/>
    <path d="M43 78 C55 54 83 54 97 78 C87 100 54 100 43 78 Z" fill="${palette.b}" stroke="${palette.glow}" stroke-width="4"/>
    <path d="M56 92 C48 76 60 62 75 52 C68 70 88 74 80 95" fill="${palette.glow}" opacity=".55"/>
    <path d="M49 50 C59 40 81 40 91 50" fill="none" stroke="#7450aa" stroke-width="4" stroke-linecap="round"/>
    ${text}
  </g>`;
  }

  return `<g filter="url(#shadow)">
    <circle cx="70" cy="70" r="45" fill="${palette.dark}" stroke="${palette.c}" stroke-width="5"/>
    <path d="M70 26 L80 56 L112 56 L86 75 L96 106 L70 87 L44 106 L54 75 L28 56 L60 56 Z" fill="${palette.b}" stroke="${palette.glow}" stroke-width="4"/>
    <circle cx="70" cy="70" r="23" fill="${palette.glow}" opacity=".34"/>
    <path d="M70 38 V102 M38 70 H102" stroke="${palette.c}" stroke-width="4" opacity=".65"/>
    ${text}
  </g>`;
}

function renderSkinFrameDecor(skin, palette) {
  if (!skin) return '';
  const common = `<path d="M52 96 C72 62 96 42 132 34" fill="none" stroke="${palette.c}" stroke-width="4" opacity=".56"/>
  <path d="M460 96 C440 62 416 42 380 34" fill="none" stroke="${palette.c}" stroke-width="4" opacity=".56"/>
  <path d="M52 700 C112 724 400 724 460 700" fill="none" stroke="${palette.c}" stroke-width="4" opacity=".52"/>
  <circle cx="48" cy="52" r="13" fill="${palette.dark}" stroke="${palette.c}" stroke-width="3" opacity=".92"/>
  <circle cx="464" cy="52" r="13" fill="${palette.dark}" stroke="${palette.c}" stroke-width="3" opacity=".92"/>
  <circle cx="48" cy="716" r="10" fill="${palette.dark}" stroke="${palette.c}" stroke-width="3" opacity=".78"/>
  <circle cx="464" cy="716" r="10" fill="${palette.dark}" stroke="${palette.c}" stroke-width="3" opacity=".78"/>`;

  if (skin.key === 'ironclad') {
    return `${common}
  <path d="M35 150 C68 210 36 258 66 322 C92 378 44 438 72 512" fill="none" stroke="${palette.glow}" stroke-width="8" opacity=".34"/>
  <path d="M477 150 C444 210 476 258 446 322 C420 378 468 438 440 512" fill="none" stroke="${palette.glow}" stroke-width="8" opacity=".34"/>
  <path d="M48 120 L75 154 L45 184 L72 218 L42 250" fill="none" stroke="${palette.c}" stroke-width="5" opacity=".65"/>
  <path d="M464 120 L437 154 L467 184 L440 218 L470 250" fill="none" stroke="${palette.c}" stroke-width="5" opacity=".65"/>`;
  }

  if (skin.key === 'silent') {
    return `${common}
  <path d="M40 188 C96 224 82 292 42 336 C92 368 90 448 42 492" fill="none" stroke="${palette.glow}" stroke-width="6" opacity=".36"/>
  <path d="M472 188 C416 224 430 292 470 336 C420 368 422 448 470 492" fill="none" stroke="${palette.glow}" stroke-width="6" opacity=".36"/>
  <path d="M44 140 L68 164 L44 188 L20 164 Z M44 252 L68 276 L44 300 L20 276 Z" fill="${palette.dark}" stroke="${palette.c}" stroke-width="4" opacity=".86"/>
  <path d="M468 140 L492 164 L468 188 L444 164 Z M468 252 L492 276 L468 300 L444 276 Z" fill="${palette.dark}" stroke="${palette.c}" stroke-width="4" opacity=".86"/>`;
  }

  if (skin.key === 'defect') {
    return `${common}
  <path d="M42 152 H78 V214 H48 V286 H78 V348 H44" fill="none" stroke="${palette.glow}" stroke-width="6" opacity=".46"/>
  <path d="M470 152 H434 V214 H464 V286 H434 V348 H468" fill="none" stroke="${palette.glow}" stroke-width="6" opacity=".46"/>
  <path d="M48 135 L72 149 L72 177 L48 191 L24 177 L24 149 Z" fill="${palette.dark}" stroke="${palette.c}" stroke-width="4" opacity=".9"/>
  <path d="M464 135 L488 149 L488 177 L464 191 L440 177 L440 149 Z" fill="${palette.dark}" stroke="${palette.c}" stroke-width="4" opacity=".9"/>
  <circle cx="48" cy="163" r="7" fill="${palette.glow}" opacity=".75"/>
  <circle cx="464" cy="163" r="7" fill="${palette.glow}" opacity=".75"/>`;
  }

  if (skin.key === 'necrobinder') {
    return `${common}
  <path d="M44 168 C74 212 70 264 44 314 C76 374 76 424 46 484" fill="none" stroke="${palette.glow}" stroke-width="6" opacity=".4"/>
  <path d="M468 168 C438 212 442 264 468 314 C436 374 436 424 466 484" fill="none" stroke="${palette.glow}" stroke-width="6" opacity=".4"/>
  <path d="M42 138 C64 154 64 184 42 200 C50 178 50 160 42 138 Z" fill="${palette.glow}" opacity=".34"/>
  <path d="M470 138 C448 154 448 184 470 200 C462 178 462 160 470 138 Z" fill="${palette.glow}" opacity=".34"/>
  <path d="M42 230 C70 250 70 286 42 306" fill="none" stroke="#7653b0" stroke-width="5" opacity=".62"/>
  <path d="M470 230 C442 250 442 286 470 306" fill="none" stroke="#7653b0" stroke-width="5" opacity=".62"/>`;
  }

  return `${common}
  <path d="M40 150 L74 184 L48 218 L74 252 L48 286 L74 320" fill="none" stroke="${palette.glow}" stroke-width="6" opacity=".46"/>
  <path d="M472 150 L438 184 L464 218 L438 252 L464 286 L438 320" fill="none" stroke="${palette.glow}" stroke-width="6" opacity=".46"/>
  <path d="M48 130 L58 158 L88 158 L64 176 L74 205 L48 187 L22 205 L32 176 L8 158 L38 158 Z" fill="${palette.b}" stroke="${palette.c}" stroke-width="4" opacity=".92"/>
  <path d="M464 130 L474 158 L504 158 L480 176 L490 205 L464 187 L438 205 L448 176 L424 158 L454 158 Z" fill="${palette.b}" stroke="${palette.c}" stroke-width="4" opacity=".92"/>`;
}

function renderPortraitSkinDecor(skin, palette) {
  const base = `<rect x="${ART.x}" y="${ART.y}" width="${ART.w}" height="${ART.h}" rx="22" fill="none" stroke="#fff1b8" stroke-opacity=".35" stroke-width="2"/>`;
  if (!skin) return base;

  if (skin.key === 'ironclad') {
    return `${base}
  <path d="M58 126 C104 102 154 103 202 126" fill="none" stroke="${palette.glow}" stroke-width="5" opacity=".72"/>
  <path d="M454 388 C406 424 344 424 302 392" fill="none" stroke="${palette.glow}" stroke-width="5" opacity=".54"/>
  <path d="M56 408 L80 384 L104 408 M408 118 L432 142 L456 118" fill="none" stroke="${palette.c}" stroke-width="4" opacity=".72"/>`;
  }

  if (skin.key === 'silent') {
    return `${base}
  <path d="M58 126 L82 150 L58 174 L34 150 Z M454 352 L478 376 L454 400 L430 376 Z" fill="${palette.dark}" stroke="${palette.c}" stroke-width="4" opacity=".9"/>
  <path d="M72 400 C132 358 174 354 232 396" fill="none" stroke="${palette.glow}" stroke-width="4" opacity=".56"/>
  <path d="M278 126 C336 166 380 166 438 126" fill="none" stroke="${palette.glow}" stroke-width="4" opacity=".52"/>`;
  }

  if (skin.key === 'defect') {
    return `${base}
  <path d="M58 128 H106 V148 H142 M370 148 H410 V128 H454" fill="none" stroke="${palette.glow}" stroke-width="4" opacity=".65"/>
  <path d="M58 398 H108 V378 H144 M368 378 H408 V398 H454" fill="none" stroke="${palette.glow}" stroke-width="4" opacity=".65"/>
  <circle cx="108" cy="148" r="6" fill="${palette.c}" opacity=".78"/>
  <circle cx="408" cy="378" r="6" fill="${palette.c}" opacity=".78"/>`;
  }

  if (skin.key === 'necrobinder') {
    return `${base}
  <path d="M68 134 C106 156 112 196 76 226 C124 214 150 166 118 128" fill="none" stroke="${palette.glow}" stroke-width="4" opacity=".62"/>
  <path d="M444 394 C406 372 400 332 436 302 C388 314 362 362 394 400" fill="none" stroke="${palette.glow}" stroke-width="4" opacity=".62"/>
  <path d="M52 260 C82 244 82 286 52 270 M460 260 C430 244 430 286 460 270" fill="none" stroke="#7653b0" stroke-width="4" opacity=".68"/>`;
  }

  return `${base}
  <path d="M256 118 L267 145 L296 148 L274 166 L280 194 L256 179 L232 194 L238 166 L216 148 L245 145 Z" fill="${palette.b}" stroke="${palette.c}" stroke-width="4" opacity=".9"/>
  <path d="M64 400 C118 372 178 372 232 400 M280 400 C334 372 394 372 448 400" fill="none" stroke="${palette.glow}" stroke-width="4" opacity=".6"/>`;
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
  const skin = skinFor(card);
  const palette = skin?.palette ?? paletteFor(card);
  const energyFile = skin?.energy ? path.join(uiAssetDir, skin.energy) : null;
  const energyHref = energyFile && await exists(energyFile) ? await imageDataUrl(energyFile) : null;
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
  ${renderSkinFrameDecor(skin, palette)}

  ${renderEnergyOrb(cost, palette, skin, energyHref)}

  <g font-family="Microsoft YaHei, SimHei, Noto Sans CJK SC, sans-serif" font-weight="900" fill="#fff4d4" stroke="#1b0b04" stroke-width="5" paint-order="stroke fill">
    ${titleLines.map((line, i) => `<text x="256" y="${70 + i * 38}" text-anchor="middle" font-size="${titleFont}">${esc(line)}</text>`).join('\n    ')}
  </g>

  <rect x="${ART.x - 6}" y="${ART.y - 6}" width="${ART.w + 12}" height="${ART.h + 12}" rx="26" fill="#130b08" stroke="${palette.c}" stroke-width="4"/>
  <image href="${artHref}" x="${ART.x}" y="${ART.y}" width="${ART.w}" height="${ART.h}" preserveAspectRatio="xMidYMid slice" clip-path="url(#artClip)"/>
  ${renderPortraitSkinDecor(skin, palette)}

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
