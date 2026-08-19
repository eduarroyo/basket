---
codigo: BAS-17
estado: Completado
tags:
  - tasks
---

# BAS-17: Tareas

- [x] Crear el proyecto `src/BasketBaseTracker.Seed` con la CLI de `dotnet` (skill `dotnet`), con `ProjectReference` a `BasketBaseTracker.Web`; añadirlo a `BasketBaseTracker.slnx`.
- [x] Implementar el algoritmo round-robin (método del círculo) como clase de dominio pura, soportando N par/impar (con *bye*) y liga a una o dos vueltas.
- [x] Test unitario del algoritmo round-robin: no-repetición de equipo por jornada, número de rondas correcto, comportamiento con N impar.
- [x] Generar el catálogo estático (municipios/nombres de club de la provincia de Sevilla, categorías estándar) y la lógica que combina ese catálogo para producir `Categoria`, `Sede`, `Club` según `--clubes`.
- [x] Generar `Temporada` y `Competicion`: N temporadas históricas (`Finalizada`/`Archivada`) + 1 `EnCurso`, con una `Competicion` por combinación razonable de `Temporada` x `Categoria`.
- [x] Generar `Equipo` (participación de cada club en cada competición) y `FichaJugador` (8-20 por equipo, dorsal único, posición aleatoria).
- [x] Generar `Jornada`/`Partido` de cada competición con el algoritmo round-robin; para la temporada `EnCurso`, repartir jornadas en fines de semana y marcar `Jugado`/`Programado` según la fecha de ejecución.
- [x] Generar resultado y `PartidoParcial` (4 periodos, suma coherente con el resultado) para los partidos en `Jugado`.
- [x] Implementar los argumentos `--temporadas`, `--clubes`, `--reset` (parseo manual) y la lógica de borrado completo de datos deportivos cuando se indica `--reset`. Salvaguarda añadida a nivel de comando (no solo de workflow): `--reset` exige además `--confirmar BORRAR`.
- [x] Test de integración: ejecutar el comando contra base de datos de test real (`Aspire.Hosting.Testing`) y verificar volumen/estados esperados.
- [x] Documentar en el propio proyecto (o en `README.md`) cómo ejecutar el comando en local contra Aspire, incluida la configuración por `dotnet user-secrets`.
- [x] Crear `.github/workflows/seed-demo.yml` (`workflow_dispatch`, inputs `temporadas`/`clubes`/`reset`/`confirmar`), reutilizando el patrón de firewall temporal de `deploy.yml` y el login `basketbasetracker_app`. Pendiente de una acción humana fuera del control de versiones: configurar el secreto de repositorio `AZURE_SQL_APP_PASSWORD` en GitHub (ver corrección en `plan.md`, decisión técnica 7) antes de poder disparar este workflow contra producción.
- [x] Verificación manual: ejecutar el seeding en local y comprobar en las pantallas públicas (calendario, resultados, clasificación) que los datos de la temporada `EnCurso` son coherentes. Detectado y corregido durante esta verificación un caso límite real: si "hoy" cae en el parón estival (julio-agosto, fuera de cualquier ventana Sept-jun realista), la temporada en curso no tenía ninguna jornada `Programado` (todas caían ya en el pasado). Corregido recentrando el calendario de esa competición sobre "hoy" en ese caso — ver `DemoSeeder.SeedAsync`.
- [x] Actualizar `data-model.md`/`architecture.md` si la implementación revela algún cambio de diseño no anticipado (paso de Cierre). No ha hecho falta: ni el modelo de datos ni las pantallas cambian — este incremento solo añade un comando de seeding fuera de `Web`.
