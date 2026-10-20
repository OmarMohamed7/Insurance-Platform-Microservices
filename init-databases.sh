#!/bin/bash
set -e

for db in claims payment notification; do
  if psql -v ON_ERROR_STOP=1 \
    --username "$POSTGRES_USER" \
    --dbname "$POSTGRES_DB" \
    -tAc "SELECT 1 FROM pg_database WHERE datname='$db'" | grep -q 1; then
    echo "Database '$db' already exists. Skipping."
  else
    psql -v ON_ERROR_STOP=1 \
      --username "$POSTGRES_USER" \
      --dbname "$POSTGRES_DB" \
      -c "CREATE DATABASE \"$db\";"
    echo "Database '$db' created."
  fi
done