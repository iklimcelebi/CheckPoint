import { execFileSync } from 'node:child_process'
import { rmSync } from 'node:fs'

const TEST_PORTS = [5199, 5174]

function powershell(script: string) {
  execFileSync('powershell', ['-NoProfile', '-NonInteractive', '-Command', script], {
    stdio: 'inherit',
  })
}

export default function globalTeardown() {
  powershell(
    `Get-NetTCPConnection -LocalPort ${TEST_PORTS.join(',')} -State Listen -ErrorAction SilentlyContinue | ` +
      'ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }',
  )

  const database = process.env.E2E_DATABASE ?? ''
  if (/^CheckPointE2E_[0-9a-f]+$/.test(database)) {
    const sql =
      `IF DB_ID('${database}') IS NOT NULL BEGIN ` +
      `ALTER DATABASE [${database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [${database}]; END`
    powershell(
      [
        "$connection = New-Object System.Data.SqlClient.SqlConnection('Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=True')",
        '$connection.Open()',
        '$command = $connection.CreateCommand()',
        `$command.CommandText = "${sql}"`,
        '[void]$command.ExecuteNonQuery()',
        '$connection.Close()',
      ].join('; '),
    )
  }

  const workDir = process.env.E2E_WORK_DIR
  if (workDir?.includes('checkpoint-e2e-')) {
    rmSync(workDir, { recursive: true, force: true, maxRetries: 10, retryDelay: 500 })
  }
}
