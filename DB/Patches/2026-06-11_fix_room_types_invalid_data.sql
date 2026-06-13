USE Ingenieria_Purrujas_BD;
GO

-- 1. Desactivar registros con nombre vacío o precio cero
UPDATE dbo.RoomType
SET IsActive = 0
WHERE IsActive = 1
  AND (LEN(LTRIM(RTRIM(Name))) = 0 OR BasePrice <= 0);
GO

-- 2. Corregir Capacity = 0 para los tipos base
--    La condición no depende de Description para ser robusta
--    contra el fallo del patch 2026-05-26 (que usaba AND Description IS NULL).
UPDATE dbo.RoomType
SET Capacity = 2
WHERE Name COLLATE Latin1_General_100_CI_AI = N'Habitación Doble'
  AND IsActive = 1
  AND (Capacity IS NULL OR Capacity = 0);

UPDATE dbo.RoomType
SET Capacity = 3
WHERE Name COLLATE Latin1_General_100_CI_AI = N'Suite Volcán'
  AND IsActive = 1
  AND (Capacity IS NULL OR Capacity = 0);

UPDATE dbo.RoomType
SET Capacity = 6
WHERE Name COLLATE Latin1_General_100_CI_AI = N'Villa Familiar'
  AND IsActive = 1
  AND (Capacity IS NULL OR Capacity = 0);
GO

-- 3. Agregar CHECK CONSTRAINT para prevenir nombres vacíos futuros (idempotente)
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_NAME = 'CK_RoomType_Name_NotEmpty'
      AND TABLE_NAME = 'RoomType'
)
    ALTER TABLE dbo.RoomType
    ADD CONSTRAINT CK_RoomType_Name_NotEmpty
    CHECK (LEN(LTRIM(RTRIM(Name))) > 0);
GO
