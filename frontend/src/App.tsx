import { useEffect, useState } from 'react'

function App() {
  const [message, setMessage] = useState('Loading…')

  useEffect(() => {
    fetch('/api/hello')
      .then((response) => response.text())
      .then(setMessage)
      .catch(() => setMessage('Failed to reach the backend'))
  }, [])

  return (
    <main>
      <h1>Rumble Raffle</h1>
      <p>
        Backend says: <strong>{message}</strong>
      </p>
    </main>
  )
}

export default App
