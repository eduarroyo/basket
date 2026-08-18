---
codigo: BAS-4
estado: Planificado
tags:
  - tasks
---

# BAS-4: Tareas

- [ ] **Bloqueante**: registrar `basketbase.es` en Hostinger (techo de gasto: 15€/año).
- [ ] Crear el sitio `basketbase.es` en Cloudflare (plan gratuito) y anotar los *nameservers* asignados.
- [ ] Delegar los *nameservers* del dominio a Cloudflare desde el panel de Hostinger; esperar propagación.
- [ ] Verificar manualmente: `dig NS basketbase.es` (o equivalente) devuelve los *nameservers* de Cloudflare, y el dominio aparece como "activo" en el panel de Cloudflare.
- [ ] Añadir el registro DNS del apex en Cloudflare **en modo "solo DNS" (sin proxy)**, apuntando al dominio por defecto de Container Apps.
- [ ] Añadir `basketbase.es` como dominio personalizado en la Container App `web` (`az containerapp hostname add` o equivalente), con el TXT de verificación que pida Azure creado en Cloudflare.
- [ ] Solicitar el certificado gestionado de Azure Container Apps para `basketbase.es`.
- [ ] Verificar manualmente: `https://basketbase.es` sirve la aplicación con certificado válido (sin proxy de Cloudflare todavía).
- [ ] Activar el proxy de Cloudflare (nube naranja) sobre el registro del apex; configurar el modo SSL `Full (strict)`.
- [ ] Verificar manualmente: `https://basketbase.es` sigue sirviendo la app con certificado válido, ahora a través de Cloudflare (cabecera `cf-ray` presente en la respuesta).
- [ ] Cloudflare Redirect Rule: `www.basketbase.es/*` → 301 → `https://basketbase.es/$1`.
- [ ] Verificar manualmente: `https://www.basketbase.es` redirige con 301 a `https://basketbase.es`.
- [ ] Cache Rule de Cloudflare ("Cache Everything" respetando `Cache-Control` del origen) sobre las rutas públicas, excluyendo explícitamente `/Admin/*` (`architecture.md` punto 5).
- [ ] Verificar manualmente: dos peticiones seguidas a una página pública dentro del TTL muestran `cf-cache-status: HIT` en la segunda; una petición a `/Admin/*` muestra `cf-cache-status: DYNAMIC`/`BYPASS` (nunca `HIT`).
- [ ] Activar el WAF gestionado del plan gratuito de Cloudflare y configurar el rate limiting de borde (`architecture.md` punto 6).
- [ ] Añadir a `Web` (`Program.cs`) el middleware que redirige (301) a `https://basketbase.es{PathAndQuery}` cualquier petición cuyo `Host` no sea `basketbase.es`, excepto en `Development`.
- [ ] Test de integración: una petición con cabecera `Host` distinta de `basketbase.es` recibe 301 con `Location: https://basketbase.es...`; una petición con `Host: basketbase.es` no se redirige.
- [ ] Verificar manualmente: la URL antigua (`https://web.<entorno>.azurecontainerapps.io`) redirige a `https://basketbase.es`.
- [ ] Revisar la Definición de Hecho (`workflow.md`) antes de abrir el PR a `develop`.
