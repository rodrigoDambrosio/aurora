using Aurora.Domain.Enums;

namespace Aurora.Domain.Services;

/// <summary>
/// Servicio de dominio con configuraciones predeterminadas para categorías del sistema
/// </summary>
public static class DefaultEventCategories
{
    /// <summary>
    /// Configuraciones de las categorías predeterminadas del sistema
    /// </summary>
    public static readonly Dictionary<DefaultEventCategoryType, (string Name, string Color, string Icon, string Description)> Configurations = new()
    {
        {
            DefaultEventCategoryType.Work,
            ("Trabajo", "#3b82f6", "work", "Eventos relacionados con el trabajo y actividades profesionales")
        },
        {
            DefaultEventCategoryType.Personal,
            ("Personal", "#8b5cf6", "home", "Eventos personales y actividades familiares")
        },
        {
            DefaultEventCategoryType.Health,
            ("Salud", "#10b981", "health", "Eventos relacionados con la salud y bienestar")
        },
        {
            DefaultEventCategoryType.Social,
            ("Social", "#f59e0b", "social", "Eventos sociales y actividades con amigos")
        }
    };

    /// <summary>
    /// Obtiene la configuración de una categoría predeterminada
    /// </summary>
    public static (string Name, string Color, string Icon, string Description) GetConfiguration(DefaultEventCategoryType type)
    {
        return Configurations[type];
    }

    /// <summary>
    /// Obtiene todas las configuraciones de categorías predeterminadas
    /// </summary>
    public static IEnumerable<(DefaultEventCategoryType Type, string Name, string Color, string Icon, string Description)> GetAllConfigurations()
    {
        return Configurations.Select(kvp => (kvp.Key, kvp.Value.Name, kvp.Value.Color, kvp.Value.Icon, kvp.Value.Description));
    }

    /// <summary>
    /// Crea las categorías del sistema para un usuario específico (usado en desarrollo)
    /// </summary>
    /// <param name="userId">ID del usuario (null para categorías del sistema)</param>
    /// <returns>Lista de categorías predeterminadas</returns>
    public static List<Entities.EventCategory> CreateSystemCategories(Guid? userId = null)
    {
        var categories = new List<Entities.EventCategory>();
        int sortOrder = 1;

        foreach (var (type, name, color, icon, description) in GetAllConfigurations())
        {
            categories.Add(new Entities.EventCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                Color = color,
                Icon = icon,
                IsSystemDefault = true,
                SortOrder = sortOrder++,
                UserId = userId, // null para sistema, o userId para categorías personales
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        return categories;
    }
}