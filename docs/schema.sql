-- CustomerNotificationService PostgreSQL Schema
-- Target: PostgreSQL 14+
-- Notes:
-- - Uses UTC timestamps (timestamptz)
-- - Naming: snake_case tables and columns
-- - Soft delete optional via deleted_at on some tables
-- - Keep sequences/identity as GENERATED ALWAYS for safety

BEGIN;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================
-- Tenancy and common types
-- =============================
CREATE SCHEMA IF NOT EXISTS notification;
SET search_path = notification, public;

-- Status enums
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'notification_status') THEN
    CREATE TYPE notification_status AS ENUM (
      'pending',       -- created, queued
      'processing',    -- picked by worker
      'sent',          -- delivered to provider
      'delivered',     -- confirmed delivered
      'failed',        -- permanently failed
      'canceled'       -- canceled by user/system
    );
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'delivery_channel') THEN
    CREATE TYPE delivery_channel AS ENUM (
      'email', 'sms', 'push', 'webhook'
    );
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'priority_level') THEN
    CREATE TYPE priority_level AS ENUM ('low','normal','high','urgent');
  END IF;
END $$;

-- =============================
-- Templates
-- =============================
CREATE TABLE IF NOT EXISTS templates (
  id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  name            TEXT NOT NULL,
  channel         delivery_channel NOT NULL,
  locale          TEXT NOT NULL DEFAULT 'en-US',
  subject         TEXT,
  body            TEXT NOT NULL, -- can hold handlebars/liquid placeholders
  version         INTEGER NOT NULL DEFAULT 1,
  is_active       BOOLEAN NOT NULL DEFAULT true,
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  deleted_at      timestamptz
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_templates_name_channel_locale
  ON templates (lower(name), channel, lower(locale))
  WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_templates_active
  ON templates (is_active) WHERE deleted_at IS NULL;

-- =============================
-- Notifications (logical entity requested by client)
-- =============================
CREATE TABLE IF NOT EXISTS notifications (
  id                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  external_id       TEXT,            -- client-supplied idempotency key or reference
  template_id       UUID REFERENCES templates(id) ON DELETE RESTRICT,
  channel           delivery_channel NOT NULL,
  recipient         TEXT NOT NULL,   -- email address, phone, device token, or URL
  payload           JSONB NOT NULL,  -- template variables, metadata
  priority          priority_level NOT NULL DEFAULT 'normal',
  schedule_at       timestamptz,     -- optional future schedule
  status            notification_status NOT NULL DEFAULT 'pending',
  error_code        TEXT,
  error_message     TEXT,
  created_at        timestamptz NOT NULL DEFAULT now(),
  updated_at        timestamptz NOT NULL DEFAULT now(),
  archived_at       timestamptz
);

CREATE INDEX IF NOT EXISTS ix_notifications_status
  ON notifications (status);

CREATE INDEX IF NOT EXISTS ix_notifications_template
  ON notifications (template_id);

CREATE INDEX IF NOT EXISTS ix_notifications_external_id
  ON notifications (lower(external_id));

CREATE INDEX IF NOT EXISTS ix_notifications_schedule
  ON notifications (schedule_at) WHERE status IN ('pending','processing');

CREATE INDEX IF NOT EXISTS ix_notifications_created_desc
  ON notifications (created_at DESC);

-- =============================
-- Notification Queue (work items for dispatchers)
-- =============================
CREATE TABLE IF NOT EXISTS notification_queue (
  id                BIGSERIAL PRIMARY KEY,
  notification_id   UUID NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
  available_at      timestamptz NOT NULL DEFAULT now(),
  locked_at         timestamptz,
  locked_by         TEXT,
  attempt           INTEGER NOT NULL DEFAULT 0,
  max_attempts      INTEGER NOT NULL DEFAULT 10,
  created_at        timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_queue_notification
  ON notification_queue (notification_id);

CREATE INDEX IF NOT EXISTS ix_queue_available
  ON notification_queue (available_at) WHERE locked_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_queue_locked
  ON notification_queue (locked_at);

-- =============================
-- Delivery Attempts (each try to deliver a notification)
-- =============================
CREATE TABLE IF NOT EXISTS delivery_attempts (
  id                BIGSERIAL PRIMARY KEY,
  notification_id   UUID NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
  channel           delivery_channel NOT NULL,
  provider          TEXT,           -- e.g., ses, twilio, fcm
  request_payload   JSONB,
  response_payload  JSONB,
  status            TEXT NOT NULL,  -- 'success' | 'retry' | 'failure'
  error_code        TEXT,
  error_message     TEXT,
  duration_ms       INTEGER,
  created_at        timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_attempts_notification
  ON delivery_attempts (notification_id);

CREATE INDEX IF NOT EXISTS ix_attempts_created
  ON delivery_attempts (created_at);

-- =============================
-- Audit Logs (append-only)
-- =============================
CREATE TABLE IF NOT EXISTS audit_logs (
  id                BIGSERIAL PRIMARY KEY,
  entity_type       TEXT NOT NULL,     -- 'template' | 'notification' | 'queue' | 'attempt'
  entity_id         TEXT NOT NULL,     -- UUID or bigint as text
  action            TEXT NOT NULL,     -- 'create' | 'update' | 'delete' | 'status_change' | 'enqueue' | ...
  actor_type        TEXT NOT NULL,     -- 'system' | 'user' | 'api'
  actor_id          TEXT,              -- user id or client id
  changes           JSONB,             -- diff or snapshot
  created_at        timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_audit_entity
  ON audit_logs (entity_type, entity_id);

CREATE INDEX IF NOT EXISTS ix_audit_created
  ON audit_logs (created_at);

-- =============================
-- Triggers
-- =============================
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_templates_updated ON templates;
CREATE TRIGGER trg_templates_updated
BEFORE UPDATE ON templates
FOR EACH ROW EXECUTE PROCEDURE set_updated_at();

DROP TRIGGER IF EXISTS trg_notifications_updated ON notifications;
CREATE TRIGGER trg_notifications_updated
BEFORE UPDATE ON notifications
FOR EACH ROW EXECUTE PROCEDURE set_updated_at();

-- Enqueue on create (optional pattern)
CREATE OR REPLACE FUNCTION enqueue_notification()
RETURNS TRIGGER AS $$
BEGIN
  IF NEW.status = 'pending' THEN
    INSERT INTO notification_queue (notification_id, available_at)
    VALUES (NEW.id, COALESCE(NEW.schedule_at, now()))
    ON CONFLICT (notification_id) DO NOTHING;
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_notifications_enqueue ON notifications;
CREATE TRIGGER trg_notifications_enqueue
AFTER INSERT ON notifications
FOR EACH ROW EXECUTE PROCEDURE enqueue_notification();

COMMIT;
