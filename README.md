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
| `tests/` | Pruebas de arquitectura, de la base compartida, de las migraciones y de los módulos Identity y Catalog |

## Compilar y probar

```bash
dotnet build
dotnet test
dotnet run --project src/Vibra.Api     # http://localhost:5080
./scripts/export-openapi.sh            # genera artifacts/openapi/openapi.json (3.1.x)
```

Para probar la API, abre <http://localhost:5080/scalar> (documento en `/openapi/v1.json`). Solo existe en el entorno `Development`.

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

4. Registrar la clave que firma los JWT (obligatoria: sin una de al menos 32 caracteres, la API no arranca). Genera una aleatoria:

   ```bash
   dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/Vibra.Api
   ```

   En despliegue, usa la variable de entorno `Jwt__SigningKey`. Cambiarla invalida los JWT emitidos; los tokens de renovación siguen sirviendo. `Jwt:Issuer` y `Jwt:Audience` están en `appsettings.json`. Ejecuta este comando desde la raíz del repositorio.

5. Detener: `podman compose --env-file .env down` (añade `-v` para borrar los datos).

Los secretos nunca se versionan; `appsettings*.json` no contiene credenciales.

## Autenticación (decisión 007)

| Endpoint | Éxito | Errores esperados |
|---|---|---|
| `POST /v1/auth/register` | 201 con tokens | 400, 409 (correo ya registrado) |
| `POST /v1/auth/login` | 200 con tokens | 400, 401 |
| `POST /v1/auth/refresh` | 200 con tokens nuevos | 400, 401 |

Un 400 agrupa en `errors` todos los mensajes por campo (`body` si falla el cuerpo entero). Las reglas de fortaleza de la contraseña las aplica Identity y solo aparecen cuando el resto de los campos es válido.

```json
{
  "type": "urn:vibra:problem:validation",
  "title": "Solicitud no válida",
  "status": 400,
  "detail": "Revisa los campos indicados.",
  "errors": {
    "email": ["El correo no es válido."],
    "displayName": ["El nombre es obligatorio."]
  },
  "traceId": "..."
}
```

- La entrada es estricta: se rechazan los campos desconocidos o repetidos. El correo de una cuenta nueva necesita un dominio con punto.
- El JWT dura 15 minutos y su `sub` es `UserCode`. Se protege un endpoint con `[Authorize]`.
- Cada token de renovación sirve una vez y dura 30 días. Reutilizar uno revoca todos los de ese inicio de sesión. Solo se guarda su hash. El cliente no debe renovar en paralelo: si se pierde la respuesta, hay que iniciar sesión otra vez.
- Contraseña y bloqueo con los valores predeterminados de Identity: 6 caracteres como mínimo (con mayúscula, minúscula, número y símbolo; máximo 128) y bloqueo de 5 minutos tras 5 fallos.
- **Limitación:** no hay límite de solicitudes por cliente. Añádelo antes de exponer la API.

## Catálogo

Todas las lecturas requieren sesión. Solo muestran las pistas `Ready`; un álbum necesita al menos una, y un artista, un álbum visible o un crédito en una pista `Ready`. Lo oculto responde 404.

| Endpoint | `operationId` | Orden |
|---|---|---|
| `GET /v1/artists` | `listArtists` | Nombre |
| `GET /v1/artists/{id}` | `getArtist` | |
| `GET /v1/artists/{id}/albums` | `listArtistAlbums` (artista principal) | Fecha de salida |
| `GET /v1/albums` | `listAlbums` | Título |
| `GET /v1/albums/{id}` | `getAlbum` | |
| `GET /v1/albums/{id}/tracks` | `listAlbumTracks` | Disco y número |
| `GET /v1/tracks` | `listTracks` | Título |
| `GET /v1/tracks/{id}` | `getTrack` | |

- **Paginación:** `limit` (1 a 50; 20 por defecto) y `cursor`. La respuesta es `{ "items": [...], "nextCursor": "..." }`; `nextCursor` es `null` en la última página. El cursor es opaco y solo sirve para la lista que lo devolvió.
- **Consulta estricta:** un valor no válido o un parámetro desconocido o repetido responde 400. Las rutas de detalle no admiten parámetros.
- **ETag:** cada respuesta 200 lleva uno. Con `If-None-Match` coincidente, la API responde 304. Ahorra transferencia, no consultas a la base.
- **Precios:** `priceCents`, `currency` y `storeProductId` (puede ser `null`).

### Portadas

`cover` es una lista de `{ width, url }` con la forma `{Images:BaseUrl}/covers/{CoverKey}/{width}.jpg`. Es `null` si el álbum no tiene portada o no está configurado `Images:BaseUrl`.

| Opción | Valor |
|---|---|
| `Images:BaseUrl` | URL https pública de la CDN de `vibra-images`. Sin valor por defecto |
| `Images:CoverWidths` | `300`, `600` y `800` por defecto |

**Limitación:** los anchos no se guardan por álbum, así que todas las portadas deben existir en los anchos configurados. La ingesta (Sprint 3) tendrá que resolver otros tamaños de origen.

## Migraciones

La API aplica las migraciones con DbUp al arrancar, en este orden: Identity, Catalog, Streaming, Library, History y Billing. Cada módulo tiene su diario (`public.<módulo>_schemaversions`). Si una falla, la API no arranca.

- Se controla con `Database:MigrateOnStartup` (`true` en `appsettings.json`; si falta, no se migra). DbUp no bloquea entre procesos: solo una instancia debe migrar a la vez.
- El SQL se escribe en `vibra-db`. Para copiarlo aquí:

  ```bash
  ./scripts/sync-db.sh     # VIBRA_DB_DIR cambia el origen (por defecto, ../vibra-db)
  ```

- No edites las copias. Un script ya aplicado no vuelve a ejecutarse: para cambiar el esquema, añade `NNNN_nombre.sql` con el número siguiente.
