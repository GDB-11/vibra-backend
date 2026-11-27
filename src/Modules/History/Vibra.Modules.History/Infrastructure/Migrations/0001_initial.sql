-- History: reproducciones (decisiones 017 y 018 de vibra-docs).
-- "Escuchado recientemente" y los contadores se obtienen con consultas.

CREATE SCHEMA IF NOT EXISTS history;

CREATE FUNCTION history.set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW."UpdatedAt" := now();
    RETURN NEW;
END;
$$;

CREATE TABLE history."PlayEvent" (
    "PlayEventId" bigint      GENERATED ALWAYS AS IDENTITY,
    "UserId"      bigint      NOT NULL,
    "TrackId"     bigint      NOT NULL,
    "PlayedAt"    timestamptz NOT NULL,
    "PlayedMs"    integer     NOT NULL,
    "CreatedAt"   timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_play_event PRIMARY KEY ("PlayEventId"),
    CONSTRAINT fk_play_event_user_id FOREIGN KEY ("UserId")
        REFERENCES identity."User" ("UserId") ON DELETE RESTRICT,
    CONSTRAINT fk_play_event_track_id FOREIGN KEY ("TrackId")
        REFERENCES catalog."Track" ("TrackId") ON DELETE RESTRICT,
    CONSTRAINT ck_play_event_played_ms CHECK ("PlayedMs" >= 0)
);

CREATE INDEX ix_play_event_user_id_played_at ON history."PlayEvent" ("UserId", "PlayedAt" DESC);
CREATE INDEX ix_play_event_track_id ON history."PlayEvent" ("TrackId");

CREATE TRIGGER trg_play_event_updated_at BEFORE UPDATE ON history."PlayEvent"
    FOR EACH ROW EXECUTE FUNCTION history.set_updated_at();
