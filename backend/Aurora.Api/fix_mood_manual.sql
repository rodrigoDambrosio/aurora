-- Verificar si las columnas existen antes de agregarlas
PRAGMA table_info(Events);

-- Solo si no existen, agregamos las columnas
-- Nota: SQLite no soporta "IF NOT EXISTS" para columnas, 
-- así que este script puede fallar si las columnas ya existen

-- Intentar agregar las columnas (puede fallar si ya existen)
ALTER TABLE Events ADD COLUMN MoodRating INTEGER NULL;
ALTER TABLE Events ADD COLUMN MoodNotes TEXT NULL;

-- Marcar la migración como aplicada
INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
VALUES ('20251102010440_AddEventMoodFields', '9.0.9');