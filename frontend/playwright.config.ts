import os from 'node:os'
import path from 'node:path'
import { defineConfig, devices } from '@playwright/test'

const e2eDatabasePath =
  process.env.PLAYWRIGHT_E2E_DB_PATH ??
  path.join(os.tmpdir(), `postly-e2e-${process.pid}.db`)

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:5000',
    trace: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: {
    command: 'dotnet run --project ../backend/src/Postly.Api/Postly.Api.csproj',
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
      DOTNET_ENVIRONMENT: 'Development',
      ConnectionStrings__DefaultConnection: `Data Source=${e2eDatabasePath}`,
    },
    url: 'http://localhost:5000',
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
})
