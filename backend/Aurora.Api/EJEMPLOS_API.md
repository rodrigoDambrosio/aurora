# Ejemplos de Request para probar la validación de IA con Contexto

## Contexto: ¿Cómo funciona?

La IA analiza **eventos cercanos** (1 día antes hasta 7 días después) del evento que intentas crear.

---

## 1. Evento apropiado sin conflictos (? Aprobado)

**Escenario**: Primer evento del día, horario laboral normal

```bash
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Reunión de equipo",
    "description": "Reunión semanal de seguimiento",
    "startDate": "2024-06-10T10:00:00",
    "endDate": "2024-06-10T11:00:00",
    "isAllDay": false,
    "location": "Sala de conferencias",
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'
```

**Resultado esperado**: ? HTTP 201 - Evento creado
**Razón**: Horario apropiado, sin conflictos, primera reunión del día

---

## 2. Conflicto directo de horario (? Rechazado - Critical)

**Escenario**: Ya existe "Standup daily" de 9:00 a 9:30

**Paso 1**: Crear el primer evento
```bash
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Standup daily",
    "description": "Reunión diaria del equipo",
    "startDate": "2024-06-10T09:00:00",
    "endDate": "2024-06-10T09:30:00",
    "isAllDay": false,
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'
```

**Paso 2**: Intentar crear evento que se superpone
```bash
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Llamada con cliente",
    "description": "Discusión de proyecto",
    "startDate": "2024-06-10T09:15:00",
    "endDate": "2024-06-10T10:00:00",
    "isAllDay": false,
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'
```

**Resultado esperado**: ? HTTP 400 - Conflicto detectado
```json
{
  "title": "Recomendación de IA",
  "detail": "Ya tienes 'Standup daily' programado de 9:00 a 9:30. Este evento se superpone directamente.",
  "severity": "Critical",
  "suggestions": [
    "Reprograma la llamada para después del standup (9:30 AM)",
    "Considera acortar el standup si es urgente"
  ]
}
```

---

## 3. Día sobrecargado (?? Warning)

**Escenario**: Ya tienes 4 reuniones programadas ese día

**Paso 1**: Crear varios eventos primero
```bash
# Evento 1
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Standup daily",
    "startDate": "2024-06-11T09:00:00",
    "endDate": "2024-06-11T09:30:00",
    "isAllDay": false,
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'

# Evento 2
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Planning de sprint",
    "startDate": "2024-06-11T10:00:00",
    "endDate": "2024-06-11T12:00:00",
    "isAllDay": false,
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'

# Evento 3
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Revisión de código",
    "startDate": "2024-06-11T14:00:00",
    "endDate": "2024-06-11T15:30:00",
    "isAllDay": false,
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'

# Evento 4
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Demo con cliente",
    "startDate": "2024-06-11T16:00:00",
    "endDate": "2024-06-11T17:00:00",
    "isAllDay": false,
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'
```

**Paso 2**: Intentar agregar una quinta reunión
```bash
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Reunión de ventas",
    "startDate": "2024-06-11T17:30:00",
    "endDate": "2024-06-11T18:30:00",
    "isAllDay": false,
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'
```

**Resultado esperado**: ?? HTTP 400 - Warning por sobrecarga
```json
{
  "title": "Recomendación de IA",
  "detail": "Ya tienes 4 reuniones ese día. Agregar otra más sobrecarga tu calendario y no deja tiempo para trabajo enfocado.",
  "severity": "Warning",
  "suggestions": [
    "Reprograma para mañana si no es urgente",
    "Considera combinarla con la 'Demo con cliente' si están relacionadas",
    "Deja al menos 2 horas libres para trabajo productivo"
  ]
}
```

---

## 4. Evento a deshora con consecuencias (? Rechazado)

**Escenario**: Tienes "Presentación importante" a las 8 AM, intentas crear fiesta hasta tarde la noche anterior

**Paso 1**: Crear evento importante temprano
```bash
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Presentación importante Q1",
    "description": "Presentación de resultados al CEO",
    "startDate": "2024-06-12T08:00:00",
    "endDate": "2024-06-12T10:00:00",
    "isAllDay": false,
    "location": "Sala ejecutiva",
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'
```

**Paso 2**: Intentar crear evento nocturno previo
```bash
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Fiesta de cumpleaños",
    "description": "Celebración",
    "startDate": "2024-06-11T22:00:00",
    "endDate": "2024-06-12T02:00:00",
    "isAllDay": false,
    "location": "Casa de Juan",
    "eventCategoryId": "00000000-0000-0000-0000-000000000003"
  }'
```

**Resultado esperado**: ? HTTP 400 - Rechazado por afectar descanso
```json
{
  "title": "Recomendación de IA",
  "detail": "Tienes 'Presentación importante Q1' a las 8:00 AM del día siguiente. Una fiesta que termina a las 2:00 AM te dará solo 6 horas de descanso y afectará tu desempeño.",
  "severity": "Critical",
  "suggestions": [
    "Reprograma la fiesta para el viernes cuando no tienes eventos importantes temprano",
    "Si es inevitable, termina la fiesta antes de las 11 PM",
    "Considera cambiar la presentación para la tarde si es posible"
  ]
}
```

---

## 5. Evento saludable después de día cargado (? Aprobado con recomendación)

**Escenario**: Después de 8 horas de trabajo, quieres hacer ejercicio

**Paso 1**: Crear día de trabajo completo
```bash
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Jornada laboral",
    "startDate": "2024-06-13T09:00:00",
    "endDate": "2024-06-13T17:00:00",
    "isAllDay": false,
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'
```

**Paso 2**: Agregar sesión de ejercicio
```bash
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Gimnasio",
    "description": "Entrenamiento de fuerza",
    "startDate": "2024-06-13T18:00:00",
    "endDate": "2024-06-13T19:30:00",
    "isAllDay": false,
    "location": "Gym Life",
    "eventCategoryId": "00000000-0000-0000-0000-000000000002"
  }'
```

**Resultado esperado**: ? HTTP 201 - Aprobado
**Razón**: La IA reconoce que después de un día de trabajo, ejercicio es saludable y está en horario apropiado

---

## 6. Sin tiempo de descanso entre reuniones (?? Warning)

**Paso 1**: Crear reunión
```bash
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Reunión de diseño",
    "startDate": "2024-06-14T10:00:00",
    "endDate": "2024-06-14T12:00:00",
    "isAllDay": false,
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'
```

**Paso 2**: Intentar crear otra inmediatamente después
```bash
curl -X POST "http://localhost:5000/api/events" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Revisión de presupuesto",
    "startDate": "2024-06-14T12:00:00",
    "endDate": "2024-06-14T13:30:00",
    "isAllDay": false,
    "eventCategoryId": "00000000-0000-0000-0000-000000000001"
  }'
```

**Resultado esperado**: ?? HTTP 400 o aprobado con warning
```json
{
  "title": "Recomendación de IA",
  "detail": "No hay tiempo de descanso entre 'Reunión de diseño' y este evento. Es importante tener breaks entre reuniones.",
  "severity": "Warning",
  "suggestions": [
    "Deja al menos 15 minutos entre reuniones",
    "Programa la revisión para las 12:15 PM",
    "Aprovecha para tomar agua, estirar o ir al baño"
  ]
}
```

---

## Comandos útiles para testing

### Ver todos los eventos
```bash
curl http://localhost:5000/api/events
```

### Ver eventos de una semana específica
```bash
curl -X POST "http://localhost:5000/api/events/weekly" \
  -H "Content-Type: application/json" \
  -d '{
    "weekStart": "2024-06-10T00:00:00"
  }'
```

### Eliminar un evento
```bash
curl -X DELETE "http://localhost:5000/api/events/{event-id}"
```

### Verificar estado del servicio
```bash
curl http://localhost:5000/health
```

---

## Tips para probar

1. **Usa fechas futuras**: Usa fechas próximas pero futuras para evitar conflictos con eventos antiguos
2. **Limpia la BD**: Si quieres empezar desde cero, elimina `aurora.db`
3. **Revisa logs**: Los logs muestran cuántos eventos se encontraron como contexto
4. **Prueba patrones**: Crea secuencias de eventos para ver cómo la IA analiza patrones
5. **Varía categorías**: Diferentes categorías (trabajo, personal, ejercicio) afectan las recomendaciones

---

## Notas

- La IA da respuestas ligeramente diferentes cada vez (es generativa)
- Si la llamada a Gemini falla, el sistema aprueba por defecto
- Los eventos se buscan en ventana: **1 día antes** hasta **7 días después**
- Puedes ajustar la ventana de contexto en el código si necesitas más/menos contexto
