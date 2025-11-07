-- Limpieza de categorías duplicadas
-- Paso 1: Ver las categorías duplicadas
SELECT UserId, Name, COUNT(*) as Count, GROUP_CONCAT(Id) as Ids, GROUP_CONCAT(IsActive) as IsActiveFlags
FROM EventCategories 
WHERE IsSystemDefault = 0
GROUP BY UserId, Name
HAVING COUNT(*) > 1;

-- Paso 2: Eliminar físicamente todas las categorías inactivas duplicadas
DELETE FROM EventCategories 
WHERE Id IN (
    SELECT ec.Id 
    FROM EventCategories ec
    INNER JOIN (
        SELECT UserId, Name
        FROM EventCategories
        WHERE IsSystemDefault = 0
        GROUP BY UserId, Name
        HAVING COUNT(*) > 1
    ) dups ON ec.UserId = dups.UserId AND ec.Name = dups.Name
    WHERE ec.IsActive = 0 AND ec.IsSystemDefault = 0
);

-- Paso 3: Para las que quedan duplicadas (todas activas), mantener solo la más reciente
DELETE FROM EventCategories
WHERE Id IN (
    SELECT ec.Id
    FROM EventCategories ec
    INNER JOIN (
        SELECT UserId, Name, MAX(CreatedAtUtc) as LatestDate
        FROM EventCategories
        WHERE IsSystemDefault = 0
        GROUP BY UserId, Name
        HAVING COUNT(*) > 1
    ) latest ON ec.UserId = latest.UserId AND ec.Name = latest.Name
    WHERE ec.CreatedAtUtc < latest.LatestDate AND ec.IsSystemDefault = 0
);

-- Paso 4: Verificar que no queden duplicados
SELECT UserId, Name, COUNT(*) as Count
FROM EventCategories 
WHERE IsSystemDefault = 0
GROUP BY UserId, Name
HAVING COUNT(*) > 1;
