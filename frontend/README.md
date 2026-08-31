# Rumble Raffle — Frontend

React + TypeScript (Vite). Currently a static placeholder page — no backend
calls yet — plus placeholder unit and integration test steps, proving out
the build/test/deploy pipeline before any real feature work starts.

## Run it locally

```bash
npm install
npm run dev
```

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
