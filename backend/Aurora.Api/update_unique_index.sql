-- Script para actualizar el índice único de EventCategories
-- Permite reutilizar nombres de categorías eliminadas (IsActive=false)

-- Paso 1: Eliminar el índice único antiguo
DROP INDEX IF EXISTS IX_EventCategories_UserId_Name;

-- Paso 2: Crear nuevo índice único que incluye IsActive
-- Esto permite múltiples registros con el mismo UserId+Name siempre que solo uno tenga IsActive=true
CREATE UNIQUE INDEX IX_EventCategories_UserId_Name_IsActive 
ON EventCategories(UserId, Name, IsActive);

-- Verificar el resultado
SELECT name, sql FROM sqlite_master WHERE type='index' AND tbl_name='EventCategories';
