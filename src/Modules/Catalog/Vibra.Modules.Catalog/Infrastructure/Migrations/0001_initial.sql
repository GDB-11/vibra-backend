-- Catalog: artistas, álbumes, pistas y búsqueda (decisiones 017 y 018 de vibra-docs).

CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE SCHEMA IF NOT EXISTS catalog;

CREATE FUNCTION catalog.set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW."UpdatedAt" := now();
    RETURN NEW;
END;
$$;

CREATE TABLE catalog."Artist" (
    "ArtistId"   bigint      GENERATED ALWAYS AS IDENTITY,
    "ArtistCode" uuid        NOT NULL DEFAULT gen_random_uuid(),
    "Name"       text        NOT NULL,
    "CreatedAt"  timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_artist PRIMARY KEY ("ArtistId"),
    CONSTRAINT ux_artist_artist_code UNIQUE ("ArtistCode")
);

CREATE TRIGGER trg_artist_updated_at BEFORE UPDATE ON catalog."Artist"
    FOR EACH ROW EXECUTE FUNCTION catalog.set_updated_at();

CREATE TABLE catalog."Album" (
    "AlbumId"        bigint      GENERATED ALWAYS AS IDENTITY,
    "AlbumCode"      uuid        NOT NULL DEFAULT gen_random_uuid(),
    -- Artista principal; "Various Artists" en las recopilaciones.
    "ArtistId"       bigint      NOT NULL,
    "Title"          text        NOT NULL,
    "ReleaseDate"    date        NOT NULL,
    -- Portadas en R2: covers/{CoverKey}/{width}.jpg
    "CoverKey"       uuid        NULL,
    "PriceCents"     integer     NOT NULL,
    "Currency"       char(3)     NOT NULL,
    "StoreProductId" text        NULL,
    "CreatedAt"      timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_album PRIMARY KEY ("AlbumId"),
    CONSTRAINT ux_album_album_code UNIQUE ("AlbumCode"),
    CONSTRAINT ux_album_store_product_id UNIQUE ("StoreProductId"),
    CONSTRAINT fk_album_artist_id FOREIGN KEY ("ArtistId")
        REFERENCES catalog."Artist" ("ArtistId") ON DELETE RESTRICT,
    CONSTRAINT ck_album_price_cents CHECK ("PriceCents" >= 0)
);

CREATE INDEX ix_album_artist_id ON catalog."Album" ("ArtistId");

CREATE TRIGGER trg_album_updated_at BEFORE UPDATE ON catalog."Album"
    FOR EACH ROW EXECUTE FUNCTION catalog.set_updated_at();

CREATE TABLE catalog."Track" (
    "TrackId"        bigint      GENERATED ALWAYS AS IDENTITY,
    "TrackCode"      uuid        NOT NULL DEFAULT gen_random_uuid(),
    "AlbumId"        bigint      NOT NULL,
    "Title"          text        NOT NULL,
    "DiscNumber"     integer     NOT NULL,
    "TrackNumber"    integer     NOT NULL,
    "DurationMs"     integer     NOT NULL,
    "Status"         text        NOT NULL,
    "PriceCents"     integer     NOT NULL,
    "Currency"       char(3)     NOT NULL,
    "StoreProductId" text        NULL,
    "CreatedAt"      timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_track PRIMARY KEY ("TrackId"),
    CONSTRAINT ux_track_track_code UNIQUE ("TrackCode"),
    CONSTRAINT ux_track_store_product_id UNIQUE ("StoreProductId"),
    -- También sirve como índice de la clave foránea "AlbumId".
    CONSTRAINT ux_track_album_id_disc_number_track_number UNIQUE ("AlbumId", "DiscNumber", "TrackNumber"),
    CONSTRAINT fk_track_album_id FOREIGN KEY ("AlbumId")
        REFERENCES catalog."Album" ("AlbumId") ON DELETE RESTRICT,
    CONSTRAINT ck_track_status CHECK ("Status" IN ('Pending', 'Ready', 'Failed')),
    CONSTRAINT ck_track_disc_number CHECK ("DiscNumber" > 0),
    CONSTRAINT ck_track_track_number CHECK ("TrackNumber" > 0),
    CONSTRAINT ck_track_duration_ms CHECK ("DurationMs" >= 0),
    CONSTRAINT ck_track_price_cents CHECK ("PriceCents" >= 0)
);

CREATE TRIGGER trg_track_updated_at BEFORE UPDATE ON catalog."Track"
    FOR EACH ROW EXECUTE FUNCTION catalog.set_updated_at();

-- Créditos y colaboraciones.
CREATE TABLE catalog."TrackArtist" (
    "TrackArtistId" bigint      GENERATED ALWAYS AS IDENTITY,
    "TrackId"       bigint      NOT NULL,
    "ArtistId"      bigint      NOT NULL,
    "Position"      integer     NOT NULL,
    "CreatedAt"     timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_track_artist PRIMARY KEY ("TrackArtistId"),
    CONSTRAINT ux_track_artist_track_id_artist_id UNIQUE ("TrackId", "ArtistId"),
    CONSTRAINT fk_track_artist_track_id FOREIGN KEY ("TrackId")
        REFERENCES catalog."Track" ("TrackId") ON DELETE CASCADE,
    CONSTRAINT fk_track_artist_artist_id FOREIGN KEY ("ArtistId")
        REFERENCES catalog."Artist" ("ArtistId") ON DELETE RESTRICT
);

CREATE INDEX ix_track_artist_artist_id ON catalog."TrackArtist" ("ArtistId");

CREATE TRIGGER trg_track_artist_updated_at BEFORE UPDATE ON catalog."TrackArtist"
    FOR EACH ROW EXECUTE FUNCTION catalog.set_updated_at();

-- Búsqueda: trigramas (coincidencias parciales) y texto completo.
-- La configuración 'simple' no aplica lematización de ningún idioma; las consultas
-- deben usar la misma expresión para aprovechar el índice.
CREATE INDEX ix_artist_name_trgm ON catalog."Artist" USING gin ("Name" gin_trgm_ops);
CREATE INDEX ix_album_title_trgm ON catalog."Album" USING gin ("Title" gin_trgm_ops);
CREATE INDEX ix_track_title_trgm ON catalog."Track" USING gin ("Title" gin_trgm_ops);

CREATE INDEX ix_artist_name_fts ON catalog."Artist" USING gin (to_tsvector('simple', "Name"));
CREATE INDEX ix_album_title_fts ON catalog."Album" USING gin (to_tsvector('simple', "Title"));
CREATE INDEX ix_track_title_fts ON catalog."Track" USING gin (to_tsvector('simple', "Title"));
