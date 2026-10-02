const fs = require('node:fs/promises');
const path = require('node:path');
const sharp = require(process.env.AEGIS_NODE_MODULES
    ? path.join(process.env.AEGIS_NODE_MODULES, 'sharp') : 'sharp');

const root = path.resolve(__dirname, '..');
const ui = path.join(root, 'Assets/GameContent/Images/UI');
const modules = ['Core', 'Extractor', 'Shield', 'Drone', 'Radar', 'AmmoFabricator', 'CommandUnit', 'TemporalModulator'];
const upgrades = ['StructuralIntegrity', 'AutoCollecting', 'CollectingEfficiency', 'ShieldCapacity', 'RechargeTime',
    'DeflectionChance', 'DroneCount', 'DroneHP', 'DroneBuildTime', 'DroneDamage', 'FireRange', 'FireRate',
    'Damage', 'RotationSpeed', 'TargetPriority', 'TimeMultiplier'];
const labels = { AmmoFabricator: 'Ammo Fabricator', CommandUnit: 'Command Unit', TemporalModulator: 'Temporal Modulator',
    StructuralIntegrity: 'Core Health', AutoCollecting: 'Auto Collection', CollectingEfficiency: 'Material Yield',
    ShieldCapacity: 'Shield Strength', RechargeTime: 'Shield Recharge', DeflectionChance: 'Shot Reflection',
    DroneCount: 'Drone Slots', DroneHP: 'Drone Health', DroneBuildTime: 'Drone Build Speed', DroneDamage: 'Drone Damage',
    FireRange: 'Fire Range', FireRate: 'Fire Rate', RotationSpeed: 'Rotation Speed', TargetPriority: 'Target Priority',
    TimeMultiplier: 'Game Speed' };

async function exportGroup(names, sourceFolder, outputFolder) {
    // The game keeps the original icons; SVG drafts must not overwrite active sprites.
    const destination = path.join(root, 'Temp/UiSymbolPrototypes', outputFolder);
    await fs.mkdir(destination, { recursive: true });
    const existing = await fs.readdir(destination);
    for (const name of names) {
        const fileName = existing.find(file => file.toLowerCase() === `${name}.png`.toLowerCase()) || `${name}.png`;
        await sharp(path.join(ui, sourceFolder, `${name}.svg`)).resize(512, 512).png()
            .toFile(path.join(destination, fileName));
    }
}

async function overview() {
    let svg = '<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="620" viewBox="0 0 1280 620">';
    svg += '<rect width="1280" height="620" fill="#182226"/>';
    svg += '<g fill="#94ecf4" font-family="Arial,sans-serif" font-size="22" font-weight="bold">';
    svg += '<text x="24" y="34">MODULES</text><text x="24" y="240">UPGRADES</text></g>';
    for (const [names, folder, start] of [[modules, 'ModuleSources', 64], [upgrades, 'UpgradeSources', 266]]) {
        for (let i = 0; i < names.length; i++) {
            const x = (i % 8) * 160;
            const y = start + Math.floor(i / 8) * 190;
            const body = (await fs.readFile(path.join(ui, folder, `${names[i]}.svg`), 'utf8'))
                .replace(/<svg[^>]*>/, '').replace(/<\/svg>\s*$/, '');
            svg += `<svg x="${x + 30}" y="${y}" width="100" height="100" viewBox="0 0 512 512">${body}</svg>`;
            svg += `<text x="${x + 80}" y="${y + 128}" text-anchor="middle" fill="#e6eff0" font-family="Arial,sans-serif" font-size="14">${labels[names[i]] || names[i]}</text>`;
        }
    }
    await sharp(Buffer.from(svg + '</svg>')).png().toFile(path.join(root, 'Assets/Docs/UI_SYMBOL_OVERVIEW.png'));
}

(async () => {
    await exportGroup(modules, 'ModuleSources', 'ModuleS');
    await exportGroup(upgrades, 'UpgradeSources', 'UpgradeS');
    await overview();
    console.log('Exported 24 SVG drafts to Temp/UiSymbolPrototypes and the overview. Active game sprites remain unchanged.');
})().catch(error => { console.error(error); process.exitCode = 1; });
