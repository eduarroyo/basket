---
codigo: BAS-15
estado: Completado
tags:
  - tasks
---

# BAS-15: Tareas

- [x] Añadir `ResultadoReglas.SumarParciales` (`Domain/ResultadoReglas.cs`) y su test unitario (varios parciales, lista vacía, parciales de prórroga).
- [x] `Resultado/Edit.cshtml.cs`: cargar los `PartidoParcial` del partido en `OnGetAsync`, exponer `SumaPuntosLocal`, `SumaPuntosVisitante` y `TieneParciales`.
- [x] `Resultado/Edit.cshtml`: añadir los tres botones (técnica local, técnica visitante, calcular desde parciales) junto a los campos de marcador; `disabled` en el de parciales si `!TieneParciales`.
- [x] Añadir `data-equipo-visitante-id` al `ganadorSelect` (hoy solo lleva `data-equipo-local-id`) y `data-suma-local`/`data-suma-visitante` al botón de parciales.
- [x] Script inline: los tres botones rellenan marcador y, cuando el resultado determina un ganador, el desplegable de ganador (parciales: limpio si hay empate en la suma).
- [x] Retirar el listener existente que reescribe el marcador al cambiar el desplegable de ganador.
- [x] Test de integración: HTML de `Resultado/Edit` con y sin parciales — atributos `data-*` correctos, botón de parciales `disabled` cuando corresponde.
- [x] Verificación manual en local: los tres botones rellenan marcador y ganador correctamente; cambiar el ganador a mano ya no toca el marcador; guardar sigue exigiendo marcador sin empate. Encontrado y corregido de paso un bug preexistente de BAS-10 (`estadoSelect.value === 'Jugado'` comparaba contra el valor numérico de la opción, nunca coincidía — el grupo del marcador no llegaba a mostrarse nunca al elegir Jugado/Resuelto). Reproducido el escenario exacto reportado por el usuario (Jugado + calcular desde parciales + Guardar) de extremo a extremo con éxito.
- [x] Ejecutar la suite completa (`dotnet test`) y confirmar que pasa en CI. Unitarios+integración 84/84 y E2E 2/2 en verde por separado (juntos en esta máquina compiten por recursos entre dos contenedores SQL efímeros simultáneos — riesgo ya documentado en `architecture.md`, no una regresión).
- [x] Cierre: mover `docs/specs/BAS-15/` a `docs/specs/archive/BAS-15/`, actualizar `BasketBaseTracker.slnx` y abrir el PR de `feature/BAS-15` a `develop` (sin fusionar sin confirmación humana).
