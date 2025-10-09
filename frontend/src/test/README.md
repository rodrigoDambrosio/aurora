# Frontend Testing - Aurora

Este documento describe la implementación de testing para el frontend de Aurora, incluyendo pruebas unitarias, de integración y E2E.

## 🏗️ Arquitectura de Testing

### Stack de Testing
- **Vitest**: Framework de testing (reemplazo de Jest para Vite)
- **Testing Library**: Para testing de componentes React
- **MSW**: Mock Service Worker para mocking de APIs
- **Playwright**: Testing E2E multi-browser
- **@vitest/ui**: Interfaz web para ejecutar y ver tests

### Estructura de Directorios

```
src/test/
├── components/          # Unit tests para componentes React
├── services/           # Tests para servicios API  
├── hooks/             # Tests para hooks personalizados
├── e2e/              # Tests End-to-End con Playwright
├── mocks/            # Mocks de API con MSW
├── utils/            # Utilidades para testing
└── setup.ts          # Configuración global de tests
```

## 🧪 Tipos de Tests

### 1. Unit Tests (Vitest + Testing Library)
- **Componentes React**: Render, interacciones, props
- **Hooks personalizados**: Lógica de estado y efectos
- **Servicios**: Lógica de negocio y transformaciones
- **Utilities**: Funciones puras y helpers

### 2. Integration Tests (MSW)
- **API Integration**: Requests/responses con mocked backend
- **Component Integration**: Interacciones entre componentes
- **State Management**: Flujos de datos completos

### 3. E2E Tests (Playwright)
- **User Journeys**: Flujos completos de usuario
- **Cross-browser**: Chrome, Firefox, Safari, Mobile
- **Real API**: Tests contra backend real o staging
- **Performance**: Métricas de carga y respuesta

## 🚀 Comandos de Testing

```bash
# Instalar dependencias
npm install

# Unit tests
npm run test              # Ejecutar todos los tests
npm run test:ui          # UI web para tests
npm run test:coverage    # Tests con cobertura

# E2E tests
npm run test:e2e         # Ejecutar E2E tests
npm run test:e2e:ui      # UI web para E2E tests

# Watch mode para desarrollo
npm run test -- --watch
```

## 📊 Cobertura de Testing

### Objetivos de Cobertura
- **Líneas**: 80%+ 
- **Funciones**: 80%+
- **Branches**: 80%+
- **Statements**: 80%+

### Componentes Críticos (100% cobertura)
- `AuroraWeeklyCalendar`
- `MainDashboard`
- `apiService`
- Hooks de estado

### Reporte de Cobertura
```bash
npm run test:coverage
# Genera reporte HTML en coverage/
```

## 🔧 Configuración

### Vitest Config (`vitest.config.ts`)
- Entorno jsdom para DOM testing
- Setup files para mocks globales
- Thresholds de cobertura
- Reporters HTML y JSON

### Playwright Config (`playwright.config.ts`)
- Multi-browser testing
- Mobile viewports
- Web server integration
- Retry policies

### MSW Mocks (`src/test/mocks/`)
- Mock server para APIs
- Realistic data generation  
- Error scenarios
- Request/response validation

## 🧩 Patterns de Testing

### Component Testing Pattern
```typescript
describe('ComponentName', () => {
  it('should render correctly', () => {
    render(<ComponentName />)
    expect(screen.getByText('Expected Text')).toBeInTheDocument()
  })
  
  it('should handle user interactions', async () => {
    const user = userEvent.setup()
    render(<ComponentName onClick={mockFn} />)
    
    await user.click(screen.getByRole('button'))
    expect(mockFn).toHaveBeenCalled()
  })
})
```

### API Testing Pattern
```typescript
describe('apiService', () => {
  it('should fetch data correctly', async () => {
    const result = await apiService.getData()
    
    expect(result).toEqual(expectedData)
  })
  
  it('should handle errors', async () => {
    mockServer.use(http.get('/api/data', () => HttpResponse.error()))
    
    await expect(apiService.getData()).rejects.toThrow()
  })
})
```

### E2E Testing Pattern
```typescript
test('user journey name', async ({ page }) => {
  await page.goto('/')
  
  // User actions
  await page.click('[data-testid="button"]')
  await page.fill('input[name="title"]', 'Test Event')
  
  // Assertions
  await expect(page.locator('.success-message')).toBeVisible()
})
```

## 📝 Testing Checklist

### ✅ Para cada componente:
- [ ] Rendering básico
- [ ] Props handling
- [ ] Event handlers
- [ ] Conditional rendering
- [ ] Error boundaries
- [ ] Accessibility (a11y)

### ✅ Para cada service:
- [ ] Happy path scenarios
- [ ] Error handling
- [ ] Input validation
- [ ] Response transformation
- [ ] Loading states

### ✅ Para cada hook:
- [ ] Initial state
- [ ] State transitions
- [ ] Side effects
- [ ] Cleanup
- [ ] Dependencies

### ✅ Para E2E:
- [ ] Core user journeys
- [ ] Mobile responsiveness
- [ ] Cross-browser compatibility
- [ ] Performance metrics
- [ ] Error scenarios

## 🐛 Debugging Tests

### Debug Tools
```bash
# Run specific test file
npm run test -- AuroraWeeklyCalendar.test.tsx

# Run in watch mode
npm run test -- --watch

# Debug with browser tools
npm run test:ui

# E2E debugging
npm run test:e2e:ui
```

### Common Issues
- **Mock imports**: Verificar paths relativos
- **Async operations**: Usar waitFor correctamente  
- **DOM cleanup**: Asegurar cleanup entre tests
- **MSW handlers**: Verificar orden de handlers

## 📈 Métricas de Calidad

### Test Performance
- Unit tests: < 2s total
- Integration tests: < 10s total
- E2E tests: < 2min total

### Coverage Reports
- HTML: `coverage/index.html`
- JSON: `coverage/coverage-final.json`
- Lcov: `coverage/lcov.info`

### CI Integration
Tests se ejecutan automáticamente en:
- Pre-commit hooks
- Pull requests
- Deploy pipeline
- Scheduled runs

---

**Próximos Pasos:**
1. ✅ Implementar tests unitarios básicos
2. ✅ Configurar MSW para API mocking
3. ✅ Crear tests E2E fundamentales
4. 🔄 Aumentar cobertura a 80%+
5. 🔄 Integrar con CI/CD pipeline