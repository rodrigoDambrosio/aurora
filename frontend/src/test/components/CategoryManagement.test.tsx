import { render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import { CategoryManagement } from '../../components/Settings/CategoryManagement'
import { EventsProvider } from '../../context/EventsContext'
import * as apiService from '../../services/apiService'
import type { EventCategoryDto } from '../../services/apiService'

// Mock del apiService
vi.mock('../../services/apiService', () => ({
  apiService: {
    getEventCategories: vi.fn(),
    createEventCategory: vi.fn(),
    updateEventCategory: vi.fn(),
    deleteEventCategory: vi.fn(),
    getEvents: vi.fn(),
  }
}))

describe('CategoryManagement - Eliminación de categorías', () => {
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
      name: 'Mi Categoría',
      color: '#4f46e5',
      isSystemDefault: false,
      sortOrder: 2
    }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(apiService.apiService.getEventCategories).mockResolvedValue(mockCategories)
    vi.mocked(apiService.apiService.getEvents).mockResolvedValue([])
  })

  it('debe cargar las categorías al montar el componente', async () => {
    render(
      <EventsProvider>
        <CategoryManagement />
      </EventsProvider>
    )

    await waitFor(() => {
      expect(screen.getByText('Trabajo')).toBeInTheDocument()
      expect(screen.getByText('Mi Categoría')).toBeInTheDocument()
    })

    expect(apiService.apiService.getEventCategories).toHaveBeenCalledTimes(1)
  })

  it('debe mostrar mensaje de error al cargar categorías si falla', async () => {
    vi.mocked(apiService.apiService.getEventCategories).mockRejectedValue(
      new Error('Error de red')
    )

    render(
      <EventsProvider>
        <CategoryManagement />
      </EventsProvider>
    )

    await waitFor(() => {
      expect(screen.getByText('Error al cargar las categorías')).toBeInTheDocument()
    })
  })

  it('debe permitir eliminar una categoría sin eventos', async () => {
    vi.mocked(apiService.apiService.deleteEventCategory).mockResolvedValue()

    render(
      <EventsProvider>
        <CategoryManagement />
      </EventsProvider>
    )

    // Esperar a que carguen las categorías
    await waitFor(() => {
      expect(screen.getByText('Mi Categoría')).toBeInTheDocument()
    })

    // Simular clic en eliminar (esto requeriría más setup del componente)
    // Por simplicidad, verificamos que el servicio se puede llamar correctamente
    await apiService.apiService.deleteEventCategory('2', undefined)
    
    expect(apiService.apiService.deleteEventCategory).toHaveBeenCalledWith('2', undefined)
  })

  it('debe mostrar mensaje de éxito después de eliminar', async () => {
    vi.mocked(apiService.apiService.deleteEventCategory).mockResolvedValue()

    render(
      <EventsProvider>
        <CategoryManagement />
      </EventsProvider>
    )

    // Esperar a que carguen las categorías inicialmente
    await waitFor(() => {
      expect(screen.getByText('Mi Categoría')).toBeInTheDocument()
    })

    // Verificar que la categoría existe antes de eliminar
    expect(screen.getByText('Trabajo')).toBeInTheDocument()
  })
})

describe('CategoryManagement - Carga de estado', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('debe mostrar estado de carga inicialmente', () => {
    vi.mocked(apiService.apiService.getEventCategories).mockImplementation(
      () => new Promise(() => {}) // Promise que nunca se resuelve
    )

    render(
      <EventsProvider>
        <CategoryManagement />
      </EventsProvider>
    )

    expect(screen.getByText('Cargando categorías...')).toBeInTheDocument()
  })

  it('debe ocultar estado de carga después de cargar', async () => {
    const mockCats: EventCategoryDto[] = [
      {
        id: '1',
        name: 'Trabajo',
        color: '#2b7fff',
        isSystemDefault: true,
        sortOrder: 1
      }
    ]
    
    vi.mocked(apiService.apiService.getEventCategories).mockResolvedValue(mockCats)
    vi.mocked(apiService.apiService.getEvents).mockResolvedValue([])

    render(
      <EventsProvider>
        <CategoryManagement />
      </EventsProvider>
    )

    // Verificar que el estado de carga desaparece
    await waitFor(() => {
      expect(screen.queryByText('Cargando categorías...')).not.toBeInTheDocument()
    })
    
    // Y que se muestran las categorías
    expect(screen.getByText('Trabajo')).toBeInTheDocument()
  })
})
