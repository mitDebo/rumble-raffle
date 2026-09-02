import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

// Connectivity check for 1.8: proves the browser can hold a live WebSocket
// connection to the backend through both Vite's dev proxy (ws: true in
// vite.config.ts) and, once deployed, nginx's /api block. No real feature
// hangs off this yet, so there's nothing to assert in an automated test —
// open the browser console/devtools Network tab after connecting to
// confirm the connection opens and "pong" is logged. Real domain hubs
// will get their own connection helpers alongside this one as they're
// built, following the same shape.
export function connectToPingHub() {
  const connection = new HubConnectionBuilder()
    .withUrl('/api/hubs/ping')
    .configureLogging(LogLevel.Information)
    .build()

  connection.on('Ping', (message: string) => {
    console.log('[PingHub] received:', message)
  })

  connection.start()
    .then(() => console.log('[PingHub] connected'))
    .catch((error: unknown) => console.error('[PingHub] connection failed', error))

  return connection
}
