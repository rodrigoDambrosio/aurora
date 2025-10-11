namespace Aurora.Domain.Constants;

/// <summary>
/// Constantes del dominio para el funcionamiento del sistema
/// </summary>
public static class DomainConstants
{
    /// <summary>
    /// Usuario demo/anónimo para desarrollo y testing
    /// Se usa cuando no hay autenticación implementada
    /// </summary>
    public static class DemoUser
    {
        /// <summary>
        /// ID fijo del usuario demo
        /// </summary>
        public static readonly Guid Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        /// <summary>
        /// Email del usuario demo
        /// </summary>
        public const string Email = "demo@aurora.local";

        /// <summary>
        /// Nombre del usuario demo
        /// </summary>
        public const string Name = "Usuario Demo";
    }

    /// <summary>
    /// Configuración para categorías de eventos
    /// </summary>
    public static class EventCategories
    {
        /// <summary>
        /// Prefijo para categorías del sistema
        /// </summary>
        public const string SystemCategoryPrefix = "SYSTEM_";
    }

    /// <summary>
    /// Configuración para desarrollo
    /// </summary>
    public static class Development
    {
        /// <summary>
        /// Indica si estamos en modo de desarrollo sin autenticación
        /// </summary>
        public const bool AllowAnonymousAccess = false;

        /// <summary>
        /// Número de eventos demo a crear automáticamente
        /// </summary>
        public const int DemoEventsCount = 15;
    }
}