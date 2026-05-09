ALTER TABLE "PrivateMessages" ADD COLUMN IF NOT EXISTS "MediaUrl" character varying(2000);
ALTER TABLE "PrivateMessages" ADD COLUMN IF NOT EXISTS "MediaPublicId" character varying(500);
ALTER TABLE "PrivateMessages" ADD COLUMN IF NOT EXISTS "MediaType" character varying(50);
ALTER TABLE "PrivateMessages" ADD COLUMN IF NOT EXISTS "MediaName" character varying(255);
ALTER TABLE "PrivateMessages" ADD COLUMN IF NOT EXISTS "MediaBytes" bigint;

ALTER TABLE "Messages" ADD COLUMN IF NOT EXISTS "MediaUrl" character varying(2000);
ALTER TABLE "Messages" ADD COLUMN IF NOT EXISTS "MediaPublicId" character varying(500);
ALTER TABLE "Messages" ADD COLUMN IF NOT EXISTS "MediaType" character varying(50);
ALTER TABLE "Messages" ADD COLUMN IF NOT EXISTS "MediaName" character varying(255);
ALTER TABLE "Messages" ADD COLUMN IF NOT EXISTS "MediaBytes" bigint;
