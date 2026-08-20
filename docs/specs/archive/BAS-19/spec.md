---
codigo: BAS-19
titulo: Presentación del proyecto para evaluación (reveal.js)
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-20
tags:
  - documentación
---

# BAS-19: Presentación del proyecto para evaluación (reveal.js)

## Descripción

BasketBaseTracker se presenta como trabajo de fin de curso de un curso sobre desarrollo con IA. El evaluador necesita una presentación de apoyo, accesible públicamente sin credenciales, que explique tanto **qué hace la aplicación** (producto) como **cómo se ha desarrollado con asistencia de IA** (proceso: desarrollo dirigido por especificaciones descrito en `workflow.md`, uso de Claude Code, gobernanza de ramas/PRs). Se implementa como una página de diapositivas con [reveal.js](https://revealjs.com), servida como recurso estático de `BasketBaseTracker.Web` para que quede disponible en producción tras el despliegue, sin depender de un hosting externo (GitHub Pages, Artifact de claude.ai...) que el evaluador tendría que localizar por separado.

## Alcance

- Página HTML autocontenida (`reveal.js` + estilos vía CDN o embebido) en `src/BasketBaseTracker.Web/wwwroot/presentacion/`, servida como fichero estático — sin controlador, sin Razor Page, sin autenticación.
- Contenido en español, ~12-15 diapositivas, mitad centradas en el producto (qué resuelve, capturas de pantallas reales) y mitad en el proceso de desarrollo con IA (ciclo spec→plan→tasks→implementación→cierre, gobernanza de ramas y PRs, testing).
- Capturas de pantalla reales de las páginas **públicas** de producción (`https://web.ashymoss-b1c8f995.spaincentral.azurecontainerapps.io/`), obtenidas navegando con automatización de navegador.
- Al menos un diagrama (arquitectura de despliegue y/o ciclo de vida de un incremento BAS-N), reutilizando o adaptando los ya existentes en `architecture.md`/`workflow.md` donde tenga sentido.
- Un incremento BAS-N real (completado y archivado) usado como caso de estudio breve del proceso spec-driven.

## Fuera de alcance

- Capturas del área **Admin** de producción: exigiría usar credenciales de administrador reales sobre el entorno de producción, algo que no se justifica para un recurso de presentación — esa sección de la charla se apoya en descripción/diagrama, no en captura real.
- Cualquier cambio de rutas, layout compartido o navegación del sitio existente — la página vive aislada en su propia carpeta de `wwwroot`, no enlazada desde `Public`/`Admin`.
- Versión editable/interactiva más allá de reveal.js estándar (sin control de acceso, sin analítica, sin modo orador persistente más allá de lo que reveal.js ofrece de serie).
- Traducción a otros idiomas.

## Criterios de aceptación

- [x] La página es accesible en `/presentacion/` sin autenticación — verificado en local con `aspire run` (mismo binario que se despliega); quedará disponible en la URL pública de producción automáticamente en cuanto este PR se fusione y el pipeline existente despliegue `main`, sin ningún paso de despliegue propio de este incremento.
- [x] Contiene 14 diapositivas, en español: portada, contexto/problema, qué hace la app, capturas de portada+calendario, capturas de detalle de partido+clasificación, panel de administración (descripción), diagrama de arquitectura, decisiones destacadas, ciclo SDD, caso de estudio BAS-3, gobernanza, testing, estado actual, cierre.
- [x] Las capturas de pantalla proceden de la URL de producción real (`web.ashymoss-b1c8f995.spaincentral.azurecontainerapps.io`), no de mockups ni de un entorno local.
- [x] La página funciona correctamente sirviéndose como fichero estático — verificado en local con `aspire run` tras corregir el orden del pipeline (ver `plan.md`).
- [x] No introduce ninguna dependencia nueva en `Directory.Packages.props`. El único cambio en `Program.cs` es `app.UseDefaultFiles()` (middleware estándar, sin lógica de negocio), reordenado antes de `UseRouting()` — necesario para que `/presentacion/` resuelva a `index.html`, descubierto durante la validación local (ver `plan.md`). Los 110 tests existentes (unitarios + integración) siguen en verde tras el cambio.
- [ ] El PR a `develop` está abierto; fusionarlo sigue exigiendo confirmación humana explícita.

## Aclaraciones

- **¿Foco de la presentación?** Mitad producto (qué hace la app, con capturas), mitad proceso (cómo se ha desarrollado con IA) — decidido explícitamente por el autor, en vez de volcarse solo en uno de los dos.
- **¿Capturas reales o mockups?** Reales, y tomadas contra producción (no contra un entorno local con datos de demostración sintéticos) porque producción tiene más datos cargados y resulta más representativo para el evaluador. Se complementan con diagramas donde una captura no aporta o no es apropiada (p. ej. Admin).
- **¿Duración/extensión?** Breve: ~12-15 diapositivas, pensada para una exposición de 8-10 minutos.
- **¿Dónde se sirve el resultado final?** Como página pública dentro de la propia aplicación (no como Artifact de claude.ai ni como fichero suelto en el repo sin desplegar), para que el evaluador pueda acceder a ella de forma pública tras el release, con una URL estable.
- **¿Sigue el ciclo completo BAS-N (spec→plan→tasks) o se trata como recurso ligero?** Ciclo completo — decisión explícita del autor pese a que este incremento no toca `data-model.md` ni añade una pantalla de producto en el sentido de `screens.md`, por consistencia con la regla de `CLAUDE.md` de que todo el trabajo de desarrollo sigue `workflow.md`.
- **¿Cómo se sirve técnicamente dentro de la app?** Fichero estático en `wwwroot/` (no una Razor Page dedicada), servido directamente por el middleware de archivos estáticos de ASP.NET Core ya presente — más simple, no necesita reutilizar el layout del sitio ni tocar rutas.
- **URL de producción para las capturas**: `https://web.ashymoss-b1c8f995.spaincentral.azurecontainerapps.io/` — el dominio propio (`basketbase.es`, BAS-4) sigue sin desplegarse, así que esta es la URL pública real actual.
- **Admin fuera de alcance de las capturas**: no se navega el área Admin de producción con credenciales reales para este recurso; esa parte de la charla usa diagrama/descripción, no pantallazo.

## Referencias

- [[architecture]] — diagrama de despliegue y decisiones destacadas para las diapositivas de arquitectura.
- [[screens]] — inventario de pantallas públicas usado para elegir qué capturar.
- [[workflow]] — ciclo SDD que se explica en las diapositivas de proceso.
