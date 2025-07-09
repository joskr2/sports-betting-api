-- =============================================================================
-- SPORTS BETTING API - DATABASE INITIALIZATION
-- =============================================================================
-- Script de inicialización para la base de datos PostgreSQL

-- Crear extensiones necesarias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Configurar timezone
SET timezone = 'UTC';

-- Mensaje de confirmación
SELECT 'Database initialized successfully' AS status; 
