---
codigo: BAS-15
titulo: Atajos de marcador en Resultados (parciales, victoria técnica)
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-19
dependeDe:
  - "[[archive/BAS-10/spec|BAS-10]]"
tags:
  - frontend
---

# BAS-15: Atajos de marcador en Resultados (parciales, victoria técnica)

## Descripción

En `BAS-10` se decidió deliberadamente que `PartidoParcial` (parciales por periodo) y el marcador final (`Partido.PuntosLocal`/`PuntosVisitante`) fueran independientes entre sí, sin validación cruzada, para no obligar al administrador a llevar cuenta exacta de los parciales (`archive/BAS-10/spec.md`, Fuera de alcance). En la práctica esto genera una fricción real detectada en producción: tras introducir los parciales de un partido y abrir `Resultados` para marcarlo como `Jugado`, aparece el error de validación "marcador sin empate" porque el marcador sigue vacío — nada conecta ambos formularios más allá de un enlace de navegación entre pantallas.

Este incremento **no revierte** la decisión de `BAS-10`: parciales y marcador siguen sin acoplarse ni validarse entre sí a nivel de servidor. Añade en su lugar atajos de relleno en el propio formulario de `Resultados`, reutilizando el patrón que ya existe ahí mismo para la victoria técnica al resolver administrativamente un partido (autorrelleno por script en el cliente, marcador siempre editable a mano, sin validación de servidor contra ese valor).

De paso, y a petición expresa, se revisan los formularios de administración de `Partido` (`Partido/Edit`, `Resultados`, `PartidoParcial`) en busca de otros campos del modelo de datos ausentes de cualquier formulario, de la misma naturaleza que el marcador. Resultado de esa revisión: **no se ha encontrado ningún campo del modelo sin cobertura** — todos los campos de `Partido` y `PartidoParcial` ya son editables desde algún formulario, y la navegación entre `Partido/Edit` ↔ `Resultados` ↔ `Parciales` ya está enlazada en ambos sentidos. El problema real no era un campo ausente, sino la falta de un atajo entre dos formularios ya existentes — que es lo que resuelve este incremento.

## Alcance

- Tres acciones en el formulario de `Resultados` (`Areas/Admin/Pages/Resultado/Edit.cshtml`) que rellenan `PuntosLocal`/`PuntosVisitante` en el propio formulario, sin recargar la página ni guardar automáticamente:
  1. **Calcular desde los parciales**: suma los `PartidoParcial` ya guardados de ese partido.
  2. **Victoria técnica local (2-0)**.
  3. **Victoria técnica visitante (0-2)**.
- El marcador resultante sigue siendo un campo de texto editable tras pulsar cualquiera de los tres — son un atajo de relleno, no un valor fijo ni bloqueado.
- Disponibles con independencia del `Estado` seleccionado en el formulario (no solo al resolver administrativamente con motivo/ganador) — igual que el marcador ya es visible para `Jugado` y `Resuelto`.
- El botón "Calcular desde los parciales" se deshabilita si el partido no tiene ningún `PartidoParcial` registrado.
- Los tres botones, al rellenar el marcador, fijan también el desplegable de equipo ganador (`Partido.EquipoGanadorResolucionId`) cuando el marcador resultante determina un ganador: técnica local → equipo local; técnica visitante → equipo visitante; calcular desde parciales → el equipo con más puntos en la suma, o **sin seleccionar / limpio** si la suma da empate.
- Se retira el autorrelleno inverso que existe hoy (cambiar el desplegable de ganador ya no vuelve a escribir el marcador) — pasa a ser una relación de un solo sentido: los botones que fijan el marcador fijan también el ganador, pero cambiar el ganador a mano después no toca el marcador.
- **Corrige de paso un bug preexistente de BAS-10** encontrado durante la verificación manual: el script de `Resultado/Edit.cshtml` comparaba `estadoSelect.value === 'Jugado'`, pero el valor real de cada `<option>` generada por `Html.GetEnumSelectList<PartidoEstado>()` es el entero subyacente del enum (`"1"`, no `"Jugado"`) — la comparación nunca era cierta, así que el grupo del marcador **no llegaba a mostrarse nunca** al elegir `Jugado` o `Resuelto`. Es la causa real, más fundamental que la falta de atajos, del problema que motivó este incremento: sin el grupo visible, no había forma de introducir un marcador desde la interfaz en absoluto. Corregido comparando por el texto de la opción seleccionada en vez de por su valor.

## Fuera de alcance

- Validación cruzada entre parciales y marcador, o auto-cálculo persistido sin acción explícita del administrador — decisión ya tomada en `BAS-10`, sin cambios; los botones rellenan el formulario, no disparan un guardado ni una validación nueva.
- Cambios al modelo de datos: ni `Partido` ni `PartidoParcial` cambian.
- Cualquier otro campo o formulario del área Admin fuera de `Partido`/`Resultados`/`Parciales` — la auditoría de esta spec se limitó a esos tres, según lo pedido, y no encontró más huecos que cubrir.

## Criterios de aceptación

- [x] En `Resultados`, un botón "Calcular desde los parciales" rellena `PuntosLocal`/`PuntosVisitante` con la suma de los `PartidoParcial` existentes de ese partido (sumando lo que haya, sin exigir que estén todos los periodos), sin recargar la página ni guardar.
- [x] Ese botón aparece deshabilitado si el partido no tiene ningún parcial registrado.
- [x] Un botón "Victoria técnica local (2-0)" rellena el marcador a 2-0; un botón "Victoria técnica visitante (0-2)" lo rellena a 0-2 — ambos disponibles con independencia del `Estado` seleccionado.
- [x] El marcador sigue siendo editable a mano después de pulsar cualquiera de los tres botones.
- [x] Los botones de victoria técnica fijan también el desplegable de equipo ganador (local/visitante según el botón); el botón de parciales lo fija al equipo con más puntos en la suma, o lo deja sin seleccionar si la suma da empate.
- [x] Cambiar el desplegable de equipo ganador a mano, después de usar cualquiera de los tres botones, no modifica el marcador ya rellenado (se retira el autorrelleno inverso que existe hoy).
- [x] Guardar sigue exigiendo un marcador sin empate (regla de validación de servidor sin cambios, `ResultadoReglas.MarcadorValido`) — los botones ayudan a rellenarlo, no sustituyen la validación existente.

## Aclaraciones

- **Parciales incompletos**: "Calcular desde los parciales" suma lo que exista, sin avisar ni bloquear si faltan periodos — coherente con la decisión de BAS-10 de no exigir cuenta exacta. El administrador ve el resultado y decide si le vale o lo corrige a mano.
- **Relación con el desplegable de ganador**: los tres botones que fijan el marcador fijan también el equipo ganador cuando el resultado lo determina (técnica local/visitante siempre; parciales solo si no hay empate en la suma, si no lo deja sin seleccionar). A cambio, se retira el autorrelleno inverso que existe hoy (cambiar el ganador a mano ya no reescribe el marcador) — la relación pasa a ser de un solo sentido.
- **Auditoría de campos del formulario de partido**: revisados `Partido/Edit`, `Resultados` y `Parciales` (Create/Edit/Delete/Index) contra `data-model.md`. No se ha encontrado ningún campo de `Partido` ni `PartidoParcial` sin cobertura en algún formulario, ni ningún enlace de navegación que falte entre las tres pantallas (ya están enlazadas en ambos sentidos). El problema real que motivó este incremento no era un campo ausente, sino la falta de un atajo entre `Parciales` y `Resultados`.

## Referencias

- [[data-model#Partido|data-model.md — Partido]], [[data-model#PartidoParcial|PartidoParcial]] — entidades implicadas, sin cambios.
- [[archive/BAS-10/spec|BAS-10]] — decisión original de independencia entre parciales y marcador (no se revierte) y patrón ya existente de autorrelleno por script para la victoria técnica al resolver administrativamente.
- [[screens#Área admin (autenticado)|screens.md]] — pantalla `Resultados` (línea "Introducir marcador y parciales, o cambiar estado").
