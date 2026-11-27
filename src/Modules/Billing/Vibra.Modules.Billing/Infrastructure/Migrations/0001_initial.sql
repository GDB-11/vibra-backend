-- Billing: planes, suscripciones, pagos y compras (decisiones 009, 017 y 018 de vibra-docs).

CREATE SCHEMA IF NOT EXISTS billing;

CREATE FUNCTION billing.set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW."UpdatedAt" := now();
    RETURN NEW;
END;
$$;

CREATE TABLE billing."Plan" (
    "PlanId"         bigint      GENERATED ALWAYS AS IDENTITY,
    "PlanCode"       uuid        NOT NULL DEFAULT gen_random_uuid(),
    "Interval"       text        NOT NULL,
    "PriceCents"     integer     NOT NULL,
    "Currency"       char(3)     NOT NULL,
    "StoreProductId" text        NOT NULL,
    "CreatedAt"      timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_plan PRIMARY KEY ("PlanId"),
    CONSTRAINT ux_plan_plan_code UNIQUE ("PlanCode"),
    CONSTRAINT ux_plan_interval UNIQUE ("Interval"),
    CONSTRAINT ux_plan_store_product_id UNIQUE ("StoreProductId"),
    CONSTRAINT ck_plan_interval CHECK ("Interval" IN ('Monthly', 'Annual')),
    CONSTRAINT ck_plan_price_cents CHECK ("PriceCents" >= 0)
);

CREATE TRIGGER trg_plan_updated_at BEFORE UPDATE ON billing."Plan"
    FOR EACH ROW EXECUTE FUNCTION billing.set_updated_at();

CREATE TABLE billing."Subscription" (
    "SubscriptionId"     bigint      GENERATED ALWAYS AS IDENTITY,
    "SubscriptionCode"   uuid        NOT NULL DEFAULT gen_random_uuid(),
    "UserId"             bigint      NOT NULL,
    "PlanId"             bigint      NOT NULL,
    "Status"             text        NOT NULL,
    "CurrentPeriodStart" timestamptz NOT NULL,
    "CurrentPeriodEnd"   timestamptz NOT NULL,
    "CancelAtPeriodEnd"  boolean     NOT NULL,
    "CreatedAt"          timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"          timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_subscription PRIMARY KEY ("SubscriptionId"),
    CONSTRAINT ux_subscription_subscription_code UNIQUE ("SubscriptionCode"),
    CONSTRAINT fk_subscription_user_id FOREIGN KEY ("UserId")
        REFERENCES identity."User" ("UserId") ON DELETE RESTRICT,
    CONSTRAINT fk_subscription_plan_id FOREIGN KEY ("PlanId")
        REFERENCES billing."Plan" ("PlanId") ON DELETE RESTRICT,
    CONSTRAINT ck_subscription_status CHECK ("Status" IN ('Active', 'Canceled', 'PastDue', 'Expired'))
);

CREATE INDEX ix_subscription_user_id ON billing."Subscription" ("UserId");
CREATE INDEX ix_subscription_plan_id ON billing."Subscription" ("PlanId");
-- Una sola suscripción activa o con pago pendiente por usuario.
CREATE UNIQUE INDEX ux_subscription_user_id_current ON billing."Subscription" ("UserId")
    WHERE "Status" IN ('Active', 'PastDue');

CREATE TRIGGER trg_subscription_updated_at BEFORE UPDATE ON billing."Subscription"
    FOR EACH ROW EXECUTE FUNCTION billing.set_updated_at();

CREATE TABLE billing."Payment" (
    "PaymentId"          bigint      GENERATED ALWAYS AS IDENTITY,
    "PaymentCode"        uuid        NOT NULL DEFAULT gen_random_uuid(),
    "UserId"             bigint      NOT NULL,
    "AmountCents"        integer     NOT NULL,
    "Currency"           char(3)     NOT NULL,
    "Status"             text        NOT NULL,
    "Kind"               text        NOT NULL,
    "IdempotencyKey"     text        NOT NULL,
    -- Sin restricción única: por verificar si StoreKit local repite los identificadores.
    "StoreTransactionId" text        NULL,
    "CreatedAt"          timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"          timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_payment PRIMARY KEY ("PaymentId"),
    CONSTRAINT ux_payment_payment_code UNIQUE ("PaymentCode"),
    CONSTRAINT ux_payment_idempotency_key UNIQUE ("IdempotencyKey"),
    CONSTRAINT fk_payment_user_id FOREIGN KEY ("UserId")
        REFERENCES identity."User" ("UserId") ON DELETE RESTRICT,
    CONSTRAINT ck_payment_status CHECK ("Status" IN ('Succeeded', 'Failed', 'Refunded')),
    CONSTRAINT ck_payment_kind CHECK ("Kind" IN ('Subscription', 'Purchase')),
    CONSTRAINT ck_payment_amount_cents CHECK ("AmountCents" >= 0)
);

CREATE INDEX ix_payment_user_id ON billing."Payment" ("UserId");

CREATE TRIGGER trg_payment_updated_at BEFORE UPDATE ON billing."Payment"
    FOR EACH ROW EXECUTE FUNCTION billing.set_updated_at();

CREATE TABLE billing."Purchase" (
    "PurchaseId"   bigint      GENERATED ALWAYS AS IDENTITY,
    "PurchaseCode" uuid        NOT NULL DEFAULT gen_random_uuid(),
    "UserId"       bigint      NOT NULL,
    "ItemType"     text        NOT NULL,
    "TrackId"      bigint      NULL,
    "AlbumId"      bigint      NULL,
    "PriceCents"   integer     NOT NULL,
    "Currency"     char(3)     NOT NULL,
    "PaymentId"    bigint      NOT NULL,
    "PurchasedAt"  timestamptz NOT NULL,
    "CreatedAt"    timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt"    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_purchase PRIMARY KEY ("PurchaseId"),
    CONSTRAINT ux_purchase_purchase_code UNIQUE ("PurchaseCode"),
    CONSTRAINT fk_purchase_user_id FOREIGN KEY ("UserId")
        REFERENCES identity."User" ("UserId") ON DELETE RESTRICT,
    CONSTRAINT fk_purchase_track_id FOREIGN KEY ("TrackId")
        REFERENCES catalog."Track" ("TrackId") ON DELETE RESTRICT,
    CONSTRAINT fk_purchase_album_id FOREIGN KEY ("AlbumId")
        REFERENCES catalog."Album" ("AlbumId") ON DELETE RESTRICT,
    CONSTRAINT fk_purchase_payment_id FOREIGN KEY ("PaymentId")
        REFERENCES billing."Payment" ("PaymentId") ON DELETE RESTRICT,
    -- Exige exactamente la columna que corresponde a "ItemType" (y limita sus valores).
    CONSTRAINT ck_purchase_item CHECK (
        ("ItemType" = 'Track' AND "TrackId" IS NOT NULL AND "AlbumId" IS NULL)
        OR ("ItemType" = 'Album' AND "AlbumId" IS NOT NULL AND "TrackId" IS NULL)
    ),
    CONSTRAINT ck_purchase_price_cents CHECK ("PriceCents" >= 0)
);

CREATE INDEX ix_purchase_user_id ON billing."Purchase" ("UserId");
CREATE INDEX ix_purchase_track_id ON billing."Purchase" ("TrackId");
CREATE INDEX ix_purchase_album_id ON billing."Purchase" ("AlbumId");
CREATE INDEX ix_purchase_payment_id ON billing."Purchase" ("PaymentId");
-- Impiden comprar dos veces lo mismo.
CREATE UNIQUE INDEX ux_purchase_user_id_track_id ON billing."Purchase" ("UserId", "TrackId")
    WHERE "TrackId" IS NOT NULL;
CREATE UNIQUE INDEX ux_purchase_user_id_album_id ON billing."Purchase" ("UserId", "AlbumId")
    WHERE "AlbumId" IS NOT NULL;

CREATE TRIGGER trg_purchase_updated_at BEFORE UPDATE ON billing."Purchase"
    FOR EACH ROW EXECUTE FUNCTION billing.set_updated_at();
