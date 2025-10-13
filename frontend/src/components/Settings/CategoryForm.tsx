import { AlertCircle, Save, X } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import type { CreateEventCategoryDto, EventCategoryDto, UpdateEventCategoryDto } from '../../services/apiService';

interface CategoryFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (data: CreateEventCategoryDto | UpdateEventCategoryDto) => Promise<void>;
  category?: EventCategoryDto;
  existingCategories: EventCategoryDto[];
}

interface FormErrors {
  name?: string;
  color?: string;
  description?: string;
  icon?: string;
}

const PRESET_COLORS = [
  '#EF4444', '#F97316', '#F59E0B', '#EAB308', '#84CC16',
  '#22C55E', '#10B981', '#14B8A6', '#06B6D4', '#0EA5E9',
  '#3B82F6', '#6366F1', '#8B5CF6', '#A855F7', '#D946EF',
  '#EC4899', '#F43F5E', '#64748B', '#78716C', '#57534E'
];

export const CategoryForm: React.FC<CategoryFormProps> = ({
  isOpen,
  onClose,
  onSave,
  category,
  existingCategories
}) => {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    color: '#3B82F6',
    icon: ''
  });
  const [errors, setErrors] = useState<FormErrors>({});
  const [isSaving, setIsSaving] = useState(false);
  const [touched, setTouched] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (category) {
      setFormData({
        name: category.name,
        description: category.description || '',
        color: category.color,
        icon: category.icon || ''
      });
    } else {
      setFormData({
        name: '',
        description: '',
        color: '#3B82F6',
        icon: ''
      });
    }
    setErrors({});
    setTouched(new Set());
  }, [category, isOpen]);

  const validateField = (field: string, value: string): string | undefined => {
    switch (field) {
      case 'name': {
        if (!value.trim()) return 'El nombre es obligatorio';
        if (value.length > 50) return 'El nombre no puede superar los 50 caracteres';
        if (!/\S/.test(value)) return 'El nombre no puede contener solo espacios';
        // Check duplicate (case-insensitive, excluding current category)
        const duplicateName = existingCategories.find(
          cat => cat.name.toLowerCase() === value.toLowerCase() && cat.id !== category?.id
        );
        if (duplicateName) return 'Ya existe una categoría con este nombre';
        return undefined;
      }

      case 'color':
        if (!value) return 'El color es obligatorio';
        if (!/^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/.test(value)) {
          return 'El color debe estar en formato hexadecimal válido (#RRGGBB)';
        }
        return undefined;

      case 'description':
        if (value && value.length > 200) {
          return 'La descripción no puede superar los 200 caracteres';
        }
        return undefined;

      case 'icon':
        if (value && value.length > 100) {
          return 'El icono no puede superar los 100 caracteres';
        }
        return undefined;

      default:
        return undefined;
    }
  };

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {};
    newErrors.name = validateField('name', formData.name);
    newErrors.color = validateField('color', formData.color);
    newErrors.description = validateField('description', formData.description);
    newErrors.icon = validateField('icon', formData.icon);

    setErrors(newErrors);
    return !Object.values(newErrors).some(error => error !== undefined);
  };

  const handleBlur = (field: string) => {
    setTouched(prev => new Set(prev).add(field));
    const error = validateField(field, formData[field as keyof typeof formData]);
    setErrors(prev => ({ ...prev, [field]: error }));
  };

  const handleChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Real-time validation if field was touched
    if (touched.has(field)) {
      const error = validateField(field, value);
      setErrors(prev => ({ ...prev, [field]: error }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Mark all fields as touched
    setTouched(new Set(['name', 'color', 'description', 'icon']));

    if (!validateForm()) return;

    try {
      setIsSaving(true);
      await onSave(formData);
      onClose();
    } catch (error: unknown) {
      console.error('Error saving category:', error);
      // Handle API validation errors
      const err = error as { response?: { data?: { errors?: Record<string, string[]> } } };
      if (err.response?.data?.errors) {
        const apiErrors: FormErrors = {};
        Object.entries(err.response.data.errors).forEach(([key, messages]) => {
          apiErrors[key.toLowerCase() as keyof FormErrors] = messages[0];
        });
        setErrors(apiErrors);
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="category-form-overlay" onClick={onClose}>
      <div className="category-form-modal" onClick={e => e.stopPropagation()}>
        <div className="category-form-header">
          <h2>{category ? 'Editar categoría' : 'Nueva categoría'}</h2>
          <button className="close-button" onClick={onClose} disabled={isSaving}>
            <X size={20} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="category-form-content">
          {/* Name field */}
          <div className="form-group">
            <label htmlFor="name">
              Nombre <span className="required">*</span>
            </label>
            <input
              id="name"
              type="text"
              value={formData.name}
              onChange={e => handleChange('name', e.target.value)}
              onBlur={() => handleBlur('name')}
              maxLength={50}
              disabled={isSaving}
              className={errors.name && touched.has('name') ? 'error' : ''}
              placeholder="Ej: Reuniones"
            />
            {errors.name && touched.has('name') && (
              <div className="error-message">
                <AlertCircle size={14} />
                <span>{errors.name}</span>
              </div>
            )}
            <div className="char-count">
              {formData.name.length}/50
            </div>
          </div>

          {/* Description field */}
          <div className="form-group">
            <label htmlFor="description">Descripción</label>
            <textarea
              id="description"
              value={formData.description}
              onChange={e => handleChange('description', e.target.value)}
              onBlur={() => handleBlur('description')}
              maxLength={200}
              rows={3}
              disabled={isSaving}
              className={errors.description && touched.has('description') ? 'error' : ''}
              placeholder="Describe el tipo de eventos que incluye esta categoría"
            />
            {errors.description && touched.has('description') && (
              <div className="error-message">
                <AlertCircle size={14} />
                <span>{errors.description}</span>
              </div>
            )}
            <div className="char-count">
              {formData.description.length}/200
            </div>
          </div>

          {/* Color picker */}
          <div className="form-group">
            <label htmlFor="color">
              Color <span className="required">*</span>
            </label>
            <div className="color-picker-container">
              <div className="preset-colors">
                {PRESET_COLORS.map(color => (
                  <button
                    key={color}
                    type="button"
                    className={`color-preset ${formData.color === color ? 'selected' : ''}`}
                    style={{ backgroundColor: color }}
                    onClick={() => handleChange('color', color)}
                    disabled={isSaving}
                    title={color}
                  />
                ))}
              </div>
              <div className="custom-color-input">
                <input
                  id="color"
                  type="color"
                  value={formData.color}
                  onChange={e => handleChange('color', e.target.value)}
                  onBlur={() => handleBlur('color')}
                  disabled={isSaving}
                />
                <input
                  type="text"
                  value={formData.color}
                  onChange={e => handleChange('color', e.target.value)}
                  onBlur={() => handleBlur('color')}
                  maxLength={7}
                  disabled={isSaving}
                  className={errors.color && touched.has('color') ? 'error' : ''}
                  placeholder="#3B82F6"
                />
              </div>
            </div>
            {errors.color && touched.has('color') && (
              <div className="error-message">
                <AlertCircle size={14} />
                <span>{errors.color}</span>
              </div>
            )}
          </div>

          {/* Icon field */}
          <div className="form-group">
            <label htmlFor="icon">Icono (emoji o texto)</label>
            <input
              id="icon"
              type="text"
              value={formData.icon}
              onChange={e => handleChange('icon', e.target.value)}
              onBlur={() => handleBlur('icon')}
              maxLength={100}
              disabled={isSaving}
              className={errors.icon && touched.has('icon') ? 'error' : ''}
              placeholder="📅 o Calendar"
            />
            {errors.icon && touched.has('icon') && (
              <div className="error-message">
                <AlertCircle size={14} />
                <span>{errors.icon}</span>
              </div>
            )}
            <div className="char-count">
              {formData.icon.length}/100
            </div>
          </div>

          {/* Form actions */}
          <div className="form-actions">
            <button
              type="button"
              className="btn-secondary"
              onClick={onClose}
              disabled={isSaving}
            >
              Cancelar
            </button>
            <button
              type="submit"
              className="btn-primary"
              disabled={isSaving}
            >
              <Save size={16} />
              {isSaving ? 'Guardando...' : 'Guardar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
