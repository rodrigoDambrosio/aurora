using Microsoft.Data.Sqlite;

namespace Aurora.Api;

public static class CleanupDuplicates
{
    public static void Execute(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        Console.WriteLine("🧹 Limpiando categorías duplicadas...");

        // Paso 1: Ver las categorías duplicadas
        Console.WriteLine("\n📋 Verificando duplicados...");
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT UserId, Name, COUNT(*) as Count, GROUP_CONCAT(Id) as Ids
                FROM EventCategories 
                WHERE IsSystemDefault = 0
                GROUP BY UserId, Name
                HAVING COUNT(*) > 1";
            
            using var reader = cmd.ExecuteReader();
            var hasDuplicates = false;
            while (reader.Read())
            {
                hasDuplicates = true;
                Console.WriteLine($"  ⚠️  Duplicado: {reader.GetString(1)} - {reader.GetInt32(2)} instancias");
            }
            
            if (!hasDuplicates)
            {
                Console.WriteLine("  ✓ No se encontraron duplicados");
                return;
            }
        }

        // Paso 2: Eliminar físicamente todas las categorías inactivas duplicadas
        Console.WriteLine("\n🗑️  Eliminando categorías inactivas duplicadas...");
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
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
                )";
            
            var deleted = cmd.ExecuteNonQuery();
            Console.WriteLine($"  ✓ {deleted} categorías inactivas eliminadas");
        }

        // Paso 3: Para las que quedan duplicadas (todas activas), mantener solo la más reciente
        Console.WriteLine("\n🔧 Limpiando duplicados activos (manteniendo el más reciente)...");
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                DELETE FROM EventCategories
                WHERE Id IN (
                    SELECT ec.Id
                    FROM EventCategories ec
                    INNER JOIN (
                        SELECT UserId, Name, MAX(CreatedAt) as LatestDate
                        FROM EventCategories
                        WHERE IsSystemDefault = 0
                        GROUP BY UserId, Name
                        HAVING COUNT(*) > 1
                    ) latest ON ec.UserId = latest.UserId AND ec.Name = latest.Name
                    WHERE ec.CreatedAt < latest.LatestDate AND ec.IsSystemDefault = 0
                )";
            
            var deleted = cmd.ExecuteNonQuery();
            Console.WriteLine($"  ✓ {deleted} categorías antiguas eliminadas");
        }

        // Paso 4: Verificar que no queden duplicados
        Console.WriteLine("\n✅ Verificando resultado...");
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT UserId, Name, COUNT(*) as Count
                FROM EventCategories 
                WHERE IsSystemDefault = 0
                GROUP BY UserId, Name
                HAVING COUNT(*) > 1";
            
            using var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                Console.WriteLine("  ⚠️  Aún quedan duplicados:");
                while (reader.Read())
                {
                    Console.WriteLine($"    - {reader.GetString(1)}: {reader.GetInt32(2)} instancias");
                }
            }
            else
            {
                Console.WriteLine("  ✓ No quedan duplicados. Base de datos limpia!");
            }
        }

        Console.WriteLine("\n✨ Limpieza completada\n");
    }
}
