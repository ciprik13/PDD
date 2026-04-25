---
name: update-api-docs
description: Use whenever a controller action is added or modified, or a DTO field is renamed/added/removed. Keeps Swagger XML docs, ProducesResponseType, and docs/api-contract.md in lockstep so frontend devs always see truth.
---

# Skill: update-api-docs

The frontend team builds against Swagger without reading our code. If docs lag the implementation, they ship bugs and blame us. Don't let that happen.

## When to invoke

- New controller action added.
- Existing controller action's response shape changed.
- New DTO created, or a DTO field added/renamed/removed.
- Status codes returned changed (added a new error response, removed one).

## Steps

### 1. XML doc comments
Every public controller action gets:

```csharp
/// <summary>One-sentence description of what this endpoint does.</summary>
/// <param name="diseaseClass">Plain-English description with examples, e.g. <c>late_blight</c>.</param>
/// <response code="200">What 200 means in this endpoint's context.</response>
/// <response code="400">Validation error — see ProblemDetails.errors.</response>
/// <response code="401">Missing or invalid bearer token.</response>
/// <response code="404">Resource not found — see ProblemDetails.detail for the missing id.</response>
```

`<remarks>` is optional and reserved for "this endpoint is a stub, real implementation tracked in backlog" notes.

### 2. ProducesResponseType
For every status code your action can return, add an attribute. The type goes on the success path; error paths use `ProblemDetails`:

```csharp
[HttpGet("{id:guid}")]
[ProducesResponseType(typeof(DetectionDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<DetectionDto>> GetById(Guid id, CancellationToken ct) ...
```

If you return `204 NoContent`, attribute it. If you return `201 Created`, set `typeof(DetectionDto)` if the response body is the resource.

### 3. Swagger renders types automatically
You don't need to write JSON schemas — Swashbuckle reads the C# types and the XML doc file. As long as the DTO records have descriptive property names, Swagger UI shows them correctly.

### 4. `docs/api-contract.md` — only update when conventions change
Do NOT add a section for each new endpoint — Swagger is the source of truth. Update this file ONLY when:

- A new error code or error format is introduced.
- Pagination scheme changes (e.g. cursor-based instead of offset).
- Versioning policy changes.
- New auth flow.
- Rate limiting introduced.

When you do update it, also update the "Endpoints" table at the bottom (which is just a quick index, not a contract).

### 5. PR description — "API contract change"
If the response shape changed AND the endpoint is consumed by `frontend/src/services/api.ts`, the PR body MUST include a "**API contract changes**" section listing the affected fields. The `/create-pr` template prompts for this.

## HARD RULES

- **Never** describe an endpoint in `docs/api-contract.md` that isn't documented in code with XML + ProducesResponseType. Single source of truth.
- **Never** ship a controller action without `ProducesResponseType` for at least the success path.
- **Never** rename a DTO field that is consumed by `frontend/src/services/api.ts` without flagging it in the PR description.

## Acceptance checklist

- [ ] All public controller actions have XML `<summary>`, `<param>`, and `<response>` tags.
- [ ] All status codes returned are listed with `[ProducesResponseType]`.
- [ ] Swagger UI (`/swagger`) shows the action with full descriptions and example schemas.
- [ ] If response shape changed: PR description has the "API contract changes" bullet list.
- [ ] If conventions changed: `docs/api-contract.md` updated with the new convention.
