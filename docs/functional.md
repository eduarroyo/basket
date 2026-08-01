# Funcionalidades

Este fichero describe la funcionalidad deseada para el proyecto BasketBaseTracker

## Funcionalidades iniciales

* Gestión de temporadas de competición. Los equipos, sedes, etc. pueden cambiar de una temporada a otra.
* Gestión de categorías (benjamín, alevín, infantil, cadete, juvenil...)
* Gestión de competiciones por categoría y temporada
* Gestión de sedes (lugares donde se juegan los partidos, generalmente pabellones municipales)
* Gestión de clubes
* Gestión de equipos (pertenencia a clubes, categoría, participación en una temporada)
* Gestión de jugadores (pertenencia a equipos, número de jugador, posición, etc. Sin datos personales)
* Gestión de calendarios (planificación de jornadas: nº de jornada, equipos enfrentados, fecha-hora, sede donde se juega)
* Gestión de resultados de la jornada (marcadores, estadísticas básicas, pendiente, aplazado, cancelado, posibilidad de decalrar victoria de un equipo sin que se haya disputado, por ejemplo por incomparecencia o por alineación indebida)
* Consulta de calendarios de partidos
* Consulta de resultados por jornada, por equipo, etc.
* Consulta de clasificación de equipos por categoría
* Observabilidad basada en Open Telemetry (logs, métricas, trazas) para monitorizar el rendimiento y la salud del sistema.
* Política de retención de datos: se definirán políticas claras para la retención y eliminación de datos, especialmente en lo que respecta a los resultados de los partidos y la información de los equipos, para cumplir con las regulaciones aplicables y garantizar la privacidad de los usuarios.
* Aislamiento de datos por temporada y categoría.
* Gestión de datos personales: para cumplir con el RGPD, se evitará el uso de datos personales. En su lugar, se utilizarán identificadores anónimos como el número de jugador.
* Como consecuencia de la decisión anterior, los jugadores no serán entidades fuertes en el sistema, sino que se tratarán como atributos de los equipos. Sacrificamos la capacidad de gestionar información detallada de los jugadores (como estadísticas individuales, cambios de equipo, etc.) a cambio de una mayor simplicidad y cumplimiento normativo.
* Importación/exportación de datos
* Calendario ical/google calendar: El servicio publicará el calendario de partidos en formato ical/google calendar y lo mantendrá acutalizado.

## Requisitos no funcionales iniciales

* Soportar picos de 300RPS durante los días de partido
* Latencia máxima de 200ms para consultas de calendarios, resultados y clasificaciones.
* Disponibilidad del servicio del 99.5% durante la temporada de competición. Tiempo máximo de inactividad de 3 horas por mes.
* Tasa de error inferior al 0,5% en endpoints críticos (consultas de calendarios, resultados y clasificaciones).
* Consistencia eventual: dado que los datos se actualizan principalmente durante los días de partido, se puede tolerar una consistencia eventual en la actualización de resultados y clasificaciones, siempre que las consultas reflejen los datos actualizados en un plazo máximo de 5 minutos.
* Seguridad: el sistema debe protegerse contra ataques comunes como inyección SQL, cross-site scripting (XSS) y denegación de servicio (DoS). Se implementarán medidas de seguridad adecuadas para garantizar la integridad y disponibilidad del sistema.
* Integridad de operaciones administrativas: las operaciones de gestión de competiciones, equipos, jugadores, calendarios y resultados deben ser atómicas y garantizar la integridad de los datos. El % de intervenciones manuales para corregir datos inconsistentes no debe superar el 0,1% de las operaciones administrativas.

## Fuera de alcance

* Compra de entradas/pases y pagos.
* Integraciones con sistemas de terceros (por ejemplo, sistemas de gestión de instalaciones deportivas, sistemas de estadísticas avanzadas, federaciones deportivas, etc.)

## Recursos de desarrollo

Al tratarse de un proyecto voluntario sin financiación, se cuenta con recursos limitados en términos de tiempo, personal y presupuesto. Esto implica que:

* Se optará por soluciones gratuitas de infraestructura, por lo que se acepta incurrir en altos tiempos de primera respuesta por arranques en frío.
* El equipo de desarrollo es una sola persona que adoptará todos los roles según la fase.

## Público objetivo

* Familias de jugadores y aficionados
* Organizadores de la competición
* Equipos y jugadores

## Carga esperada del sistema

Dado el número de competiciones, equipos y partidos, se espera una carga moderada en términos de usuarios concurrentes y volumen de datos. Al no tratarse de un sistema crítico y con un público objetivo relativamente pequeño, se puede optar por una arquitectura sencilla y escalable que permita evolucionar si aumenta el uso y se consigue financiación.

En cada temporada se organiza una competición por categoría (benjamín, alevín, infantil, cadete, juvenil...), no necesariamente todas las categorías cada temporada. Se espera que cada categoría tenga entre 8 y 14 equipos y cada equipo tiene entre 8 y 20 jugadores (es una estimación, no un límite).

La temporada de competición dura aproximadamente 6 meses, con partidos principalmente los fines de semana. Se espera que durante los días de partido haya picos de hasta 500 usuarios concurrentes consultando calendarios, resultados y clasificaciones.

## Usuarios del sistema

* Usuarios anónimos: pueden consultar calendarios, resultados y clasificaciones y estadísticas sin autenticación.
* Administradores de la competición: gestionan competiciones, temporadas, equipos, jugadores, calendarios, resultados y sedes. En el futuro, este perfil podría desglosarse en varios roles más específicos (gestor de competiciones, gestor de equipos, gestor de resultados, etc.)
* Administradores del sistema: gestionan la infraestructura, despliegues, monitorización y mantenimiento del sistema. Los procesos de importación/exportación sólo los pueden realizar los administradores del sistema.
