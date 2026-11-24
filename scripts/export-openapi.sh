#!/usr/bin/env bash
# Exporta el documento OpenAPI de Vibra.Api a artifacts/openapi/openapi.json.
# No modifica vibra-contracts: la CI del backend abre el PR en ese repositorio.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output_dir="$root/artifacts/openapi"
output="$output_dir/openapi.json"

rm -f "$output"

# El generador ignora la versión fijada en Program.cs: necesita --openapi-version.
# --no-incremental: sin él, una compilación ya al día no volvería a generar el documento.
dotnet build "$root/src/Vibra.Api/Vibra.Api.csproj" --no-incremental --nologo \
  -p:OpenApiGenerateDocuments=true \
  -p:OpenApiDocumentsDirectory="$output_dir" \
  -p:OpenApiGenerateDocumentsOptions="--file-name openapi --openapi-version OpenApi3_1"

if [[ ! -f "$output" ]]; then
  echo "Error: no se generó $output" >&2
  exit 1
fi

# swift-openapi-generator no admite todavía 3.2 de forma estable (decisión 005).
python3 -I - "$output" <<'PY'
import json, sys

version = json.load(open(sys.argv[1], encoding="utf-8"))["openapi"]
if not version.startswith("3.1."):
    sys.exit(f"Error: se esperaba OpenAPI 3.1.x y se obtuvo {version}")
print(f"OpenAPI {version} exportado en {sys.argv[1]}")
PY
