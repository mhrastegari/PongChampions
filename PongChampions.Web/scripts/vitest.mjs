// Windows terminals can start in "c:\..." while Node resolves modules to "C:\...".
// Vitest then loads two copies of itself and every suite fails. Re-launch it from
// the canonical path; on other platforms this is a plain pass-through.
import { spawnSync } from 'node:child_process';
import { realpathSync } from 'node:fs';
import { join } from 'node:path';

const cwd = realpathSync.native(process.cwd());
const cli = join(cwd, 'node_modules', 'vitest', 'vitest.mjs');

const { status } = spawnSync(process.execPath, [cli, ...process.argv.slice(2)], {
  cwd,
  stdio: 'inherit',
});

process.exit(status ?? 1);
