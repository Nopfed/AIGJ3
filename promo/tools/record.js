// Records gameplay clips for the trailer as PNG frame sequences (plus times.json with each frame's
// timestamp) using Chrome's screencast. Needs the dev server running and puppeteer-core on NODE_PATH.
//   node promo/tools/record.js <outDir> [clip,clip,...]
// The viewport is 1920x864: the game picks a 4x scale (480x216 virtual) so the frames drop straight into
// the trailer's letterbox with crisp pixels. Click positions below are in that 1920x864 space.
const puppeteer = require('puppeteer-core');
const path = require('path');
const fs = require('fs');

const W = 1920, H = 864;
const base = 'http://localhost:5259/';
// Virtual (480x216 view) -> CSS pixels at 4x
const v = (x, y) => [x * 4, y * 4];
// Panel buttons, from Panels.cs (Frame = 16,18,352,186; the view is 48 virtual px wider on each side)
const OFF = 48;
const CLOSE = v(OFF + 16 + 352 - 12, 18 + 6);           // the x in the title bar
const PLOT_HARVEST = v(OFF + 211, 90);                    // Harvest button on the plot panel
const COOK_FIRST = v(OFF + 169, 50);                      // Cook beside the first recipe
const MORTAR_PLUS = row => v(OFF + 181, row);             // + buttons down the mortar list
const BLEND = v(OFF + 230, 190);
const RISE = v(OFF + 16 + 352 - 55, 18 + 186 - 14);
// Yard stations (centre of the sprite in the design box)
const PLOT_GHOST = v(OFF + 104, 158);
const CAULDRON = v(OFF + 272, 184);
const BOARD = v(OFF + 176, 114);
const PARK = v(OFF + 150, 205);

const clips = {
  title:   [base,                        [['wait', 6000]]],
  yard:    [base + '?demo',              [['wait', 2500], ['click', ...PLOT_GHOST], ['wait', 3200], ['click', ...PLOT_HARVEST], ['wait', 1400], ['click', ...CLOSE], ['wait', 2500]]],
  cook:    [base + '?demo',              [['wait', 800], ['click', ...CAULDRON], ['wait', 3000], ['click', ...COOK_FIRST], ['wait', 1400], ['click', ...CLOSE], ['wait', 3000]]],
  blend:   [base + '?demo&panel=Mortar', [['wait', 1200], ['click', ...MORTAR_PLUS(61)], ['wait', 450], ['click', ...MORTAR_PLUS(96)], ['wait', 450], ['click', ...MORTAR_PLUS(107)], ['wait', 450], ['click', ...MORTAR_PLUS(173)], ['wait', 800], ['click', ...BLEND], ['wait', 1500], ['click', ...CLOSE], ['wait', 2500]]],
  board:   [base + '?demo',              [['wait', 600], ['click', ...BOARD], ['wait', 4000]]],
  night:   [base + '?demo=night',        [['wait', 7000]]],
  rain:    [base + '?demo=rain',         [['wait', 6000]]],
  windy:   [base + '?demo=windy',        [['wait', 6000]]],
  levelup: [base + '?demo=levelup',      [['wait', 12000], ['click', ...RISE], ['wait', 2500]]],
};

(async () => {
  const outDir = process.argv[2] || path.join(__dirname, '..', '..', 'build', 'clips');
  const only = process.argv[3] ? process.argv[3].split(',') : Object.keys(clips);
  const browser = await puppeteer.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true,
    args: [`--window-size=${W},${H}`, '--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'],
  });
  for (const name of only) {
    const [url, steps] = clips[name];
    const dir = path.join(outDir, name);
    fs.rmSync(dir, { recursive: true, force: true });
    fs.mkdirSync(dir, { recursive: true });
    const page = await browser.newPage();
    await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
    page.on('pageerror', e => console.log('pageerror', name, String(e).slice(0, 200)));
    await page.goto(url, { waitUntil: 'load', timeout: 120000 });
    await page.waitForFunction(() => window.theInstance !== undefined, { timeout: 120000 });
    await new Promise(r => setTimeout(r, 1500));
    const cdp = await page.createCDPSession();
    let n = 0; const times = [], events = [];
    cdp.on('Page.screencastFrame', async ({ data, metadata, sessionId }) => {
      fs.writeFileSync(path.join(dir, `f${String(n).padStart(4, '0')}.png`), Buffer.from(data, 'base64'));
      times.push(metadata.timestamp); n++;
      await cdp.send('Page.screencastFrameAck', { sessionId }).catch(() => {});
    });
    await cdp.send('Page.startScreencast', { format: 'png', everyNthFrame: 1 });
    for (const [kind, a, b] of steps) {
      if (kind === 'wait') await new Promise(r => setTimeout(r, a));
      if (kind === 'click') {
        events.push({ t: Date.now() / 1000, x: a, y: b });
        await page.mouse.move(a, b); await page.mouse.down();
        await new Promise(r => setTimeout(r, 100)); await page.mouse.up();
        // park the pointer on empty grass so button tooltips do not hang over the footage
        await new Promise(r => setTimeout(r, 150)); await page.mouse.move(...PARK);
      }
    }
    await cdp.send('Page.stopScreencast');
    await new Promise(r => setTimeout(r, 300));
    // timestamps relative to the first frame; click times on the same clock so the trailer can cue sounds
    const first = times[0];
    fs.writeFileSync(path.join(dir, 'times.json'), JSON.stringify({ times: times.map(t => t - first), clicks: events.map(e => ({ t: e.t - first, x: e.x, y: e.y })) }));
    console.log(name, 'frames', n, 'seconds', (times[n - 1] - first).toFixed(2));
    await page.close();
  }
  await browser.close();
})().catch(e => { console.error(e); process.exit(1); });
