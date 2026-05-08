IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HomePageContent')
BEGIN
    CREATE TABLE HomePageContent (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ContentJson NVARCHAR(MAX) NOT NULL DEFAULT '{}',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END;
GO

CREATE OR ALTER PROCEDURE usp_HomePageContent_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        ISNULL(ContentJson, '{}') AS ContentJson
    FROM HomePageContent
    ORDER BY CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE usp_HomePageContent_Upsert
    @ContentJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM HomePageContent)
    BEGIN
        UPDATE HomePageContent
        SET ContentJson = @ContentJson,
            UpdatedAt = GETUTCDATE()
        WHERE Id = (SELECT TOP 1 Id FROM HomePageContent ORDER BY CreatedAt DESC);
    END
    ELSE
    BEGIN
        INSERT INTO HomePageContent (ContentJson, CreatedAt, UpdatedAt)
        VALUES (@ContentJson, GETUTCDATE(), GETUTCDATE());
    END
END;
GO

IF NOT EXISTS (SELECT 1 FROM HomePageContent)
BEGIN
    INSERT INTO HomePageContent (ContentJson)
    VALUES (N'{"heroEyebrow":"Bienvenido","heroTitle":"Hotel Las Purrujas","heroSubtitle":"Donde la naturaleza te abraza y Costa Rica te enamora."}');
END;
GO
