-- Agrega los titulos editables de las tarjetas Mision, Vision y Valores al JSON actual.
-- Mantiene los valores existentes si ya fueron personalizados desde el panel admin.
IF EXISTS (SELECT 1 FROM AboutUsPageContent)
BEGIN
    UPDATE AboutUsPageContent
    SET ContentJson = JSON_MODIFY(
        JSON_MODIFY(
            JSON_MODIFY(
                ISNULL(NULLIF(ContentJson, ''), '{}'),
                '$.missionTitle',
                COALESCE(NULLIF(JSON_VALUE(ContentJson, '$.missionTitle'), ''), 'Mision')
            ),
            '$.visionTitle',
            COALESCE(NULLIF(JSON_VALUE(ContentJson, '$.visionTitle'), ''), 'Vision')
        ),
        '$.valuesTitle',
        COALESCE(NULLIF(JSON_VALUE(ContentJson, '$.valuesTitle'), ''), 'Valores')
    ),
    UpdatedAt = GETUTCDATE()
    WHERE Id = (SELECT TOP 1 Id FROM AboutUsPageContent ORDER BY CreatedAt DESC);
END;
GO
