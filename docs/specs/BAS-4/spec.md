---
codigo: BAS-4
titulo: Dominio propio y Cloudflare (CDN, WAF, rate limiting)
estado: En aclaración
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

- Registro del dominio `basketbase.es` (techo de gasto: 15€/año), en el registrador que se decida en `## Aclaraciones`.
- Delegación de los *nameservers* del dominio a Cloudflare (plan gratuito).
- Dominio personalizado en Azure Container Apps (`az containerapp hostname add`/equivalente en Aspire) con certificado TLS gestionado, para que `basketbase.es` (y/o `www.basketbase.es`, ver aclaraciones) sirvan la app directamente, no solo la URL por defecto de `azurecontainerapps.io`.
- Cache Rule de Cloudflare ("Cache Everything" respetando `Cache-Control` del origen) sobre las rutas públicas, excluyendo explícitamente `/Admin/*` — arquitectura ya decidida en `architecture.md` punto 5.
- WAF básico + rate limiting de borde de Cloudflare (plan gratuito) — arquitectura ya decidida en `architecture.md` punto 6.
- Verificación de que la URL por defecto de Container Apps sigue funcionando o se redirige correctamente (decisión pendiente, ver aclaraciones).

## Fuera de alcance

- Cualquier cambio a la lógica de caché de origen (Output Caching de ASP.NET Core) — ya implementado, si lo estuviera, en el incremento que añada las primeras páginas públicas cacheables; este incremento solo añade la capa de Cloudflare delante.
- Purga activa de caché (Cloudflare API por tags) — `architecture.md` MF-1, mejora futura, no aquí.
- Email/DNS adicional sobre el dominio (buzones de correo, subdominios de terceros) — solo el registro necesario para servir la web.
- Monitorización activa de uptime vía Cloudflare — ya cubierto por las alertas de Azure Monitor de `BAS-3`.

## Criterios de aceptación

- [ ] El dominio `basketbase.es` está registrado y sus *nameservers* apuntan a Cloudflare.
- [ ] La aplicación es accesible por HTTPS en `basketbase.es` (y/o `www.basketbase.es`, según se resuelva en aclaraciones), con certificado válido de extremo a extremo (Cloudflare edge → origen).
- [ ] Las páginas públicas se sirven con cabeceras de caché de Cloudflare (`cf-cache-status: HIT` en peticiones repetidas dentro del TTL); `/Admin/*` nunca se cachea.
- [ ] El WAF de Cloudflare está activo (reglas gestionadas del plan gratuito) y el rate limiting de borde configurado según `architecture.md` punto 6.
- [ ] Un intento de acceso directo a la URL de `azurecontainerapps.io` sigue funcionando o redirige a `basketbase.es`, según lo que se decida en aclaraciones — no debe quedar en un estado indefinido.

## Aclaraciones

- **¿Dónde se registra el dominio?** → Pendiente de confirmar: `basketbase.es` está disponible en Hostinger (comprobado por el usuario). Cloudflare Registrar (precio de coste, sin margen) es la alternativa habitual cuando ya se va a usar Cloudflare para DNS, pero no admite altas nuevas de dominios `.es` (solo transferencias de dominios ya registrados en otro sitio) — así que el alta inicial tendría que ser en Hostinger (u otro registrador) de todos modos, delegando después los *nameservers* a Cloudflare. **Pendiente de decisión final del usuario.**
- **¿Apex (`basketbase.es`) o `www.basketbase.es` como dominio canónico?** → Pendiente. Afecta a qué registro DNS es el principal y hacia dónde redirige el otro (patrón habitual: uno de los dos redirige con 301 al canónico).
- **¿Qué pasa con la URL de `azurecontainerapps.io` una vez el dominio propio esté activo?** → Pendiente. Opciones: (a) dejarla accesible en paralelo (más simple, pero dos URLs indexables para el mismo contenido — posible impacto SEO/duplicado); (b) redirigir a `basketbase.es` a nivel de aplicación. `architecture.md` no lo cubre todavía.
- **Modo SSL de Cloudflare (Flexible / Full / Full strict)** → Sin resolver todavía, se cierra en `plan.md`: `Full (strict)` es lo recomendable (valida el certificado del origen, no solo cifra el tramo Cloudflare→usuario) — Azure Container Apps ya da certificados gestionados gratuitos para dominios personalizados, así que no debería haber fricción, pero falta confirmarlo en la práctica.

## Referencias

- [[architecture]] — puntos 5 (caché y rendimiento) y 6 (seguridad).
- [[archive/BAS-3/spec|BAS-3]] — incremento del que depende (deja la app accesible en Azure, condición para poder configurar el dominio delante).
