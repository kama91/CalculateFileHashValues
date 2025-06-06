CREATE DATABASE hashes;

CREATE USER hashes_user WITH PASSWORD '123';

GRANT ALL PRIVILEGES ON DATABASE hashes TO hashes_user;

\c hashes

CREATE SCHEMA IF NOT EXISTS hashes AUTHORIZATION hashes_user;

CREATE TABLE hashes.hashes (
    id SERIAL PRIMARY KEY,
    path VARCHAR(512),
    hash VARCHAR(256),
    CONSTRAINT unique_path_hash UNIQUE (path, hash)
);

CREATE TABLE hashes.errors (
    id SERIAL PRIMARY KEY,
    path VARCHAR
);