import { AlertTriangle, Trash2, X } from 'lucide-react';
import React, { useState } from 'react';
import type { EventCategoryDto } from '../../services/apiService';

interface DeleteCategoryDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (reassignToCategoryId?: string) => Promise<void>;
  category: EventCategoryDto;
  eventCount: number;
  availableCategories: EventCategoryDto[];
}

export const DeleteCategoryDialog: React.FC<DeleteCategoryDialogProps> = ({
  isOpen,
  onClose,
  onConfirm,
  category,
  eventCount,
  availableCategories
}) => {
  const [reassignToCategoryId, setReassignToCategoryId] = useState<string>('');
  const [isDeleting, setIsDeleting] = useState(false);
  const [error, setError] = useState<string>('');

  const hasEvents = eventCount > 0;
  const canDelete = !hasEvents || (hasEvents && reassignToCategoryId);

  const handleConfirm = async () => {
    if (!canDelete) {
      setError('Debes seleccionar una categoría de destino para reasignar los eventos');
      return;
    }

    try {
      setIsDeleting(true);
      setError('');
      await onConfirm(reassignToCategoryId || undefined);
      onClose();
    } catch (err: unknown) {
      console.error('Error deleting category:', err);
      const errorMessage = err instanceof Error ? err.message : 'Error al eliminar la categoría';
      setError(errorMessage);
    } finally {
      setIsDeleting(false);
    }
  };

  const handleClose = () => {
    if (!isDeleting) {
      setReassignToCategoryId('');
      setError('');
      onClose();
    }
  };

  if (!isOpen) return null;

  return (
    <div className="delete-dialog-overlay" onClick={handleClose}>
      <div className="delete-dialog-modal" onClick={e => e.stopPropagation()}>
        <div className="delete-dialog-header">
          <div className="delete-dialog-icon">
            <AlertTriangle size={24} />
          </div>
          <button className="close-button" onClick={handleClose} disabled={isDeleting}>
            <X size={20} />
          </button>
        </div>

        <div className="delete-dialog-content">
          <h2>¿Eliminar categoría?</h2>

          <div className="category-info">
            <div className="category-details">
              <div className="category-name">
                <div
                  className="category-color"
                  style={{ backgroundColor: category.color }}
                />
                <span>{category.name}</span>
              </div>
              {category.description && (
                <p className="category-description">{category.description}</p>
              )}
            </div>
          </div>

          {hasEvents && (
            <div className="events-warning">
              <AlertTriangle size={16} />
              <p>
                Esta categoría tiene <strong>{eventCount}</strong> evento{eventCount !== 1 ? 's' : ''} asociado{eventCount !== 1 ? 's' : ''}.
                Debes reasignarlos a otra categoría antes de eliminar.
              </p>
            </div>
          )}

          {hasEvents && (
            <div className="reassign-section">
              <label htmlFor="reassign-category">
                Reasignar eventos a: <span className="required">*</span>
              </label>
              <select
                id="reassign-category"
                value={reassignToCategoryId}
                onChange={e => {
                  setReassignToCategoryId(e.target.value);
                  setError('');
                }}
                disabled={isDeleting}
                className={error ? 'error' : ''}
              >
                <option value="">Selecciona una categoría</option>
                {availableCategories
                  .filter(cat => cat.id !== category.id)
                  .map(cat => (
                    <option key={cat.id} value={cat.id}>
                      {cat.icon ? `${cat.icon} ` : ''}{cat.name}
                      {cat.isSystemDefault ? ' (Sistema)' : ''}
                    </option>
                  ))}
              </select>
              {error && (
                <div className="error-message">
                  <AlertTriangle size={14} />
                  <span>{error}</span>
                </div>
              )}
            </div>
          )}

          {!hasEvents && (
            <p className="no-events-message">
              Esta categoría no tiene eventos asociados. Puedes eliminarla sin problemas.
            </p>
          )}
        </div>

        <div className="delete-dialog-actions">
          <button
            className="btn-secondary"
            onClick={handleClose}
            disabled={isDeleting}
          >
            Cancelar
          </button>
          <button
            className="btn-danger"
            onClick={handleConfirm}
            disabled={isDeleting || (hasEvents && !reassignToCategoryId)}
          >
            <Trash2 size={16} />
            {isDeleting ? 'Eliminando...' : 'Eliminar categoría'}
          </button>
        </div>
      </div>
    </div>
  );
};
