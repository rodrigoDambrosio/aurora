# Configuración de Validación de IA con Gemini

Este proyecto incluye **validación inteligente y contextual** de eventos usando Google Gemini AI.

## ?? ¿Qué hace diferente a esta validación?

La IA no solo valida el evento individual, sino que **analiza el contexto completo de tu calendario** para dar recomendaciones personalizadas y evitar problemas como:

? **Conflictos de horario** - Detecta si ya tienes otro evento a esa hora  
? **Sobrecarga de trabajo** - Te avisa si ya tienes demasiados eventos ese día  
? **Balance vida-trabajo** - Sugiere si necesitas más tiempo libre  
? **Descanso entre eventos** - Verifica que tengas tiempo para respirar  
? **Patrones saludables** - Cuida que respetes horarios de descanso  
? **Recomendaciones específicas** - Basadas en tus eventos existentes

## Configuración de la API Key

### Paso 1: Obtener tu API Key de Gemini

1. Ve a [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Crea una nueva API Key
3. Copia la API Key generada

### Paso 2: Configurar localmente

Edita el archivo `Aurora.Api/appsettings.Development.json` y reemplaza `TU_API_KEY_AQUI` con tu API Key real:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Gemini": {
    "ApiKey": "AIzaSy..."  // Tu API Key aquí
  }
}
```

**?? IMPORTANTE**: El archivo `appsettings.Development.json` está en `.gitignore` para evitar que tu API Key se suba al repositorio.

### Paso 3: Ejecutar el proyecto

```bash
cd Aurora.Api
dotnet run
```

## ¿Cómo funciona?

Cuando creas un nuevo evento a través del endpoint `POST /api/events`, el sistema:

### 1. **Obtiene el contexto del calendario**
   - Busca eventos desde 1 día antes hasta 1 semana después del evento a crear
   - Recopila información sobre títulos, horarios, duraciones y categorías

### 2. **Envía el contexto completo a la IA**
   - El nuevo evento que quieres crear
   - Todos los eventos cercanos para dar contexto
   - Prompt estructurado con criterios de análisis

### 3. **La IA analiza inteligentemente**:
   - ? **Conflictos directos**: "Ya tienes 'Reunión de equipo' de 10:00 a 11:00"
   - ?? **Sobrecarga**: "Ya tienes 5 reuniones ese día, considera reprogramar"
   - ?? **Balance**: "Has trabajado 8 horas, este evento de ejercicio es buena idea"
   - ?? **Descanso**: "Programar esto a las 2 AM afecta tu descanso después de un día completo"

### 4. **Toma acción**:
   - ? **Si aprueba**: Crea el evento normalmente
   - ? **Si no aprueba**: Retorna HTTP 400 con recomendaciones específicas y contextuales

## Ejemplo de Respuesta cuando la IA rechaza un evento

### Escenario: Intentas crear "Reunión de ventas" cuando ya tienes 4 reuniones ese día

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Recomendación de IA",
  "status": 400,
  "detail": "Ya tienes 4 reuniones programadas ese día (Standup daily a las 9:00, Planning de sprint a las 10:30, Revisión de diseño a las 14:00, y Demo con cliente a las 16:00). Agregar otra reunión sobrecarga tu día y no deja tiempo para trabajo enfocado.",
  "severity": "Warning",
  "suggestions": [
    "Considera programar esta reunión para mañana que solo tienes 2 eventos",
    "Combina esta reunión con la 'Demo con cliente' si los temas están relacionados",
    "Deja al menos 2 horas libres entre reuniones para trabajo productivo"
  ]
}
```

### Escenario: Intentas crear "Fiesta" a las 11 PM un martes cuando tienes "Reunión importante" a las 8 AM del miércoles

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Recomendación de IA",
  "status": 400,
  "detail": "Tienes 'Reunión importante' programada para las 8:00 AM del día siguiente. Una fiesta que termina tarde puede afectar tu desempeño en esa reunión.",
  "severity": "Critical",
  "suggestions": [
    "Reprograma la fiesta para el viernes que no tienes eventos temprano el sábado",
    "Si es inevitable, termina la fiesta más temprano (antes de las 10 PM)",
    "Considera cambiar la reunión importante a la tarde si es posible"
  ]
}
```

## Arquitectura

La validación de IA sigue los principios de Clean Architecture:

```
Aurora.Api (Presentation)
    ??? EventsController
         ??? Obtiene eventos cercanos del calendario (contexto)
         ??? Llama a IAIValidationService con contexto

Aurora.Application (Application)
    ??? IAIValidationService (Interface)
         ??? ValidateEventCreationAsync(evento, userId, eventosExistentes)

Aurora.Infrastructure (Infrastructure)
    ??? GeminiAIValidationService (Implementation)
         ??? Construye prompt con contexto del calendario
         ??? Llama a Gemini API
         ??? Parsea respuesta estructurada
```

## Ventana de Contexto

Por defecto, la IA analiza:
- **1 día antes** del evento a crear
- **7 días después** del evento a crear

Esto da suficiente contexto para detectar:
- Conflictos inmediatos
- Sobrecarga semanal
- Patrones de trabajo/descanso

Puedes ajustar esta ventana en `EventsController.CreateEvent()`:
```csharp
var contextStartDate = createEventDto.StartDate.AddDays(-1);  // Cambiar aquí
var contextEndDate = createEventDto.StartDate.AddDays(7);     // Cambiar aquí
```

## Criterios de la IA

La IA usa estos criterios para aprobar o rechazar:

| Criterio | Aprobado | Warning | Critical |
|----------|----------|---------|----------|
| **Sin conflictos** | ? | - | - |
| **Conflicto parcial** | - | ?? | - |
| **Conflicto directo** | - | - | ? |
| **1-3 eventos/día** | ? | - | - |
| **4-6 eventos/día** | - | ?? | - |
| **7+ eventos/día** | - | - | ? |
| **Hora apropiada** | ? | - | - |
| **Hora cuestionable** | - | ?? | - |
| **Hora inapropiada** | - | - | ? |

## Desactivar la validación de IA (Opcional)

Si necesitas desactivar temporalmente la validación:

### Opción 1: Aprobar siempre en el servicio
En `GeminiAIValidationService.cs`:
```csharp
public async Task<AIValidationResult> ValidateEventCreationAsync(...)
{
    // Desactivar validación temporalmente
    return new AIValidationResult
    {
        IsApproved = true,
        Severity = AIValidationSeverity.Info,
        RecommendationMessage = "Validación desactivada"
    };
}
```

### Opción 2: Comentar la validación en el controlador
En `EventsController.cs`:
```csharp
// var aiValidation = await _aiValidationService.ValidateEventCreationAsync(...);
// if (!aiValidation.IsApproved) { ... }

// Crear directamente sin validación
var createdEvent = await _eventService.CreateEventAsync(userId, createEventDto);
```

## Costos

Google Gemini tiene un tier gratuito generoso:
- **Gemini 2.0 Flash**: 1,500 requests/día gratis
- Cada validación = 1 request

Para la mayoría de usuarios personales, nunca excederás el límite gratuito.

Revisa los límites en [Google AI Pricing](https://ai.google.dev/pricing).

## Troubleshooting

### Error: "Gemini API Key no configurada"
- Asegúrate de haber configurado la API Key en `appsettings.Development.json`
- Verifica que el formato sea correcto (sin espacios ni comillas extra)

### Error: "403 Forbidden"
- Verifica que tu API Key sea válida
- Asegúrate de que tu API Key tenga permisos para usar Gemini API
- Verifica que no hayas excedido el rate limit

### La validación siempre aprueba
- Revisa los logs para ver si hay errores en la comunicación con Gemini
- Por diseño, si la IA falla, el sistema aprueba el evento por defecto para no bloquear la funcionalidad
- Verifica que tu API Key esté configurada correctamente

### La IA no detecta conflictos obvios
- Verifica que los eventos existentes se estén obteniendo correctamente (revisa logs)
- La ventana de contexto puede ser demasiado pequeña, ajústala si es necesario
- Verifica que las fechas de los eventos sean correctas en la base de datos

### Respuestas muy largas o timeout
- El timeout por defecto es 30 segundos
- Puedes ajustarlo en `Program.cs`:
```csharp
builder.Services.AddHttpClient<IAIValidationService, GeminiAIValidationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60); // Aumentar a 60 segundos
});
```

## Ejemplos de Uso

Ver archivo `EJEMPLOS_API.md` para ejemplos completos de curl.

## Mejoras Futuras

Ideas para extender la funcionalidad:

1. **Preferencias de usuario**: Respetar horarios preferidos (no molestar, horas de trabajo)
2. **Análisis de productividad**: Detectar patrones de sobrecarga recurrentes
3. **Sugerencias proactivas**: "Parece que siempre programas gym los martes, ¿quieres crear un evento recurrente?"
4. **Integración con clima/tráfico**: "Hay lluvia ese día, considera una actividad indoor"
5. **Machine Learning personalizado**: Aprender de tus patrones y preferencias
