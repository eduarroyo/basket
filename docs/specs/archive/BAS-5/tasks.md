---
codigo: BAS-5
estado: Archivado
tags:
  - tasks
---

# BAS-5: Tareas

- [x] Añadir `docs/specs/BAS-5/` a `BasketBaseTracker.slnx` (carpeta de solución, `workflow.md`).
- [x] Crear `Data/Entities/Categoria.cs`, `Club.cs`, `Sede.cs` (catálogo).
- [x] Crear `Data/Entities/Temporada.cs`, `Competicion.cs`, `Equipo.cs`, `FichaJugador.cs` (ancladas a temporada).
- [x] Crear `Data/Entities/Jornada.cs`, `Partido.cs`, `PartidoParcial.cs` (calendario y resultados).
- [x] Crear `Data/Entities/PenalizacionClasificacion.cs`.
- [x] Crear `Data/Configurations/` con una `IEntityTypeConfiguration<T>` por entidad: relaciones, restricciones únicas, `HasConversion<string>()` en enums, `HasMaxLength` en strings, `RowVersion` en `Partido`/`Equipo` (ver `plan.md`).
- [x] Añadir `DbSet<T>` de las once entidades a `ApplicationDbContext` y `modelBuilder.ApplyConfigurationsFromAssembly(...)` en `OnModelCreating`.
- [x] Generar la migración de EF Core (`dotnet ef migrations add AddDominioEntities` desde `src/BasketBaseTracker.Web`).
- [x] Verificar en local: `aspire run` (aplica la migración automáticamente en Development) — confirmado en los logs del recurso `web` (`aspire logs web`) que las once tablas se crean sin errores y la app responde en `/` y `/health`.
- [x] Test de integración (`tests/BasketBaseTracker.Tests/Integration/`) que levanta el `AppHost` (mismo patrón que `AppHostTests.WebRespondeAlHealthCheck`, vía fixture compartida `AppHostSqlFixture`) y comprueba que la migración se aplica sin errores.
- [x] Test de integración que confirma cada restricción única (`Competicion`, `FichaJugador`, `Jornada`) rechazando una inserción duplicada.
- [x] `dotnet build` y `dotnet test` en verde en local antes de empujar.
- [ ] Confirmar CI (build + tests) en verde en el PR.
- [ ] Cierre: mover `docs/specs/BAS-5/` a `docs/specs/archive/BAS-5/`, abrir el PR de `feature/BAS-5` a `develop` y esperar confirmación humana para fusionarlo.
