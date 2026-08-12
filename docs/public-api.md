# Public API & Webhooks

A versioned, rate-limited surface intended primarily for mobile apps (see `mobile/`, below) and
third-party integrations, additive to the internal `/api/...` routes the React SPA uses (those
are unchanged).

## Versioning

All Public API routes are under `/api/v1/...`. A future breaking change ships as `/api/v2/...`
alongside v1, not a replacement — existing integrations keep working.

## Authentication

Two schemes are accepted, checked in order — either is sufficient:

- **`Authorization: Bearer <jwt>`** — the same access token issued by `/api/auth/login`. Use
  this when the caller acts as a specific user; permissions/roles apply exactly as in the rest
  of the app.
- **`X-Api-Key: <key>`** — a company-scoped credential for server-to-server/mobile-backend
  integrations that don't have an interactive user session. Manage keys under **API Keys** in
  the app (CompanyAdmin/SuperAdmin). The plaintext key is shown once, at creation — only its
  SHA-256 hash is stored. Revoking a key takes effect immediately.

API keys carry a **scope**: `read` or `read,write`. A `read`-only key gets `403 Forbidden` on
any POST/PUT/DELETE under `/api/v1`. Bearer-JWT requests are not scope-restricted here — normal
role-based permissions apply as elsewhere in the app.

Every `/api/v1` request is still tenant-isolated exactly like the rest of the app: the
company is resolved from the authenticated identity (JWT claim or API key), never trusted from
the request body or query string.

## Rate limiting

120 requests/minute per API key (or per authenticated user id for Bearer requests), fixed
window, no burst queue — an over-limit request gets `429 Too Many Requests` immediately rather
than queuing. Partition key: the `X-Api-Key` header value, else the caller's user id, else IP.

## Endpoints (v1)

| Resource  | Routes |
|---|---|
| Leads     | `GET /api/v1/leads`, `GET /api/v1/leads/{id}`, `POST /api/v1/leads`, `PUT /api/v1/leads/{id}` |
| Deals     | `GET /api/v1/deals`, `GET /api/v1/deals/{id}` |
| Units     | `GET /api/v1/units`, `GET /api/v1/units/{id}`, `GET /api/v1/units/available` |
| Projects  | `GET /api/v1/projects`, `GET /api/v1/projects/{id}` |
| Dashboard | `GET /api/v1/dashboard/summary` |

All list endpoints use the same pagination/filtering/sorting/validation conventions as the
internal API (see `docs/api.md`) — `page`, `pageSize`, entity-specific filters, `sortBy`,
`sortDirection`. Writes are validated with the same FluentValidation rules as the internal
endpoints (they call the same Application-layer services).

More resources (Tasks, Commissions, Users) can be added the same way — each is a thin
controller under `Api/Controllers/V1` delegating to the existing Application service; no
business logic is duplicated.

## Webhooks

Register an HTTPS endpoint under **Webhooks** in the app (CompanyAdmin/SuperAdmin) and pick
which events to receive:

- `lead.created`
- `lead.status_changed`
- `deal.contracted`

### Payload

```json
{
  "eventType": "lead.created",
  "occurredAt": "2026-08-10T12:00:00Z",
  "data": { "...": "the same DTO shape returned by the matching GET endpoint" }
}
```

### Signing

Every delivery carries:

- `X-Webhook-Event`: the event type
- `X-Webhook-Signature`: `HMAC-SHA256(secret, rawBody)`, hex-encoded
- `X-Webhook-Delivery-Id`: a unique id for this delivery attempt

Verify the signature by recomputing the HMAC over the exact raw request body using the secret
shown once at subscription creation, and comparing it (constant-time) to the header.

### Retries & delivery history

A non-2xx response or a network error is retried up to 3 times with backoff (1m, 5m, 15m) — 4
attempts total. Every attempt (success or failure) is recorded and visible under **Webhooks →
Deliveries**: event type, attempt number, HTTP status, error message, timestamps. Deleting or
deactivating a subscription stops future retries; already-recorded history is kept.

Webhook delivery never blocks or fails the request that triggered the event — publishing is
fire-and-forget via a background job (Hangfire).

## Mobile app

`mobile/` is a Flutter (Dart) client built against this API — see
[`mobile/README.md`](../mobile/README.md). It authenticates the same way as the web app
(`/api/auth/login`, JWT bearer) and calls the `/api/v1` endpoints above. It does not use API
keys — that path is for server-to-server integrations, not an interactive mobile user session.
(An earlier Expo/React Native client used to live here — replaced by Flutter; see
`docs/roadmap.md`.)
