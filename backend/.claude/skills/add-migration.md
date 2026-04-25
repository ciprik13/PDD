---
name: add-migration
description: Use whenever the EF Core model changes — new entity, new property, schema rename, FK change. Walks the safe migration playbook (review SQL, never edit applied migrations, test against fresh and populated DBs).
---

# Skill: add-migration

EF Core migrations are silent footguns: a single bad operation can drop a column you wanted to keep. Every migration in this repo passes through this checklist.

## When to invoke

- New entity in `AgriCure.Domain` or `AgriCure.Infrastructure/Persistence/Configurations/`
- New property on an existing entity
- Renamed property / table
- Changed nullability, length, or default
- New / changed foreign key

## Steps

### 1. Make the model change
- Add the entity in `Domain` (or value object).
- Add an `IEntityTypeConfiguration<T>` in `src/AgriCure.Infrastructure/Persistence/Configurations/`.
- Update `AppDbContext.OnModelCreating` if needed (usually `ApplyConfigurationsFromAssembly` handles it).

### 2. Generate the migration
From `backend/`:

```bash
dotnet ef migrations add <DescriptiveName> \
  --project src/AgriCure.Infrastructure \
  --startup-project src/AgriCure.Api \
  --output-dir Persistence/Migrations
```

Names: `AddDetectionsTable`, `AddRefreshTokenIndex`, `RenamePlantPassportColumn`. Avoid generic names like `Update1`.

### 3. **READ THE GENERATED SQL.**
Open the new file under `src/AgriCure.Infrastructure/Persistence/Migrations/`. Look for:

- **`migrationBuilder.DropColumn` followed by `AddColumn`** — EF picked this when a `RenameColumn` would preserve data. Hand-edit to a `RenameColumn`.
- **`AlterColumn` with type narrowing** (e.g. `varchar(200)` → `varchar(100)`) — will truncate. Either widen instead or write a custom data check.
- **`DropTable`** — confirm this is what you want; you might want to write a custom migration that copies data first.
- **`AddColumn` with `nullable: false` and no default** — fails on populated DBs. Add `defaultValue:` or `defaultValueSql:`.

If you hand-edit, also hand-edit the corresponding `.Designer.cs` snapshot (or regenerate by deleting and re-running `migrations add`).

### 4. Test against a fresh DB

```bash
docker compose down -v   # nuke volumes
docker compose up -d postgres
dotnet ef database update \
  --project src/AgriCure.Infrastructure \
  --startup-project src/AgriCure.Api
```

Should run cleanly with no errors.

### 5. Test against a populated DB
- Reset, run the *previous* migration, seed some realistic data, then apply the new migration.
- Confirm row counts before/after for the affected tables.
- For destructive migrations (drop column/table), this is non-negotiable.

### 6. Commit migration + designer + snapshot together
The three files (migration `.cs`, `.Designer.cs`, and `AppDbContextModelSnapshot.cs`) are inseparable. Single commit, no exceptions.

## HARD RULES

- **Never edit a migration that has been merged to `develop`.** Add a new migration that fixes the issue.
- **Never delete a migration that has been merged to `develop`.** Same reason.
- **Never run `dotnet ef database drop` against shared databases** (staging, prod). Local only.
- **Never use `EnsureCreated` in production code paths** — it bypasses migrations and creates schema drift.

## Acceptance checklist

- [ ] Generated SQL was read line-by-line.
- [ ] Any DROP/ADD-pair-that-should-be-RENAME was hand-edited.
- [ ] Migration applies cleanly to a fresh DB.
- [ ] Migration applies cleanly to a previously-migrated DB with sample data.
- [ ] Migration `.cs`, `.Designer.cs`, and snapshot all committed in one commit.
