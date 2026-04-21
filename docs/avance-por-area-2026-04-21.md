# Limpieza de Código y Porcentajes de Desarrollo por Área

Fecha: 2026-04-21

## Parte 1 — Limpieza de rastros

Se barrió todo `src/` buscando nombres de emuladores clásicos (Butterfly, Phoenix, Arcturus, UberEmu, Holograph, Plus Emulator, Azure, RageZone), URLs de GitHub, copyrights ajenos, comentarios mezclados de otros proyectos, tabs, trailing whitespace y líneas kilométricas. **El código de producción no contiene rastros de esos proyectos**, solo sus análisis viven en `research/` (que es precisamente su propósito). Las dos únicas coincidencias en `src/` son legítimas y deben quedarse:

- `src/Epsilon.Persistence/InMemoryHotelSeedBuilder.cs` usa el literal `"flash_release63_baseline"` para etiquetar el paquete de cliente — coincide con el target de compatibilidad declarado en el `README` (Flash `RELEASE63`) y en `docs/compatibility/target-client.md`.
- `src/Epsilon.Content/InteractionTypeCatalog.cs` contiene la cadena `"habbowheel"` — es el nombre oficial de un tipo de interacción en el protocolo clásico; cambiarlo rompería la compatibilidad con el cliente.

No se detectaron `TODO` / `FIXME` / `HACK` en código de producción. No hay `using` sin usar triviales. El estilo es consistente (espacios, `namespace` file-scoped, nullability habilitada por `Directory.Build.props`).

## Parte 2 — Defectos estructurales encontrados y arreglados

Tres bugs reales en la configuración del build, aislados y corregidos en esta pasada:

1. **`src/Epsilon.Protocol/Epsilon.Protocol.csproj`** — solo incluía `packet-manifests/*.json` como `Content` pero olvidaba `command-manifests/*.json`. Agregado el `<Content Include="command-manifests\*.json">` con `CopyToOutputDirectory="PreserveNewest"`.
2. **`src/Epsilon.Gateway/Epsilon.Gateway.csproj`** — los `ProjectReference` no copian `Content` transitivamente, así que los manifests no llegaban al output del Gateway. Ahora el `.csproj` los enlaza explícitamente con `<None ... Link="packet-manifests\%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest"/>` (y el mismo patrón para `command-manifests`).
3. **`src/Epsilon.Gateway/appsettings.json`** — las rutas `"../Epsilon.Protocol/packet-manifests/release63.json"` apuntaban al árbol de código fuente y fallaban en tiempo de ejecución porque el binario corre desde `bin/Debug/net10.0/`. Cambiadas a `"packet-manifests/release63.json"` y `"command-manifests/release63.commands.json"` (relativas al `AppContext.BaseDirectory`, que coincide con las copias enlazadas del punto 2).

Bonus, aunque el usuario no lo pidió explícitamente:

4. **`tests/Epsilon.Protocol.Tests/Epsilon.Protocol.Tests.csproj`** estaba incompleto: sin `Microsoft.NET.Test.Sdk`, sin xUnit, sin `AssemblyName`/`RootNamespace`, sin un solo test `.cs`. `dotnet test` era ruido. Ahora incluye los paquetes de test (xUnit 2.9.2, Test.Sdk 17.11.1, runner 2.8.2), marca `IsTestProject=true`, enlaza los manifests al output del test, y agregué `PacketManifestLoaderTests.cs` con dos smoke tests: uno que carga el manifest `RELEASE63` del disco y otro que verifica que el loader lanza una excepción clara cuando falta configuración.

## Parte 3 — Inconsistencia no resuelta (requiere tu decisión)

Hay un "choque" suave entre diseño declarado e infraestructura operativa que no toqué porque cualquiera de las dos direcciones es razonable y debes elegir tú:

- `docs/architecture/document-storage-strategy.md` defiende usar MongoDB para agregados document-oriented y `src/Epsilon.Persistence/PersistenceOptions.cs` + `src/Epsilon.AdminApi/InfrastructureOptions.cs` exponen `MongoConnectionString` y `MongoDatabaseName`.
- En cambio `compose.yaml` solo levanta Postgres + Redis, `.env.example` no tiene credenciales Mongo, y no existe ni un solo repositorio `Mongo*`. Todas las branches `"Document"` en el DI lanzan `NotSupportedException`.

Opciones: o se compromete la ADR de Mongo (añadir el servicio al `compose.yaml`, variables al `.env.example`, e implementar los primeros repos `Mongo*`), o se retira la ADR y se consolida Postgres como único store SQL + Redis. Hoy el repositorio sugiere dos caminos a la vez.

## Parte 4 — Porcentaje de desarrollo por área

Los porcentajes estiman qué tanto del alcance *realista* de un emulador Habbo moderno está cubierto en ese módulo. No son "% de tareas completadas" sino cobertura de dominio (modelos, contratos, lógica, impl. de producción, wiring, tests). Son estimaciones con criterio, no una métrica automática.

| Módulo / Área            | Avance | Qué hay                                                                                                       | Qué falta para llegar al 100%                                                                                                                                                         |
|--------------------------|-------:|---------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Epsilon.Protocol`       |  20 %  | Loaders, registros y self-check; manifiestos externos; estructura lista                                       | Mapeo real de RELEASE63 (hoy **8 de ~400** paquetes declarados); serializador binario clásico (VL64, Base64, framing); encoders/decoders por paquete; batería de tests por fixture     |
| `Epsilon.Gateway`        |  10 %  | `Program.cs`, opciones, `StartupValidationExtensions`, health endpoint                                        | Listener TCP clásico (puerto 30000), puente WebSocket/TLS moderno, dispatcher de paquetes contra `PacketRegistry`, sesiones conectadas, emisor de paquetes salientes                   |
| `Epsilon.Auth`           |  35 %  | Contratos + `DevelopmentAuthenticator` + `Base64UrlTicketGenerator` + `InMemorySessionStore` + `SystemClock`  | Autenticador real (Postgres users + hash de password), SSO ticket clásico, session store en Redis, challenge de RC4 clásico, rate-limit                                               |
| `Epsilon.CoreGame`       |  40 %  | 82 archivos: snapshots (hotel, housekeeping, wallet, messenger, support, room-runtime), servicios de lectura  | Motor de ejecución Wired, pathfinding + colisión + heightmap, tick de sala, trading, quests, rentas, economía, moderación en runtime                                                  |
| `Epsilon.Rooms`          |  15 %  | Definiciones de sala, layout, settings, items, posiciones, dos records de Wired                               | Cálculo de heightmap, estado de tile-runtime, lógica de puertas/teletransportes, stacking, rotación, sit/lay/effects                                                                   |
| `Epsilon.Content`        |  25 %  | Definiciones (item, pet, catálogo, texts, navigator rooms, client package, public room assets)                | Importadores ejecutados contra furnidata/external_texts reales, repositorio de paquetes de cliente, registry de interactions completo, versionado por familia                           |
| `Epsilon.Persistence`    |  50 %  | 19 repos `InMemory*` (100 % del catálogo), 7 `Postgres*` implementados, DI con provider-switch, seed dev      | 12 repos Postgres restantes (Wallet, Messenger, Badge, Achievement, ChatCommand, RoleAccess, HotelAdvertisement, SupportCenter, RoomRuntime, NavigatorPublicRoom, PublicRoomAssetPackage, ClientPackage), unit-of-work, store Mongo o descartar Mongo |
| `Epsilon.AdminApi`       |  12 %  | 3 endpoints: `/health`, `/readiness`, `/housekeeping/characters/{id}`                                         | CRUD admin de users/rooms/items/catalog, panel de seed, disparadores de importers, auth admin, telemetría                                                                             |
| `tests/`                 |   5 %  | `Epsilon.Protocol.Tests` con 2 smoke tests creados hoy                                                        | Tests por paquete (encode/decode), tests de persistencia Postgres con Testcontainers, tests de flujos end-to-end, fixtures con provenance                                              |
| `sql/`                   |  15 %  | `001_hotel_read_model.sql`, `002_hotel_read_seed.sql`                                                         | Migraciones para users, rooms, catalog, items instanciados, wired, economía; runner de migraciones; rollback documentado                                                              |
| `tools/importers/`       |  20 %  | 4 scripts Python (legacy package manifest, public room assets, SWF analyzer, visual asset manifest)           | Pipeline ejecutable end-to-end, tests de importers, integración con `catalog/`, soporte para furnidata / external_texts / productdata clásicos                                        |
| `catalog/` (schemas)     |  70 %  | 4 esquemas JSON (source catalog, legacy package, public room assets, visual assets)                          | Esquemas para furnidata, external_texts, client-vars, productdata; ejemplos firmados con provenance                                                                                   |
| `docs/`                  |  60 %  | Arquitectura, principios, compatibilidad, 2 ADRs, roadmap (phase-01, local bootstrap, first hotel read)       | Guías de operación, runbook de despliegue, política de versionado de manifests, guía de contribución, glosario                                                                        |
| `research/`              |  70 %  | 6 análisis de emuladores + 3 análisis de DBs clásicas                                                         | Traducción de esos hallazgos a tickets concretos en `docs/roadmap/`                                                                                                                   |

**Promedio ponderado subjetivo del proyecto**: aproximadamente **25 %–30 %** del camino hacia un emulador jugable end-to-end. El porcentaje parece bajo, pero es honesto: la calidad de lo que hay es alta (arquitectura modular real, DI limpia, manifiestos externos, separación estricta `research` → `importers` → `catalog` → `runtime`). Lo que falta es volumen — sobre todo en protocolo, gateway y wired.

## Parte 5 — Siguientes pasos de mayor impacto

Ordenado por impacto sobre "jugabilidad":

1. **Subir `Epsilon.Protocol` del 20 % al 60 %**: completar el mapeo de paquetes RELEASE63 en `release63.json` (de 8 a los ~400 reales) usando los análisis de `research/project-analyses/phoenix-3-11-0-analysis.md` y `holograph-emulator-r59-analysis.md`, e implementar el serializador binario clásico (VL64, Base64, framing de longitud-prefixed).
2. **Subir `Epsilon.Gateway` del 10 % al 50 %**: listener TCP en 30000 + dispatcher que use el `PacketRegistry` + `ProtocolCommandRegistry` + puente WebSocket para cliente moderno. Aquí el proyecto empieza a aceptar conexiones reales.
3. **Subir `Epsilon.CoreGame` del 40 % al 65 %**: motor de ejecución Wired y tick de sala (pathfinding + colisión + heightmap). Con eso las habitaciones ya "viven".
4. **Subir `Epsilon.Persistence` del 50 % al 75 %**: los 12 repos Postgres restantes, más la decisión Mongo-o-no (ver Parte 3).
5. **Subir `tests/` del 5 % al 40 %**: tests de encode/decode por paquete y un suite de persistencia contra Postgres con Testcontainers.

## Nota sobre verificación de compilación

En este momento la solución ya está alineada con `.NET 10`. El flujo esperado de validación local es: `dotnet restore && dotnet build && dotnet test`.
