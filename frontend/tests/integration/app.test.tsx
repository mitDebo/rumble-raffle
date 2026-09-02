import { render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import App from '@/App'

// App opens a SignalR connection on mount (1.8) as a connectivity check.
// There's no real backend under jsdom, and @microsoft/signalr's URL
// resolution throws synchronously outside a real browser context anyway
// (jsdom still looks like Node to its platform detection) -- so it's
// mocked here to keep this test focused on the rendering pipeline, not
// SignalR. The connection itself is covered server-side by PingHubTests;
// the client side is checked manually in the browser per 1.8's plan.
vi.mock('@/lib/signalr', () => ({
  connectToPingHub: vi.fn(() => ({ stop: vi.fn() })),
}))

// Smoke test for 1.3: proves the whole rendering pipeline actually works
// end to end (React, Vitest + jsdom, React Testing Library, the @/ path
// alias) by mounting the real App component and asserting on real content,
// rather than just asserting the test runner runs at all.
describe('App', () => {
  it('renders the placeholder landing content', () => {
    render(<App />)

    expect(screen.getByRole('heading', { name: 'Rumble Raffle' })).toBeInTheDocument()
    expect(screen.getByText('Coming soon.')).toBeInTheDocument()
  })
})
