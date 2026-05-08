-- Patch: 2026-05-07-reservation-online
-- Agrega stored procedures para disponibilidad de habitaciones y búsqueda de clientes por email

-- =============================================
-- usp_Room_GetFirstAvailableByTypeKey
-- =============================================
CREATE OR ALTER PROCEDURE usp_Room_GetFirstAvailableByTypeKey
    @RoomTypeKey NVARCHAR(50),
    @StartDate   DATE,
    @EndDate     DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @StartDate >= @EndDate
        THROW 50031, 'La fecha de salida debe ser posterior a la fecha de entrada.', 1;

    DECLARE @RoomTypeId INT;

    SELECT TOP 1 @RoomTypeId = rt.RoomTypeId
    FROM RoomType rt
    WHERE rt.IsActive = 1
      AND (
            (LOWER(@RoomTypeKey) = 'doble'  AND LOWER(rt.Name) LIKE '%doble%')
         OR (LOWER(@RoomTypeKey) = 'suite'  AND LOWER(rt.Name) LIKE '%suite%')
         OR (LOWER(@RoomTypeKey) = 'villa'  AND LOWER(rt.Name) LIKE '%villa%')
         OR LOWER(rt.Name) LIKE '%' + LOWER(@RoomTypeKey) + '%'
      );

    IF @RoomTypeId IS NULL
        THROW 50029, 'Tipo de habitación no encontrado.', 1;

    SELECT TOP 1
        r.RoomId,
        r.RoomNumber,
        rt.Name        AS RoomTypeName,
        rt.BasePrice
    FROM Room r
    INNER JOIN RoomType rt  ON r.RoomTypeId   = rt.RoomTypeId
    INNER JOIN RoomStatus rs ON r.RoomStatusId = rs.RoomStatusId
    WHERE r.RoomTypeId = @RoomTypeId
      AND r.IsActive   = 1
      AND rs.IsAvailableForBooking = 1
      AND NOT EXISTS (
          SELECT 1
          FROM Reservation res
          WHERE res.RoomId = r.RoomId
            AND res.IsActive = 1
            AND res.ReservationStatusId NOT IN (
                SELECT ReservationStatusId
                FROM ReservationStatus
                WHERE Name IN ('Cancelada', 'Finalizada')
            )
            AND @StartDate < CAST(res.EndDate AS DATE)
            AND @EndDate   > CAST(res.StartDate AS DATE)
      )
    ORDER BY r.RoomNumber;

    IF @@ROWCOUNT = 0
        THROW 50030, 'No hay habitaciones disponibles de ese tipo para las fechas indicadas.', 1;
END;
GO

-- =============================================
-- usp_Room_CountAvailableByTypeKey
-- =============================================
CREATE OR ALTER PROCEDURE usp_Room_CountAvailableByTypeKey
    @RoomTypeKey NVARCHAR(50),
    @StartDate   DATE,
    @EndDate     DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @StartDate >= @EndDate
    BEGIN
        SELECT 0 AS AvailableCount, '' AS RoomTypeName;
        RETURN;
    END

    DECLARE @RoomTypeId INT;
    DECLARE @RoomTypeName NVARCHAR(255) = '';

    SELECT TOP 1 @RoomTypeId = rt.RoomTypeId, @RoomTypeName = rt.Name
    FROM RoomType rt
    WHERE rt.IsActive = 1
      AND (
            (LOWER(@RoomTypeKey) = 'doble'  AND LOWER(rt.Name) LIKE '%doble%')
         OR (LOWER(@RoomTypeKey) = 'suite'  AND LOWER(rt.Name) LIKE '%suite%')
         OR (LOWER(@RoomTypeKey) = 'villa'  AND LOWER(rt.Name) LIKE '%villa%')
         OR LOWER(rt.Name) LIKE '%' + LOWER(@RoomTypeKey) + '%'
      );

    IF @RoomTypeId IS NULL
    BEGIN
        SELECT 0 AS AvailableCount, '' AS RoomTypeName;
        RETURN;
    END

    SELECT
        COUNT(*) AS AvailableCount,
        @RoomTypeName AS RoomTypeName
    FROM Room r
    INNER JOIN RoomType rt   ON r.RoomTypeId   = rt.RoomTypeId
    INNER JOIN RoomStatus rs  ON r.RoomStatusId = rs.RoomStatusId
    WHERE r.RoomTypeId = @RoomTypeId
      AND r.IsActive   = 1
      AND rs.IsAvailableForBooking = 1
      AND NOT EXISTS (
          SELECT 1
          FROM Reservation res
          WHERE res.RoomId = r.RoomId
            AND res.IsActive = 1
            AND res.ReservationStatusId NOT IN (
                SELECT ReservationStatusId
                FROM ReservationStatus
                WHERE Name IN ('Cancelada', 'Finalizada')
            )
            AND @StartDate < CAST(res.EndDate AS DATE)
            AND @EndDate   > CAST(res.StartDate AS DATE)
      );
END;
GO

-- =============================================
-- usp_Customer_GetByEmail
-- =============================================
CREATE OR ALTER PROCEDURE usp_Customer_GetByEmail
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CustomerId,
        Name,
        LastName,
        Email,
        Phone,
        CreditCard,
        IsActive
    FROM Customer
    WHERE Email   = @Email
      AND IsActive = 1;
END;
GO
