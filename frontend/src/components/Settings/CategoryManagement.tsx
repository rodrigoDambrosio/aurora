import { AlertCircle, Edit2, Plus, Tag, Trash2 } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import { useEvents } from '../../context/useEvents';
import { apiService, type CreateEventCategoryDto, type EventCategoryDto, type UpdateEventCategoryDto } from '../../services/apiService';
import { Button } from '../ui/button';
import { Card } from '../ui/card';
import { CategoryForm } from './CategoryForm';
import './CategoryForm.css';
import './CategoryManagement.css';
import { DeleteCategoryDialog } from './DeleteCategoryDialog';
import './DeleteCategoryDialog.css';

export const CategoryManagement: React.FC = () => {
  const [categories, setCategories] = useState<EventCategoryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [successMessage, setSuccessMessage] = useState<string>('');
  const { refreshEvents } = useEvents();

  // Form state
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<EventCategoryDto | undefined>();

  // Delete dialog state
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [categoryToDelete, setCategoryToDelete] = useState<EventCategoryDto | undefined>();
  const [eventCountForDelete, setEventCountForDelete] = useState(0);

  useEffect(() => {
    loadCategories();
  }, []);

    const loadCategories = async () => {
    try {
      setIsLoading(true);
      const userCategories = await apiService.getEventCategories();
      setCategories(userCategories);
    } catch (error) {
      console.error('Error loading categories:', error);
      setError('Error al cargar las categorías');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateCategory = async (categoryData: CreateEventCategoryDto) => {
    try {
      const newCategory = await apiService.createEventCategory(categoryData);
      setCategories(prev => [...prev, newCategory]);
      setSuccessMessage('Categoría creada correctamente');
      setTimeout(() => setSuccessMessage(''), 3000);
    } catch (err: unknown) {
      console.error('Error creating category:', err);
      throw err; // Re-throw to let form handle it
    }
  };

  const handleUpdateCategory = async (categoryData: UpdateEventCategoryDto) => {
    if (!editingCategory) return;

    try {
      const updatedCategory = await apiService.updateEventCategory(editingCategory.id, categoryData);
      setCategories(prev =>
        prev.map(cat => cat.id === updatedCategory.id ? updatedCategory : cat)
      );
      setSuccessMessage('Categoría actualizada correctamente');
      setTimeout(() => setSuccessMessage(''), 3000);
    } catch (err: unknown) {
      console.error('Error updating category:', err);
      throw err; // Re-throw to let form handle it
    }
  };

  const handleDeleteClick = async (category: EventCategoryDto) => {
    setCategoryToDelete(category);

    // Get event count for this category
    try {
      const events = await apiService.getEvents();
      const count = events.filter(e => e.eventCategory?.id === category.id).length;
      setEventCountForDelete(count);
      setIsDeleteDialogOpen(true);
    } catch (err) {
      console.error('Error getting event count:', err);
      setError('Error al verificar eventos de la categoría');
    }
  };

  const handleDeleteConfirm = async (reassignToCategoryId?: string) => {
    if (!categoryToDelete) return;

    try {
      const deleteData = reassignToCategoryId ? { reassignToCategoryId } : undefined;
      await apiService.deleteEventCategory(categoryToDelete.id, deleteData);

      // Reload categories from backend to ensure we have the latest state
      await loadCategories();
      
      // Close the delete dialog
      handleDeleteDialogClose();
      
      setSuccessMessage('Categoría eliminada correctamente');
      setTimeout(() => setSuccessMessage(''), 3000);
      
      // Refresh events in calendars when a category is deleted
      refreshEvents();
    } catch (err: unknown) {
      console.error('Error deleting category:', err);
      const errorMessage = err instanceof Error ? err.message : 'Error al eliminar la categoría';
      setError(errorMessage);
      throw err; // Re-throw to let dialog handle it
    }
  };

  const handleEditClick = (category: EventCategoryDto) => {
    setEditingCategory(category);
    setIsFormOpen(true);
  };

  const handleCreateClick = () => {
    setEditingCategory(undefined);
    setIsFormOpen(true);
  };

  const handleFormClose = () => {
    setIsFormOpen(false);
    setEditingCategory(undefined);
  };

  const handleDeleteDialogClose = () => {
    setIsDeleteDialogOpen(false);
    setCategoryToDelete(undefined);
    setEventCountForDelete(0);
  };

  const systemCategories = categories.filter(cat => cat.isSystemDefault);
  const customCategories = categories.filter(cat => !cat.isSystemDefault);

  if (isLoading) {
    return (
      <div className="category-management-loading">
        <p>Cargando categorías...</p>
      </div>
    );
  }

  return (
    <div className="category-management">
      <div className="category-management-header">
        <div className="header-content">
          <Tag size={20} />
          <div>
            <h2>Gestión de Categorías</h2>
            <p>Organiza tus eventos con categorías personalizadas</p>
          </div>
        </div>
        <Button onClick={handleCreateClick} className="create-button">
          <Plus size={16} />
          <span>Nueva categoría</span>
        </Button>
      </div>

      {error && (
        <div className="category-error">
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {successMessage && (
        <div className="category-success">
          <span>{successMessage}</span>
        </div>
      )}

      {/* System Categories */}
      <Card className="category-section">
        <div className="section-header">
          <h3>Categorías del Sistema</h3>
          <span className="category-count">{systemCategories.length}</span>
        </div>
        <div className="category-grid">
          {systemCategories.map(category => (
            <div key={category.id} className="category-card system">
              <div className="category-info">
                <div className="category-name">
                  <div className="category-color" style={{ backgroundColor: category.color }} />
                  <span>{category.name}</span>
                  <span className="system-badge">Sistema</span>
                </div>
                {category.description && (
                  <p className="category-description">{category.description}</p>
                )}
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Custom Categories */}
      <Card className="category-section">
        <div className="section-header">
          <h3>Mis Categorías</h3>
          <span className="category-count">{customCategories.length}</span>
        </div>
        {customCategories.length === 0 ? (
          <div className="empty-state">
            <Tag size={48} />
            <p>No tienes categorías personalizadas</p>
            <p className="empty-subtitle">Crea tu primera categoría para organizar mejor tus eventos</p>
            <Button onClick={handleCreateClick} className="empty-state-button">
              Crear categoría
            </Button>
          </div>
        ) : (
          <div className="category-grid">
            {customCategories.map(category => (
              <div key={category.id} className="category-card custom">
                <div className="category-info">
                  <div className="category-name">
                    <div className="category-color" style={{ backgroundColor: category.color }} />
                    <span>{category.name}</span>
                  </div>
                  <div className="category-actions">
                    <button
                      className="action-button edit"
                      onClick={() => handleEditClick(category)}
                      title="Editar categoría"
                    >
                      <Edit2 size={14} />
                      <span>Editar</span>
                    </button>
                    <button
                      className="action-button delete"
                      onClick={() => handleDeleteClick(category)}
                      title="Eliminar categoría"
                    >
                      <Trash2 size={14} />
                      <span>Eliminar</span>
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>

      {/* Category Form Modal */}
      <CategoryForm
        isOpen={isFormOpen}
        onClose={handleFormClose}
        onSave={editingCategory ? handleUpdateCategory : handleCreateCategory}
        category={editingCategory}
        existingCategories={categories}
      />

      {/* Delete Category Dialog */}
      {categoryToDelete && (
        <DeleteCategoryDialog
          isOpen={isDeleteDialogOpen}
          onClose={handleDeleteDialogClose}
          onConfirm={handleDeleteConfirm}
          category={categoryToDelete}
          eventCount={eventCountForDelete}
          availableCategories={categories}
        />
      )}
    </div>
  );
};
