import { useEffect } from 'react'
import { cn } from '@/lib/utils'
import { connectToPingHub } from '@/lib/signalr'

function App() {
  // Temporary connectivity check for 1.8 -- establishes the SignalR
  // connection on mount and tears it down on unmount, purely so the
  // WebSocket handshake actually happens somewhere. Move into a real
  // feature (and drop the console logging) once there's an actual reason
  // for the browser to hold this connection.
  useEffect(() => {
    const connection = connectToPingHub()
    return () => {
      void connection.stop()
    }
  }, [])

  return (
    <main
      className={cn(
        'flex min-h-screen flex-col items-center justify-center gap-2',
        'bg-background text-foreground',
      )}
    >
      <h1 className="text-3xl font-bold">Rumble Raffle</h1>
      <p className="text-muted-foreground">Coming soon.</p>
    </main>
  )
}

export default App
