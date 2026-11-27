#!/usr/bin/env bash
# Copia las migraciones de vibra-db a la carpeta Infrastructure/Migrations de cada módulo.
# Las copias no se editan a mano: cambia el SQL en vibra-db y vuelve a sincronizar.
# Las semillas no se copian.
# Variable opcional: VIBRA_DB_DIR (por defecto, ../vibra-db).
set -euo pipefail
shopt -s nullglob

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
db_dir="${VIBRA_DB_DIR:-$root/../vibra-db}"
modules=(Identity Catalog Streaming Library History Billing)

# Se valida todo antes de tocar nada para no dejar las copias a medias.
for module in "${modules[@]}"; do
  scripts=("$db_dir/migrations/${module,,}"/*.sql)
  if (( ${#scripts[@]} == 0 )); then
    echo "Error: no hay migraciones en $db_dir/migrations/${module,,}" >&2
    exit 1
  fi
done

for module in "${modules[@]}"; do
  source_dir="$db_dir/migrations/${module,,}"
  target_dir="$root/src/Modules/$module/Vibra.Modules.$module/Infrastructure/Migrations"
  mkdir -p "$target_dir"

  # Quita las copias que ya no existen en el origen.
  for copy in "$target_dir"/*.sql; do
    [[ -f "$source_dir/$(basename "$copy")" ]] || rm "$copy"
  done

  cp "$source_dir"/*.sql "$target_dir"/
done

echo "Migraciones sincronizadas desde $db_dir/migrations."
