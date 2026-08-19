---
codigo: BAS-17
estado: Planificado
tags:
  - plan
---

# BAS-17: Plan técnico

## Entidades del modelo de datos afectadas

Todas las de `data-model.md` salvo `PenalizacionClasificacion` (fuera de alcance, ver `spec.md`): `Categoria`, `Sede`, `Club`, `Temporada`, `Competicion`, `Equipo`, `FichaJugador`, `Jornada`, `Partido`, `PartidoParcial`.

## Pantallas afectadas

Ninguna directamente — este incremento no añade ni modifica pantallas de `screens.md`. Alimenta datos para las pantallas públicas de consulta ya existentes o previstas (calendario, resultados, clasificación, fichas de equipo/club/sede), que sirven para verificar manualmente el resultado del seeding (último criterio de aceptación de `spec.md`).

## Decisiones técnicas específicas de este incremento

### 1. Proyecto de la herramienta de seeding

Nuevo proyecto de consola `src/BasketBaseTracker.Seed`, creado con la CLI de `dotnet` (skill `dotnet`, nunca a mano), con `ProjectReference` a `BasketBaseTracker.Web` para reutilizar directamente `ApplicationDbContext` y las entidades de `Data/Entities` — mismo patrón de reutilización que ya usa el propio proyecto Web internamente, sin duplicar el modelo. Se añade a `BasketBaseTracker.slnx` y a `Directory.Build.props`/`Directory.Packages.props` si hiciera falta alguna versión de paquete nueva (en principio ninguna: ver punto 4).

### 2. Generación del catálogo (Categoria, Sede, Club)

Listas estáticas con datos razonables de la provincia de Sevilla (municipios, nombres de club plausibles), combinadas programáticamente para producir el número de clubes/sedes que pida el parámetro correspondiente (punto 4). Sin librería de datos falsos (Bogus, Faker...) — el proyecto no la tiene como dependencia y el volumen de datos catálogo es pequeño (decenas de filas, no miles), así que no compensa añadir un paquete nuevo solo para esto.

### 3. Generación del calendario (Jornada, Partido) — algoritmo round-robin

Por cada `Competicion` (una combinación `Temporada` x `Categoria`), se genera el calendario con el **método del círculo** (*circle method*) de emparejamiento round-robin sobre los `Equipo` de esa competición:

- Con un número par de equipos, cada ronda empareja todos los equipos entre sí sin repetir ninguno; con impar, se añade un equipo "fantasma" que le da un *bye* (jornada de descanso) a quien le toque cada ronda.
- Una ronda completa (todos contra todos una vez) = liga a una vuelta; repetir el proceso invirtiendo local/visitante = liga a dos vueltas (por defecto, según el reglamento resumido en `reglamento/resumen-reglas-relevantes.md`).
- Cada ronda generada se persiste como una `Jornada` (`Numero` correlativo, `CuentaParaClasificacion = true`), y cada emparejamiento de la ronda como un `Partido`.
- Esta construcción garantiza **por diseño** que ningún equipo se repite en la misma jornada (criterio de aceptación de `spec.md`) — no hace falta ninguna validación a posteriori para esa invariante.
- Es lógica de dominio pura (sin I/O): vive en una clase propia del proyecto `Seed` (o se extrae a un método estático testeable), con **test unitario** en `tests/BasketBaseTracker.Tests/Unit/` que verifica la propiedad de no-repetición para N equipos, par e impar, siguiendo `architecture.md` punto 15.

### 4. Parámetros del comando

Argumentos de línea de comandos simples, parseados a mano (sin `System.CommandLine` ni librería adicional — el proyecto no la tiene y son pocos flags):

- `--temporadas <n>`: número de temporadas históricas a generar además de la actual (por defecto un valor razonable, p. ej. 3).
- `--clubes <n>`: número de clubes a generar (por defecto dentro del rango de `functional.md`).
- `--reset`: si se indica, borra primero todos los datos deportivos existentes (todas las entidades listadas arriba) antes de regenerar; sin el flag, el comando falla si ya hay datos (evita duplicar o mezclar sin querer un dataset de demo sobre datos reales).

### 5. Temporada actual a medio disputar

La última temporada generada queda en estado `EnCurso`. Su calendario se genera igual que las demás (punto 3), pero al asignar `FechaHora` a cada `Partido` se reparten las jornadas en fines de semana consecutivos desde el inicio de temporada; las jornadas cuya fecha ya haya pasado respecto a la fecha de ejecución del comando quedan en `Estado = Jugado` (con resultado y `PartidoParcial`, ver punto 6), y las de fecha futura quedan en `Estado = Programado` (sin resultado). Las temporadas anteriores (`Finalizada`/`Archivada`) generan todos sus partidos ya `Jugado`.

### 6. Resultados y parciales

Para cada `Partido` en `Estado = Jugado`: se genera un resultado plausible (rangos de puntuación realistas para baloncesto base) y 4 `PartidoParcial` (uno por periodo) cuya suma coincide exactamente con `PuntosLocal`/`PuntosVisitante` del partido — invariante de coherencia razonable aunque no esté declarada como tal en `data-model.md`, para que las pantallas de resultados no muestren datos contradictorios.

### 7. Conexión a base de datos y credenciales

Reutiliza el login de aplicación de bajo privilegio (`basketbasetracker_app`: lectura/escritura, sin DDL — `architecture.md` punto 6) en vez del login `sqladmin`, porque el seeding solo hace inserts/deletes de datos, nunca DDL. En local, cadena de conexión vía `dotnet user-secrets` (mismo mecanismo que `Seed:AdminEmail`/`Seed:AdminPassword` de `IdentitySeeder`, ver skill `dotnet`); en el workflow de GitHub Actions, el secreto ya existente en Key Vault para ese login (el mismo que usa `Web` en producción).

### 8. Workflow de GitHub Actions

Nuevo fichero `.github/workflows/seed-demo.yml`, disparado solo por `workflow_dispatch` (nunca por push/PR), con inputs: `temporadas`, `clubes`, `reset` (booleano) y `confirmar` (string, obligatorio literal `"BORRAR"` cuando `reset = true` — salvaguarda adicional más allá del propio disparo manual, dado que `architecture.md` punto 13 confirma que no existe entorno de `staging`: este workflow se ejecuta contra la única base de datos en la nube, la de producción). Pasos, calcados del job `deploy` de `deploy.yml`:

1. Checkout + `setup-dotnet` (mismo `global.json`).
2. `azure/login` por OIDC (mismos `vars.AZURE_*` ya configurados).
3. Abrir regla de firewall temporal en Azure SQL para la IP del runner (`az sql server firewall-rule create`, nombre único por `github.run_id`, igual que `deploy.yml`).
4. Leer la cadena de conexión del login `basketbasetracker_app` desde Key Vault.
5. `dotnet restore` + `dotnet run --project src/BasketBaseTracker.Seed -- --temporadas <n> --clubes <n> [--reset]`, con la cadena de conexión por variable de entorno.
6. Cerrar la regla de firewall temporal (`if: always()`, igual que `deploy.yml`).

Sin ningún recurso de Azure nuevo (nada de Azure Functions ni Storage Account) — coherente con la aclaración ya cerrada en `spec.md`.

**Alternativa considerada y descartada por ahora**: un [GitHub Environment](https://docs.github.com/actions/deployment/targeting-different-environments/using-environments-for-deployment) con *required reviewers* apuntando el job a ese entorno añadiría una aprobación manual nativa de GitHub (el job queda pausado hasta aprobarlo desde la UI), sin script propio que mantener. Se deja fuera de este incremento porque es una configuración manual en Settings → Environments, fuera del control de versiones — se puede añadir en cualquier momento sin tocar este workflow ni requiere coordinarse con el resto de `tasks.md`.

### 9. Tests

- Unitario (`tests/BasketBaseTracker.Tests/Unit/`): algoritmo round-robin (punto 3) — no-repetición de equipo por jornada, número correcto de rondas para N equipos par/impar, y el *bye* cuando N es impar.
- Integración (`tests/BasketBaseTracker.Tests/Integration/`): ejecutar el comando de seeding contra la base de datos de test real (mismo patrón `Aspire.Hosting.Testing` que el resto de la suite, `architecture.md` punto 15) y verificar que produce el volumen y los estados esperados (al menos una temporada archivada completa, una `EnCurso` con partidos `Jugado` y `Programado`).

## Pendiente de definir

- Rangos por defecto exactos de `--temporadas`/`--clubes` cuando no se especifican — se fijan en `tasks.md` al implementar, dentro de los rangos ya acotados por `functional.md`.
