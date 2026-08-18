---
codigo: BAS-5
estado: Planificado
tags:
  - tasks
---

# BAS-5: Tareas

- [x] Añadir `docs/specs/BAS-5/` a `BasketBaseTracker.slnx` (carpeta de solución, `workflow.md`).
- [ ] Crear `Data/Entities/Categoria.cs`, `Club.cs`, `Sede.cs` (catálogo).
- [ ] Crear `Data/Entities/Temporada.cs`, `Competicion.cs`, `Equipo.cs`, `FichaJugador.cs` (ancladas a temporada).
- [ ] Crear `Data/Entities/Jornada.cs`, `Partido.cs`, `PartidoParcial.cs` (calendario y resultados).
- [ ] Crear `Data/Entities/PenalizacionClasificacion.cs`.
- [ ] Crear `Data/Configurations/` con una `IEntityTypeConfiguration<T>` por entidad: relaciones, restricciones únicas, `HasConversion<string>()` en enums, `HasMaxLength` en strings, `RowVersion` en `Partido`/`Equipo` (ver `plan.md`).
- [ ] Añadir `DbSet<T>` de las once entidades a `ApplicationDbContext` y `modelBuilder.ApplyConfigurationsFromAssembly(...)` en `OnModelCreating`.
- [ ] Generar la migración de EF Core (`dotnet ef migrations add AddDominioEntities` desde `src/BasketBaseTracker.Web`).
- [ ] Verificar en local: `aspire run` + `dotnet ef database update`, confirmar el esquema con un cliente SQL.
- [ ] Test de integración (`tests/BasketBaseTracker.Tests/Integration/`) que levanta el `AppHost` (mismo patrón que `AppHostTests.WebRespondeAlHealthCheck`) y comprueba que la migración se aplica sin errores.
- [ ] Test de integración que confirma cada restricción única (`Competicion`, `FichaJugador`, `Jornada`) rechazando una inserción duplicada.
- [ ] `dotnet build` y `dotnet test` en verde en local antes de empujar.
- [ ] Confirmar CI (build + tests) en verde en el PR.
- [ ] Cierre: mover `docs/specs/BAS-5/` a `docs/specs/archive/BAS-5/`, abrir el PR de `feature/BAS-5` a `develop` y esperar confirmación humana para fusionarlo.
