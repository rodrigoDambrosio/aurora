namespace Aurora.Application.DTOs;

/// <summary>
/// DTO para la eliminación de una categoría con reasignación de eventos
/// </summary>
public class DeleteEventCategoryDto
{
    /// <summary>
    /// ID de la categoría a la que se reasignarán los eventos (opcional)
    /// Si no se proporciona y hay eventos asociados, la operación fallará
    /// </summary>
    public Guid? ReassignToCategoryId { get; set; }
}
