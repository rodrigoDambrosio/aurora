import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { CategoryFilter } from '../../components/CategoryFilter'
import type { EventCategoryDto } from '../../services/apiService'

describe('CategoryFilter', () => {
  const mockCategories: EventCategoryDto[] = [
    {
      id: '1',
      name: 'Trabajo',
      color: '#2b7fff',
      isSystemDefault: true,
      sortOrder: 1
    },
    {
      id: '2',
      name: 'Personal',
      color: '#4f46e5',
      isSystemDefault: true,
      sortOrder: 2
    },
    {
      id: '3',
      name: 'Estudio',
      color: '#00c950',
      isSystemDefault: true,
      sortOrder: 3
    }
  ]

  it('debería renderizar todas las categorías', () => {
    const mockOnChange = vi.fn()

    render(
      <CategoryFilter
        categories={mockCategories}
        selectedCategoryId={null}
        onCategoryChange={mockOnChange}
      />
    )

    // Verificar que se muestre el botón "Todas"
    expect(screen.getByText('Todas')).toBeInTheDocument()

    // Verificar que se muestren todas las categorías
    expect(screen.getByText('Trabajo')).toBeInTheDocument()
    expect(screen.getByText('Personal')).toBeInTheDocument()
    expect(screen.getByText('Estudio')).toBeInTheDocument()
  })

  it('debería marcar "Todas" como activo cuando selectedCategoryId es null', () => {
    const mockOnChange = vi.fn()

    render(
      <CategoryFilter
        categories={mockCategories}
        selectedCategoryId={null}
        onCategoryChange={mockOnChange}
      />
    )

    const todasButton = screen.getByText('Todas').closest('button')
    expect(todasButton).toHaveClass('active')
  })

  it('debería marcar la categoría seleccionada como activa', () => {
    const mockOnChange = vi.fn()

    render(
      <CategoryFilter
        categories={mockCategories}
        selectedCategoryId="1"
        onCategoryChange={mockOnChange}
      />
    )

    const trabajoButton = screen.getByText('Trabajo').closest('button')
    expect(trabajoButton).toHaveClass('active')

    const todasButton = screen.getByText('Todas').closest('button')
    expect(todasButton).not.toHaveClass('active')
  })

  it('debería llamar onCategoryChange con null cuando se hace clic en "Todas"', () => {
    const mockOnChange = vi.fn()

    render(
      <CategoryFilter
        categories={mockCategories}
        selectedCategoryId="1"
        onCategoryChange={mockOnChange}
      />
    )

    const todasButton = screen.getByText('Todas')
    fireEvent.click(todasButton)

    expect(mockOnChange).toHaveBeenCalledWith(null)
    expect(mockOnChange).toHaveBeenCalledTimes(1)
  })

  it('debería llamar onCategoryChange con el ID correcto cuando se selecciona una categoría', () => {
    const mockOnChange = vi.fn()

    render(
      <CategoryFilter
        categories={mockCategories}
        selectedCategoryId={null}
        onCategoryChange={mockOnChange}
      />
    )

    const trabajoButton = screen.getByText('Trabajo')
    fireEvent.click(trabajoButton)

    expect(mockOnChange).toHaveBeenCalledWith('1')
    expect(mockOnChange).toHaveBeenCalledTimes(1)
  })

  it('debería cambiar de una categoría a otra correctamente', () => {
    const mockOnChange = vi.fn()

    const { rerender } = render(
      <CategoryFilter
        categories={mockCategories}
        selectedCategoryId="1"
        onCategoryChange={mockOnChange}
      />
    )

    // Clic en Personal
    const personalButton = screen.getByText('Personal')
    fireEvent.click(personalButton)

    expect(mockOnChange).toHaveBeenCalledWith('2')

    // Simular actualización del estado
    rerender(
      <CategoryFilter
        categories={mockCategories}
        selectedCategoryId="2"
        onCategoryChange={mockOnChange}
      />
    )

    // Verificar que Personal esté activo ahora
    expect(personalButton.closest('button')).toHaveClass('active')

    const trabajoButton = screen.getByText('Trabajo').closest('button')
    expect(trabajoButton).not.toHaveClass('active')
  })

  it('debería manejar una lista vacía de categorías', () => {
    const mockOnChange = vi.fn()

    render(
      <CategoryFilter
        categories={[]}
        selectedCategoryId={null}
        onCategoryChange={mockOnChange}
      />
    )

    // Solo debería mostrar el botón "Todas"
    expect(screen.getByText('Todas')).toBeInTheDocument()

    // No debería haber otras categorías
    expect(screen.queryByText('Trabajo')).not.toBeInTheDocument()
  })

  it('debería aplicar los colores de categoría correctamente', () => {
    const mockOnChange = vi.fn()

    render(
      <CategoryFilter
        categories={mockCategories}
        selectedCategoryId={null}
        onCategoryChange={mockOnChange}
      />
    )

    const trabajoButton = screen.getByText('Trabajo').closest('button')

    // Verificar que el botón tenga el indicador de color
    const colorIndicator = trabajoButton?.querySelector('.category-filter-dot')
    expect(colorIndicator).toBeTruthy()
    expect(colorIndicator).toHaveStyle({ backgroundColor: '#2b7fff' })
  })

  it('debería deseleccionar una categoría al hacer clic en ella cuando ya está seleccionada (toggle)', () => {
    const mockOnChange = vi.fn()

    render(
      <CategoryFilter
        categories={mockCategories}
        selectedCategoryId="1"
        onCategoryChange={mockOnChange}
      />
    )

    // Verificar que Trabajo esté seleccionado
    const trabajoButton = screen.getByText('Trabajo').closest('button')
    expect(trabajoButton).toHaveClass('active')

    // Hacer clic en Trabajo que ya está seleccionado
    fireEvent.click(screen.getByText('Trabajo'))

    // Debería llamar onCategoryChange con null para deseleccionar
    expect(mockOnChange).toHaveBeenCalledWith(null)
    expect(mockOnChange).toHaveBeenCalledTimes(1)
  })

  it('debería seleccionar una categoría al hacer clic en ella cuando NO está seleccionada', () => {
    const mockOnChange = vi.fn()

    render(
      <CategoryFilter
        categories={mockCategories}
        selectedCategoryId="1"
        onCategoryChange={mockOnChange}
      />
    )

    // Hacer clic en Personal que NO está seleccionado
    fireEvent.click(screen.getByText('Personal'))

    // Debería llamar onCategoryChange con el ID de Personal
    expect(mockOnChange).toHaveBeenCalledWith('2')
    expect(mockOnChange).toHaveBeenCalledTimes(1)
  })
})
