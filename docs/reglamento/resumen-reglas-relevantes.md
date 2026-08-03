# Resumen de reglas relevantes para BasketBaseTracker

Extracto de las reglas de baloncesto que afectan al modelo de datos y a la lógica de la aplicación (sistema de puntos, desempates, marcador técnico, incomparecencia, alineación indebida). No es un resumen completo de las reglas de juego — solo lo relevante para cómo se registra y calcula un resultado administrativamente.

## Fuentes

1. **Reglas Oficiales de Baloncesto FIBA 2022** — reglas de juego sobre la pista.
   https://www.feb.es/Documentos/Enlaces/[5612]Reglas%20Oficiales%20de%20Baloncesto%20FIBA%202022_V2.pdf
2. **Reglamento General y de Competiciones de la Federación Andaluza de Baloncesto** (aprobado por Asamblea General, ratificado el 29/07/2022) — normativa administrativa de competición, la relevante para esta aplicación.
   https://www.andaluzabaloncesto.org/descargar?seccion=documentos&id=2303&delegacion=1

Copias locales en este mismo directorio (`RGyC-FAB.pdf`, `Reglas_Oficiales_de_Baloncesto_FIBA_2022_V2.pdf`) por si hay que volver a consultarlas — el fichero de la FAB es un PDF escaneado/firmado electrónicamente sin capa de texto, así que para releerlo hace falta renderizar sus páginas como imagen (no sirve `pdftotext`); ver la nota de metodología al final.

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

El reglamento usa consistentemente **2-0** como marcador técnico en los distintos supuestos de partido no completado por causas imputables a un equipo — no 20-0 como se había apuntado tentativamente en `data-model.md` antes de consultar la normativa real:

- **Art. 80** — si el partido se da por finalizado porque un equipo se queda con un solo jugador: el resultado final es el que reflejara el marcador en ese momento, **o 2-0 si iba ganando el equipo que se quedó con un jugador** (para que el equipo infractor no se beneficie de ir ganando). En este caso concreto el reglamento aclara explícitamente que **no se descuenta ningún punto adicional** de clasificación al equipo infractor.
- **Art. 148** — si un encuentro no se puede celebrar por falta de Fuerza Pública (orden público) y los órganos federativos deciden no celebrarlo: se da por perdido al equipo local por **0-2**.
- **Art. 149.2** — si un encuentro se suspende por actitud incorrecta imputable a un solo equipo: gana el otro equipo por el resultado que hubiera en ese momento, **o por 2-0 si ese resultado le fuera desfavorable**.
- **Art. 79** — si un encuentro termina por decisión arbitral antes de tiempo por conducta incorrecta, el equipo culpable se asimila a un "equipo retirado" a efectos de determinar el resultado (por tanto, mismo tratamiento que el retiro voluntario del Art. 78) y de las sanciones adicionales que pudiera imponer el Reglamento Disciplinario.

**No confirmado en este reglamento**: el marcador exacto para la incomparecencia total de un equipo (no presentarse) y para la alineación indebida se remiten explícitamente al **Reglamento Disciplinario** de la FAB (Art. 78, Art. 150), que es un documento distinto no consultado todavía. Por el patrón consistente de 2-0 en todos los demás supuestos de esta lista, es razonable *asumir* 2-0 también aquí, pero no está confirmado por un artículo textual — si hace falta certeza total, habría que localizar y consultar el Reglamento Disciplinario de la FAB.

## 5. Incomparecencia de un equipo (FAB, Art. 78, 92, 150)

- **Art. 78**: la incomparecencia de un equipo, o su retirada del terreno de juego antes de finalizar el encuentro, se sanciona según el Reglamento Disciplinario (no detallado aquí).
- **Art. 92**: si un equipo se niega a iniciar el partido cuando lo requieren los árbitros, esa negativa puede considerarse incomparecencia.
- **Art. 150.1**: si el encuentro se suspende por incomparecencia de un equipo y este **justifica adecuadamente** (a criterio del Juez Único de Competición) el motivo de su no presentación, el partido se vuelve a celebrar — sin marcador técnico — corriendo el club ausente con los nuevos gastos de desplazamiento y arbitraje. Es decir: incomparecencia justificada ⇒ aplazamiento con repetición del partido, no derrota técnica.
- Implícito por Art. 150.1 a contrario: incomparecencia **no** justificada ⇒ no hay repetición, se aplica la sanción del Reglamento Disciplinario (presumiblemente el 2-0 técnico visto en §4, sin confirmación textual).

## 6. Alineación indebida / elegibilidad de jugadores (FAB, Art. 23–31; FIBA)

- **FIBA**: si un jugador no elegible participa, su equipo queda descalificado de ese partido (sin más detalle administrativo — es una regla de juego, no de competición).
- **FAB Art. 23**: los jugadores se clasifican por sexo, edad y categoría de la competición en que participen. Los años de nacimiento de cada categoría (benjamín/alevín/infantil/cadete/juvenil...) **los fija anualmente la Junta Directiva de la FAB** — no están fijados en este reglamento, así que no hay un valor estable que copiar al modelo de datos; se obtienen de la circular de la temporada correspondiente.
  - Excepción permanente ya fijada en el reglamento: un jugador con licencia junior o Sub-22 puede alinearse en encuentros de categoría senior de su mismo club; un jugador con licencia minibasket puede alinearse en categoría infantil (Art. 23.2). También se permite a cadetes alinearse con equipos senior autonómicos o provinciales (Art. 31.4).
- **FAB Art. 25, 30–31 — qué constituye alineación indebida**:
  - Un jugador solo puede tener licencia por un equipo de un mismo club (Art. 30).
  - Si un jugador tiene licencias por más de un equipo del mismo club en distinta categoría, solo es válida la expedida primero (o la de categoría máxima si no puede determinarse el orden); jugar con las demás licencias anuladas **se considera alineación indebida explícitamente** (Art. 30, párrafo final).
  - Las licencias son de la clase que corresponde a la edad del jugador, expedidas a favor de un equipo determinado; jugar fuera de esas condiciones (salvo las excepciones expresas del Art. 31) es alineación indebida por construcción del propio sistema de licencias.
  - Un jugador no puede alinearse en dos o más encuentros de distintas categorías que se disputen de forma simultánea (Art. 31.3).
- **No confirmado en este reglamento**: el marcador/sanción específica de un partido en el que se detecta alineación indebida a posteriori — se remite igualmente al Reglamento Disciplinario.

## 7. Lo que NO cubre ninguno de los dos documentos

- **Marcador técnico exacto para incomparecencia total y alineación indebida** (solo inferible por patrón, ver §4–§6) — está en el Reglamento Disciplinario de la FAB, no consultado.
- **Categorías de edad exactas** (benjamín/alevín/infantil/cadete/juvenil) — se fijan anualmente por circular, no en este reglamento.
- **Posiciones de jugador** (base/escolta/alero/ala-pívot/pívot) — no es una regla oficial en ninguno de los dos documentos, es una convención de análisis del juego. La decisión ya tomada en `architecture.md` (5 posiciones clásicas, campo opcional) es una convención propia de la aplicación, no una obligación reglamentaria.

## Nota de metodología

El PDF de la FAB (`RGyC-FAB.pdf`) es un documento escaneado y firmado electrónicamente por la Junta de Andalucía: cada página es una imagen sin capa de texto seleccionable (solo el pie de firma es texto real), por lo que `pdftotext` no extrae el contenido. Se renderizaron las páginas relevantes como imágenes (con `PyMuPDF`, instalado vía `pip install pymupdf`) y se leyeron visualmente artículo por artículo. El PDF de la FIBA sí tiene capa de texto y se consultó vía fetch web directo. Si en el futuro hace falta ampliar este resumen (p. ej. para localizar el Reglamento Disciplinario y confirmar el marcador de incomparecencia/alineación indebida), repetir el mismo procedimiento de renderizado sobre `RGyC-FAB.pdf` o buscar el Reglamento Disciplinario específico en la web de Baloncesto Andalucía.
