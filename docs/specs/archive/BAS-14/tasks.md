---
codigo: BAS-14
estado: Completado
tags:
  - tasks
---

# BAS-14: Tareas

- [x] Añadir el paquete `Ical.Net` a `BasketBaseTracker.Web.csproj` (`dotnet add package`, skill `dotnet`).
- [x] Crear `Domain/IcsFeedBuilder.cs` con `Construir` sobre `PartidoIcs` (ver `plan.md`), incluyendo UID estable, duración fija de 2 horas y `LOCATION` condicional. El filtro `FechaHora != null && Estado != Cancelado` se aplica en el endpoint, no en el builder.
- [x] Test unitario de `IcsFeedBuilder` (`tests/BasketBaseTracker.Tests/Unit/`): campos del `VEVENT`, ausencia de `LOCATION` sin sede, estabilidad del UID.
- [x] Endpoint `GET /competiciones/{id}/calendario.ics` en `Program.cs` (Minimal API, `OutputCache` policy `Publico`, 404 si no existe la competición).
- [x] Endpoint `GET /equipos/{id}/calendario.ics` en `Program.cs` (Minimal API, `OutputCache` policy `Publico`, 404 si no existe el equipo).
- [x] Test de integración de ambos endpoints (200 + `Content-Type: text/calendar` + número de `VEVENT` correcto; 404 en id inexistente) en `tests/BasketBaseTracker.Tests/Integration/`.
- [x] Enlace "Añadir a mi calendario" en `Areas/Public/Pages/Competiciones/Calendario.cshtml` apuntando a `calendario.ics` de esa competición.
- [x] Enlace "Añadir a mi calendario" en `Areas/Public/Pages/Equipos/Index.cshtml` apuntando a `calendario.ics` de ese equipo.
- [x] Test de integración: el enlace de suscripción aparece en el HTML de ambas páginas.
- [x] Añadir `docs/specs/BAS-14/spec.md`, `plan.md` y `tasks.md` a `BasketBaseTracker.slnx` (carpeta de solución `/docs/specs/BAS-14/`).
- [x] Validación manual: importado `calendario.ics` de una competición real en Google Calendar, evento mostrado correctamente (equipos, fecha, duración). Verificación de la mecánica real de suscripción ("Desde URL", auto-actualizable) pendiente de despliegue con URL pública — no es un cambio de código, ver `spec.md`, Aclaraciones.
- [x] Ejecutar la suite completa (`dotnet test`) y confirmar que pasa en CI.
- [x] Cierre: mover `docs/specs/BAS-14/` a `docs/specs/archive/BAS-14/`, actualizar `BasketBaseTracker.slnx` y abrir el PR de `feature/BAS-14` a `develop` (sin fusionar sin confirmación humana). Fusionado (PR #23), promocionado a `main` (PR #24) y desplegado a producción — validado en vivo con Google Calendar.
