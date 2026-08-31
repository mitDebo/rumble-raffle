# Rumble Raffle — Frontend

React + TypeScript (Vite). Currently just a walking-skeleton milestone: one
page that calls the backend's `GET /api/hello` and displays whatever it
returns, plus placeholder unit and integration test steps, proving out the
build/test/deploy pipeline before any real feature work starts.

## Run it locally

Start the backend first (`dotnet run --project ../backend/src/RumbleRaffle.Api`,
which listens on `http://localhost:5100` per its `launchSettings.json`), then:

```bash
npm install
npm run dev
```

The dev server proxies `/api` requests to `http://localhost:5100` (see
`vite.config.ts`) so the page works the same locally as it will in production,
where nginx does that same routing instead.

## Run the tests

```bash
npm run test:unit
npm run test:integration
```

Both are placeholder tests for now (`expect(true).toBe(true)`) — they exist
to prove the CI pipeline actually runs a unit-test step and an
integration-test step, not to cover real behavior yet.

## Build

```bash
npm run build
```
