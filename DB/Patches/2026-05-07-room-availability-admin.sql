IF OBJECT_ID('dbo.RoomType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoomType (
        RoomTypeId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        BasePrice DECIMAL(10,2) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
END;
GO

IF OBJECT_ID('dbo.RoomStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoomStatus (
        RoomStatusId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255) NULL,
        IsAvailableForBooking BIT NOT NULL DEFAULT 1
    );
END;
GO

IF OBJECT_ID('dbo.Room', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Room (
        RoomId INT IDENTITY(1,1) PRIMARY KEY,
        RoomNumber NVARCHAR(50) NOT NULL UNIQUE,
        IsActive BIT NOT NULL DEFAULT 1,
        RoomTypeId INT NOT NULL,
        RoomStatusId INT NOT NULL,
        CONSTRAINT FK_Room_RoomType FOREIGN KEY (RoomTypeId) REFERENCES dbo.RoomType(RoomTypeId),
        CONSTRAINT FK_Room_RoomStatus FOREIGN KEY (RoomStatusId) REFERENCES dbo.RoomStatus(RoomStatusId)
    );
END;
GO

IF OBJECT_ID('dbo.ReservationStatus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReservationStatus (
        ReservationStatusId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255) NULL,
        IsFinal BIT NOT NULL DEFAULT 0
    );
END;
GO

IF OBJECT_ID('dbo.Customer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customer (
        CustomerId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        LastName NVARCHAR(255) NOT NULL,
        Email NVARCHAR(255) NOT NULL,
        Phone NVARCHAR(50) NULL,
        CreditCard NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
END;
GO

IF OBJECT_ID('dbo.Reservation', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reservation (
        ReservationId INT IDENTITY(1,1) PRIMARY KEY,
        ReservationDate DATETIME NOT NULL,
        StartDate DATETIME NOT NULL,
        EndDate DATETIME NOT NULL,
        CustomerId INT NOT NULL,
        RoomId INT NOT NULL,
        ReservationStatusId INT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_Reservation_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(CustomerId),
        CONSTRAINT FK_Reservation_Room FOREIGN KEY (RoomId) REFERENCES dbo.Room(RoomId),
        CONSTRAINT FK_Reservation_ReservationStatus FOREIGN KEY (ReservationStatusId) REFERENCES dbo.ReservationStatus(ReservationStatusId),
        CONSTRAINT CK_Reservation_Dates_AdminAvailability CHECK (EndDate > StartDate)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.RoomType WHERE Name = N'Habitacion Doble')
    INSERT INTO dbo.RoomType (Name, BasePrice, IsActive) VALUES (N'Habitacion Doble', 95.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.RoomType WHERE Name = N'Suite Volcan')
    INSERT INTO dbo.RoomType (Name, BasePrice, IsActive) VALUES (N'Suite Volcan', 135.00, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.RoomType WHERE Name = N'Villa Familiar')
    INSERT INTO dbo.RoomType (Name, BasePrice, IsActive) VALUES (N'Villa Familiar', 180.00, 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.RoomStatus WHERE Name = N'Disponible')
    INSERT INTO dbo.RoomStatus (Name, Description, IsAvailableForBooking) VALUES (N'Disponible', N'Habitacion lista para reservar.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.RoomStatus WHERE Name = N'Limpieza')
    INSERT INTO dbo.RoomStatus (Name, Description, IsAvailableForBooking) VALUES (N'Limpieza', N'Habitacion en preparacion operativa.', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.RoomStatus WHERE Name = N'Mantenimiento')
    INSERT INTO dbo.RoomStatus (Name, Description, IsAvailableForBooking) VALUES (N'Mantenimiento', N'Habitacion fuera de servicio temporalmente.', 0);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ReservationStatus WHERE Name = N'Pendiente')
    INSERT INTO dbo.ReservationStatus (Name, Description, IsFinal) VALUES (N'Pendiente', N'Reserva pendiente de confirmacion.', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.ReservationStatus WHERE Name = N'Confirmada')
    INSERT INTO dbo.ReservationStatus (Name, Description, IsFinal) VALUES (N'Confirmada', N'Reserva confirmada por el hotel.', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.ReservationStatus WHERE Name = N'Finalizada')
    INSERT INTO dbo.ReservationStatus (Name, Description, IsFinal) VALUES (N'Finalizada', N'Estadia finalizada.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.ReservationStatus WHERE Name = N'Cancelada')
    INSERT INTO dbo.ReservationStatus (Name, Description, IsFinal) VALUES (N'Cancelada', N'Reserva cancelada.', 1);
GO

DECLARE @DoubleTypeId INT = (SELECT TOP 1 RoomTypeId FROM dbo.RoomType WHERE Name = N'Habitacion Doble' ORDER BY RoomTypeId);
DECLARE @SuiteTypeId INT = (SELECT TOP 1 RoomTypeId FROM dbo.RoomType WHERE Name = N'Suite Volcan' ORDER BY RoomTypeId);
DECLARE @VillaTypeId INT = (SELECT TOP 1 RoomTypeId FROM dbo.RoomType WHERE Name = N'Villa Familiar' ORDER BY RoomTypeId);
DECLARE @AvailableStatusId INT = (SELECT TOP 1 RoomStatusId FROM dbo.RoomStatus WHERE Name = N'Disponible' ORDER BY RoomStatusId);
DECLARE @CleaningStatusId INT = (SELECT TOP 1 RoomStatusId FROM dbo.RoomStatus WHERE Name = N'Limpieza' ORDER BY RoomStatusId);
DECLARE @MaintenanceStatusId INT = (SELECT TOP 1 RoomStatusId FROM dbo.RoomStatus WHERE Name = N'Mantenimiento' ORDER BY RoomStatusId);

IF NOT EXISTS (SELECT 1 FROM dbo.Room WHERE RoomNumber = N'101')
    INSERT INTO dbo.Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId) VALUES (N'101', 1, @DoubleTypeId, @AvailableStatusId);
IF NOT EXISTS (SELECT 1 FROM dbo.Room WHERE RoomNumber = N'102')
    INSERT INTO dbo.Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId) VALUES (N'102', 1, @DoubleTypeId, @AvailableStatusId);
IF NOT EXISTS (SELECT 1 FROM dbo.Room WHERE RoomNumber = N'103')
    INSERT INTO dbo.Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId) VALUES (N'103', 1, @DoubleTypeId, @CleaningStatusId);
IF NOT EXISTS (SELECT 1 FROM dbo.Room WHERE RoomNumber = N'201')
    INSERT INTO dbo.Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId) VALUES (N'201', 1, @SuiteTypeId, @AvailableStatusId);
IF NOT EXISTS (SELECT 1 FROM dbo.Room WHERE RoomNumber = N'202')
    INSERT INTO dbo.Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId) VALUES (N'202', 1, @SuiteTypeId, @MaintenanceStatusId);
IF NOT EXISTS (SELECT 1 FROM dbo.Room WHERE RoomNumber = N'301')
    INSERT INTO dbo.Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId) VALUES (N'301', 1, @VillaTypeId, @AvailableStatusId);
IF NOT EXISTS (SELECT 1 FROM dbo.Room WHERE RoomNumber = N'302')
    INSERT INTO dbo.Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId) VALUES (N'302', 1, @VillaTypeId, @AvailableStatusId);
GO

DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @CustomerId INT;
DECLARE @ConfirmedStatusId INT = (SELECT TOP 1 ReservationStatusId FROM dbo.ReservationStatus WHERE Name = N'Confirmada' ORDER BY ReservationStatusId);
DECLARE @Room101Id INT = (SELECT TOP 1 RoomId FROM dbo.Room WHERE RoomNumber = N'101' ORDER BY RoomId);
DECLARE @Room201Id INT = (SELECT TOP 1 RoomId FROM dbo.Room WHERE RoomNumber = N'201' ORDER BY RoomId);

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE Email = N'disponibilidad.demo@laspurrujas.local')
BEGIN
    INSERT INTO dbo.Customer (Name, LastName, Email, Phone, CreditCard, IsActive)
    VALUES (N'Reserva', N'Demo', N'disponibilidad.demo@laspurrujas.local', N'0000-0000', NULL, 1);
END;

SELECT @CustomerId = CustomerId
FROM dbo.Customer
WHERE Email = N'disponibilidad.demo@laspurrujas.local';

IF @Room101Id IS NOT NULL AND @ConfirmedStatusId IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.Reservation
        WHERE RoomId = @Room101Id
          AND IsActive = 1
          AND CAST(StartDate AS DATE) <= @Today
          AND CAST(EndDate AS DATE) > @Today
   )
BEGIN
    INSERT INTO dbo.Reservation (ReservationDate, StartDate, EndDate, CustomerId, RoomId, ReservationStatusId, IsActive)
    VALUES (GETDATE(), CAST(@Today AS DATETIME), DATEADD(DAY, 1, CAST(@Today AS DATETIME)), @CustomerId, @Room101Id, @ConfirmedStatusId, 1);
END;

IF @Room201Id IS NOT NULL AND @ConfirmedStatusId IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.Reservation
        WHERE RoomId = @Room201Id
          AND IsActive = 1
          AND CAST(StartDate AS DATE) <= @Today
          AND CAST(EndDate AS DATE) > @Today
   )
BEGIN
    INSERT INTO dbo.Reservation (ReservationDate, StartDate, EndDate, CustomerId, RoomId, ReservationStatusId, IsActive)
    VALUES (GETDATE(), CAST(@Today AS DATETIME), DATEADD(DAY, 2, CAST(@Today AS DATETIME)), @CustomerId, @Room201Id, @ConfirmedStatusId, 1);
END;
GO
