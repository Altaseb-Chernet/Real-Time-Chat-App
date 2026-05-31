CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES
    ('20260501143023_InitialCreate', '8.0.0'),
    ('20260501143222_AddMessageEditFields', '8.0.0'),
    ('20260509130000_AddMediaColumnsToMessagesAndPrivateMessages', '8.0.0')
ON CONFLICT DO NOTHING;
