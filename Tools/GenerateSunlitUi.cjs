// Editable SVG sources and their PNG exports for the Sunlit Storybook UI.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../Roll-a-Ball/Assets/Art/UI/SunlitStorybook');
const source = path.join(root, 'Source');
const textures = path.join(root, 'Textures');
fs.mkdirSync(source, { recursive: true });
fs.mkdirSync(textures, { recursive: true });
const ink = '#344438', cream = '#F6F1DF', green = '#456646';
const icons = {
  leaf: '<path d="M24 103C4 46 55 11 108 20c6 54-28 96-73 73" fill="#88A57A" stroke="#456646" stroke-width="5"/><path d="M16 115l67-67M41 85l-2-25M59 68l23 2" fill="none" stroke="#456646" stroke-width="5" stroke-linecap="round"/>',
  lock: '<rect x="29" y="53" width="70" height="57" rx="12"/><path d="M43 52V35a21 21 0 0142 0v17" fill="none" stroke="currentColor" stroke-width="9"/><circle cx="64" cy="79" r="7" fill="#F6F1DF"/><path d="M64 81v12" stroke="#F6F1DF" stroke-width="7"/>',
  check: '<path d="M24 66l25 25 55-61" fill="none" stroke="currentColor" stroke-width="12" stroke-linecap="round" stroke-linejoin="round"/>',
  left: '<path d="M80 24L40 64l40 40" fill="none" stroke="currentColor" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/>',
  right: '<path d="M48 24l40 40-40 40" fill="none" stroke="currentColor" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/>',
  close: '<path d="M32 32l64 64M96 32L32 96" fill="none" stroke="currentColor" stroke-width="9" stroke-linecap="round"/>',
  settings: '<path d="M53 11h22l4 15 12 7 15-4 11 19-11 11v14l11 11-11 19-15-4-12 7-4 15H53l-4-15-12-7-15 4-11-19 11-11V59L11 48l11-19 15 4 12-7z"/><circle cx="64" cy="66" r="19" fill="#F6F1DF"/>',
  circle: '<circle cx="64" cy="64" r="58" fill="#FFFFFF"/>',
  rounded: '<rect x="4" y="4" width="120" height="120" rx="24" fill="#FFFFFF"/>',
  keycap: `<rect x="7" y="11" width="114" height="108" rx="22" fill="#B39263"/><rect x="7" y="6" width="114" height="107" rx="22" fill="${cream}" stroke="#C9AC7A" stroke-width="4"/>`,
};
(async () => {
  for (const [name, contents] of Object.entries(icons)) {
    const tint = name.endsWith('_light') ? cream : ink;
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 128 128" fill="${tint}" color="${tint}">${contents}</svg>`;
    const filename = `UI-C-ICON_${name}`;
    fs.writeFileSync(path.join(source, filename + '.svg'), svg);
    await sharp(Buffer.from(svg)).png().toFile(path.join(textures, filename + '.png'));
  }
  const palettePath = path.join(source, 'palette.json');
  const existingPalette = fs.existsSync(palettePath) ? JSON.parse(fs.readFileSync(palettePath, 'utf8')) : {};
  fs.writeFileSync(palettePath, JSON.stringify({
    ...existingPalette,
    theme: 'B: Sunlit Storybook', referenceResolution: [1920, 1080],
    colors: { ink, cream, green, sage: '#88A57A', birch: '#C99C64', gold: '#D7AE55' },
    issues: [160, 161, 162, 163, 164],
    coin: { source: 'Assets/Prefabs/InGame/10pt_coin.prefab', export: 'Unity PreviewRenderUtility, orthographic view, transparent PNG; preserves the existing elongated diamond mesh and material.' },
    movementGuide: { W: '↑', A: '←', S: '↓', D: '→', arrowInput: 'Guide artwork only; input implementation is separate.' },
    rasterGeneration: { mode: 'built-in image_gen',
      background: 'Warm illustrated low-poly forest diorama, winding path, ivory rolling ball, quiet center, no text or UI.',
      panel: 'Transparent outside, light honey birch rounded frame, opaque quiet warm ivory center, no text, front view, 9-slice.',
      primaryButton: 'Transparent outside, matte leaf-green center, slim ivory and light honey wood rim, no text, front view, 9-slice.' },
    approvedReferenceRefinement: { reference: 'B Sunlit Storybook comparison board', mode: 'built-in image_gen edit',
      menu: 'Extract the B honey wooden stage-select frame, preserve golden woodgrain and corner leaf clusters; remove text and controls and continue the original woodgrain; transparent exterior.',
      secondaryButton: 'Extract the B pause light woodgrain secondary button, preserve cream rim and woodgrain, remove text; transparent exterior.',
      ornament: 'Extract the B panel corner leaf sprig, preserve illustrated shading and veins; no wood underneath, transparent exterior.' },
    delivery: { text: 'Editable TextMeshPro labels in prefabs', states: 'Selectable ColorTint and stage-card status sprite', sources: 'SVG files for icons and palette.json for design values; generated raster originals are preserved in Textures.' },
    refinement: existingPalette.refinement || 'Lean layout with readable labels and restrained wood accents.',
    ...(existingPalette.titleAndExit ? { titleAndExit: existingPalette.titleAndExit } : {})
  }, null, 2) + '\n');
  console.log(`Exported ${Object.keys(icons).length} SVG/PNG UI parts.`);
})();
