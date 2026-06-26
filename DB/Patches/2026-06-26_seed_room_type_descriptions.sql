USE Ingenieria_Purrujas_BD;
GO

-- ============================================================================
-- Rellena la descripción y capacidad de los tipos de habitación base.
--
-- Motivo: el seed inicial (Ingenieria_Purrujas_BD.sql) inserta RoomType solo
-- con (Name, BasePrice, IsActive), dejando Description = NULL. En el entorno
-- local las descripciones se cargaron manualmente desde el panel admin, pero
-- en Docker quedaban vacías y la página pública de Cotización Rápida no
-- mostraba la descripción de la habitación.
--
-- Idempotente: solo actualiza filas cuya Description está vacía/NULL, de modo
-- que NUNCA sobrescribe textos editados posteriormente por el administrador.
-- Se ejecuta también sobre volúmenes ya existentes (mecanismo de Patches).
-- ============================================================================

UPDATE dbo.RoomType
SET Description = N'Habitación cómoda para dos personas con vista al jardín tropical. Incluye cama doble, baño privado con ducha de lluvia, aire acondicionado, TV de pantalla plana y conexión Wi-Fi de alta velocidad.',
    Capacity = 2
WHERE Name COLLATE Latin1_General_100_CI_AI = N'Habitación Doble'
  AND IsActive = 1
  AND (Description IS NULL OR LEN(LTRIM(RTRIM(Description))) = 0);
GO

UPDATE dbo.RoomType
SET Description = N'Suite de lujo con vista privilegiada al Volcán Arenal. Cuenta con jacuzzi privado, sala de estar, cama king size, terraza panorámica, minibar y servicio de mayordomo incluido.',
    Capacity = 3
WHERE Name COLLATE Latin1_General_100_CI_AI = N'Suite Volcán'
  AND IsActive = 1
  AND (Description IS NULL OR LEN(LTRIM(RTRIM(Description))) = 0);
GO

UPDATE dbo.RoomType
SET Description = N'Amplia villa diseñada para familias o grupos. Incluye dos habitaciones, cocina completamente equipada, sala de estar, jardín privado con área de BBQ y piscina de uso exclusivo.',
    Capacity = 6
WHERE Name COLLATE Latin1_General_100_CI_AI = N'Villa Familiar'
  AND IsActive = 1
  AND (Description IS NULL OR LEN(LTRIM(RTRIM(Description))) = 0);
GO
