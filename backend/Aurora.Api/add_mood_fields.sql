-- PLAN-88: Agregar campos de mood tracking a la tabla Events
ALTER TABLE Events
ADD COLUMN MoodRating INTEGER NULL;
ALTER TABLE Events
ADD COLUMN MoodNotes TEXT NULL;