-- Library: listas, "me gusta" y artistas seguidos (decisiones 017 y 018 de vibra-docs).

CREATE SCHEMA IF NOT EXISTS library;

CREATE FUNCTION library.set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW."UpdatedAt" := now();
    RETURN NEW;
END;
$$;

CREATE TABLE library."Playlist" (
    "PlaylistId"   bigint      GENERATED ALWAYS AS IDENTITY,
    "PlaylistCode" uuid        NOT NULL DEFAULT gen_random_uuid(),
    "UserId"       bigint      NOT NULL,
    "Name"         text        NOT NULL,
    "CreatedAt"    timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_playlist PRIMARY KEY ("PlaylistId"),
    CONSTRAINT ux_playlist_playlist_code UNIQUE ("PlaylistCode"),
    CONSTRAINT fk_playlist_user_id FOREIGN KEY ("UserId")
        REFERENCES identity."User" ("UserId") ON DELETE RESTRICT
);

CREATE INDEX ix_playlist_user_id ON library."Playlist" ("UserId");

CREATE TRIGGER trg_playlist_updated_at BEFORE UPDATE ON library."Playlist"
    FOR EACH ROW EXECUTE FUNCTION library.set_updated_at();

-- La misma pista puede repetirse en una lista.
CREATE TABLE library."PlaylistTrack" (
    "PlaylistTrackId" bigint      GENERATED ALWAYS AS IDENTITY,
    "PlaylistId"      bigint      NOT NULL,
    "TrackId"         bigint      NOT NULL,
    "Position"        integer     NOT NULL,
    "CreatedAt"       timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"       timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_playlist_track PRIMARY KEY ("PlaylistTrackId"),
    CONSTRAINT fk_playlist_track_playlist_id FOREIGN KEY ("PlaylistId")
        REFERENCES library."Playlist" ("PlaylistId") ON DELETE CASCADE,
    CONSTRAINT fk_playlist_track_track_id FOREIGN KEY ("TrackId")
        REFERENCES catalog."Track" ("TrackId") ON DELETE RESTRICT
);

CREATE INDEX ix_playlist_track_playlist_id_position ON library."PlaylistTrack" ("PlaylistId", "Position");
CREATE INDEX ix_playlist_track_track_id ON library."PlaylistTrack" ("TrackId");

CREATE TRIGGER trg_playlist_track_updated_at BEFORE UPDATE ON library."PlaylistTrack"
    FOR EACH ROW EXECUTE FUNCTION library.set_updated_at();

CREATE TABLE library."TrackLike" (
    "TrackLikeId" bigint      GENERATED ALWAYS AS IDENTITY,
    "UserId"      bigint      NOT NULL,
    "TrackId"     bigint      NOT NULL,
    "CreatedAt"   timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_track_like PRIMARY KEY ("TrackLikeId"),
    CONSTRAINT ux_track_like_user_id_track_id UNIQUE ("UserId", "TrackId"),
    CONSTRAINT fk_track_like_user_id FOREIGN KEY ("UserId")
        REFERENCES identity."User" ("UserId") ON DELETE RESTRICT,
    CONSTRAINT fk_track_like_track_id FOREIGN KEY ("TrackId")
        REFERENCES catalog."Track" ("TrackId") ON DELETE RESTRICT
);

CREATE INDEX ix_track_like_track_id ON library."TrackLike" ("TrackId");

CREATE TRIGGER trg_track_like_updated_at BEFORE UPDATE ON library."TrackLike"
    FOR EACH ROW EXECUTE FUNCTION library.set_updated_at();

CREATE TABLE library."ArtistFollow" (
    "ArtistFollowId" bigint      GENERATED ALWAYS AS IDENTITY,
    "UserId"         bigint      NOT NULL,
    "ArtistId"       bigint      NOT NULL,
    "CreatedAt"      timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_artist_follow PRIMARY KEY ("ArtistFollowId"),
    CONSTRAINT ux_artist_follow_user_id_artist_id UNIQUE ("UserId", "ArtistId"),
    CONSTRAINT fk_artist_follow_user_id FOREIGN KEY ("UserId")
        REFERENCES identity."User" ("UserId") ON DELETE RESTRICT,
    CONSTRAINT fk_artist_follow_artist_id FOREIGN KEY ("ArtistId")
        REFERENCES catalog."Artist" ("ArtistId") ON DELETE RESTRICT
);

CREATE INDEX ix_artist_follow_artist_id ON library."ArtistFollow" ("ArtistId");

CREATE TRIGGER trg_artist_follow_updated_at BEFORE UPDATE ON library."ArtistFollow"
    FOR EACH ROW EXECUTE FUNCTION library.set_updated_at();
