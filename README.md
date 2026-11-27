# Vibra Backend

API ASP.NET Core 11 de Vibra.

## Requisitos

- .NET SDK 11 **con el targeting pack de ASP.NET Core**. Sin él, la compilación falla con `NETSDK1226`. En Arch está en el AUR: `aspnet-targeting-pack-bin` (misma versión que `dotnet-sdk-bin`).
- Podman 5+. Las pruebas de integración usan Testcontainers.
- Python 3 (lo usa `export-openapi.sh`).

## Estructura

| Carpeta | Contenido |
|---|---|
| `src/Vibra.Api` | Host HTTP |
| `src/Vibra.Worker` | Ingesta y transcodificación (todavía sin trabajos) |
| `src/Modules/<Módulo>/` | Un proyecto por módulo, con las carpetas `Domain`, `Application`, `Infrastructure` y `Api` |
| `src/Shared/` | Contrato de errores, `FunctionalController` y `BaseDatabaseService` |
| `tests/` | Pruebas de arquitectura, de la base compartida y de las migraciones |

## Compilar y probar

```bash
dotnet build
dotnet test
dotnet run --project src/Vibra.Api     # http://localhost:5080
./scripts/export-openapi.sh            # genera artifacts/openapi/openapi.json (3.1.x)
```

Testcontainers necesita el socket de Podman. Si no lo detecta:

```bash
systemctl --user enable --now podman.socket
echo "docker.host=unix://$XDG_RUNTIME_DIR/podman/podman.sock" >> ~/.testcontainers.properties
```

En Podman sin root, `exec` puede fallar en contenedores de Testcontainers (`crun: ... oom_score_adj: Permission denied`) y la espera por defecto no termina nunca. Usa una estrategia de espera que no dependa de `exec` (ver `PostgresFixture`).

## Base de datos y caché locales

El archivo de composición (PostgreSQL 18 + Valkey) vive en el repositorio `vibra-db`, carpeta `compose/`.

1. Requisitos: Podman con `podman compose`, .NET SDK 11.
2. Levantar los servicios (ver `vibra-db/README.md` para el `.env`):

   ```bash
   cd ../vibra-db/compose
   cp .env.example .env          # edita las contraseñas
   podman compose --env-file .env up -d
   ```

3. Registrar las cadenas de conexión como secretos de usuario (desde `src/Vibra.Api`; el proyecto ya tiene `UserSecretsId`):

   ```bash
   dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=VibraDB;Username=vibra;Password=<POSTGRES_PASSWORD>"
   dotnet user-secrets set "ConnectionStrings:Valkey" "localhost:6379,password=<VALKEY_PASSWORD>"
   ```

4. Detener: `podman compose --env-file .env down` (añade `-v` para borrar los datos).

Los secretos nunca se versionan; `appsettings*.json` no contiene credenciales.

## Migraciones

La API aplica las migraciones con DbUp al arrancar, en este orden: Identity, Catalog, Streaming, Library, History y Billing. Cada módulo tiene su diario (`public.<módulo>_schemaversions`). Si una falla, la API no arranca.

- Se controla con `Database:MigrateOnStartup` (`true` en `appsettings.json`; si falta, no se migra). DbUp no bloquea entre procesos: solo una instancia debe migrar a la vez.
- El SQL se escribe en `vibra-db`. Para copiarlo aquí:

  ```bash
  ./scripts/sync-db.sh     # VIBRA_DB_DIR cambia el origen (por defecto, ../vibra-db)
  ```

- No edites las copias. Un script ya aplicado no vuelve a ejecutarse: para cambiar el esquema, añade `NNNN_nombre.sql` con el número siguiente.
