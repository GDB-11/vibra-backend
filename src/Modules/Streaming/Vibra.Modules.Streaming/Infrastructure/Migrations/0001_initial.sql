-- Streaming: archivos de audio (decisiones 017 y 018 de vibra-docs).
-- "IngestJob" llegará en una migración posterior (Sprint 3).

CREATE SCHEMA IF NOT EXISTS streaming;

CREATE FUNCTION streaming.set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW."UpdatedAt" := now();
    RETURN NEW;
END;
$$;

CREATE TABLE streaming."AudioFile" (
    "AudioFileId"  bigint      GENERATED ALWAYS AS IDENTITY,
    "TrackId"      bigint      NOT NULL,
    "Kind"         text        NOT NULL,
    -- Objeto en R2: tracks/{FileKey}.flac o previews/{FileKey}.m4a
    "FileKey"      uuid        NOT NULL,
    "SizeBytes"    bigint      NOT NULL,
    "DurationMs"   integer     NOT NULL,
    "SampleRateHz" integer     NOT NULL,
    "BitDepth"     integer     NULL,
    "Channels"     integer     NOT NULL,
    "CreatedAt"    timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_audio_file PRIMARY KEY ("AudioFileId"),
    CONSTRAINT ux_audio_file_file_key UNIQUE ("FileKey"),
    -- También sirve como índice de la clave foránea "TrackId".
    CONSTRAINT ux_audio_file_track_id_kind UNIQUE ("TrackId", "Kind"),
    CONSTRAINT fk_audio_file_track_id FOREIGN KEY ("TrackId")
        REFERENCES catalog."Track" ("TrackId") ON DELETE RESTRICT,
    CONSTRAINT ck_audio_file_kind CHECK ("Kind" IN ('Full', 'Preview')),
    CONSTRAINT ck_audio_file_size_bytes CHECK ("SizeBytes" >= 0),
    CONSTRAINT ck_audio_file_duration_ms CHECK ("DurationMs" >= 0)
);

CREATE TRIGGER trg_audio_file_updated_at BEFORE UPDATE ON streaming."AudioFile"
    FOR EACH ROW EXECUTE FUNCTION streaming.set_updated_at();
