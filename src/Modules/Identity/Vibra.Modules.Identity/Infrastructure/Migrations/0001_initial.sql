-- Identity: usuarios y tokens de renovación (decisiones 017 y 018 de vibra-docs).

CREATE SCHEMA IF NOT EXISTS identity;

-- "UpdatedAt" es solo de auditoría: usa el reloj real, no se usa en reglas de negocio.
CREATE FUNCTION identity.set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW."UpdatedAt" := now();
    RETURN NEW;
END;
$$;

CREATE TABLE identity."User" (
    "UserId"            bigint      GENERATED ALWAYS AS IDENTITY,
    "UserCode"          uuid        NOT NULL DEFAULT gen_random_uuid(),
    "Email"             text        NOT NULL,
    "NormalizedEmail"   text        NOT NULL,
    "PasswordHash"      text        NOT NULL,
    "DisplayName"       text        NOT NULL,
    "SecurityStamp"     text        NOT NULL,
    "ConcurrencyStamp"  text        NOT NULL,
    "LockoutEnd"        timestamptz NULL,
    "AccessFailedCount" integer     NOT NULL,
    "CreatedAt"         timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"         timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_user PRIMARY KEY ("UserId"),
    CONSTRAINT ux_user_user_code UNIQUE ("UserCode"),
    CONSTRAINT ux_user_normalized_email UNIQUE ("NormalizedEmail")
);

CREATE TRIGGER trg_user_updated_at BEFORE UPDATE ON identity."User"
    FOR EACH ROW EXECUTE FUNCTION identity.set_updated_at();

CREATE TABLE identity."RefreshToken" (
    "RefreshTokenId"           bigint      GENERATED ALWAYS AS IDENTITY,
    "UserId"                   bigint      NOT NULL,
    "TokenHash"                text        NOT NULL,
    "TokenFamily"              uuid        NOT NULL,
    "ExpiresAt"                timestamptz NOT NULL,
    "RevokedAt"                timestamptz NULL,
    "ReplacedByRefreshTokenId" bigint      NULL,
    "CreatedAt"                timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"                timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_refresh_token PRIMARY KEY ("RefreshTokenId"),
    CONSTRAINT ux_refresh_token_token_hash UNIQUE ("TokenHash"),
    CONSTRAINT fk_refresh_token_user_id FOREIGN KEY ("UserId")
        REFERENCES identity."User" ("UserId") ON DELETE CASCADE,
    -- NO ACTION (y no RESTRICT): se comprueba al final de la sentencia, para no
    -- bloquear el borrado en cascada de todos los tokens de un usuario.
    CONSTRAINT fk_refresh_token_replaced_by_refresh_token_id FOREIGN KEY ("ReplacedByRefreshTokenId")
        REFERENCES identity."RefreshToken" ("RefreshTokenId") ON DELETE NO ACTION
);

CREATE INDEX ix_refresh_token_user_id ON identity."RefreshToken" ("UserId");
CREATE INDEX ix_refresh_token_replaced_by_refresh_token_id ON identity."RefreshToken" ("ReplacedByRefreshTokenId");

CREATE TRIGGER trg_refresh_token_updated_at BEFORE UPDATE ON identity."RefreshToken"
    FOR EACH ROW EXECUTE FUNCTION identity.set_updated_at();
