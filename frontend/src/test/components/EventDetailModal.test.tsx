import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import EventDetailModal from '../../components/EventDetailModal'

const mockEvent = {
  id: 'evt-1',
  title: 'Reunión semanal',
  description: 'Planificación del sprint',
  startDate: '2025-10-10T10:00:00',
  endDate: '2025-10-10T11:00:00',
  isAllDay: false,
  location: 'Sala Norte',
  color: '#1447e6',
  notes: 'Alta',
  isRecurring: false,
  eventCategory: {
    id: 'cat-1',
    name: 'Trabajo',
    description: 'Eventos laborales',
    color: '#1447e6',
    icon: undefined,
    isSystemDefault: true,
    sortOrder: 1
  }
}

describe('EventDetailModal', () => {
  it('renders event information when open', () => {
    render(
      <EventDetailModal
        isOpen
        event={mockEvent}
        onClose={() => { }}
        onEdit={() => { }}
        onDelete={() => { }}
      />
    )

    expect(screen.getByRole('dialog', { name: /Reunión semanal/i })).toBeInTheDocument()
    expect(screen.getByText('Planificación del sprint')).toBeInTheDocument()
    expect(screen.getByText('Sala Norte')).toBeInTheDocument()
    expect(screen.getByText('Horario')).toBeInTheDocument()
  })

  it('calls edit and delete callbacks', async () => {
    const user = userEvent.setup()
    const onEdit = vi.fn()
    const onDelete = vi.fn()

    render(
      <EventDetailModal
        isOpen
        event={mockEvent}
        onClose={() => { }}
        onEdit={onEdit}
        onDelete={onDelete}
      />
    )

    await user.click(screen.getByText('Editar'))
    await user.click(screen.getByText('Eliminar'))

    expect(onEdit).toHaveBeenCalledWith(mockEvent)
    expect(onDelete).toHaveBeenCalledWith(mockEvent)
  })
})
