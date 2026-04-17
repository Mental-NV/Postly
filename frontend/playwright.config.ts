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
    trace: process.env.CI ? 'on-first-retry' : 'off',
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
      Logging__LogLevel__Default: 'Warning',
      Logging__LogLevel__Microsoft: 'Warning',
      'Logging__LogLevel__Microsoft.Hosting.Lifetime': 'Warning',
      'Logging__LogLevel__Microsoft.EntityFrameworkCore': 'Warning',
    },
    url: 'http://localhost:5000',
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
    stdout: 'ignore',
    stderr: 'pipe',
  },
})
