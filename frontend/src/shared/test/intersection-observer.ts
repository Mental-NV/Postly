import { vi } from 'vitest'

type ObserverInstance = {
  callback: IntersectionObserverCallback
  elements: Set<Element>
}

export function installMockIntersectionObserver() {
  const observers: ObserverInstance[] = []

  class MockIntersectionObserver implements IntersectionObserver {
    readonly root = null
    readonly rootMargin = '0px'
    readonly thresholds = [0]

    private readonly instance: ObserverInstance

    constructor(callback: IntersectionObserverCallback) {
      this.instance = {
        callback,
        elements: new Set<Element>(),
      }
      observers.push(this.instance)
    }

    disconnect(): void {
      this.instance.elements.clear()
    }

    observe(target: Element): void {
      this.instance.elements.add(target)
    }

    takeRecords(): IntersectionObserverEntry[] {
      return []
    }

    unobserve(target: Element): void {
      this.instance.elements.delete(target)
    }
  }

  vi.stubGlobal('IntersectionObserver', MockIntersectionObserver)

  return {
    trigger(target: Element, isIntersecting = true): void {
      const entry = {
        boundingClientRect: target.getBoundingClientRect(),
        intersectionRatio: isIntersecting ? 1 : 0,
        intersectionRect: isIntersecting
          ? target.getBoundingClientRect()
          : new DOMRectReadOnly(),
        isIntersecting,
        rootBounds: null,
        target,
        time: Date.now(),
      } satisfies IntersectionObserverEntry

      observers.forEach((observer) => {
        if (observer.elements.has(target)) {
          observer.callback([entry], {} as IntersectionObserver)
        }
      })
    },
  }
}
