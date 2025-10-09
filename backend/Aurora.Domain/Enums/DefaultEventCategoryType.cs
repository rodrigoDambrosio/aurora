namespace Aurora.Domain.Enums;

/// <summary>
/// Enum auxiliar para identificar las categorías predeterminadas del sistema
/// Se usa para crear las categorías por defecto en la base de datos
/// </summary>
public enum DefaultEventCategoryType
{
    /// <summary>
    /// Eventos relacionados con el trabajo
    /// </summary>
    Work = 1,

    /// <summary>
    /// Eventos personales
    /// </summary>
    Personal = 2,

    /// <summary>
    /// Eventos relacionados con la salud y bienestar
    /// </summary>
    Health = 3,

    /// <summary>
    /// Eventos sociales y actividades con otros
    /// </summary>
    Social = 4
}