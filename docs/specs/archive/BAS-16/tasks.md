---
codigo: BAS-16
estado: Completado
tags:
  - tasks
---

# BAS-16: Tareas

## Roles

- [x] `Program.cs`: política `GestorCompeticion` nueva; convención propia (no `AuthorizeAreaFolder`, se acumularía con la de `ImportExport` — ver comentario en el código) que exige `GestorCompeticion` en toda página del área Admin salvo `ImportExport`; `Administrador` protege solo `ImportExport`.
- [x] `IdentitySeeder`: `MigrarRolesAsync` idempotente (renombra `Administrador`→`GestorCompeticion` conservando asignaciones, crea `Administrador` si falta, añade `Administrador` al único administrador legado existente); el *seed* del primer administrador asigna ambos roles.
- [x] Test de integración de la migración de roles (rol legado sembrado a mano → tras el *seed*, mismo usuario con ambos roles nuevos, mismo `Id` de rol conservado en el renombrado).
- [x] Test de integración: `GestorCompeticion` sigue accediendo a una página cualquiera del área Admin ya existente, y `403` sin `Administrador` en `ImportExport` — hecho junto con los tests de la pantalla nueva.

## Formato y validación

- [x] `Data/ImportExport/ExportacionDatos.cs`: DTOs de exportación por entidad + contenedor raíz con `SchemaVersion`.
- [x] `Domain/ImportExportService.cs`: `Exportar`, `Validar` (versión, integridad referencial, restricciones únicas, equipo repetido en jornada vía `PartidoReglas`).
- [x] Tests unitarios de `Exportar`/`Validar` (versión no soportada, FK huérfana, restricción única violada, equipo repetido, fichero válido).

## Importación y backup

- [x] `ImportadorDatos.ImportarAsync`: borrado en orden inverso de dependencia + inserción con `IDENTITY_INSERT` preservando `Id`, todo en una transacción (envuelta en `CreateExecutionStrategy` por los reintentos de SqlResilience).
- [x] Recurso Azure Blob Storage en `AppHost.cs` (`aspire add azure-storage`), emulador Azurite en local/tests.
- [x] Backup automático (reexportación + subida a Blob) como paso previo obligatorio a `ImportarAsync` — si falla la subida, no se importa.

## Pantalla `ImportExport`

- [x] `Areas/Admin/Pages/ImportExport/Index.cshtml(.cs)`: exportar (descarga directa) e importar (fichero + checkbox de confirmación obligatorio). Autorización vía convención en `Program.cs` (política `Administrador`), no atributo.
- [x] Enlace a la pantalla desde el panel de administración (`Admin/Index`, solo visible para `Administrador`).
- [x] Test de integración: exportar y reimportar el mismo fichero reproduce datos equivalentes (mismos `Id`); versión no soportada o checkbox sin marcar no cambian nada; 403 para `GestorCompeticion` sin `Administrador` y viceversa.
- [x] Test de integración: `GestorCompeticion` sigue accediendo a una página cualquiera del área Admin ya existente, y `403` sin `Administrador` en `ImportExport` (se completó junto con los tests de arriba).
- [x] Hallazgos adicionales corregidos durante la verificación: sin `AccessDeniedPath` configurado, un usuario autenticado sin el rol requerido caía en un 404 en vez de 403 (nunca antes probado, con un único rol no podía darse el caso); el panel de aterrizaje (`Admin/Index`, destino por defecto tras el login) exigía `GestorCompeticion` y dejaba fuera a una cuenta solo-`Administrador` — nueva política `GestorCompeticionOAdministrador` para esa página compartida.

## Cierre

- [x] Actualizar `screens.md` (quitar el *placeholder* de `Importación/exportación`) y `architecture.md` (puntos 7 y 10: división de roles, backup en Blob Storage, preservación de `Id`).
- [x] Ejecutar la suite completa (`dotnet test`) y confirmar que pasa en CI. 95/95 en verde (varios reintentos por contención de recursos en esta máquina al arrancar ~20 AppHost en paralelo, cada uno con su propio SQL + Azurite — no es una regresión, mismo patrón ya documentado con SQL antes de BAS-16).
- [x] Verificación manual en local: la cuenta ya sembrada (con el rol legado real, migrado al arrancar) ve el panel completo con la tarjeta "Importación/exportación"; la pantalla se ve como se diseñó. Cobertura funcional completa (round-trip, versión no soportada, checkbox, control de acceso) ya confirmada por los tests de integración.
- [x] Cierre: mover `docs/specs/BAS-16/` a `docs/specs/archive/BAS-16/`, actualizar `BasketBaseTracker.slnx` y abrir el PR de `feature/BAS-16` a `develop` (sin fusionar sin confirmación humana).
