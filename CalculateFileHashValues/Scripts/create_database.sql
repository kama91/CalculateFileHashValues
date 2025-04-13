CREATE DATABASE hashes;

CREATE USER hashes_user WITH PASSWORD '123';

GRANT ALL PRIVILEGES ON DATABASE hashes TO hashes_user;

\c hashes

CREATE SCHEMA IF NOT EXISTS hashes AUTHORIZATION hashes_user;

CREATE TABLE hashes.hashes (
    id SERIAL PRIMARY KEY,
    path VARCHAR,
    hash VARCHAR(256)
);

CREATE TABLE hashes.errors (
    id SERIAL PRIMARY KEY,
    path VARCHAR
);