const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const viewports = {
  mobile: { width: 375, height: 812 },
  tablet: { width: 768, height: 1024 },
  desktop: { width: 1440, height: 900 }
};

const pages = [
  'user-list.html',
  'user-detail.html',
  'role-list.html',
  'role-detail.html',
  'apikey-list.html',
  'apikey-detail.html'
];

async function captureScreenshots() {
  const browser = await chromium.launch();
  const screenshotsDir = path.join(__dirname, 'screenshots');

  // Create screenshots directory if it doesn't exist
  if (!fs.existsSync(screenshotsDir)) {
    fs.mkdirSync(screenshotsDir, { recursive: true });
  }

  for (const pageName of pages) {
    const pagePath = path.join(__dirname, pageName);
    const baseName = pageName.replace('.html', '');

    for (const [viewportName, viewport] of Object.entries(viewports)) {
      console.log(`Capturing ${baseName} - ${viewportName}...`);

      const context = await browser.newContext({
        viewport: viewport,
        deviceScaleFactor: 2 // For crisp screenshots
      });

      const page = await context.newPage();

      // Load the HTML file
      await page.goto(`file://${pagePath}`, { waitUntil: 'networkidle' });

      // Wait a bit for fonts and icons to load
      await page.waitForTimeout(1000);

      // Capture full page screenshot
      const screenshotPath = path.join(screenshotsDir, `${baseName}-${viewportName}.png`);
      await page.screenshot({
        path: screenshotPath,
        fullPage: true
      });

      console.log(`  Saved: ${screenshotPath}`);

      await context.close();
    }
  }

  await browser.close();
  console.log('\nAll screenshots captured successfully!');
}

captureScreenshots().catch(console.error);
