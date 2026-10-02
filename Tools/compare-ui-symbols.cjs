const fs = require('node:fs/promises');
const path = require('node:path');
const { execFileSync } = require('node:child_process');
const sharp = require(process.env.AEGIS_NODE_MODULES
    ? path.join(process.env.AEGIS_NODE_MODULES, 'sharp') : 'sharp');
const root = path.resolve(__dirname, '..');
const ui = 'Assets/GameContent/Images/UI';
const baseline = '55ac66a';
const modules = ['Core', 'Extractor', 'Shield', 'Drone', 'Radar', 'AmmoFabricator', 'CommandUnit', 'TemporalModulator'];
const upgrades = ['StructuralIntegrity', 'AutoCollecting', 'CollectingEfficiency', 'ShieldCapacity', 'RechargeTime',
    'DeflectionChance', 'DroneCount', 'DroneHp', 'DroneBuildTime', 'DroneDamage', 'FireRange', 'FireRate',
    'Damage', 'RotationSpeed', 'TargetPriority', 'TimeMultiplier'];
const labels = { AmmoFabricator: 'Ammo Fabricator', CommandUnit: 'Command Unit', TemporalModulator: 'Temporal Modulator',
    StructuralIntegrity: 'Core Health', AutoCollecting: 'Auto Collection', CollectingEfficiency: 'Material Yield',
    ShieldCapacity: 'Shield Strength', RechargeTime: 'Shield Recharge', DeflectionChance: 'Shot Reflection',
    DroneCount: 'Drone Slots', DroneHp: 'Drone Health', DroneBuildTime: 'Drone Build Speed', DroneDamage: 'Drone Damage',
    FireRange: 'Fire Range', FireRate: 'Fire Rate', RotationSpeed: 'Rotation Speed', TargetPriority: 'Target Priority',
    TimeMultiplier: 'Game Speed' };
function original(file) {
    return execFileSync('git', ['show', `${baseline}:${file}`], { cwd: root, maxBuffer: 20 * 1024 * 1024 });
}

(async () => {
    const atlas = original(`${ui}/ModuleSymbols.png`);
    const atlasHeight = (await sharp(atlas).metadata()).height;
    const meta = original(`${ui}/ModuleSymbols.png.meta`).toString();
    const regions = [...meta.matchAll(/name: (.+)\r?\n\s+rect:\r?\n\s+serializedVersion: \d+\r?\n\s+x: (\d+)\r?\n\s+y: (\d+)\r?\n\s+width: (\d+)\r?\n\s+height: (\d+)/g)];
    const rows = [];
    for (const name of modules) {
        const region = regions.find(match => match[1].replaceAll(' ', '') === name);
        if (!region) throw Error(`Missing old module sprite: ${name}`);
        const [, , x, y, width, height] = region;
        rows.push({ name, group: 'MODULE', old: await sharp(atlas).extract({ left: +x, top: atlasHeight - +y - +height,
            width: +width, height: +height }).png().toBuffer(), current: `${ui}/ModuleS/${name}.png` });
    }
    for (const name of upgrades) rows.push({ name, group: 'UPGRADES',
        old: original(`${ui}/UpgradeS/${name}.png`), current: `${ui}/UpgradeS/${name}.png` });

    const width = 800, rowHeight = 180, header = 100, section = 48;
    const height = header + rows.length * rowHeight + section * 2;
    let svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">`;
    svg += `<rect width="${width}" height="${height}" fill="#182226"/>`;
    svg += '<g font-family="Arial,sans-serif" text-anchor="middle">';
    svg += '<g fill="#94ecf4" font-size="28" font-weight="bold"><text x="200" y="48">ALT</text><text x="600" y="48">NEU</text></g>';
    svg += '<text x="400" y="78" fill="#99afb4" font-size="15">Stand vor dem Symbol-Update und aktueller Stand</text>';
    const images = [];
    let y = header, lastGroup;
    for (const row of rows) {
        if (row.group !== lastGroup) {
            svg += `<text x="400" y="${y + 30}" fill="#94ecf4" font-size="24" font-weight="bold">${row.group}</text>`;
            y += section;
            lastGroup = row.group;
        }
        svg += `<path d="M 32 ${y} H 768" stroke="#304347"/>`;
        svg += `<text x="400" y="${y + 26}" fill="#e6eff0" font-size="20">${labels[row.name] || row.name}</text>`;
        for (const [input, x] of [[row.old, 136], [path.join(root, row.current), 536]]) {
            images.push({ input: await sharp(input).resize(128, 128, { fit: 'contain', background: '#00000000' }).png().toBuffer(),
                left: x, top: y + 38 });
        }
        y += rowHeight;
    }
    svg += '</g></svg>';
    const output = path.join(root, 'Assets/Docs/UI_SYMBOL_COMPARISON.png');
    await sharp(Buffer.from(svg)).composite(images).png().toFile(output);
    console.log(`Created comparison: ${output} (${rows.length} rows, old left / new right)`);
})().catch(error => { console.error(error); process.exitCode = 1; });
