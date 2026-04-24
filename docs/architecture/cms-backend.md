# CMS Backend

La CMS de Epsilon ya no se modela como "unas páginas antes del cliente". Se modela como una capa web propia, conectada al emulador y separada del launcher.

La regla modular completa queda fijada en [modular-cms-launcher-loader.md](/Users/yasminluengo/Documents/Playground/EpsilonEmulator/docs/architecture/modular-cms-launcher-loader.md): CMS, Launcher App y Game Loader son componentes separados.

## Estructura obligatoria

Las referencias revisadas (`RetroCMS`, `DevCMS`, `Hylib`, `ReactCMS-V2`, `Chocolatey`) coinciden en estas zonas:

1. `Auth`
- registro
- login
- logout
- sesión web

2. `Hotel handoff`
- generación o recuperación de sesión de juego
- salto limpio al launcher
- el juego no corre dentro de la CMS

3. `Community`
- noticias
- fotos
- leaderboard
- perfiles

4. `Support`
- ayuda
- tickets
- reglas
- seguridad

5. `Hotel status`
- estado de servicios
- persistencia
- realtime
- señales operativas del hotel

6. `Settings`
- idioma
- cliente activo
- launcher
- políticas base del hotel

## Decisión de Epsilon

La separación correcta queda así:

- `CMS`
  - cuenta
  - sesión web
  - contenido
  - perfil
  - soporte

- `Launcher`
  - loader
  - validación de ticket
  - preparación de runtime

- `Gateway`
  - auth
  - estado del hotel
  - habitaciones
  - realtime
  - collectibles

## Endpoints CMS actuales

- `GET /cms/api/home`
- `GET /cms/api/status`
- `GET /cms/api/hotel`
- `GET /cms/api/me`
- `GET /cms/api/profile/{publicId}`
- `GET /cms/api/news`
- `GET /cms/api/photos`
- `GET /cms/api/leaderboard`
- `GET /cms/api/community`
- `GET /cms/api/support`
- `GET /cms/api/settings`
- `POST /cms/api/auth/register`
- `POST /cms/api/auth/login`
- `POST /cms/api/auth/logout`
- `POST /cms/api/launcher/session`

## Límite actual

La CMS ya está conectada al emulador y al launcher, pero la persistencia sigue en `InMemory`.

Eso significa:
- la arquitectura ya es correcta
- la experiencia web ya es más profesional
- la base de datos real todavía debe pasar a `Postgres`

## Siguiente paso correcto

1. mover `Persistence` a `Postgres`
2. añadir `tickets/support` reales
3. añadir `news` y `photos` persistidas
4. añadir `settings/profile` editables
5. sustituir el runtime temporal del launcher cuando exista el cliente web final
