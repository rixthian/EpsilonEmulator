ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS public_id VARCHAR(64);

UPDATE characters
SET public_id = CONCAT('usr_', lower(to_hex(character_id)))
WHERE public_id IS NULL OR btrim(public_id) = '';

ALTER TABLE characters
    ALTER COLUMN public_id SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS idx_characters_public_id_lower
    ON characters (lower(public_id));
