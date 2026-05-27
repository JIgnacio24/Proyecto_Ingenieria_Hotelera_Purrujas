-- Patch: 2026-05-26 — Detalles de tipo de habitación
-- Agrega Description y Capacity a la tabla RoomType.
-- Crea la tabla RoomTypeImage para imágenes asociadas por tipo.
-- Inserta datos de ejemplo para los tres tipos existentes.

-- Columna Description
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.RoomType') AND name = 'Description'
)
    ALTER TABLE dbo.RoomType ADD Description NVARCHAR(MAX) NULL;
GO

-- Columna Capacity (valor por defecto 2 para no romper filas existentes)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.RoomType') AND name = 'Capacity'
)
    ALTER TABLE dbo.RoomType ADD Capacity INT NOT NULL DEFAULT 2;
GO

-- Tabla de imágenes por tipo de habitación
IF OBJECT_ID('dbo.RoomTypeImage', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoomTypeImage (
        RoomTypeImageId INT IDENTITY(1,1) PRIMARY KEY,
        RoomTypeId      INT             NOT NULL,
        Url             NVARCHAR(500)   NOT NULL,
        AltText         NVARCHAR(255)   NULL,
        DisplayOrder    INT             NOT NULL DEFAULT 0,
        CONSTRAINT FK_RoomTypeImage_RoomType
            FOREIGN KEY (RoomTypeId) REFERENCES dbo.RoomType(RoomTypeId)
    );
END;
GO

-- Poblar descripciones y capacidades de los tipos existentes
UPDATE dbo.RoomType
SET Description = N'Habitación cómoda para dos personas con vista al jardín tropical. ' +
                  N'Incluye cama doble, baño privado con ducha de lluvia, aire acondicionado, ' +
                  N'TV de pantalla plana y conexión Wi-Fi de alta velocidad.',
    Capacity    = 2
WHERE Name = N'Habitacion Doble'
  AND (Description IS NULL OR Description = N'');

UPDATE dbo.RoomType
SET Description = N'Suite de lujo con vista privilegiada al Volcán Arenal. ' +
                  N'Cuenta con jacuzzi privado, sala de estar, cama king size, ' +
                  N'terraza panorámica, minibar y servicio de mayordomo incluido.',
    Capacity    = 3
WHERE Name = N'Suite Volcan'
  AND (Description IS NULL OR Description = N'');

UPDATE dbo.RoomType
SET Description = N'Amplia villa diseñada para familias o grupos. ' +
                  N'Incluye dos habitaciones, cocina completamente equipada, sala de estar, ' +
                  N'jardín privado con área de BBQ y piscina de uso exclusivo.',
    Capacity    = 6
WHERE Name = N'Villa Familiar'
  AND (Description IS NULL OR Description = N'');
GO
