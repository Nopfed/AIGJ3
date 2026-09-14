// In-game screenshots for the promo folder. Needs the dev server running (dotnet run --project src/SpiceWizard.Web)
// and puppeteer-core on NODE_PATH; Chrome renders through SwiftShader so it works headless.
//   node promo/tools/screenshots.js [outDir] [name,name,...]
// Uses the ?demo states (mid-game, day 23, level 9) so nothing from the late game shows up.
const puppeteer = require('puppeteer-core');
const path = require('path');
const fs = require('fs');

const W = 1920, H = 1080;            // 5x the 384x216 design box: crisp integer scale, no margins
const base = 'http://localhost:5259/';
const shots = [
  // name, url, extra steps
  ['01-title',            base,                                   [['wait', 1500]]],
  ['02-yard-afternoon',   base + '?demo',                         [['wait', 6000]]],
  ['03-ghost-pepper',     base + '?demo&panel=Plot&index=3',      []],
  ['04-cauldron',         base + '?demo&panel=Cauldron',          []],
  ['05-mortar-blend',     base + '?demo&panel=Mortar',            []],
  ['06-fermenting-shelf', base + '?demo&panel=Shelf',             []],
  ['07-notice-board',     base + '?demo&panel=Board',             []],
  ['08-dusk',             base + '?demo=night',                   [['wait', 5000]]],
  ['09-rain',             base + '?demo=rain',                    [['wait', 5000]]],
  ['10-morning-report',   base + '?demo&panel=Morning',           []],
  // goes to bed at once; the level-up cheer plays in the yard about 8.5s in, before the morning report opens
  ['11-level-up',         base + '?demo=levelup',                 [['wait', 5600]]],
  ['11b-morning-yard',    base + '?demo=levelup',                 [['wait', 9000], ['click', 1566, 948], ['wait', 1500]]],
  ['12-windy',            base + '?demo=windy',                   [['wait', 4000]]],
];

(async () => {
  const outDir = process.argv[2] || path.join(__dirname, '..', 'screenshots');
  const only = process.argv[3] ? process.argv[3].split(',') : null;
  fs.mkdirSync(outDir, { recursive: true });
  const browser = await puppeteer.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true,
    args: [`--window-size=${W},${H}`, '--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--autoplay-policy=no-user-gesture-required'],
  });
  for (const [name, url, steps] of shots) {
    if (only && !only.some(o => name.includes(o))) continue;
    const page = await browser.newPage();
    await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
    page.on('pageerror', e => console.log('pageerror', name, String(e).slice(0, 200)));
    await page.goto(url, { waitUntil: 'load', timeout: 120000 });
    await page.waitForFunction(() => window.theInstance !== undefined, { timeout: 120000 });
    await new Promise(r => setTimeout(r, 3000));
    for (const [kind, a, b] of steps) {
      if (kind === 'wait') await new Promise(r => setTimeout(r, a));
      // hold the button for a frame or two: KNI polls the mouse state, so an instant click can be missed
      if (kind === 'click') { await page.mouse.move(a, b); await page.mouse.down(); await new Promise(r => setTimeout(r, 100)); await page.mouse.up(); await new Promise(r => setTimeout(r, 400)); }
    }
    const file = path.join(outDir, `spice-wizard-${name}.png`);
    await page.screenshot({ path: file });
    console.log('saved', file);
    await page.close();
  }
  await browser.close();
})().catch(e => { console.error(e); process.exit(1); });
