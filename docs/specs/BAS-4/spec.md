---
codigo: BAS-4
titulo: Dominio propio y Cloudflare (CDN, WAF, rate limiting)
estado: Planificado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-17
dependeDe:
  - "[[archive/BAS-3/spec|BAS-3]]"
tags:
  - infraestructura
---

# BAS-4: Dominio propio y Cloudflare (CDN, WAF, rate limiting)

## Descripción

BAS-3 dejó la aplicación accesible en la URL por defecto de Azure Container Apps (`web.<entorno>.azurecontainerapps.io`), y explícitamente fuera de su alcance la capa de borde (Cloudflare) hasta que hubiera un dominio propio listo. Este incremento la añade: adquiere el dominio `basketbase.es`, lo delega a Cloudflare, y configura delante de Container Apps las tres piezas ya decididas en `architecture.md` (puntos 5 y 6) pero nunca implementadas — CDN (Cache Rules sobre las páginas públicas), WAF básico + rate limiting de borde, y protección DDoS —, todo en el plan gratuito de Cloudflare salvo el propio dominio.

## Alcance

- Registro del dominio `basketbase.es` en Hostinger (techo de gasto: 15€/año).
- Delegación de los *nameservers* del dominio a Cloudflare (plan gratuito).
- Dominio personalizado en Azure Container Apps (`az containerapp hostname add`/equivalente en Aspire) con certificado TLS gestionado, para que `basketbase.es` (apex, dominio canónico) sirva la app directamente, no solo la URL por defecto de `azurecontainerapps.io`.
- `www.basketbase.es` como redirección 301 al apex (Cloudflare Redirect Rule, sin necesidad de un segundo dominio personalizado en Container Apps).
- Redirección 301 de la URL por defecto de Container Apps (`*.azurecontainerapps.io`) a `basketbase.es` — a nivel de aplicación (middleware de redirección por host), ya que esa URL no pasa por Cloudflare.
- Cache Rule de Cloudflare ("Cache Everything" respetando `Cache-Control` del origen) sobre las rutas públicas, excluyendo explícitamente `/Admin/*` — arquitectura ya decidida en `architecture.md` punto 5.
- WAF básico + rate limiting de borde de Cloudflare (plan gratuito) — arquitectura ya decidida en `architecture.md` punto 6.
- Modo SSL `Full (strict)` en Cloudflare, con el registro DNS del dominio en modo "solo DNS" (sin proxy) mientras Azure valida el dominio y emite el certificado gestionado, activando el proxy (naranja) después.

## Fuera de alcance

- Cualquier cambio a la lógica de caché de origen (Output Caching de ASP.NET Core) — ya implementado, si lo estuviera, en el incremento que añada las primeras páginas públicas cacheables; este incremento solo añade la capa de Cloudflare delante.
- Purga activa de caché (Cloudflare API por tags) — `architecture.md` MF-1, mejora futura, no aquí.
- Email/DNS adicional sobre el dominio (buzones de correo, subdominios de terceros) — solo el registro necesario para servir la web.
- Monitorización activa de uptime vía Cloudflare — ya cubierto por las alertas de Azure Monitor de `BAS-3`.

## Criterios de aceptación

- [ ] El dominio `basketbase.es` está registrado en Hostinger y sus *nameservers* apuntan a Cloudflare.
- [ ] La aplicación es accesible por HTTPS en `basketbase.es` (dominio canónico), con certificado válido de extremo a extremo (Cloudflare edge → origen, modo `Full strict`).
- [ ] `www.basketbase.es` redirige (301) a `basketbase.es`.
- [ ] `*.azurecontainerapps.io` redirige (301) a `basketbase.es`.
- [ ] Las páginas públicas se sirven con cabeceras de caché de Cloudflare (`cf-cache-status: HIT` en peticiones repetidas dentro del TTL); `/Admin/*` nunca se cachea.
- [ ] El WAF de Cloudflare está activo (reglas gestionadas del plan gratuito) y el rate limiting de borde configurado según `architecture.md` punto 6.

## Aclaraciones

- **¿Dónde se registra el dominio?** → Hostinger, donde `basketbase.es` ya está comprobado como disponible. Cloudflare Registrar (precio de coste, sin margen) no admite altas nuevas de dominios `.es` — solo transferencias de dominios ya registrados en otro sitio —, así que el alta inicial en Hostinger es la única opción razonable de partida. Sin conflicto entre registrador y Cloudflare: registrador (facturación/titularidad) y DNS (Cloudflare, vía delegación de *nameservers*) son piezas independientes, un cambio de NS en el panel de Hostinger no requiere transferencia ni aprobación especial.
- **¿Apex (`basketbase.es`) o `www.basketbase.es` como dominio canónico?** → Apex. Más corto, más natural de escribir de memoria. `www` redirige con 301 al apex (Cloudflare Redirect Rule). El apex no puede llevar un CNAME por especificación DNS, pero Cloudflare resuelve esto con *CNAME flattening* automático — sin fricción adicional por elegir el apex como canónico.
- **¿Qué pasa con la URL de `azurecontainerapps.io` una vez el dominio propio esté activo?** → Redirige (301) a `basketbase.es`, para evitar dos URLs indexables sirviendo el mismo contenido. Esa URL no pasa por Cloudflare (es del propio Container App), así que la redirección se implementa en la aplicación (middleware que compara `HttpContext.Request.Host` y redirige si no es `basketbase.es`), no como regla de borde.
- **Modo SSL de Cloudflare (Flexible / Full / Full strict)** → `Full (strict)`: valida el certificado del origen, no solo cifra el tramo Cloudflare→usuario. Azure Container Apps da certificados gestionados gratuitos para dominios personalizados, así que no debería haber fricción — con un matiz de orden de pasos: mientras Azure valida el dominio y emite ese certificado, el registro DNS en Cloudflare debe estar en modo "solo DNS" (nube gris), no en modo proxy (naranja), porque el proxy podría interferir con la validación. Se activa el proxy una vez el certificado está emitido y la app responde correctamente por HTTPS en el dominio propio.

## Referencias

- [[architecture]] — puntos 5 (caché y rendimiento) y 6 (seguridad).
- [[archive/BAS-3/spec|BAS-3]] — incremento del que depende (deja la app accesible en Azure, condición para poder configurar el dominio delante).
