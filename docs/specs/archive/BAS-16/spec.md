---
codigo: BAS-16
titulo: Importación/exportación completa de datos
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-19
dependeDe:
  - "[[archive/BAS-15/spec|BAS-15]]"
tags:
  - backend
  - frontend
---

# BAS-16: Importación/exportación completa de datos

## Descripción

`functional.md` pide un mecanismo de importación/exportación de datos, y `architecture.md` (punto 10) ya fijó la decisión completa: un único endpoint de exportación e importación **completa** de todos los datos del sistema (no parcial por entidad), en formato JSON, pensado como backup y vía de migración — no como herramienta de gestión del día a día. `screens.md` ya reserva la pantalla `Importación/exportación` como *placeholder* del área Admin. Este incremento la construye.

## Alcance

- **Renombra el rol único `Administrador` a `GestorCompeticion`** — pasa a proteger exactamente lo mismo que protegía `Administrador` hoy (todas las páginas actuales del área Admin: Temporadas, Categorías, Clubes, Sedes, Competiciones, Equipos, FichasJugador, Jornadas, Partidos, Resultados, Parciales, Clasificación).
- **Crea un rol nuevo `Administrador`** — el "administrador del sistema" al que `functional.md`/`screens.md` restringen import/export. Protege únicamente la pantalla nueva de `Importación/exportación`.
- La cuenta ya sembrada recibe ambos roles (`GestorCompeticion` + `Administrador`) — sigue siendo la misma persona operando ambos frentes; no hace falta una segunda cuenta.
- **Exportación**: un botón/acción en el área Admin que descarga un fichero JSON con todas las entidades catálogo y transaccionales de `data-model.md` (Temporada, Categoria, Club, Sede, Competicion, Equipo, FichaJugador, Jornada, Partido, PartidoParcial, PenalizacionClasificacion). La clasificación queda fuera (es una consulta calculada, no una tabla — `architecture.md` punto 14).
- **Versionado de esquema**: el JSON incluye un campo de versión de esquema en la raíz.
- **Importación en modo reemplazo completo**: borra todos los datos actuales de esas entidades y carga los del fichero, dentro de una única transacción (todo o nada).
- **Backup automático antes de importar**: se reutiliza internamente la propia exportación para conservar el estado justo antes del reemplazo, como vía de deshacer una importación equivocada.
- **Confirmación explícita** del administrador antes de ejecutar una importación (no un único clic).
- **Validación antes de aplicar**: el fichero se parsea y se valida (versión de esquema soportada, integridad referencial entre entidades del fichero, invariantes de negocio como "un equipo no puede aparecer dos veces en la misma jornada") antes de tocar la base de datos. Si algo falla, no se aplica nada.
- Pantalla `Importación/exportación` del área Admin (`screens.md`), sustituyendo el *placeholder*.

## Fuera de alcance

- Importación/exportación parcial por entidad — el propio `architecture.md` ya lo descarta: es una foto completa del sistema, no una herramienta de sincronización.
- Fusión/upsert de datos importados con los ya existentes — el modo es reemplazo completo, sin resolver fusiones ni relaciones huérfanas.
- Datos de `AspNetUsers`/`AspNetRoles` (cuentas de administrador) — el fichero cubre los datos de la competición (`data-model.md`), no las cuentas de acceso al sistema.
- Pantalla de gestión de usuarios/roles (`screens.md` la lista como *placeholder* aparte) — dar de alta una segunda cuenta con uno u otro rol sigue sin tener interfaz propia; se haría, si hiciera falta, igual que hoy (directamente en base de datos o ampliando el *seed*).
- Cualquier otro cambio a la matriz de permisos más allá de la división `GestorCompeticion`/`Administrador` — MF-7 (roles más granulares por área: gestor de equipos, de resultados...) sigue aplazado.

## Criterios de aceptación

- [x] Un administrador puede descargar un fichero JSON con todas las entidades en alcance, incluyendo un campo de versión de esquema en la raíz.
- [x] Importar ese mismo fichero (sin cambios) reemplaza los datos actuales por una copia idéntica — la exportación siguiente es equivalente a la original.
- [x] Antes de aplicar una importación se exige confirmación explícita (no un único clic) y se genera un backup automático del estado previo, descargable o recuperable.
- [x] Un fichero con una versión de esquema no soportada se rechaza sin aplicar ningún cambio.
- [x] Un fichero con una violación de integridad referencial o de una invariante de negocio (p. ej. un equipo repetido en la misma jornada) se rechaza sin aplicar ningún cambio.
- [x] Una importación válida se aplica de forma atómica: si falla a mitad, no queda el sistema en un estado parcial.
- [x] El endpoint de importación/exportación solo es accesible para el rol `Administrador` (sistema) autenticado; el resto de páginas del área Admin pasan a exigir `GestorCompeticion` en vez de `Administrador`, sin perder acceso ningún administrador ya existente.
- [x] El backup automático generado antes de una importación queda guardado en Azure Blob Storage, recuperable después de aplicar el reemplazo.

## Aclaraciones

- **Migración de roles, sin tocar producción a mano**: el renombrado `Administrador` → `GestorCompeticion` y la creación del rol nuevo `Administrador` se aplican mediante una extensión idempotente del *seed* que ya existe (`IdentitySeeder`, corre en cada arranque de contenedor — `architecture.md` punto 7). Al desplegar este incremento, la próxima revisión de producción migra los roles sola al arrancar, con el mismo pipeline de despliegue ya usado en incrementos anteriores — sin script manual contra la base de datos de producción.
- **La cuenta ya sembrada recibe ambos roles** (`GestorCompeticion` + `Administrador`) durante esa migración — sigue siendo la misma persona operando ambos frentes.
- **Backup de importación en Azure Blob Storage**: a diferencia de lo que sugería `architecture.md` (descargar el backup junto con la operación), se decide guardarlo en un contenedor de Blob Storage — más robusto ante la pérdida del fichero descargado por el administrador. Añade un recurso de infraestructura nuevo (`AddAzureStorage`/`AddBlobContainer` en `AppHost.cs`, mismo patrón ya usado para Key Vault) no contemplado explícitamente en `architecture.md`; se documenta aquí como ampliación de esa decisión, no como sustitución.
- **Dos bugs preexistentes encontrados durante la verificación de la división de roles** (ninguno visible antes de BAS-16, porque antes solo había un rol y "autenticado pero sin el rol correcto" nunca podía darse): (1) sin `AccessDeniedPath` configurado en la cookie de Identity, un usuario sin el rol requerido caía en el `AccessDeniedPath` por defecto (`/Account/AccessDenied`, inexistente en esta app) y veía un 404 en vez de un 403 — corregido devolviendo 403 directamente sin redirigir; (2) el panel de administración (`Admin/Index`, destino por defecto tras el login) exigía `GestorCompeticion`, dejando fuera a una cuenta solo-`Administrador` nada más iniciar sesión — nueva política `GestorCompeticionOAdministrador` (con al menos uno de los dos roles) para esa página compartida.

## Referencias

- [[architecture#10. Importación/exportación|architecture.md, punto 10]] — decisión completa ya tomada (formato, modo reemplazo, salvaguardas, versionado, validación, alcance de entidades).
- [[architecture#14. Clasificación como consulta calculada|architecture.md, punto 14]] — por qué la clasificación no entra en el fichero.
- [[architecture#MF-7. Roles administrativos granulares|architecture.md, MF-7]] — por qué la granularidad de roles no va más allá de `GestorCompeticion`/`Administrador` en este incremento.
- [[data-model]] — entidades en alcance del fichero.
- [[screens#Área admin (autenticado)|screens.md]] — pantalla `Importación/exportación`, hoy *placeholder*.
- [[functional#Funcionalidades iniciales|functional.md]] — requisito de importación/exportación de datos.
