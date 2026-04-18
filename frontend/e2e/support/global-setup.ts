import { FullConfig } from '@playwright/test';

async function globalSetup(_config: FullConfig): Promise<void> {
  // Global setup runs once before all tests.
  // Add auth state initialization here when needed.
}

export default globalSetup;
