# Asset Portfolio Ops – web

React + TypeScript + Vite dashboard for the Asset Portfolio Ops API.
See the [root README](../../README.md) for the overall architecture.

## Scripts

| Command | Description |
| --- | --- |
| `npm run dev` | Start the Vite dev server (default http://localhost:5173) |
| `npm run build` | Type-check with `tsc -b` and build to `dist/` |
| `npm run lint` | Run ESLint |
| `npm test` | Run Vitest + React Testing Library tests once |
| `npm run preview` | Serve the production build locally |

## Configuration

The API base URL comes from `VITE_API_BASE_URL` (see `.env.example`).
It defaults to `http://localhost:5107`, which matches the API's local launch profile.

## Structure

- `src/api.ts` – typed API client. Failed requests throw `ApiError` with the backend's `{ "error": "..." }` message when one is returned.
- `src/App.tsx` – dashboard: portfolio, inventory, risk indicators, purchase request workflow and audit events.
- `src/test/` – test setup and an in-memory fake of the API used by the tests.
