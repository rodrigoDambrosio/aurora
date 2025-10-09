# Template de Épica - Proyecto

## EPIC [ID]: [NOMBRE DE LA ÉPICA]

### Información General
- **ID**: PLAN-[XXX]
- **Título**: [Nombre descriptivo de la épica]
- **Prioridad**: [Alta | Media | Baja]
- **Estimación Total**: [XX puntos de historia]

### Descripción
[Descripción detallada de la épica y el contexto del problema que resuelve]

### User Stories Incluidas
| ID | Título | Estimación | Prioridad | Estado |
|---|---|---|---|---|
| PLAN-[XXX] | [Título de la User Story] | [X pts] | [Alta/Media/Baja] | [Por hacer/En Progreso/Completada] |
| PLAN-[XXX] | [Título de la User Story] | [X pts] | [Alta/Media/Baja] | [Por hacer/En Progreso/Completada] |

---

# Template de User Story - Proyecto

## USER STORY [ID]: [NOMBRE DE LA USER STORY]

### Información General
- **ID**: PLAN-[XXX]
- **Título**: [Nombre descriptivo de la user story]
- **Épica**: [PLAN-XXX: Nombre de la épica padre]
- **Prioridad**: [Alta | Media | Baja]
- **Estimación**: [X puntos de historia]
- **Asignado a**: [Nombre del desarrollador]

### Historia de Usuario
**Como** [tipo de usuario]  
**Quiero** [funcionalidad específica que necesita]  
**Para** [beneficio/valor que obtiene]

### Contexto
[Descripción detallada del problema que enfrenta el usuario, el contexto de uso, y por qué esta funcionalidad es necesaria. Incluir consideraciones de usabilidad, dispositivos objetivo, y restricciones específicas.]

### Criterios de Aceptación
- [Criterio funcional específico y medible 1]
- [Criterio de interfaz y experiencia de usuario 2]
- [Criterio de rendimiento o técnico 3]
- [Criterio de accesibilidad o responsive 4]
- [Criterio de integración con otras funcionalidades 5]
- [Criterio adicional según la complejidad de la funcionalidad]

### Mockups/Wireframes
- [Link a diseños específicos o descripción detallada de la interfaz]
- [Flujos de navegación y transiciones]
- [Estados de la interfaz: carga, error, vacío, éxito]
- [Interacciones específicas: gestos, botones, formularios]
- [Variaciones para diferentes tamaños de pantalla]

### Notas Técnicas
- **API**: [Endpoints específicos con métodos: GET /endpoint?params, POST /endpoint, etc.]
- **Modelo**: [Estructura de datos: Entidad (campos específicos)]
- **Librerías sugeridas**: [Librerías específicas recomendadas para la implementación]
- **Consideraciones de rendimiento**: [Optimizaciones necesarias, carga de datos, etc.]
- **Accesibilidad**: [Contraste, navegación por teclado, etiquetas ARIA, etc.]
- **Responsive**: [Adaptación móvil, breakpoints mínimos, etc.]
- **Integraciones**: [APIs externas, servicios de terceros]

### Definición de Terminado (DoD)
- [ ] Código desarrollado según criterios de aceptación
- [ ] Pruebas unitarias escritas y pasando (>80% cobertura)
- [ ] Pruebas de integración completadas
- [ ] Revisión de código aprobada por peer
- [ ] Probado en dispositivos objetivo
- [ ] Documentación actualizada
- [ ] Demo realizada al Product Owner
- [ ] User story validada por stakeholder

### Casos de Prueba
| Escenario | Precondiciones | Pasos | Resultado Esperado |
|---|---|---|---|
| [Caso feliz principal] | [Usuario autenticado, datos disponibles] | [Secuencia de acciones del flujo principal] | [Funcionalidad completa operativa] |
| [Caso con datos vacíos] | [Usuario sin datos previos] | [Acciones con estado inicial vacío] | [Interfaz maneja estado vacío correctamente] |
| [Caso de error de conexión] | [Sin conectividad o API no disponible] | [Intentar usar funcionalidad] | [Mensaje de error claro, opción de reintento] |
| [Caso en dispositivo móvil] | [Pantalla pequeña, touch] | [Navegación y uso táctil] | [Interfaz responsive, gestos funcionando] |
| [Caso con muchos datos] | [Usuario con gran volumen de información] | [Cargar y navegar datos] | [Rendimiento aceptable, paginación si es necesario] |

### Dependencias
- **Dependencias de otras US**: [US-XXX: Nombre] - [Razón]
- **Dependencias técnicas**: [Configuración, APIs, etc.]

### Tareas Técnicas (Subtasks)
- [ ] [Implementar funcionalidad principal específica 1]
- [ ] [Implementar funcionalidad principal específica 2]
- [ ] [Integrar APIs y servicios necesarios]
- [ ] [Implementar validaciones y manejo de errores]
- [ ] [Implementar características de UI/UX específicas]
- [ ] [Validar funcionamiento en dispositivos objetivo]
- [ ] [Documentar funcionalidad y flujos principales]
- [ ] [Pruebas unitarias e integración]

### Notas y Comentarios
[Espacio para decisiones de diseño, cambios de alcance, issues encontrados, etc.]

---
