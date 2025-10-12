import { http, HttpResponse } from 'msw'
import type {
  CreateEventDto,
  EventCategoryDto,
  EventDto,
  EventPriority,
  WeeklyEventsResponseDto,
} from '../../services/apiService'

// Mock data for testing
const mockCategories: EventCategoryDto[] = [
  {
    id: '1',
    name: 'Trabajo',
    description: 'Eventos relacionados con el trabajo',
    color: '#3b82f6',
    isSystemDefault: true,
    sortOrder: 1,
  },
  {
    id: '2',
    name: 'Personal',
    description: 'Eventos personales',
    color: '#10b981',
    isSystemDefault: true,
    sortOrder: 2,
  },
]

const mockEvents: EventDto[] = [
  {
    id: '1',
    title: 'Reunión de equipo',
    description: 'Reunión semanal del equipo',
    startDate: '2025-09-29T09:00:00.000Z',
    endDate: '2025-09-29T10:00:00.000Z',
    isAllDay: false,
    isRecurring: false,
    priority: 2 as EventPriority,
    eventCategory: mockCategories[0],
  },
  {
    id: '2',
    title: 'Almuerzo con amigos',
    description: 'Almuerzo en el restaurante',
    startDate: '2025-09-30T13:00:00.000Z',
    endDate: '2025-09-30T15:00:00.000Z',
    isAllDay: false,
    isRecurring: false,
    priority: 2 as EventPriority,
    eventCategory: mockCategories[1],
  },
]

export const handlers = [
  // Health check endpoint
  http.get('/api/health', () => {
    return HttpResponse.json({
      status: 'Healthy',
      message: 'Aurora API is running successfully!',
      timestamp: new Date().toISOString(),
      version: '1.0.0',
      environment: 'Test',
    })
  }),

  // Weekly events endpoint
  http.post('/api/events/weekly', async ({ request }) => {
    const body = await request.json()
    const weekStart = (body as { weekStart: string }).weekStart

    const mockResponse: WeeklyEventsResponseDto = {
      weekStart,
      weekEnd: new Date(new Date(weekStart).getTime() + 6 * 24 * 60 * 60 * 1000).toISOString(),
      events: mockEvents,
      categories: mockCategories,
      userId: 'demo-user-id',
    }

    return HttpResponse.json(mockResponse)
  }),

  // Event categories endpoint
  http.get('/api/eventcategories', () => {
    return HttpResponse.json(mockCategories)
  }),

  // Get single event
  http.get('/api/events/:id', ({ params }) => {
    const eventId = params.id as string
    const event = mockEvents.find(e => e.id === eventId)

    if (!event) {
      return new HttpResponse(null, { status: 404 })
    }

    return HttpResponse.json(event)
  }),

  // Create event
  http.post('/api/events', async ({ request }) => {
    const newEvent = await request.json() as CreateEventDto
    const category = mockCategories.find((cat) => cat.id === newEvent.eventCategoryId) ?? mockCategories[0]

    const createdEvent: EventDto = {
      title: newEvent.title,
      description: newEvent.description,
      startDate: newEvent.startDate,
      endDate: newEvent.endDate,
      isAllDay: newEvent.isAllDay,
      isRecurring: false,
      priority: newEvent.priority,
      location: newEvent.location,
      color: newEvent.color,
      notes: newEvent.notes,
      id: Math.random().toString(36).substr(2, 9),
      eventCategory: category,
    }

    return HttpResponse.json(createdEvent, { status: 201 })
  }),

  // Manual AI validation endpoint
  http.post('/api/events/validate', async ({ request }) => {
    const payload = await request.json() as CreateEventDto
    const startHour = new Date(payload.startDate).getUTCHours()

    if (Number.isNaN(startHour)) {
      return HttpResponse.json(
        {
          isApproved: false,
          recommendationMessage: 'Horario inválido para el análisis',
          severity: 'Warning',
          suggestions: ['Revisa la fecha y hora proporcionadas'],
          usedAi: false
        },
        { status: 200 }
      )
    }

    if (startHour < 6) {
      return HttpResponse.json({
        isApproved: false,
        recommendationMessage: 'La IA recomienda evitar eventos antes de las 6 AM.',
        severity: 'Warning',
        suggestions: ['Considera reprogramar a un horario diurno'],
        usedAi: true
      })
    }

    return HttpResponse.json({
      isApproved: true,
      recommendationMessage: 'La IA no encontró conflictos con este evento.',
      severity: 'Info',
      suggestions: [],
      usedAi: true
    })
  }),

  // Update event
  http.put('/api/events/:id', async ({ params, request }) => {
    const eventId = params.id as string
    const updatedData = await request.json() as Partial<CreateEventDto>

    const existingEvent = mockEvents.find(e => e.id === eventId)
    if (!existingEvent) {
      return new HttpResponse(null, { status: 404 })
    }

    const category = updatedData.eventCategoryId
      ? mockCategories.find((cat) => cat.id === updatedData.eventCategoryId) ?? existingEvent.eventCategory
      : existingEvent.eventCategory

    const updatedEvent: EventDto = {
      ...existingEvent,
      title: updatedData.title ?? existingEvent.title,
      description: updatedData.description ?? existingEvent.description,
      startDate: updatedData.startDate ?? existingEvent.startDate,
      endDate: updatedData.endDate ?? existingEvent.endDate,
      isAllDay: updatedData.isAllDay ?? existingEvent.isAllDay,
      isRecurring: existingEvent.isRecurring,
      priority: updatedData.priority ?? existingEvent.priority,
      location: updatedData.location ?? existingEvent.location,
      color: updatedData.color ?? existingEvent.color,
      notes: updatedData.notes ?? existingEvent.notes,
      eventCategory: category,
    }

    return HttpResponse.json(updatedEvent)
  }),

  // Delete event
  http.delete('/api/events/:id', ({ params }) => {
    const eventId = params.id as string
    const eventExists = mockEvents.some(e => e.id === eventId)

    if (!eventExists) {
      return new HttpResponse(null, { status: 404 })
    }

    return new HttpResponse(null, { status: 204 })
  }),
]