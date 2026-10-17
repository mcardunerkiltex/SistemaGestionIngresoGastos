#!/bin/sh
# Copia de la base en /opt/gastos/backups, conserva los últimos 14 días.
# Programarlo con cron:  0 3 * * * /opt/gastos/backup.sh
# Restaurar:  docker compose exec -T db pg_restore -U gastos -d gastos --clean < backups/gastos-AAAA-MM-DD.dump
set -eu
cd "$(dirname "$0")"
mkdir -p backups
docker compose exec -T db pg_dump -U gastos -d gastos -Fc > "backups/gastos-$(date +%F).dump"
find backups -name 'gastos-*.dump' -mtime +14 -delete
