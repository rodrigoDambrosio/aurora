import '@testing-library/jest-dom'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { server } from './mocks/server'

// Establish API mocking before all tests.
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))

// Reset any request handlers that we may add during the tests,
// so they don't affect other tests.
afterEach(() => server.resetHandlers())

// Clean up after the tests are finished.
afterAll(() => server.close())

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => { },
    removeListener: () => { },
    addEventListener: () => { },
    removeEventListener: () => { },
    dispatchEvent: () => { },
  }),
})

// Mock IntersectionObserver
globalThis.IntersectionObserver = class IntersectionObserver {
  readonly root: Element | null = null
  readonly rootMargin: string = ''
  readonly thresholds: ReadonlyArray<number> = []

  constructor() { }
  observe() {
    return null
  }
  disconnect() {
    return null
  }
  unobserve() {
    return null
  }
  takeRecords(): IntersectionObserverEntry[] {
    return []
  }
} as unknown as typeof IntersectionObserver