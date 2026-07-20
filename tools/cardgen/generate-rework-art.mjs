import { access, mkdir } from 'node:fs/promises';
import path from 'node:path';
import sharp from 'sharp';

const root = path.resolve(import.meta.dirname, '../..');
const cardArtDir = path.join(root, 'card_art');
const iconDir = path.join(root, 'power_icons');
const bigIconDir = path.join(root, 'power_icons_big');

const cards = [
  ['chifeng', 'sha_fire', '赤', 8, 1.16],
  ['xueshou', 'bufang', '守', -8, 0.92],
  ['duren', 'sha_poison', '毒', 86, 0.96],
  ['qianxing', 'kongcheng', '潜', 112, 0.84],
  ['leiyinstarter', 'sha_thunder', '雷', 192, 1.04],
  ['jijia', 'bagua', '甲', 205, 0.90],
  ['yaoren', 'sha_calamity', '药', 318, 1.02],
  ['huhun', 'soulhealerform', '魂', 286, 0.88],
  ['haoling', 'wangjiansha', '令', 44, 1.08],
  ['shouhan', 'renwang', '汉', 30, 0.92],
  ['qiangxi', 'kurou', '袭', 356, 1.12],
  ['liegong', 'qilin', '弓', 18, 1.05],
  ['tianyi', 'duel', '义', 340, 1.06],
  ['caochuan', 'wanjian', '借', 210, 0.90],
  ['yiyidailao', 'shoushi', '逸', 164, 0.88],
  ['zhijizhibi', 'guanxing', '知', 226, 0.94],
  ['quhu', 'nanman', '虎', 24, 1.08],
  ['huoshaolianying', 'huogong', '火', 6, 1.12],
  ['shuiyanqijun', 'bingliang', '水', 202, 0.92],
  ['fangtian', 'zhangba', '戟', 350, 1.08],
  ['paoxiao', 'wushuang', '啸', 12, 1.14],
  ['wusheng', 'sha_fire', '武', 354, 1.08],
  ['ganglie', 'jianxiong', '烈', 4, 1.10],
  ['lijian', 'huogong', '间', 314, 0.98],
  ['qice', 'qixi', '策', 276, 0.90],
  ['biyue', 'nightfallscheme', '月', 244, 0.88],
  ['leiji', 'thundermandate', '击', 194, 1.12],
  ['kanpo', 'guicai', '破', 216, 0.94],
  ['kuangfeng', 'tiesuo', '风', 174, 1.02],
  ['jijiu', 'tao_necrobinder', '救', 326, 0.96],
  ['huomo', 'qingnang', '墨', 304, 0.90],
  ['duanchang', 'soulhealerform', '断', 332, 0.94],
  ['jijiang', 'wangjiansha', '将', 42, 1.04],
  ['qianchong', 'zhiheng', '谦', 138, 0.92],
  ['songwei', 'imperialedict', '威', 48, 1.08]
];

const powers = [
  ['fangtian', '戟', '#f6d18a', '#6b1b13'],
  ['ganglie', '烈', '#ffcf70', '#8f1e12'],
  ['biyue', '月', '#d5e8ff', '#34266f'],
  ['kuangfeng', '风', '#d8fff3', '#176b61'],
  ['duanchang', '断', '#ffd4e6', '#70204f'],
  ['songwei', '威', '#fff0a8', '#7a5313']
];

function sourcePath(slug) {
  const candidates = [
    path.join(cardArtDir, `${slug}.png`),
    path.join(root, 'tools', 'cardgen', 'input', 'core-art', `${slug}.png`),
    path.join(root, 'tools', 'cardgen', 'input', 'tricks-art', `${slug}.png`),
    path.join(root, 'tools', 'cardgen', 'input', 'equipment-art', `${slug}.png`)
  ];
  return candidates;
}

async function firstExisting(paths) {
  for (const candidate of paths) {
    try {
      await access(candidate);
      return candidate;
    } catch {
    }
  }
  throw new Error(`Missing source art: ${paths.join(', ')}`);
}

function sealSvg(glyph, hue) {
  const accent = `hsl(${hue} 72% 68%)`;
  return Buffer.from(`<svg width="1024" height="752" xmlns="http://www.w3.org/2000/svg">
    <defs>
      <linearGradient id="shade" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#05070d" stop-opacity=".04"/>
        <stop offset=".68" stop-color="#05070d" stop-opacity=".12"/>
        <stop offset="1" stop-color="#05070d" stop-opacity=".62"/>
      </linearGradient>
      <radialGradient id="glow" cx="78%" cy="76%" r="28%">
        <stop offset="0" stop-color="${accent}" stop-opacity=".28"/>
        <stop offset="1" stop-color="${accent}" stop-opacity="0"/>
      </radialGradient>
    </defs>
    <rect width="1024" height="752" fill="url(#shade)"/>
    <rect width="1024" height="752" fill="url(#glow)"/>
    <g transform="translate(836 570) rotate(-7)">
      <circle r="92" fill="#130d0b" fill-opacity=".52" stroke="${accent}" stroke-width="7" stroke-opacity=".82"/>
      <circle r="74" fill="none" stroke="${accent}" stroke-width="2" stroke-opacity=".55"/>
      <text x="0" y="30" text-anchor="middle" font-family="Microsoft YaHei, SimHei, sans-serif" font-size="82" font-weight="800" fill="${accent}" fill-opacity=".9">${glyph}</text>
    </g>
  </svg>`);
}

async function generateCard(slug, sourceSlug, glyph, hue, brightness) {
  const source = await firstExisting(sourcePath(sourceSlug));
  const hash = [...slug].reduce((sum, char) => sum + char.charCodeAt(0), 0);
  const left = hash % 72;
  const top = (hash * 7) % 48;
  await sharp(source)
    .resize(1120, 824, { fit: 'cover', position: 'attention' })
    .extract({ left, top, width: 1024, height: 752 })
    .modulate({ brightness, saturation: 1.08, hue })
    .sharpen({ sigma: 0.7 })
    .composite([{ input: sealSvg(glyph, hue), blend: 'over' }])
    .png({ compressionLevel: 9, adaptiveFiltering: true })
    .toFile(path.join(cardArtDir, `${slug}.png`));
}

function iconSvg(glyph, foreground, background, size) {
  const fontSize = Math.round(size * 0.48);
  const stroke = Math.max(2, Math.round(size * 0.035));
  return Buffer.from(`<svg width="${size}" height="${size}" xmlns="http://www.w3.org/2000/svg">
    <defs>
      <radialGradient id="bg" cx="38%" cy="30%" r="72%">
        <stop offset="0" stop-color="${foreground}" stop-opacity=".5"/>
        <stop offset=".48" stop-color="${background}"/>
        <stop offset="1" stop-color="#09080c"/>
      </radialGradient>
    </defs>
    <circle cx="${size / 2}" cy="${size / 2}" r="${size * 0.45}" fill="url(#bg)" stroke="${foreground}" stroke-width="${stroke}"/>
    <circle cx="${size / 2}" cy="${size / 2}" r="${size * 0.36}" fill="none" stroke="${foreground}" stroke-opacity=".42" stroke-width="${Math.max(1, stroke / 2)}"/>
    <text x="50%" y="55%" text-anchor="middle" dominant-baseline="middle" font-family="Microsoft YaHei, SimHei, sans-serif" font-size="${fontSize}" font-weight="800" fill="${foreground}">${glyph}</text>
  </svg>`);
}

async function generatePower(slug, glyph, foreground, background) {
  await sharp(iconSvg(glyph, foreground, background, 64))
    .png({ compressionLevel: 9 })
    .toFile(path.join(iconDir, `${slug}.png`));
  await sharp(iconSvg(glyph, foreground, background, 256))
    .png({ compressionLevel: 9 })
    .toFile(path.join(bigIconDir, `${slug}.png`));
}

await Promise.all([mkdir(cardArtDir, { recursive: true }), mkdir(iconDir, { recursive: true }), mkdir(bigIconDir, { recursive: true })]);
for (const card of cards) {
  await generateCard(...card);
}
for (const power of powers) {
  await generatePower(...power);
}

console.log(`Generated ${cards.length} card portraits and ${powers.length * 2} power icons.`);
