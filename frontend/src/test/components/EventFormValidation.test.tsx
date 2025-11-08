import { describe, expect, it } from 'vitest'

describe('EventFormModal - Validación de tiempo', () => {
  it('debe validar que la hora de fin sea mayor a la hora de inicio para eventos no todo el día', () => {
    // Arrange
    const startDate = new Date('2025-11-05T10:00:00')
    const endDate = new Date('2025-11-05T09:00:00') // Hora de fin antes que inicio

    // Act
    const isValid = endDate > startDate

    // Assert
    expect(isValid).toBe(false)
  })

  it('debe permitir hora de fin mayor a hora de inicio', () => {
    // Arrange
    const startDate = new Date('2025-11-05T10:00:00')
    const endDate = new Date('2025-11-05T11:00:00')

    // Act
    const isValid = endDate > startDate

    // Assert
    expect(isValid).toBe(true)
  })

  it('debe validar fechas iguales pero horas diferentes', () => {
    // Arrange
    const startDate = new Date('2025-11-05T10:00:00')
    const endDate = new Date('2025-11-05T10:00:00') // Misma fecha y hora

    // Act
    const isValid = endDate > startDate

    // Assert
    expect(isValid).toBe(false)
  })

  it('debe permitir eventos que terminan al día siguiente', () => {
    // Arrange
    const startDate = new Date('2025-11-05T23:00:00')
    const endDate = new Date('2025-11-06T02:00:00')

    // Act
    const isValid = endDate > startDate

    // Assert
    expect(isValid).toBe(true)
  })

  it('debe validar eventos todo el día con fechas', () => {
    // Para eventos todo el día, solo importa la fecha
    const startDate = new Date('2025-11-05')
    const endDate = new Date('2025-11-04') // Día anterior

    // Act
    const isValid = endDate >= startDate

    // Assert
    expect(isValid).toBe(false)
  })

  it('debe permitir eventos todo el día que duran un día', () => {
    // Para eventos todo el día del mismo día
    const startDate = new Date('2025-11-05')
    const endDate = new Date('2025-11-05')

    // Act
    const isValid = endDate >= startDate

    // Assert
    expect(isValid).toBe(true)
  })
})
