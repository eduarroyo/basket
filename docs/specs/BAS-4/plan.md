---
codigo: BAS-4
estado: Borrador
tags:
  - plan
---

# BAS-4: Plan técnico

## Entidades del modelo de datos afectadas

Ninguna — incremento de infraestructura pura, sin cambios en `data-model.md`.

## Pantallas afectadas

Ninguna directamente — no hay páginas nuevas ni cambios de UI en `screens.md`. Indirectamente, todas las páginas públicas pasan a servirse también (o exclusivamente, según se resuelva en `spec.md`) a través de `basketbase.es` en vez de la URL por defecto de Container Apps.

## Decisiones técnicas específicas de este incremento

Pendiente de completar una vez cerradas las aclaraciones de `spec.md` (registrador del dominio, apex vs `www`, tratamiento de la URL por defecto, modo SSL de Cloudflare).
