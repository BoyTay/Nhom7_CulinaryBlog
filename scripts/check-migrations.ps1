<#
.SYNOPSIS
  Verifies the EF Core migration chain against disposable PostgreSQL databases.

.DESCRIPTION
  Scenarios: model drift, clean database, upgrade from the previous migration with data, upgrade from the
  oldest migration that has the Categories table with data, and Down/Up of the newest migration.
  Needs the compose postgres container and a built solution (dotnet build -c Release).
  Prints PASS/FAIL per scenario and exits 1 on any failure. Never touches the application database.
#>
param(
    [string]$Container = 'culinary-blog-postgres-1',
    [int]$Port = 55432,
    [string]$User = 'culinary',
    [string]$Password = 'change-me-for-local-development',
    [string]$Configuration = 'Release'
)

# Native commands write progress to stderr; success is judged by exit codes, so do not stop on stderr text.
$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
$infra = Join-Path $root 'src/CulinaryBlog.Infrastructure'
$api = Join-Path $root 'src/CulinaryBlog.API'
$migrationFiles = Get-ChildItem (Join-Path $infra 'Persistence/Migrations') -Filter '*.cs' |
    Where-Object { $_.Name -match '^\d{14}_.+\.cs$' -and $_.Name -notmatch '\.Designer\.cs$' } |
    Sort-Object Name
$ids = @($migrationFiles | ForEach-Object { $_.BaseName })
$failures = New-Object System.Collections.Generic.List[string]

function Psql([string]$Database, [string]$Sql) {
    # SQL goes through stdin: Windows PowerShell 5.1 strips embedded double quotes from native arguments.
    $out = "SET client_min_messages = warning;`n$Sql" | docker exec -i $Container psql -U $User -d $Database -v ON_ERROR_STOP=1 -tA -q -f - 2>&1
    if ($LASTEXITCODE -ne 0) { throw "psql failed on ${Database}: $Sql`n$($out -join "`n")" }
    return (($out | Where-Object { $_ -notmatch '^SET$' }) -join "`n").Trim()
}
function Connection([string]$Database) { "Host=localhost;Port=$Port;Database=$Database;Username=$User;Password=$Password" }
function Ef([string[]]$Arguments, [string]$Database) {
    $output = dotnet ef @Arguments --project $infra --startup-project $api --configuration $Configuration --no-build --connection (Connection $Database) 2>&1
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef failed: $($Arguments -join ' ')`n$($output | Select-Object -Last 8 | Out-String)" }
    return $output
}
function NewDatabase([string]$Name) {
    Psql 'postgres' "DROP DATABASE IF EXISTS $Name WITH (FORCE)" | Out-Null
    Psql 'postgres' "CREATE DATABASE $Name" | Out-Null
}
function HistoryCount([string]$Database) { [int](Psql $Database 'SELECT count(*) FROM "__EFMigrationsHistory"') }
function InsertCategory([string]$Database, [string]$Name) {
    # Fill every NOT NULL column without a default, whatever the schema version at this baseline looks like.
    $columns = (Psql $Database "SELECT column_name || '|' || data_type FROM information_schema.columns WHERE table_name='Categories' AND is_nullable='NO' AND column_default IS NULL ORDER BY ordinal_position") -split "`n"
    $names = @(); $values = @()
    foreach ($column in $columns) {
        $parts = $column -split '\|'
        $names += '"' + $parts[0] + '"'
        $values += switch -Regex ($parts[1]) {
            'uuid' { 'gen_random_uuid()' }
            'character' { if ($parts[0] -eq 'Name') { "'$Name'" } else { "'$Name-slug'" } }
            'integer' { '0' }
            'boolean' { 'false' }
            'timestamp' { 'now()' }
            default { throw "unsupported column type $($parts[1]) for $($parts[0])" }
        }
    }
    Psql $Database ("INSERT INTO ""Categories"" ({0}) VALUES ({1})" -f ($names -join ','), ($values -join ',')) | Out-Null
}
function Scenario([string]$Title, [scriptblock]$Body) {
    try {
        & $Body
        Write-Host "PASS  $Title"
    } catch {
        Write-Host "FAIL  $Title -- $($_.Exception.Message)"
        $failures.Add($Title)
    }
}

Write-Host "Migrations on disk: $($ids.Count); newest: $($ids[-1])"

Scenario 'model matches latest snapshot (no pending model changes)' {
    $result = dotnet ef migrations has-pending-model-changes --project $infra --startup-project $api --configuration $Configuration --no-build 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0 -or $result -notmatch 'No changes have been made') { throw $result.Trim() }
}

Scenario 'clean database: all migrations apply and none are pending' {
    NewDatabase 'w08_clean'
    Ef @('database', 'update') 'w08_clean' | Out-Null
    if ((HistoryCount 'w08_clean') -ne $ids.Count) { throw "history has $(HistoryCount 'w08_clean') rows, expected $($ids.Count)" }
    $list = (Ef @('migrations', 'list') 'w08_clean') -join "`n"
    if ($list -match 'Pending') { throw "pending migrations remain:`n$list" }
}

Scenario 'prior state (previous migration) with data upgrades to head and keeps rows' {
    NewDatabase 'w08_prior'
    Ef @('database', 'update', $ids[-2]) 'w08_prior' | Out-Null
    InsertCategory 'w08_prior' 'W08 prior category'
    Ef @('database', 'update') 'w08_prior' | Out-Null
    if ((Psql 'w08_prior' "SELECT count(*) FROM ""Categories"" WHERE ""Name"" = 'W08 prior category'") -ne '1') { throw 'category row lost' }
    if ((HistoryCount 'w08_prior') -ne $ids.Count) { throw 'history count mismatch after upgrade' }
}

Scenario 'oldest Category schema with data upgrades to head and keeps rows' {
    $baseline = $ids | Where-Object { $_ -like '*AddCategoryFoundation' }
    NewDatabase 'w08_oldest'
    Ef @('database', 'update', $baseline) 'w08_oldest' | Out-Null
    InsertCategory 'w08_oldest' 'W08 oldest category'
    Ef @('database', 'update') 'w08_oldest' | Out-Null
    if ((Psql 'w08_oldest' "SELECT count(*) FROM ""Categories"" WHERE ""Name"" = 'W08 oldest category'") -ne '1') { throw 'category row lost' }
}

Scenario 'newest migration rolls back and re-applies' {
    Ef @('database', 'update', $ids[-2]) 'w08_clean' | Out-Null
    if ((HistoryCount 'w08_clean') -ne ($ids.Count - 1)) { throw 'rollback did not remove the newest migration' }
    Ef @('database', 'update') 'w08_clean' | Out-Null
    if ((HistoryCount 'w08_clean') -ne $ids.Count) { throw 're-apply did not restore the newest migration' }
}

foreach ($database in 'w08_clean', 'w08_prior', 'w08_oldest') {
    Psql 'postgres' "DROP DATABASE IF EXISTS $database WITH (FORCE)" | Out-Null
}
if ($failures.Count -gt 0) { Write-Host "$($failures.Count) scenario(s) failed"; exit 1 }
Write-Host 'All migration scenarios passed'
