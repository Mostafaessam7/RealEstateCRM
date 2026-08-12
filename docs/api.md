\# API



\## Style



Use REST APIs.



Base prefix:



```text

/api

```



\## Controllers



Controllers should:



\- receive HTTP input

\- authorize

\- invoke application logic

\- return HTTP response



Controllers should not contain significant business logic.



\## DTOs



Use:



\- Request DTOs

\- Response DTOs



Never expose EF Core entities directly.



Tenant create/update DTOs should not normally contain CompanyId.



\## Validation



Use FluentValidation where useful.



Validate:



\- required fields

\- string lengths

\- numeric ranges

\- enum values

\- business input rules



\## Error Format



Use consistent ProblemDetails-style responses.



Do not expose stack traces in production.



\## HTTP Status Codes



Use appropriately:



```text

200 OK

201 Created

204 No Content

400 Bad Request

401 Unauthorized

403 Forbidden

404 Not Found

409 Conflict

500 Internal Server Error

```



\## Pagination



Request:



```text

?page=1\&pageSize=20

```



Apply maximum page size.



Response example:



```json

{

&#x20; "items": \[],

&#x20; "page": 1,

&#x20; "pageSize": 20,

&#x20; "totalCount": 0,

&#x20; "totalPages": 0

}

```



\## Filtering



Examples:



```text

GET /api/leads?status=Interested

GET /api/leads?assignedAgentId=...

GET /api/leads?source=Facebook



GET /api/units?status=Available

GET /api/units?projectId=...

```



All filters operate inside current tenant scope.



\## Search



Lead search:



```text

FullName

Phone

Email

```



Unit search:



```text

UnitCode

Project

Location

```



\## Sorting



Example:



```text

?sortBy=createdAt\&sortDirection=desc

```



Use allowlisted sortable fields.



Never dynamically expose arbitrary database columns.



\## Lead Endpoints



```text

GET    /api/leads

GET    /api/leads/{id}

POST   /api/leads

PUT    /api/leads/{id}

DELETE /api/leads/{id}

POST   /api/leads/{id}/assign

POST   /api/leads/{id}/transfer

POST   /api/leads/{id}/activities

GET    /api/leads/{id}/activities

GET    /api/leads/follow-ups/upcoming

GET    /api/leads/{id}/recommendations

GET    /api/leads/{id}/ai-insight

```



\## Projects



```text

GET    /api/projects

GET    /api/projects/{id}

POST   /api/projects

PUT    /api/projects/{id}

DELETE /api/projects/{id}

```



\## Units



```text

GET    /api/units

GET    /api/units/{id}

POST   /api/units

PUT    /api/units/{id}

DELETE /api/units/{id}

```



\## Deals



```text

GET    /api/deals

GET    /api/deals/{id}

POST   /api/deals

PUT    /api/deals/{id}

```



Specific business actions can use dedicated endpoints instead of generic updates where appropriate.



\## Concurrency



Use optimistic concurrency where business conflicts matter.



Do not add concurrency complexity to every entity by default.



\## Versioning



This internal `/api/...` surface (used by the React SPA) is still unversioned by design — it's

the app's own backend, not a stability contract for outside consumers, so keeping it unversioned

is deliberate, not an oversight.



A separate, versioned `/api/v1/...` surface was added for the Public API (mobile apps and

third-party integrations) — see `docs/public-api.md`. That is where versioning lives; it was

never retrofitted onto this internal surface, and there's no plan to.



\## Other Internal Endpoints



The sections above (Lead/Projects/Units/Deals) cover the core CRUD modules from the earliest

phases. The internal `/api/...` surface also includes the following, added in later phases —

all follow the same conventions (DTOs, tenant scoping, ProblemDetails errors) as above:



```text

Auth:          POST /api/auth/login, /refresh, /logout, /change-password,

&#x20;              /forgot-password, /reset-password  (see docs/auth.md)



Companies:     GET  /api/companies/current



Users:         GET/POST /api/users, PUT /api/users/{id}/role, PUT /api/users/{id}/active,

&#x20;              POST /api/users/me/avatar



Tasks:         GET/POST /api/tasks, GET/PUT /api/tasks/{id}, POST /api/tasks/{id}/assign,

&#x20;              POST /api/tasks/{id}/complete, POST /api/tasks/{id}/cancel



Commissions:   GET/POST /api/commissions, GET /api/commissions/{id},

&#x20;              POST /api/commissions/{id}/mark-paid, POST /api/commissions/{id}/cancel



Dashboard:     GET /api/dashboard/summary



Reports:       GET /api/reports/leads, /sales, /conversion, /agent-performance,

&#x20;              /commissions, /inventory



Audit Logs:    GET /api/audit-logs   (CompanyAdmin/SuperAdmin only)



Documents:     GET/POST /api/documents, DELETE /api/documents/{id}



Project/Unit   GET/POST /api/projects/{id}/images, DELETE /api/projects/{id}/images/{imageId}

images:        GET/POST /api/units/{id}/images, DELETE /api/units/{id}/images/{imageId}



Subscriptions: GET /api/subscriptions/plans, GET /api/subscriptions/current,

&#x20;              POST /api/subscriptions/change-plan, POST /api/subscriptions/cancel,

&#x20;              GET /api/subscriptions/plans/all, POST/PUT /api/subscriptions/plans\[/{id}]

&#x20;              (SuperAdmin-only plan management)



WhatsApp:      GET/POST /api/whatsapp/templates, PUT /api/whatsapp/templates/{id},

&#x20;              GET /api/whatsapp/leads/{leadId}/messages, POST /api/whatsapp/leads/{leadId}/send



Campaigns:     GET/POST /api/campaigns, GET /api/campaigns/{id},

&#x20;              POST /api/campaigns/{id}/send, GET /api/campaigns/{id}/recipients



API Keys:      GET/POST /api/api-keys, POST /api/api-keys/{id}/revoke



Webhooks:      GET/POST /api/webhooks, DELETE /api/webhooks/{id},

&#x20;              GET /api/webhooks/event-types, GET /api/webhooks/{id}/deliveries



Marketplace:   GET /api/marketplace/units   (unauthenticated, public, rate-limited by IP)



Payments:      GET /api/deals/{dealId}/payments, POST /api/deals/{dealId}/payments/checkout,

&#x20;              POST /api/payments/webhook   (unauthenticated by design — HMAC-verified instead)



Lead extras:   GET /api/leads/{id}/recommendations, GET /api/leads/{id}/ai-insight

```



See `docs/roadmap.md` Phases 7–21 for the design rationale behind each of these, and

`docs/public-api.md` for the separate versioned `/api/v1/...` surface.

