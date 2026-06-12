d
USE Ingenieria_Purrujas_BD;
GO

-- =========================================================
-- 1. CAMBIOS DE ESQUEMA — ALTER TABLE Advertising
-- =========================================================

-- Renombrar Name -> Title
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Advertising' AND COLUMN_NAME = 'Name'
)
    EXEC sp_rename 'dbo.Advertising.Name', 'Title', 'COLUMN';
GO

-- Agregar Description
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Advertising' AND COLUMN_NAME = 'Description'
)
    ALTER TABLE dbo.Advertising
    ADD Description NVARCHAR(1000) NULL;
GO

-- Agregar ImageUrl
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Advertising' AND COLUMN_NAME = 'ImageUrl'
)
    ALTER TABLE dbo.Advertising
    ADD ImageUrl NVARCHAR(500) NULL;
GO

-- Agregar IsActive
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Advertising' AND COLUMN_NAME = 'IsActive'
)
    ALTER TABLE dbo.Advertising
    ADD IsActive BIT NOT NULL
    CONSTRAINT DF_Advertising_IsActive DEFAULT 1;
GO

-- Agregar CreatedAt
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Advertising' AND COLUMN_NAME = 'CreatedAt'
)
    ALTER TABLE dbo.Advertising
    ADD CreatedAt DATETIME2 NOT NULL
    CONSTRAINT DF_Advertising_CreatedAt DEFAULT GETUTCDATE();
GO

-- Agregar UpdatedAt
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Advertising' AND COLUMN_NAME = 'UpdatedAt'
)
    ALTER TABLE dbo.Advertising
    ADD UpdatedAt DATETIME2 NULL;
GO

-- Backfill filas existentes
UPDATE dbo.Advertising
SET IsActive  = 1,
    CreatedAt = GETUTCDATE()
WHERE IsActive IS NULL OR CreatedAt IS NULL;
GO

-- =========================================================
-- 2. STORED PROCEDURES
-- =========================================================

-- ── usp_Advertising_GetAll ────────────────────────────────
CREATE OR ALTER PROCEDURE usp_Advertising_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AdvertisingId,
        Title,
        Description,
        ImageUrl,
        Link,
        IsActive,
        CreatedAt,
        UpdatedAt
    FROM dbo.Advertising
    WHERE IsActive = 1
    ORDER BY AdvertisingId DESC;
END;
GO

-- ── usp_Advertising_GetAll_Admin (incluye inactivos) ─────
CREATE OR ALTER PROCEDURE usp_Advertising_GetAll_Admin
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AdvertisingId,
        Title,
        Description,
        ImageUrl,
        Link,
        IsActive,
        CreatedAt,
        UpdatedAt
    FROM dbo.Advertising
    ORDER BY AdvertisingId DESC;
END;
GO

-- ── usp_Advertising_GetById ───────────────────────────────
CREATE OR ALTER PROCEDURE usp_Advertising_GetById
    @AdvertisingId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AdvertisingId,
        Title,
        Description,
        ImageUrl,
        Link,
        IsActive,
        CreatedAt,
        UpdatedAt
    FROM dbo.Advertising
    WHERE AdvertisingId = @AdvertisingId;
END;
GO

-- ── usp_Advertising_Create ────────────────────────────────
CREATE OR ALTER PROCEDURE usp_Advertising_Create
    @Title       NVARCHAR(255),
    @Description NVARCHAR(1000),
    @ImageUrl    NVARCHAR(500),
    @Link        NVARCHAR(500),
    @IsActive    BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF LEN(LTRIM(RTRIM(@Title))) = 0
            THROW 50030, N'El título del anuncio no puede estar vacío.', 1;

        IF LEN(LTRIM(RTRIM(@Link))) = 0
            THROW 50031, N'El enlace de destino del anuncio no puede estar vacío.', 1;

        INSERT INTO dbo.Advertising (Title, Description, ImageUrl, Link, IsActive, CreatedAt)
        VALUES (
            LTRIM(RTRIM(@Title)),
            NULLIF(LTRIM(RTRIM(@Description)), ''),
            NULLIF(LTRIM(RTRIM(@ImageUrl)), ''),
            LTRIM(RTRIM(@Link)),
            @IsActive,
            GETUTCDATE()
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewAdvertisingId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- ── usp_Advertising_Update ────────────────────────────────
CREATE OR ALTER PROCEDURE usp_Advertising_Update
    @AdvertisingId INT,
    @Title         NVARCHAR(255),
    @Description   NVARCHAR(1000),
    @ImageUrl      NVARCHAR(500),
    @Link          NVARCHAR(500),
    @IsActive      BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.Advertising
            WHERE AdvertisingId = @AdvertisingId
        )
            THROW 50032, N'Anuncio no encontrado.', 1;

        IF LEN(LTRIM(RTRIM(@Title))) = 0
            THROW 50030, N'El título del anuncio no puede estar vacío.', 1;

        IF LEN(LTRIM(RTRIM(@Link))) = 0
            THROW 50031, N'El enlace de destino del anuncio no puede estar vacío.', 1;

        UPDATE dbo.Advertising
        SET Title       = LTRIM(RTRIM(@Title)),
            Description = NULLIF(LTRIM(RTRIM(@Description)), ''),
            ImageUrl    = NULLIF(LTRIM(RTRIM(@ImageUrl)), ''),
            Link        = LTRIM(RTRIM(@Link)),
            IsActive    = @IsActive,
            UpdatedAt   = GETUTCDATE()
        WHERE AdvertisingId = @AdvertisingId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- ── usp_Advertising_Delete (soft delete) ─────────────────
CREATE OR ALTER PROCEDURE usp_Advertising_Delete
    @AdvertisingId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE dbo.Advertising
        SET IsActive  = 0,
            UpdatedAt = GETUTCDATE()
        WHERE AdvertisingId = @AdvertisingId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
