# Rumble Raffle

A web app for running a WWE Royal Rumble (or any rumble-style event) friend-group raffle: track buy-ins, draw entrant numbers, and follow the live entrance/elimination board from your phone as the match plays out.

This is an early-stage project. The README will grow as the app takes shape — for now it's just enough to orient anyone (including future us) opening the repo.

## What it does

- Hosts create an event, invite attendees by email, and assign raffle entrant numbers (manually, as physically drawn, or via random fill).
- As the real match plays out, the host logs which wrestler enters at each number and any eliminations — every attendee's view updates live.
- An event can optionally follow an admin-maintained "official" broadcast event, automatically mirroring its entrance/elimination log in real time instead of the host tracking it by hand.
- The app never handles money — buy-ins, payouts, and auctions all happen the way the group already does them (cash, Venmo, in person). The app only tracks who owns what and what they paid.

## Tech stack

- **Backend:** C# / ASP.NET Core, EF Core (Npgsql provider)
- **Frontend:** React + TypeScript (Vite), Tailwind CSS, shadcn/ui
- **Database & Auth:** PostgreSQL via Supabase (Supabase Auth for OAuth/magic-link sign-in, Supabase Storage for images — both behind app-owned abstractions)
- **Real-time:** SignalR
- **Testing:** xUnit (backend), Vitest + React Testing Library (frontend), built test-first
- **Hosting:** Dockerized frontend and backend, deployed to a self-managed DigitalOcean droplet behind nginx
- **CI/CD:** GitHub Actions — path-aware builds, tests gate deploy, EF Core migrations run as their own pipeline step

## Project layout

This is a monorepo. `backend/` and `frontend/` will hold the two apps as they're scaffolded. Planning and spec documents (requirements, architecture decisions, task breakdowns) live in a local `spec/` folder that's intentionally gitignored and never checked in — it's a working folder, not part of the shipped project.

## Status

Pre-implementation. Requirements and the initial task breakdown are done; Phase 1 (project scaffolding) hasn't started yet.
