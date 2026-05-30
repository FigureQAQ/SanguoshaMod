#!/usr/bin/env node
import { access, mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import sharp from 'sharp';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '../..');

const batches = [
  { group: 'equipment', input: path.join(__dirname, 'equipment-cards.json'), artDir: path.join(__dirname, 'input', 'equipment-art') },
  { group: 'trick', input: path.join(__dirname, 'trick-cards.json'), artDir: path.join(__dirname, 'input', 'tricks-art') },
  { group: 'core', input: path.join(__dirname, 'core-cards.json'), artDir: path.join(__dirname, 'input', 'core-art') },
  { group: 'role-basic', input: path.join(__dirname, 'role-basic-cards.json'), artDir: path.join(__dirname, 'input', 'core-art') }
];

function parseArgs(argv) {
  const args = { out: path.join(root, 'card_art'), width: 1024, height: 752, quality: 92, skipMissing: false };
  for (let i = 2; i < argv.length; i++) {
    const key = argv[i];
    const next = argv[i + 1];
    if (key === '--out') args.out = path.resolve(next), i++;
    else if (key === '--width') args.width = Number(next), i++;
    else if (key === '--height') args.height = Number(next), i++;
    else if (key === '--quality') args.quality = Number(next), i++;
    else if (key === '--skip-missing') args.skipMissing = true;
    else if (key === '--help' || key === '-h') {
      console.log(`Usage: node tools/cardgen/export-game-assets.mjs [--out card_art] [--width 1024] [--height 752]`);
      process.exit(0);
    }
  }
  return args;
}

async function exists(file) {
  try {
    await access(file);
    return true;
  } catch {
    return false;
  }
}

function classifyCard(card, group) {
  const type = String(card.type ?? '');
  if (group === 'equipment') {
    if (type.includes('武器')) return 'weapon';
    if (type.includes('防具')) return 'armor';
    if (type.includes('坐骑')) return 'mount';
    if (type.includes('宝物')) return 'treasure';
    return 'equipment';
  }

  if (type.includes('能力牌')) return 'power';
  if (type.includes('技能牌')) return 'trick';
  if (['ShaCard', 'ShanCard', 'TaoCard', 'JiuCard'].includes(card.id)) return 'basic';
  if (type.includes('攻击牌') || type.includes('杀牌')) return 'attack';
  if (group === 'role-basic') return 'basic';
  if (group === 'core') return 'power';
  return 'trick';
}

async function resolveArt(card, batch) {
  const candidates = [];
  if (card.art) {
    candidates.push(path.resolve(batch.artDir, card.art));
    candidates.push(path.resolve(path.dirname(batch.input), card.art));
  }
  candidates.push(path.resolve(batch.artDir, `${card.slug}.png`));

  for (const candidate of candidates) {
    if (await exists(candidate)) return candidate;
  }

  return null;
}

async function loadCards() {
  const entries = [];
  for (const batch of batches) {
    const json = JSON.parse(await readFile(batch.input, 'utf8'));
    for (const rawCard of json.cards ?? []) {
      entries.push({ batch, card: { ...(json.defaults ?? {}), ...rawCard } });
    }
  }
  return entries;
}

async function exportPortrait(source, target, args) {
  await sharp(source)
    .resize({ width: args.width, height: args.height, fit: 'cover', position: 'attention' })
    .png({ quality: args.quality, compressionLevel: 9, adaptiveFiltering: true })
    .toFile(target);
}

async function exportPlaceholder(card, target, args) {
  const title = String(card.title ?? card.slug ?? '?')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${args.width}" height="${args.height}" viewBox="0 0 ${args.width} ${args.height}">
    <defs>
      <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#3b2418"/>
        <stop offset="0.5" stop-color="#15100d"/>
        <stop offset="1" stop-color="#2d355d"/>
      </linearGradient>
      <radialGradient id="glow" cx="50%" cy="42%" r="60%">
        <stop offset="0" stop-color="#f0bd73" stop-opacity=".55"/>
        <stop offset="1" stop-color="#000000" stop-opacity="0"/>
      </radialGradient>
    </defs>
    <rect width="100%" height="100%" fill="url(#bg)"/>
    <rect width="100%" height="100%" fill="url(#glow)"/>
    <path d="M120 ${args.height - 150} C260 320 610 290 ${args.width - 120} 120" fill="none" stroke="#f6d18a" stroke-width="28" stroke-linecap="round" opacity=".58"/>
    <circle cx="${args.width / 2}" cy="${args.height / 2 - 20}" r="118" fill="none" stroke="#f6d18a" stroke-width="12" opacity=".42"/>
    <text x="50%" y="52%" text-anchor="middle" dominant-baseline="middle" font-size="86" font-weight="800" fill="#f6d18a" font-family="Microsoft YaHei, SimHei, sans-serif">${title}</text>
  </svg>`;

  await sharp(Buffer.from(svg))
    .png({ quality: args.quality, compressionLevel: 9, adaptiveFiltering: true })
    .toFile(target);
}

async function main() {
  const args = parseArgs(process.argv);
  await mkdir(args.out, { recursive: true });

  const manifestCards = [];
  const missing = [];

  for (const { batch, card } of await loadCards()) {
    const source = await resolveArt(card, batch);
    const fileName = `${card.slug}.png`;
    const target = path.join(args.out, fileName);
    const portraitPath = `res://mods/sanguosha/card_art/${fileName}`;

    if (!source) {
      missing.push(`${card.id} (${card.slug})`);
      await exportPlaceholder(card, target, args);
    } else {
      await exportPortrait(source, target, args);
    }

    manifestCards.push({
      id: card.id,
      slug: card.slug,
      title: card.title,
      cost: card.cost,
      type: card.type,
      category: classifyCard(card, batch.group),
      rarity: card.rarity,
      description: card.description,
      portrait: fileName,
      portraitPath,
      sourceArt: source ? path.relative(root, source).replaceAll(path.sep, '/') : null
    });
  }

  const manifest = {
    object: 'sanguosha-game-card-assets',
    version: 1,
    generatedAt: new Date().toISOString(),
    portraitSize: { width: args.width, height: args.height },
    cards: manifestCards
  };

  await writeFile(path.join(args.out, 'cards.generated.json'), `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');

  if (missing.length > 0) {
    console.warn(`Used placeholder portrait for ${missing.length} card(s):`);
    for (const item of missing) console.warn(`- ${item}`);
  }

  console.log(`Exported ${manifestCards.length} card portrait(s) to ${path.relative(root, args.out)}`);
}

main().catch(error => {
  console.error(error);
  process.exit(1);
});
