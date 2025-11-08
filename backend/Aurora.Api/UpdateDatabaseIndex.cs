using Microsoft.Data.Sqlite;

namespace Aurora.Api;

/// <summary>
/// Script de utilidad para actualizar el índice único de EventCategories
/// Crea un índice parcial que solo aplica a categorías activas
/// </summary>
public class UpdateDatabaseIndex
{
    public static void Execute(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        Console.WriteLine("🔧 Actualizando índices de EventCategories...");

        // Eliminar índices antiguos si existen
        var dropOld1 = connection.CreateCommand();
        dropOld1.CommandText = "DROP INDEX IF EXISTS IX_EventCategories_UserId_Name;";
        dropOld1.ExecuteNonQuery();

        var dropOld2 = connection.CreateCommand();
        dropOld2.CommandText = "DROP INDEX IF EXISTS IX_EventCategories_UserId_Name_IsActive;";
        dropOld2.ExecuteNonQuery();
        
        Console.WriteLine("  ✓ Índices antiguos eliminados");

        // Crear nuevo índice parcial que solo aplica a categorías activas
        // Esto permite múltiples categorías inactivas con el mismo nombre
        var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE UNIQUE INDEX IF NOT EXISTS IX_EventCategories_UserId_Name_Active 
            ON EventCategories(UserId, Name) 
            WHERE IsActive = 1;";
        createCommand.ExecuteNonQuery();
        
        Console.WriteLine("  ✓ Nuevo índice parcial creado (solo categorías activas)");
        Console.WriteLine("✅ Ahora puedes eliminar y recrear categorías con el mismo nombre");
    }
}
