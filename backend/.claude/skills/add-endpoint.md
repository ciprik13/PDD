---
name: add-endpoint
description: Use when adding a new HTTP-callable endpoint to AgriCure.Api — walks DTO → command/query → validator → handler → controller → Swagger XML docs → integration test, in lockstep so nothing drifts from the frontend contract.
---

# Skill: add-endpoint

When the work is "expose X via the API" / "add a new route" / "frontend needs a Y endpoint" — follow these steps. They keep code, contract, and tests aligned in one shot.

## Pre-flight

1. **Check the frontend contract.** Open `frontend/src/services/api.ts` (frontend branch). If the endpoint already has a TS type, your DTO MUST match it field-for-field (camelCase JSON). If not, you're inventing a shape — flag this in the eventual PR.
2. **Pick the area folder** under `Application/Features/`, e.g. `Treatments`, `Detections`, `Auth`, `System`.

## Steps

### 1. DTO
`src/AgriCure.Application/Common/DTOs/<Entity>Dto.cs`:

```csharp
namespace AgriCure.Application.Common.DTOs;

public sealed record TreatmentDto(
    string Id,
    string Name,
    string Type,
    int Rank,
    string Description,
    string Dosage,
    int RepeatAfterDays,
    int PhiDays,
    string CostLevel,
    IReadOnlyList<string> Tags
);
```

camelCase JSON happens automatically via System.Text.Json defaults.

### 2. Query / Command + Validator
`src/AgriCure.Application/Features/Treatments/GetTreatmentsQuery.cs`:

```csharp
using MediatR;

namespace AgriCure.Application.Features.Treatments;

public sealed record GetTreatmentsQuery(string DiseaseClass) : IRequest<IReadOnlyList<TreatmentDto>>;

internal sealed class GetTreatmentsQueryValidator : AbstractValidator<GetTreatmentsQuery>
{
    public GetTreatmentsQueryValidator()
    {
        RuleFor(q => q.DiseaseClass).NotEmpty();
    }
}
```

### 3. Handler
Same folder, `GetTreatmentsQueryHandler.cs`. Talk to the DB via `IApplicationDbContext` — do NOT take a concrete `AppDbContext` here.

### 4. Integration test FIRST
`tests/AgriCure.Api.IntegrationTests/Treatments/GetTreatmentsTests.cs`:

```csharp
public class GetTreatmentsTests : IClassFixture<AgriCureWebFactory>
{
    private readonly AgriCureWebFactory _factory;
    public GetTreatmentsTests(AgriCureWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_treatments_returns_ranked_list_for_known_disease()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/treatments?diseaseClass=late_blight");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<TreatmentDto[]>();
        body.Should().NotBeNull();
        body!.Should().BeInAscendingOrder(t => t.Rank);
    }
}
```

Run it. It fails. Good.

### 5. Controller
`src/AgriCure.Api/Controllers/TreatmentsController.cs`:

```csharp
[ApiController]
[Route("api/treatments")]
public sealed class TreatmentsController(IMediator mediator) : ControllerBase
{
    /// <summary>List treatments for a disease class, ranked by recommendation order.</summary>
    /// <param name="diseaseClass">Lower-snake-case disease class, e.g. <c>late_blight</c>.</param>
    /// <response code="200">Ordered list of treatments. Empty array if no matches.</response>
    /// <response code="400">Validation error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(TreatmentDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TreatmentDto>>> GetTreatments(
        [FromQuery] string diseaseClass,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetTreatmentsQuery(diseaseClass), ct);
        return Ok(result);
    }
}
```

### 6. Make the test green, refactor, repeat.

### 7. Update docs
- XML docs and `[ProducesResponseType]` are the API contract. Don't skip them — Swagger renders them.
- `docs/api-contract.md`: only update if conventions changed (new error format, pagination, etc.). Don't list every endpoint there.

## Acceptance checklist

- [ ] DTO field names match `frontend/src/services/api.ts` if the type already exists there.
- [ ] Query/Command + validator + handler each in their own file.
- [ ] Integration test was written before the controller, watched fail, then green.
- [ ] Controller action has XML docs and `[ProducesResponseType]` for every status code returned.
- [ ] If response shape changed and is consumed by the frontend: noted in the PR description.
- [ ] `dotnet build` clean, `dotnet test` green.
