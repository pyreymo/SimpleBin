// Run with Node.js and sharp available: node generate.cjs
// SVG source: https://github.com/microsoft/fluentui-system-icons/tree/main/assets/Delete/SVG (MIT).
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');

(async () => {
  const sizes = [16, 20, 24, 28, 32, 40, 48, 64, 128, 256];
  for (const [state, style] of [['EmptyBin', 'regular'], ['Bin', 'filled']]) {
    const svg = fs.readFileSync(path.join(__dirname, `delete_16_${style}.svg`), 'utf8');
    for (const [theme, color] of [['Dark', '#212121'], ['Light', '#ffffff']]) {
      const frames = [];
      for (const size of sizes) {
        frames.push(await sharp(Buffer.from(svg.replace('#212121', color)), { density: 72 * size / 16 * 4 })
          .resize(size, size, { kernel: 'lanczos3' }).png().toBuffer());
      }
      const header = Buffer.alloc(6 + 16 * sizes.length);
      header.writeUInt16LE(1, 2);
      header.writeUInt16LE(sizes.length, 4);
      let offset = header.length;
      sizes.forEach((size, i) => {
        const entry = 6 + i * 16;
        header[entry] = header[entry + 1] = size % 256;
        header.writeUInt16LE(1, entry + 4);
        header.writeUInt16LE(32, entry + 6);
        header.writeUInt32LE(frames[i].length, entry + 8);
        header.writeUInt32LE(offset, entry + 12);
        offset += frames[i].length;
      });
      fs.writeFileSync(path.join(__dirname, '..', `${state}${theme}.ico`), Buffer.concat([header, ...frames]));
    }
  }
})();
