# Reporte de Estado — Epsilon Emulator

Fecha: 2026-04-21

## Resumen ejecutivo

El repositorio está en una fase temprana pero con una base arquitectónica sólida y deliberada. Hay una solución .NET 10 real (`EpsilonEmulator.sln`) con ocho proyectos de código fuente bajo `src/`, un proyecto de pruebas sin implementar todavía, documentación de arquitectura ya escrita, y una carpeta de investigación donde ya se analizaron varios emuladores clásicos (Butterfly, Phoenix 3.8/3.11, Arcturus, Holograph, UberEmu2) y esquemas de base de datos de referencia (HoloCMS, HoloDB, DuckDB). La intención declarada en el `README` es clara: sacar a Habbo de la era Flash/Shockwave con un monolito modular moderno, .NET 10 + PostgreSQL + Redis, y tratar los emuladores viejos como material de referencia y no como código base.

Antes de la limpieza había varias carpetas vacías que parecían restos de un layout anterior o esbozo de otro proyecto. Ya fueron eliminadas (detalle abajo). La estructura actual queda ordenada y coherente con el `.sln`.

## Estructura actual (tras la limpieza)

La raíz del repo queda con estas áreas, cada una con un propósito bien definido:

- `src/` — código C# de producción. Ocho proyectos .NET 10, uno por dominio: `Epsilon.Gateway` (entrada de red), `Epsilon.Protocol` (registro de paquetes y comandos con manifiestos JSON externos), `Epsilon.Auth` (autenticación y ticketing), `Epsilon.CoreGame` (82 archivos — es el dominio más desarrollado: sesión del hotel, perfil, badges, wired, salas, soporte, wallet, etc.), `Epsilon.Rooms` (definiciones de habitación, layout, wired), `Epsilon.Content` (catálogo, items, paquetes públicos), `Epsilon.Persistence` (repositorios en memoria por ahora, 35 archivos), `Epsilon.AdminApi` (API de administración mínima).
- `tests/` — scaffolding: `Epsilon.Protocol.Tests/` (solo `.csproj`, sin tests) y `fixtures/` (solo README).
- `docs/` — documentación viva: `architecture/`, `compatibility/`, `decisions/` (dos ADRs ya escritos), `reference-sources/`, `roadmap/` (phase-01, local bootstrap, first hotel read slice).
- `research/` — análisis de emuladores y bases de datos legacy, ya materializados en Markdown.
- `references/` — destino del material crudo del desarrollador (`references/raw/` está en `.gitignore`), no se versiona.
- `catalog/` — esquemas JSON (source catalog, legacy package manifest, visual asset manifest, public room asset manifest).
- `sql/` — migraciones numeradas (`001_hotel_read_model.sql`, `002_hotel_read_seed.sql`).
- `tools/` — `importers/` (scripts Python para convertir material legacy a formatos canónicos) y `source-catalog/`.
- Raíz — `EpsilonEmulator.sln`, `Directory.Build.props` (fija `net10.0`, nullable, implicit usings), `compose.yaml` (Postgres 16 + Redis 7), `.env.example`, `.editorconfig`, `LICENSE`, `README.md`, `.gitignore`.

## Misceláneas detectadas y acciones tomadas

Durante la revisión aparecieron carpetas que no pertenecían al proyecto actual o que eran residuos de iteraciones anteriores. Ninguna estaba referenciada desde el `.sln`, archivos `.csproj`, ni documentación. Todas se eliminaron:

- `services/` con ocho subcarpetas vacías (`admin-api`, `auth`, `content`, `core-game`, `gateway`, `persistence`, `protocol`, `rooms`). Era un esqueleto de un layout de microservicios que quedó abandonado cuando el proyecto decidió seguir un monolito modular con los proyectos `src/Epsilon.*`. Duplicaba nombres del `src/` sin contenido real. **Eliminada.**
- `references/templates/` — carpeta vacía sin propósito documentado. **Eliminada.**
- `tests/protocol/` y `tests/simulation/` — carpetas vacías, reemplazadas conceptualmente por `tests/Epsilon.Protocol.Tests/`. **Eliminadas.**
- `tools/importers/__pycache__/` — caché de Python que no debía estar en el repo. **Eliminada**, y `.gitignore` actualizado para que no vuelva a entrar (ahora ignora `__pycache__/`, `*.pyc`, `*.pyo`, `.venv/`, `venv/`).

También se agregó un `references/README.md` que explica que la carpeta está vacía a propósito (el material crudo va en `references/raw/`, que es ignorado por git), así no parece un directorio huérfano.

## Estado por módulo (dónde hay carne y dónde no)

Mirando la densidad de código se ve claramente la prioridad que tuvo cada módulo hasta ahora:

- `Epsilon.CoreGame` (82 `.cs`) y `Epsilon.Persistence` (35 `.cs`) son los más avanzados. Ya existen las interfaces y modelos para snapshot del hotel, housekeeping, perfil, wallet, mensajería, achievements, badges, wired, support, y los repositorios in-memory para alimentarlos. No hay todavía implementación de base de datos real — los repositorios son `InMemory*`.
- `Epsilon.Protocol` (12 `.cs`) ya tiene cargadores de manifiestos externos (`PacketManifestLoader`, `ProtocolCommandManifestLoader`) y manifiestos iniciales para `release63` (`command-manifests/release63.commands.json`, `packet-manifests/release63.json`). Alineado con la ADR "no-hardcoded-protocol-and-content-rules".
- `Epsilon.Gateway` (4 `.cs`) y `Epsilon.AdminApi` (4 `.cs`) están al nivel de bootstrap: `Program.cs`, opciones de runtime, validaciones de arranque. Todavía no manejan tráfico de cliente ni endpoints administrativos reales.
- `Epsilon.Auth` (13 `.cs`) tiene un autenticador de desarrollo, generador de tickets base64, store de sesión en memoria y abstracción de reloj. Listo para un autenticador real cuando toque.
- `Epsilon.Content` (13 `.cs`) y `Epsilon.Rooms` (12 `.cs`) tienen los contratos (definiciones, interfaces de repositorio) pero todavía sin lógica pesada.

## Hallazgos importantes para los próximos pasos

Hay puntos que conviene atacar pronto, y están listados en orden de impacto:

El primero: `tests/Epsilon.Protocol.Tests/` existe en la solución pero su `.csproj` no referencia ningún framework de pruebas (no hay `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, etc.) y no tiene un solo archivo `.cs`. Si se hace `dotnet test` hoy no corre nada útil y puede incluso fallar al restaurar. Conviene completar este proyecto o removerlo del `.sln` hasta que se vaya a usar.

Segundo: la capa de persistencia está a mitad de camino hacia Postgres. 19 interfaces `I*Repository` tienen implementación `InMemory*`, pero solo 7 tienen contraparte `Postgres*` (Character, Pet, Item, Room, RoomLayout, RoomItem, Subscription). Los otros 12 lanzan `NotSupportedException` explícita cuando `Infrastructure.Provider="Postgres"`. Hay migraciones SQL (`sql/001_*`, `sql/002_*`) y `compose.yaml` levanta Postgres. El siguiente hito natural del roadmap (`docs/roadmap/first-hotel-read-slice.md`) parece apuntar exactamente a cerrar ese hueco.

Tercero: `Epsilon.Gateway` todavía no sabe hablar el protocolo clásico. Tiene `Program.cs` y validaciones de arranque, pero falta el listener TCP/WebSocket y el despachador que use `PacketRegistry` y `ProtocolCommandRegistry`. Es el paso para poder conectar un cliente real.

Cuarto: los análisis en `research/project-analyses/` son buenos insumos pero ninguno se tradujo aún a tareas concretas en `docs/roadmap/`. Convertir esos hallazgos en tickets o en secciones del roadmap evitaría que el conocimiento se quede aislado.

## Coherencia con la visión declarada

La estructura actual respeta las reglas que el `README` y los ADRs establecen: los emuladores viejos viven como análisis en `research/` y nunca como dependencias binarias de `src/`, el protocolo es data-driven vía manifiestos, las reglas de catálogo están documentadas en `docs/reference-sources/cataloging-rules.md`, y el boundary `research` → `importers` → `catalog`/`sql` → `src` está explícito tanto en el README de `research/` como en el de `tools/importers/`. En corto: la intención arquitectónica del proyecto está bien cableada en la estructura de carpetas.

## Próximos pasos sugeridos

Cerrar el proyecto de pruebas para que la solución compile y corra tests de verdad. Implementar al menos un repositorio Postgres real (probablemente `PostgresHotelStore` o equivalente) basándose en las migraciones existentes. Cablear el listener del `Epsilon.Gateway` contra el `PacketRegistry` de `release63`. Y traducir los análisis de `research/` a tareas de roadmap concretas para que el trabajo previo de catalogación rinda frutos.
