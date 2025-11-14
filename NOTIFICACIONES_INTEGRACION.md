# Sistema de Notificaciones - Guía de Integración

## Resumen

Se ha implementado un sistema completo de recordatorios y notificaciones del navegador para Aurora. Este documento describe cómo completar la integración.

## ✅ Componentes Implementados

### Backend

- ✅ `Aurora.Domain/Entities/EventReminder.cs` - Entidad de recordatorio
- ✅ `Aurora.Domain/Enums/ReminderType.cs` - Enum de tipos de recordatorio
- ✅ `Aurora.Application/Services/ReminderService.cs` - Servicio de negocio
- ✅ `Aurora.Application/Services/Helpers/ReminderCalculator.cs` - Helper para cálculos
- ✅ `Aurora.Application/DTOs/ReminderDto.cs` y `CreateReminderDto.cs`
- ✅ `Aurora.Application/Validators/CreateReminderDtoValidator.cs`
- ✅ `Aurora.Infrastructure/Data/Configurations/EventReminderConfiguration.cs`
- ✅ `Aurora.Api/Controllers/RemindersController.cs`
- ✅ `Program.cs` actualizado con dependency injection

### Frontend

- ✅ `src/types/reminder.types.ts` - Tipos TypeScript
- ✅ `src/services/notificationService.ts` - Servicio de Notifications API
- ✅ `src/services/apiService.ts` - Extendido con endpoints de reminders
- ✅ `src/hooks/useNotifications.ts` - Hook de polling y notificaciones
- ✅ `src/hooks/useReminders.ts` - Hook CRUD de recordatorios
- ✅ `src/components/NotificationPermissionBanner.tsx`
- ✅ `src/components/ReminderPickerModal.tsx`
- ✅ `src/components/ReminderSection.tsx`

## 📋 Pasos Pendientes para Completar la Integración

### 1. Crear y Aplicar la Migración de Base de Datos

**IMPORTANTE**: Actualmente el backend está corriendo y bloquea los archivos DLL. Debes:

1. **Detener el backend** que está corriendo
2. Ejecutar los siguientes comandos:

```powershell
# Desde el directorio raíz del proyecto
cd c:\repos\aurora\backend\Aurora.Api

# Crear la migración
dotnet ef migrations add AddReminders --project ../Aurora.Infrastructure/Aurora.Infrastructure.csproj --context AuroraDbContext

# Aplicar la migración a la base de datos
dotnet ef database update --project ../Aurora.Infrastructure/Aurora.Infrastructure.csproj --context AuroraDbContext
```

3. **Reiniciar el backend**

### 2. Integrar ReminderSection en EventFormModal

Abre `frontend/src/components/EventFormModal.tsx` y:

**a) Importar el componente:**

```tsx
import { ReminderSection } from "./ReminderSection";
```

**b) Agregar la sección en el formulario** (después de los campos de prioridad o antes del footer):

```tsx
{
  /* Sección de recordatorios - solo para modo edición */
}
{
  !isCreateMode && eventToEdit && (
    <div className="space-y-2">
      <ReminderSection
        eventId={eventToEdit.id}
        eventStartDate={eventToEdit.startDate}
      />
    </div>
  );
}
```

**Nota**: Los recordatorios solo se pueden agregar después de crear el evento, por eso está condicionado a `!isCreateMode`.

### 3. Integrar NotificationPermissionBanner en App.tsx

Abre `frontend/src/App.tsx` (o el componente raíz principal) y:

**a) Importar componentes y hooks:**

```tsx
import { NotificationPermissionBanner } from "./components/NotificationPermissionBanner";
import { useNotifications } from "./hooks/useNotifications";
import { notificationService } from "./services/notificationService";
```

**b) Dentro del componente principal:**

```tsx
function App() {
  const { permission, requestPermission } = useNotifications();

  // Banner solo se muestra si:
  // - El navegador soporta notificaciones
  // - Los permisos están en 'default' (ni granted ni denied)
  // - El usuario no ha descartado el banner
  const shouldShowBanner =
    notificationService.isSupported() &&
    permission === "default" &&
    !notificationService.hasUserDismissedBanner();

  return (
    <>
      {shouldShowBanner && (
        <NotificationPermissionBanner onPermissionGranted={requestPermission} />
      )}

      {/* Resto de tu aplicación */}
      <MainDashboard />
    </>
  );
}
```

### 4. Agregar Íconos de Notificación (Opcional)

Si quieres personalizar los íconos de las notificaciones del navegador:

1. Agrega estos archivos en `frontend/public/`:

   - `aurora-icon.png` (256x256px o 512x512px)
   - `aurora-badge.png` (96x96px para badges pequeños)

2. Si no los agregas, las notificaciones usarán el ícono por defecto del navegador.

## 🧪 Testing Manual

### Test 1: Solicitar Permisos

1. Abre la aplicación
2. Debe aparecer el banner amarillo en la parte superior
3. Click en "Habilitar notificaciones"
4. El navegador debe mostrar el popup de permisos
5. Acepta los permisos
6. El banner debe desaparecer

### Test 2: Crear un Recordatorio

1. Abre o edita un evento existente
2. En el formulario, debe aparecer la sección "Recordatorios"
3. Click en "+ Agregar recordatorio"
4. Selecciona "15 minutos antes"
5. Click en "Agregar recordatorio"
6. El recordatorio debe aparecer en la lista

### Test 3: Recibir una Notificación

**Opción A (Para testing rápido):**

1. Crea un evento que empiece en 16 minutos
2. Agrega un recordatorio de "15 minutos antes"
3. Espera hasta 1 minuto (el polling verifica cada 60 segundos)
4. Deberías recibir la notificación del navegador

**Opción B (Simular con fecha pasada - requiere modificar código temporalmente):**

1. En el backend, comenta temporalmente la validación de fecha futura en `ReminderService.CreateReminderAsync`
2. Crea un evento con fecha/hora actual + 2 minutos
3. Agrega recordatorio de "15 minutos antes"
4. El recordatorio se disparará en el siguiente polling (máximo 60 segundos)

### Test 4: Navegación desde Notificación

1. Cuando recibas una notificación
2. Click en la notificación
3. La aplicación debe enfocarse/abrirse
4. Debe navegar al evento (si tienes rutas configuradas)

## 🎨 Estilos

Los componentes utilizan las clases de Tailwind existentes en el proyecto. Si notas algún problema de estilos:

- `text-primary-*` → Verifica que tu `tailwind.config.js` tenga definidos los colores primary
- `bg-amber-*` → Para el banner (colores de advertencia suaves)

## 🔧 Configuración Adicional

### Ajustar Intervalo de Polling

Si 60 segundos es demasiado largo/corto, edita en `frontend/src/hooks/useNotifications.ts`:

```typescript
const POLLING_INTERVAL = 60000; // Cambiar a 30000 para 30 segundos
```

### Tolerancia de Tiempo

El sistema tiene una tolerancia de ±2 minutos para disparar recordatorios. Para ajustarlo:

**Backend**: `Aurora.Application/Services/ReminderService.cs`

```csharp
var toleranceMinutes = 2; // Cambiar según necesites
```

**Frontend**: Esto se maneja automáticamente por el backend.

## 📱 Compatibilidad de Navegadores

| Navegador      | Soporte         | Notas                            |
| -------------- | --------------- | -------------------------------- |
| Chrome Desktop | ✅ Completo     |                                  |
| Edge           | ✅ Completo     |                                  |
| Firefox        | ✅ Completo     |                                  |
| Safari macOS   | ✅ 16.4+        | Solo en macOS 16.4+              |
| Safari iOS     | ❌ No soportado | iOS no soporta Notifications API |
| Chrome Android | ✅ Completo     |                                  |

## 🐛 Troubleshooting

### "No recibo notificaciones"

- Verifica que los permisos están en 'granted' (F12 → Console → `Notification.permission`)
- Verifica que el polling está activo (debería ver logs en consola cada 60s)
- Verifica que hay recordatorios pendientes: `GET /api/reminders/pending`

### "El banner no aparece"

- Verifica que `notificationService.isSupported()` retorna `true`
- Verifica que no hayas descartado el banner (`localStorage.getItem('notificationBannerDismissed')`)
- Para resetear: `localStorage.removeItem('notificationBannerDismissed')`

### "Error 404 en /api/reminders"

- Verifica que aplicaste la migración de base de datos
- Verifica que el backend compiló correctamente después de agregar el código
- Verifica que `RemindersController` está siendo escaneado por ASP.NET Core

### "Los recordatorios no se guardan"

- Verifica la validación del DTO (ver response en Network tab)
- Para "1 día antes" debes proporcionar `customTimeHours` y `customTimeMinutes`
- El recordatorio debe ser para el futuro, no para eventos pasados

## 🚀 Próximas Mejoras (No incluidas en esta implementación)

- Service Workers para notificaciones con web cerrada
- Push API con servidor push
- Múltiples recordatorios por evento (ya soportado en backend)
- Recordatorios recurrentes para eventos repetitivos
- Snooze para posponer notificaciones
- Configuración global de recordatorios por defecto
- Panel de gestión de todos los recordatorios activos

## ✅ Checklist de Integración

- [ ] Migración de base de datos creada y aplicada
- [ ] ReminderSection integrado en EventFormModal
- [ ] NotificationPermissionBanner integrado en App.tsx
- [ ] Íconos de notificación agregados (opcional)
- [ ] Testeado: Solicitar permisos
- [ ] Testeado: Crear recordatorio
- [ ] Testeado: Recibir notificación
- [ ] Testeado: Click en notificación
- [ ] Testeado en Chrome/Edge/Firefox
- [ ] Documentado comportamiento en Safari

---

¿Preguntas? Revisa los comentarios en el código o consulta la documentación de Notifications API: https://developer.mozilla.org/en-US/docs/Web/API/Notifications_API
