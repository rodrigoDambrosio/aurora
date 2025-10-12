import { describe, expect, it } from 'vitest'
import { apiService } from '../../services/apiService'

describe('ApiService', () => {
  describe('checkHealth', () => {
    it('should return health status', async () => {
      const result = await apiService.checkHealth()

      expect(result).toEqual({
        status: 'Healthy',
        message: 'Aurora API is running successfully!',
        timestamp: expect.any(String),
        version: '1.0.0',
        environment: 'Test'
      })
    })
  })

  describe('getWeeklyEvents', () => {
    it('should return weekly events', async () => {
      const result = await apiService.getWeeklyEvents('2025-09-29T00:00:00.000Z')

      expect(result).toEqual({
        weekStart: '2025-09-29T00:00:00.000Z',
        weekEnd: expect.any(String),
        events: expect.arrayContaining([
          expect.objectContaining({
            id: '1',
            title: 'Reunión de equipo',
            description: 'Reunión semanal del equipo'
          })
        ]),
        categories: expect.arrayContaining([
          expect.objectContaining({
            id: '1',
            name: 'Trabajo'
          })
        ]),
        userId: 'demo-user-id'
      })
    })
  })

  describe('getEventCategories', () => {
    it('should return event categories', async () => {
      const result = await apiService.getEventCategories()

      expect(result).toEqual([
        {
          id: '1',
          name: 'Trabajo',
          description: 'Eventos relacionados con el trabajo',
          color: '#3b82f6',
          isSystemDefault: true,
          sortOrder: 1
        },
        {
          id: '2',
          name: 'Personal',
          description: 'Eventos personales',
          color: '#10b981',
          isSystemDefault: true,
          sortOrder: 2
        }
      ])
    })
  })

  describe('createEvent', () => {
    it('should create a new event', async () => {
      const eventData = {
        title: 'Test Event',
        startDate: '2025-09-30T10:00:00.000Z',
        endDate: '2025-09-30T11:00:00.000Z',
        isAllDay: false,
        eventCategoryId: '1',
        priority: 2,
        timezoneOffsetMinutes: 0
      }

      const result = await apiService.createEvent(eventData)

      expect(result).toEqual({
        id: expect.any(String),
        title: 'Test Event',
        startDate: '2025-09-30T10:00:00.000Z',
        endDate: '2025-09-30T11:00:00.000Z',
        isAllDay: false,
        isRecurring: false,
        priority: 2,
        eventCategory: expect.objectContaining({
          id: '1',
          name: 'Trabajo'
        })
      })
    })
  })

  describe('validateEvent', () => {
    it('should return AI validation feedback', async () => {
      const eventData = {
        title: 'Evento Temprano',
        startDate: '2025-09-30T04:00:00.000Z',
        endDate: '2025-09-30T05:00:00.000Z',
        isAllDay: false,
        eventCategoryId: '1',
        priority: 2,
        timezoneOffsetMinutes: 0
      }

      const result = await apiService.validateEvent(eventData)

      expect(result).toEqual({
        isApproved: false,
        recommendationMessage: expect.stringContaining('evitar eventos antes'),
        severity: 'Warning',
        suggestions: expect.arrayContaining([
          'Considera reprogramar a un horario diurno'
        ]),
        usedAi: true
      })
    })
  })

  describe('getEvent', () => {
    it('should return a single event', async () => {
      const result = await apiService.getEvent('1')

      expect(result).toEqual({
        id: '1',
        title: 'Reunión de equipo',
        description: 'Reunión semanal del equipo',
        startDate: '2025-09-29T09:00:00.000Z',
        endDate: '2025-09-29T10:00:00.000Z',
        isAllDay: false,
        isRecurring: false,
        priority: expect.any(Number),
        eventCategory: expect.objectContaining({
          id: '1',
          name: 'Trabajo'
        })
      })
    })

    it('should handle not found errors', async () => {
      await expect(apiService.getEvent('nonexistent')).rejects.toMatchObject({ status: 404 })
    })
  })

  describe('updateEvent', () => {
    it('should update an existing event', async () => {
      const eventData = {
        title: 'Updated Event',
        startDate: '2025-09-30T10:00:00.000Z',
        endDate: '2025-09-30T11:00:00.000Z',
        isAllDay: false,
        eventCategoryId: '1',
        priority: 3,
        timezoneOffsetMinutes: 0
      }

      const result = await apiService.updateEvent('1', eventData)

      expect(result).toEqual({
        id: '1',
        title: 'Updated Event',
        startDate: '2025-09-30T10:00:00.000Z',
        endDate: '2025-09-30T11:00:00.000Z',
        isAllDay: false,
        isRecurring: false,
        priority: 3,
        description: 'Reunión semanal del equipo', // From existing data
        eventCategory: expect.objectContaining({
          id: '1',
          name: 'Trabajo'
        })
      })
    })

    it('should handle not found errors', async () => {
      const eventData = {
        title: 'Updated Event',
        startDate: '2025-09-30T10:00:00.000Z',
        endDate: '2025-09-30T11:00:00.000Z',
        isAllDay: false,
        eventCategoryId: '1',
        priority: 2,
        timezoneOffsetMinutes: 0
      }

      await expect(apiService.updateEvent('nonexistent', eventData)).rejects.toMatchObject({ status: 404 })
    })
  })

  describe('deleteEvent', () => {
    it('should delete an event', async () => {
      // Should not throw
      await apiService.deleteEvent('1')
    })

    it('should handle not found errors when deleting', async () => {
      await expect(apiService.deleteEvent('nonexistent')).rejects.toMatchObject({ status: 404 })
    })
  })
})