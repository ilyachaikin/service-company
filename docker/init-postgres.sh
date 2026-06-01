#!/bin/bash
set -e

psql -U "$POSTGRES_USER" -c "ALTER USER postgres PASSWORD 'postgres';"

psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<-EOSQL
    DROP EXTENSION IF EXISTS postgis_tiger_geocoder CASCADE;
    DROP EXTENSION IF EXISTS address_standardizer CASCADE;
    DROP EXTENSION IF EXISTS address_standardizer_data_us CASCADE;
    DROP EXTENSION IF EXISTS fuzzystrmatch CASCADE;
    DROP SCHEMA IF EXISTS tiger CASCADE;
    DROP SCHEMA IF EXISTS tiger_data CASCADE;
EOSQL
