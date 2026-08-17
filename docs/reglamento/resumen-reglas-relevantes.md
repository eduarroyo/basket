# Resumen de reglas relevantes para BasketBaseTracker

Extracto de las reglas de baloncesto que afectan al modelo de datos y a la lógica de la aplicación (sistema de puntos, desempates, marcador técnico, incomparecencia, alineación indebida). No es un resumen completo de las reglas de juego — solo lo relevante para cómo se registra y calcula un resultado administrativamente.

## Fuentes

1. **Reglas Oficiales de Baloncesto FIBA 2022** — reglas de juego sobre la pista.
   https://www.feb.es/Documentos/Enlaces/[5612]Reglas%20Oficiales%20de%20Baloncesto%20FIBA%202022_V2.pdf
2. **Reglamento General y de Competiciones de la Federación Andaluza de Baloncesto** (aprobado por Asamblea General, ratificado el 29/07/2022) — normativa administrativa de competición, la relevante para esta aplicación.
   https://www.andaluzabaloncesto.org/descargar?seccion=documentos&id=2303&delegacion=1
3. **Reglamento Disciplinario de la Federación Andaluza de Baloncesto** (temporada 2020-21) — tipifica infracciones y sanciones; es el documento al que remiten repetidamente los dos anteriores para los supuestos de incomparecencia y alineación indebida. Aportado localmente por el usuario (`DISCIPLINARIO-FAB.pdf`), sin URL pública confirmada — si se localiza una, añadirla aquí para mantener la trazabilidad como en las otras dos fuentes.

Copias locales en este mismo directorio (`RGyC-FAB.pdf`, `Reglas_Oficiales_de_Baloncesto_FIBA_2022_V2.pdf`, `DISCIPLINARIO-FAB.pdf`) por si hay que volver a consultarlas — el fichero de la FAB (Reglamento General) es un PDF escaneado/firmado electrónicamente sin capa de texto, así que para releerlo hace falta renderizar sus páginas como imagen (no sirve `pdftotext`); el Disciplinario sí tiene capa de texto real y se procesó con `pdftotext -layout`. Ver la nota de metodología al final.

## 1. Estructura de un partido (FIBA)

- 4 cuartos de 10 minutos (40 minutos de tiempo de juego).
- Prórrogas de 5 minutos si hay empate al final del último periodo, tantas como sean necesarias para deshacer el empate (FIBA; confirmado también por el reglamento FAB, Art. 83).
- Un equipo necesita un mínimo de 2 jugadores en pista para que el partido sea válido; si un equipo se queda con menos de 2, el encuentro se da por finalizado (FIBA Art. 3.2 aprox.; desarrollado con más detalle en el reglamento FAB, ver §5).

## 2. Sistema de puntos de clasificación (FAB, Art. 77)

En competiciones por sistema de liga:

- **2 puntos** al equipo vencedor.
- **1 punto** al equipo vencido.

No hay 0 puntos por derrota en el sistema base — el "0" solo aparece como parte de un marcador técnico (ver §4), no como puntos de clasificación. En sistema de play-off, el ganador de la eliminatoria es el que suma más victorias en los encuentros programados (Art. 77.2).

## 3. Desempates en la clasificación (FAB, Art. 84–85)

**Hasta el final de la primera vuelta** (ligas a doble vuelta), por este orden:
1. Mayor diferencia general de tantos a favor y en contra.
2. Mayor cociente general de tantos a favor y en contra.
3. Puntos conseguidos solo entre los equipos empatados.
4. Mayor diferencia de tantos entre los equipos que sigan empatados.
5. Mayor cociente de tantos entre los equipos que sigan empatados.

**Desde el inicio de la segunda vuelta y en Campeonatos de Andalucía**, por este orden:
- Si son 2 equipos empatados:
  1. Puntos obtenidos en los partidos jugados entre ellos.
  2. Mayor diferencia de tantos a favor/en contra en los encuentros jugados entre ellos.
  3. Mayor número de tantos a favor en los encuentros jugados entre ellos.
  4. Mayor diferencia general de tantos a favor/en contra (toda la competición).
  5. Mayor número de tantos a favor general (toda la competición).
- Si son más de 2 equipos empatados: se aplican los mismos criterios en bloque; si el número de empatados se reduce, se repite el procedimiento entre los que sigan empatados.
- **Excepción**: un equipo con un tanteo de 2-0 en contra (o que haya cometido una infracción cuya sanción pudiera ser ese resultado) ocupa la última posición entre todos los empatados con él, con independencia de los resultados que tuviera con esos equipos (Art. 84.iv / 85.c).

Si la competición se da por finalizada antes de disputarse todas sus fases, el orden se establece por el total de puntos alcanzados hasta la fecha de suspensión, con independencia del número de encuentros jugados por cada equipo (Art. 81.3).

## 4. Marcador técnico y resultados administrativos (FAB)

El Reglamento General usa consistentemente **2-0** como marcador técnico en los supuestos de partido no completado por causas imputables a un equipo — no 20-0 como se había apuntado tentativamente en `data-model.md` antes de consultar la normativa real:

- **Art. 80 (Reglamento General)** — si el partido se da por finalizado porque un equipo se queda con un solo jugador: el resultado final es el que reflejara el marcador en ese momento, **o 2-0 si iba ganando el equipo que se quedó con un jugador** (para que el equipo infractor no se beneficie de ir ganando). En este caso concreto el reglamento aclara explícitamente que **no se descuenta ningún punto adicional** de clasificación al equipo infractor.
- **Art. 148 (Reglamento General)** — si un encuentro no se puede celebrar por falta de Fuerza Pública (orden público) y los órganos federativos deciden no celebrarlo: se da por perdido al equipo local por **0-2**.
- **Art. 149.2 (Reglamento General)** — si un encuentro se suspende por actitud incorrecta imputable a un solo equipo: gana el otro equipo por el resultado que hubiera en ese momento, **o por 2-0 si ese resultado le fuera desfavorable**.
- **Art. 79 (Reglamento General)** — si un encuentro termina por decisión arbitral antes de tiempo por conducta incorrecta, el equipo culpable se asimila a un "equipo retirado" a efectos de determinar el resultado, y de las sanciones adicionales que pudiera imponer el Reglamento Disciplinario.
- **Art. 48.2 (Reglamento Disciplinario)** — confirma el mismo patrón para un supuesto distinto (club sancionado por impago que reincide): se le da por perdido el partido **"por el resultado de 2-0"**, en estos términos textuales. Es la única vez que el "2-0" aparece escrito de forma literal como marcador (en el Reglamento General se deduce siempre de fórmulas como "o 2-0 si le fuera desfavorable").

**Incomparecencia y alineación indebida — ahora sí confirmado, con una novedad importante** (Reglamento Disciplinario, Art. 10, 43, 44, 45):

- **Art. 10.B** (catálogo general de sanciones a Clubes) incluye, entre otras, "**Pérdida del encuentro o, en su caso, eliminatoria**" y, como sanción **distinta y adicional**, "**Pérdida de puntos o puestos en la clasificación**". Son dos sanciones separadas, no la misma cosa.
- **Art. 43** — la incomparecencia injustificada a un encuentro (o negativa a jugarlo) y la retirada injustificada del terreno de juego (apartados A y B), y la **alineación indebida con mala fe o negligencia** (apartado F), se tipifican como infracción **muy grave**: multa de 301€ a 600€ (irrelevante para la app) **+ pérdida del encuentro + descuento de UN PUNTO en la clasificación general** (o de la eliminatoria). El artículo no repite el "2-0" en cifras, pero por el patrón del Reglamento General (§ anterior) es razonable asumir que la "pérdida del encuentro" se registra igual, con el añadido explícito del punto de penalización — que **no** existe en el resto de supuestos de esta lista (Art. 80 dice expresamente lo contrario: sin descuento de puntos).
- **Art. 44** — excepción: si la alineación indebida se produjo **sin mala fe ni negligencia**, no hay marcador técnico ni penalización — se **anula el encuentro y se repite** (sin ese jugador en la repetición, si el equipo infractor había ganado), con los gastos de la repetición a cargo del club infractor. Es el mismo patrón que la incomparecencia justificada del Reglamento General (§5): anulación y repetición, no derrota técnica.
- **Art. 45.A** — menos de 5 jugadores presentes al inicio del encuentro (distinto del Art. 80 del Reglamento General, que es quedarse con menos de 2 **durante** el partido): infracción **grave** (no muy grave), multa 60-150€ + pérdida del encuentro, **sin mención de descuento de puntos adicional**.
- **Art. 50** — si un equipo se retira de la competición completa (no de un solo partido): se **anulan todos sus encuentros** de esa competición o fase (no se registran como perdidos 2-0, quedan sin efecto), pérdida de cuota/aval, descenso de categoría la temporada siguiente.

**Implicación para el modelo de datos — señalado aparte, no aplicado todavía**: la penalización de "descuento de un punto en la clasificación" del Art. 43 es un dato que hoy no tiene dónde guardarse — `Competicion`/`Jornada`/`Partido` no tienen ningún campo de penalización de puntos independiente del resultado del propio partido. Antes de implementar la clasificación (punto 14 de `architecture.md`) hay que decidir cómo modelarlo (¿un ajuste a nivel de `Equipo` dentro de la competición, aplicado por el administrador en el expediente disciplinario? ¿algo en `Partido`?). No lo resuelvo aquí porque cambia el modelo de datos — lo dejo explícitamente para que se decida en conversación.

## 5. Incomparecencia de un equipo (FAB Reglamento General Art. 78, 92, 150; Disciplinario Art. 43, 44, 50)

- **Art. 78 (Gral.)**: la incomparecencia de un equipo, o su retirada del terreno de juego antes de finalizar el encuentro, se sanciona según el Reglamento Disciplinario.
- **Art. 92 (Gral.)**: si un equipo se niega a iniciar el partido cuando lo requieren los árbitros, esa negativa puede considerarse incomparecencia.
- **Art. 150.1 (Gral.)**: si el encuentro se suspende por incomparecencia de un equipo y este **justifica adecuadamente** (a criterio del Juez Único de Competición) el motivo de su no presentación, el partido se vuelve a celebrar — sin marcador técnico — corriendo el club ausente con los nuevos gastos de desplazamiento y arbitraje. Incomparecencia justificada ⇒ aplazamiento con repetición del partido, no derrota técnica.
- **Art. 43.A/B (Disciplinario)**: incomparecencia **no justificada** (o negativa a jugar, o retirada injustificada) ⇒ infracción muy grave ⇒ pérdida del encuentro + **descuento de 1 punto en la clasificación** + multa. Ver detalle y matiz sobre el marcador exacto en §4.
- **Art. 50 (Disciplinario)**: si la incomparecencia/retirada es de la competición completa (no de un solo partido) el tratamiento es distinto — se anulan todos los partidos ya jugados por ese equipo en esa competición/fase, no se dan por perdidos.

## 6. Alineación indebida / elegibilidad de jugadores (FIBA; FAB Reglamento General Art. 23–31; Disciplinario Art. 43-44)

- **FIBA**: si un jugador no elegible participa, su equipo queda descalificado de ese partido (sin más detalle administrativo — es una regla de juego, no de competición).
- **FAB Art. 23 (Gral.)**: los jugadores se clasifican por sexo, edad y categoría de la competición en que participen. Los años de nacimiento de cada categoría (benjamín/alevín/infantil/cadete/juvenil...) **los fija anualmente la Junta Directiva de la FAB** — no están fijados en este reglamento, así que no hay un valor estable que copiar al modelo de datos; se obtienen de la circular de la temporada correspondiente.
  - Excepción permanente ya fijada en el reglamento: un jugador con licencia junior o Sub-22 puede alinearse en encuentros de categoría senior de su mismo club; un jugador con licencia minibasket puede alinearse en categoría infantil (Art. 23.2). También se permite a cadetes alinearse con equipos senior autonómicos o provinciales (Art. 31.4).
- **FAB Art. 25, 30–31 (Gral.) — qué constituye alineación indebida**:
  - Un jugador solo puede tener licencia por un equipo de un mismo club (Art. 30).
  - Si un jugador tiene licencias por más de un equipo del mismo club en distinta categoría, solo es válida la expedida primero (o la de categoría máxima si no puede determinarse el orden); jugar con las demás licencias anuladas **se considera alineación indebida explícitamente** (Art. 30, párrafo final).
  - Las licencias son de la clase que corresponde a la edad del jugador, expedidas a favor de un equipo determinado; jugar fuera de esas condiciones (salvo las excepciones expresas del Art. 31) es alineación indebida por construcción del propio sistema de licencias.
  - Un jugador no puede alinearse en dos o más encuentros de distintas categorías que se disputen de forma simultánea (Art. 31.3).
- **Sanción — ahora confirmada, con dos tratamientos distintos según intencionalidad** (Disciplinario Art. 43.F y 44):
  - **Con mala fe o negligencia**: mismo tratamiento que la incomparecencia no justificada — pérdida del encuentro + descuento de 1 punto en la clasificación + multa (Art. 43.F).
  - **Sin mala fe ni negligencia**: sin sanción de puntos — se anula el encuentro y se repite sin ese jugador, gastos a cargo del club infractor (Art. 44). El administrador tendría que distinguir este caso del anterior al registrar el resultado — no es un simple "alineación indebida = pérdida", depende de la intencionalidad apreciada por el Juez Único de Competición.

## 7. Lo que ya no queda por confirmar, y lo que sigue sin cubrir ningún documento

Resuelto con el Reglamento Disciplinario: el tratamiento de incomparecencia no justificada y alineación indebida (§4-§6), incluyendo el hallazgo de que la alineación indebida "de buena fe" no lleva marcador técnico sino anulación y repetición, y que la incomparecencia/alineación con mala fe añade una penalización de puntos de clasificación que ningún otro supuesto tiene.

Sigue sin cubrir ningún documento:
- **Categorías de edad exactas** (benjamín/alevín/infantil/cadete/juvenil) — se fijan anualmente por circular, no en ningún reglamento consultado.
- **Posiciones de jugador** (base/escolta/alero/ala-pívot/pívot) — no es una regla oficial en ninguno de los documentos, es una convención de análisis del juego. La decisión ya tomada en `architecture.md` (5 posiciones clásicas, campo opcional) es una convención propia de la aplicación, no una obligación reglamentaria.

## Nota de metodología

El PDF del Reglamento General de la FAB (`RGyC-FAB.pdf`) es un documento escaneado y firmado electrónicamente por la Junta de Andalucía: cada página es una imagen sin capa de texto seleccionable (solo el pie de firma es texto real), por lo que `pdftotext` no extrae el contenido. Se renderizaron las páginas relevantes como imágenes (con `PyMuPDF`, instalado vía `pip install pymupdf`) y se leyeron visualmente artículo por artículo. El PDF de la FIBA y el del Reglamento Disciplinario (`DISCIPLINARIO-FAB.pdf`) sí tienen capa de texto real: el primero se consultó vía fetch web directo, el segundo con `pdftotext -layout` + búsqueda por palabra clave sobre el texto extraído. Si en el futuro hace falta ampliar este resumen, repetir el procedimiento que corresponda según el documento.
