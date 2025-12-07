#!/usr/bin/env bash
# Replaces the hand-written migrations with a canonical EF Core history (incl. model snapshot).
# Run once on a machine that can restore NuGet packages:   ./scripts/regenerate-migrations.sh
# The result is verified against the model by EF itself; review `git diff` before committing.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INFRA="$ROOT/api/src/GameLog.Infrastructure"
API="$ROOT/api/src/GameLog.Api"
MIGRATIONS="$INFRA/Persistence/Migrations"

dotnet tool restore >/dev/null 2>&1 || true
if ! dotnet ef --version >/dev/null 2>&1; then
  echo "Installing dotnet-ef (global tool)…"
  dotnet tool install --global dotnet-ef
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

echo "Removing hand-written migrations…"
rm -f "$MIGRATIONS"/*.cs

echo "Scaffolding the schema from the model…"
dotnet ef migrations add InitialCreate --project "$INFRA" --startup-project "$API" --output-dir Persistence/Migrations

echo "Adding the pg_trgm title search index (not expressible in the model)…"
dotnet ef migrations add TrigramTitleSearch --project "$INFRA" --startup-project "$API" --output-dir Persistence/Migrations
FILE="$(ls "$MIGRATIONS"/*_TrigramTitleSearch.cs)"
python3 - "$FILE" <<'PY'
import re, sys
path = sys.argv[1]
src = open(path, encoding="utf-8").read()
up = 'migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");\n            migrationBuilder.Sql("CREATE INDEX \\"IX_Games_Title_Trgm\\" ON \\"Games\\" USING gin (lower(\\"Title\\") gin_trgm_ops);");'
down = 'migrationBuilder.Sql("DROP INDEX IF EXISTS \\"IX_Games_Title_Trgm\\";");'
src = re.sub(r"(void Up\(MigrationBuilder migrationBuilder\)\s*\{)\s*(\})", lambda m: m.group(1) + "\n            " + up + "\n        " + m.group(2), src, count=1)
src = re.sub(r"(void Down\(MigrationBuilder migrationBuilder\)\s*\{)\s*(\})", lambda m: m.group(1) + "\n            " + down + "\n        " + m.group(2), src, count=1)
open(path, "w", encoding="utf-8").write(src)
PY

echo "Done. Next: remove the 'ConfigureWarnings … PendingModelChangesWarning' override in"
echo "api/src/GameLog.Infrastructure/DependencyInjection.cs and run 'dotnet test api/GameLog.sln'."
