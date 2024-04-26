CREATE DATABASE hash_values;

CREATE USER hash_values_user WITH PASSWORD '123';

GRANT ALL PRIVILEGES ON DATABASE hash_values TO hash_values_user;

\c hash_values

CREATE SCHEMA IF NOT EXISTS hash_values AUTHORIZATION hash_values_user;