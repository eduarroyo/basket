---
codigo: BAS-4
estado: Planificado
tags:
  - plan
---

# BAS-4: Plan técnico

## Entidades del modelo de datos afectadas

Ninguna — incremento de infraestructura pura, sin cambios en `data-model.md`.

## Pantallas afectadas

Ninguna directamente — no hay páginas nuevas ni cambios de UI en `screens.md`. Indirectamente, todas las páginas públicas pasan a servirse también (o exclusivamente, según se resuelva en `spec.md`) a través de `basketbase.es` en vez de la URL por defecto de Container Apps.

## Decisiones técnicas específicas de este incremento

### Orden de las piezas (evitar el problema de huevo y gallina del certificado)

1. Registrar `basketbase.es` en Hostinger.
2. Crear el sitio en Cloudflare (plan gratuito), delegar los *nameservers* del dominio en el panel de Hostinger a los que asigne Cloudflare.
3. En Cloudflare, añadir el registro DNS del apex (`CNAME`/*flattening* automático hacia el dominio por defecto de Container Apps, o el registro que indique Azure al añadir el dominio personalizado) **en modo "solo DNS" (nube gris, sin proxy)**.
4. `az containerapp hostname add` (o el equivalente que exponga Aspire/`aspire deploy`) sobre `web`, con el TXT de verificación que pida Azure ya creado en Cloudflare.
5. Solicitar el certificado gestionado de Azure Container Apps para `basketbase.es` — requiere que la validación llegue al origen sin intermediar el proxy de Cloudflare, de ahí el paso 3.
6. Verificar `https://basketbase.es` sirve la app con certificado válido, todavía sin proxy de Cloudflare.
7. Activar el proxy de Cloudflare (nube naranja) sobre el registro del apex. Configurar modo SSL `Full (strict)`.
8. Cloudflare Redirect Rule: `www.basketbase.es/*` → `301` → `https://basketbase.es/$1`.
9. Cache Rule de Cloudflare sobre rutas públicas (excluyendo `/Admin/*`), WAF gestionado y rate limiting de borde (plan gratuito) — `architecture.md` puntos 5 y 6.
10. Middleware en `Web` (`Program.cs`) que redirige con 301 a `https://basketbase.es{PathAndQuery}` cualquier petición cuyo `Host` no sea `basketbase.es` (cubre `*.azurecontainerapps.io` y cualquier otro host inesperado) — excepto en `Development`, donde no aplica.

### Por qué un middleware y no una regla de Cloudflare para la redirección de `azurecontainerapps.io`

Cloudflare solo ve tráfico que pasa por su proxy, es decir, tráfico dirigido a `basketbase.es`. La URL `*.azurecontainerapps.io` resuelve directamente contra Azure, sin pasar por Cloudflare — así que esa redirección tiene que resolverla la propia aplicación, no el borde.

### Sin cambios en Aspire/`AppHost.cs` más allá del dominio personalizado

El dominio personalizado y el certificado gestionado de Container Apps se configuran contra el recurso ya desplegado (similar a los pasos manuales de *bootstrap* de `BAS-3`, tarea 25) — no hay una API de Aspire de alto nivel para "dominio personalizado + certificado gestionado" que valga la pena modelar en `AppHost.cs` para un recurso que se configura una sola vez. Se documenta como pasos manuales en `tasks.md`, igual que el resto de *bootstrap* de infraestructura de `BAS-3`.
