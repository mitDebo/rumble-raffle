import { render, screen } from '@testing-library/react'
import App from '@/App'

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
