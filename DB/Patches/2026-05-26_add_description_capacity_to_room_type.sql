USE Ingenieria_Purrujas_BD;
GO

-- 1. Agregar columna Description si no existe
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'RoomType'
      AND COLUMN_NAME = 'Description'
)
    ALTER TABLE dbo.RoomType
    ADD Description NVARCHAR(MAX) NULL;
GO

-- 2. Agregar columna Capacity si no existe
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'RoomType'
      AND COLUMN_NAME = 'Capacity'
)
    ALTER TABLE dbo.RoomType
    ADD Capacity INT NOT NULL
    CONSTRAINT DF_RoomType_Capacity DEFAULT 2;
GO

-- 3. Corregir filas con Capacity NULL o 0 (pueden existir si la columna
--    fue agregada con DEFAULT pero las filas no recibieron backfill correcto)
UPDATE dbo.RoomType
SET Capacity = 2
WHERE (Capacity IS NULL OR Capacity = 0)
  AND IsActive = 1;
GO
