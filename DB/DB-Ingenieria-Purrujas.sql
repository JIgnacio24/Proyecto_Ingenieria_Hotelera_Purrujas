IF DB_ID('Ingenieria_Purrujas_BD') IS NOT NULL
BEGIN
    ALTER DATABASE Ingenieria_Purrujas_BD SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE Ingenieria_Purrujas_BD;
END;
GO

CREATE DATABASE Ingenieria_Purrujas_BD;
GO

USE Ingenieria_Purrujas_BD;
GO

-- =========================================
-- TABLAS PRINCIPALES
-- =========================================

CREATE TABLE Customer (
    CustomerId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL,
    LastName NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Phone NVARCHAR(30) NULL,
    CreditCard NVARCHAR(20) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE AdminUser (
    AdminUserId INT PRIMARY KEY IDENTITY,
    FullName NVARCHAR(255) NOT NULL,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARBINARY(64) NOT NULL,
    PasswordSalt VARBINARY(32) NOT NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'Administrador',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    LastLoginAt DATETIME2 NULL
);
GO

CREATE TABLE RoomType (
    RoomTypeId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL,
    BasePrice DECIMAL(10,2) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE RoomStatus (
    RoomStatusId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    IsAvailableForBooking BIT NOT NULL
);
GO

CREATE TABLE Room (
    RoomId INT PRIMARY KEY IDENTITY,
    RoomNumber NVARCHAR(50) NOT NULL UNIQUE,
    IsActive BIT NOT NULL DEFAULT 1,
    RoomTypeId INT NOT NULL,
    RoomStatusId INT NOT NULL,
    CONSTRAINT FKRoomTypeRoom FOREIGN KEY (RoomTypeId) REFERENCES RoomType(RoomTypeId),
    CONSTRAINT FKRoomStatus FOREIGN KEY (RoomStatusId) REFERENCES RoomStatus(RoomStatusId)
);
GO

CREATE TABLE ReservationStatus (
    ReservationStatusId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    IsFinal BIT NOT NULL
);
GO

CREATE TABLE Reservation (
    ReservationId INT PRIMARY KEY IDENTITY,
    ReservationDate DATETIME NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    CustomerId INT NOT NULL,
    RoomId INT NOT NULL,
    ReservationStatusId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FKCustomerReservation FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId),
    CONSTRAINT FKRoomReservation FOREIGN KEY (RoomId) REFERENCES Room(RoomId),
    CONSTRAINT FKReservationStatus FOREIGN KEY (ReservationStatusId) REFERENCES ReservationStatus(ReservationStatusId),
    CONSTRAINT CK_Reservation_Dates CHECK (EndDate > StartDate)
);
GO

CREATE TABLE Bill (
    BillId INT PRIMARY KEY IDENTITY,
    ReservationId INT NOT NULL UNIQUE,
    BasePrice DECIMAL(10,2) NOT NULL,
    Discount DECIMAL(10,2) NOT NULL DEFAULT 0,
    SeasonAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    CONSTRAINT FKReservationBill FOREIGN KEY (ReservationId) REFERENCES Reservation(ReservationId)
);
GO

CREATE TABLE Payment (
    PaymentId INT PRIMARY KEY IDENTITY,
    ReservationId INT NOT NULL UNIQUE,
    Amount DECIMAL(10,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    PaymentDate DATETIME NOT NULL,
    IsPaid BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FKPaymentReservation FOREIGN KEY (ReservationId) REFERENCES Reservation(ReservationId)
);
GO

CREATE TABLE Season (
    SeasonId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL,
    PercentageChange INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    CONSTRAINT CK_Season_Dates CHECK (EndDate >= StartDate),
    IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE Promotion (
    PromotionId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL,
    Discount INT NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    RoomTypeId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FKPromotionRoomType FOREIGN KEY (RoomTypeId) REFERENCES RoomType(RoomTypeId)
);
GO

CREATE TABLE Feature (
    FeatureId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL
);
GO

CREATE TABLE Image (
    ImageId INT PRIMARY KEY IDENTITY,
    ImageURL NVARCHAR(500) NOT NULL
);
GO

CREATE TABLE Advertising (
    AdvertisingId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL,
    Link NVARCHAR(500) NOT NULL
);
GO

CREATE TABLE Page (
    PageId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Link NVARCHAR(500) NOT NULL
);
GO

CREATE TABLE PageInformation (
    PageInformationId INT PRIMARY KEY IDENTITY,
    Subtitle NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    PageId INT NOT NULL,
    CONSTRAINT FKPagePageInformation FOREIGN KEY (PageId) REFERENCES Page(PageId)
);
GO

CREATE TABLE PageInformationImage (
    PageInformationImageId INT PRIMARY KEY IDENTITY,
    Description NVARCHAR(1000) NULL,
    ImageId INT NOT NULL,
    PageInformationId INT NOT NULL,
    CONSTRAINT FKPageInformationImagePageId FOREIGN KEY (PageInformationId) REFERENCES PageInformation(PageInformationId),
    CONSTRAINT FKPageInformationImageId FOREIGN KEY (ImageId) REFERENCES Image(ImageId)
);
GO

CREATE TABLE RoomTypeFeature (
    RoomTypeFeatureId INT PRIMARY KEY IDENTITY,
    FeatureId INT NOT NULL,
    RoomTypeId INT NOT NULL,
    CONSTRAINT FKRoomTypeFeatureRoomType FOREIGN KEY (RoomTypeId) REFERENCES RoomType(RoomTypeId),
    CONSTRAINT FKRoomTypeFeatureFeature FOREIGN KEY (FeatureId) REFERENCES Feature(FeatureId)
);
GO

CREATE TABLE RoomTypeImage (
    RoomTypeImageId INT PRIMARY KEY IDENTITY,
    ImageId INT NOT NULL,
    RoomTypeId INT NOT NULL,
    CONSTRAINT FKRoomTypeImageRoomType FOREIGN KEY (RoomTypeId) REFERENCES RoomType(RoomTypeId),
    CONSTRAINT FKRoomTypeImageImage FOREIGN KEY (ImageId) REFERENCES Image(ImageId)
);
GO

CREATE TABLE AdvertisingImage (
    AdvertisingImageId INT PRIMARY KEY IDENTITY,
    AdvertisingId INT NOT NULL,
    ImageId INT NOT NULL,
    CONSTRAINT FKAdvertisingImageAdvertising FOREIGN KEY (AdvertisingId) REFERENCES Advertising(AdvertisingId),
    CONSTRAINT FKAdvertisingImageImage FOREIGN KEY (ImageId) REFERENCES Image(ImageId)
);
GO

CREATE TABLE ContactInformation(
    ContactInformationId INT PRIMARY KEY IDENTITY,
    Email NVARCHAR(255) NOT NULL,
    Phone NVARCHAR(50) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    PageId INT NOT NULL,
    CONSTRAINT FKPageContactInformation FOREIGN KEY (PageId) REFERENCES Page(PageId)
);
GO

-- =========================================
-- ANALÍTICA Y PREDICCIÓN
-- =========================================

CREATE TABLE DashboardMetricSnapshots (
    DashboardMetricSnapshotId BIGINT IDENTITY(1,1) PRIMARY KEY,
    SnapshotDate DATE NOT NULL UNIQUE,
    TotalReservations INT NOT NULL DEFAULT 0,
    OccupiedRooms INT NOT NULL DEFAULT 0,
    AvailableRooms INT NOT NULL DEFAULT 0,
    RevenueAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    CancellationCount INT NOT NULL DEFAULT 0,
    AverageOccupancyRate DECIMAL(8,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE ForecastSnapshots (
    ForecastSnapshotId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ForecastDate DATE NOT NULL,
    PredictedReservations INT NOT NULL DEFAULT 0,
    PredictedOccupancyRate DECIMAL(8,2) NOT NULL DEFAULT 0,
    PredictedRevenue DECIMAL(12,2) NOT NULL DEFAULT 0,
    ModelVersion NVARCHAR(100) NULL,
    ConfidenceLevel DECIMAL(8,2) NULL,
    GeneratedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

-- =========================================
-- ADMIN USER
-- =========================================

CREATE OR ALTER PROCEDURE usp_AdminUser_Register
    @FullName NVARCHAR(255),
    @Username NVARCHAR(100),
    @Email NVARCHAR(255),
    @Password NVARCHAR(255),
    @Role NVARCHAR(50) = 'Administrador'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        SET @FullName = LTRIM(RTRIM(@FullName));
        SET @Username = LTRIM(RTRIM(@Username));
        SET @Email = LTRIM(RTRIM(@Email));
        SET @Role = ISNULL(NULLIF(LTRIM(RTRIM(@Role)), ''), 'Administrador');

        IF @FullName IS NULL OR @FullName = ''
            THROW 50010, 'El nombre completo es obligatorio.', 1;

        IF @Username IS NULL OR @Username = ''
            THROW 50011, 'El nombre de usuario es obligatorio.', 1;

        IF @Email IS NULL OR @Email = ''
            THROW 50012, 'El correo es obligatorio.', 1;

        IF @Password IS NULL OR LEN(@Password) < 8
            THROW 50013, 'La contrasena debe tener al menos 8 caracteres.', 1;

        IF EXISTS (
            SELECT 1
            FROM AdminUser
            WHERE LOWER(Username) = LOWER(@Username)
        )
            THROW 50014, 'El nombre de usuario ya existe.', 1;

        IF EXISTS (
            SELECT 1
            FROM AdminUser
            WHERE LOWER(Email) = LOWER(@Email)
        )
            THROW 50015, 'El correo ya existe.', 1;

        DECLARE @PasswordSalt VARBINARY(32) = CRYPT_GEN_RANDOM(32);
        DECLARE @PasswordHash VARBINARY(64) =
            HASHBYTES('SHA2_512', @PasswordSalt + CONVERT(VARBINARY(4000), @Password));

        INSERT INTO AdminUser (
            FullName,
            Username,
            Email,
            PasswordHash,
            PasswordSalt,
            Role,
            IsActive
        )
        VALUES (
            @FullName,
            @Username,
            @Email,
            @PasswordHash,
            @PasswordSalt,
            @Role,
            1
        );

        SELECT
            AdminUserId,
            FullName,
            Username,
            Email,
            Role,
            IsActive,
            CreatedAt,
            LastLoginAt
        FROM AdminUser
        WHERE AdminUserId = CAST(SCOPE_IDENTITY() AS INT);

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_AdminUser_Login
    @Username NVARCHAR(100),
    @Password NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @AdminUserId INT;
    DECLARE @StoredHash VARBINARY(64);
    DECLARE @StoredSalt VARBINARY(32);
    DECLARE @ComputedHash VARBINARY(64);

    SELECT TOP 1
        @AdminUserId = AdminUserId,
        @StoredHash = PasswordHash,
        @StoredSalt = PasswordSalt
    FROM AdminUser
    WHERE LOWER(Username) = LOWER(LTRIM(RTRIM(@Username)))
      AND IsActive = 1;

    IF @AdminUserId IS NULL
        RETURN;

    SET @ComputedHash = HASHBYTES('SHA2_512', @StoredSalt + CONVERT(VARBINARY(4000), @Password));

    IF @ComputedHash <> @StoredHash
        RETURN;

    UPDATE AdminUser
    SET LastLoginAt = SYSDATETIME()
    WHERE AdminUserId = @AdminUserId;

    SELECT
        AdminUserId,
        FullName,
        Username,
        Email,
        Role,
        IsActive,
        CreatedAt,
        LastLoginAt
    FROM AdminUser
    WHERE AdminUserId = @AdminUserId;
END;
GO

CREATE OR ALTER PROCEDURE usp_AdminUser_GetById
    @AdminUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AdminUserId,
        FullName,
        Username,
        Email,
        Role,
        IsActive,
        CreatedAt,
        LastLoginAt
    FROM AdminUser
    WHERE AdminUserId = @AdminUserId
      AND IsActive = 1;
END;
GO

EXEC usp_AdminUser_Register
    @FullName = 'Gabriela Solano',
    @Username = 'admin.purrujas',
    @Email = 'admin@laspurrujas.local',
    @Password = 'Purrujas2026!',
    @Role = 'Administrador';
GO

EXEC usp_AdminUser_Register
    @FullName = 'Carlos Mora',
    @Username = 'recepcion.purrujas',
    @Email = 'recepcion@laspurrujas.local',
    @Password = 'Recepcion2026!',
    @Role = 'Supervisor';
GO

-- =========================================
-- CUSTOMER
-- =========================================

CREATE OR ALTER PROCEDURE usp_Customer_Create
    @Name NVARCHAR(255),
    @LastName NVARCHAR(255),
    @Email NVARCHAR(255),
    @Phone NVARCHAR(30) = NULL,
    @CreditCard NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF EXISTS (SELECT 1 FROM Customer WHERE Email = @Email AND IsActive = 1)
            THROW 50001, 'El correo ya existe.', 1;

        INSERT INTO Customer (Name, LastName, Email, Phone, CreditCard, IsActive)
        VALUES (@Name, @LastName, @Email, @Phone, @CreditCard, 1);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewCustomerId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Customer_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CustomerId, Name, LastName, Email, Phone, CreditCard, IsActive
    FROM Customer
    WHERE IsActive = 1
    ORDER BY CustomerId DESC;
END;
GO

CREATE OR ALTER PROCEDURE usp_Customer_GetById
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CustomerId, Name, LastName, Email, Phone, CreditCard, IsActive
    FROM Customer
    WHERE CustomerId = @CustomerId
      AND IsActive = 1;
END;
GO

CREATE OR ALTER PROCEDURE usp_Customer_Update
    @CustomerId INT,
    @Name NVARCHAR(255),
    @LastName NVARCHAR(255),
    @Email NVARCHAR(255),
    @Phone NVARCHAR(30) = NULL,
    @CreditCard NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM Customer WHERE CustomerId = @CustomerId AND IsActive = 1)
            THROW 50002, 'Cliente no encontrado.', 1;

        IF EXISTS (
            SELECT 1
            FROM Customer
            WHERE Email = @Email
              AND CustomerId <> @CustomerId
              AND IsActive = 1
        )
            THROW 50003, 'El correo ya está registrado por otro cliente.', 1;

        UPDATE Customer
        SET Name = @Name,
            LastName = @LastName,
            Email = @Email,
            Phone = @Phone,
            CreditCard = @CreditCard
        WHERE CustomerId = @CustomerId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Customer_Delete
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE Customer
        SET IsActive = 0
        WHERE CustomerId = @CustomerId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- =========================================
-- ROOM TYPE
-- =========================================

CREATE OR ALTER PROCEDURE usp_RoomType_Create
    @Name NVARCHAR(255),
    @BasePrice DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        INSERT INTO RoomType (Name, BasePrice, IsActive)
        VALUES (@Name, @BasePrice, 1);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewRoomTypeId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_RoomType_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RoomTypeId, Name, BasePrice, IsActive
    FROM RoomType
    WHERE IsActive = 1
    ORDER BY RoomTypeId DESC;
END;
GO

CREATE OR ALTER PROCEDURE usp_RoomType_GetById
    @RoomTypeId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RoomTypeId, Name, BasePrice, IsActive
    FROM RoomType
    WHERE RoomTypeId = @RoomTypeId
      AND IsActive = 1;
END;
GO

CREATE OR ALTER PROCEDURE usp_RoomType_Update
    @RoomTypeId INT,
    @Name NVARCHAR(255),
    @BasePrice DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM RoomType WHERE RoomTypeId = @RoomTypeId AND IsActive = 1)
            THROW 50004, 'Tipo de habitación no encontrado.', 1;

        UPDATE RoomType
        SET Name = @Name,
            BasePrice = @BasePrice
        WHERE RoomTypeId = @RoomTypeId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_RoomType_Delete
    @RoomTypeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE RoomType
        SET IsActive = 0
        WHERE RoomTypeId = @RoomTypeId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- =========================================
-- ROOM STATUS
-- =========================================

CREATE OR ALTER PROCEDURE usp_RoomStatus_Create
    @Name NVARCHAR(100),
    @Description NVARCHAR(255),
    @IsAvailableForBooking BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        INSERT INTO RoomStatus (Name, Description, IsAvailableForBooking)
        VALUES (@Name, @Description, @IsAvailableForBooking);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewRoomStatusId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_RoomStatus_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RoomStatusId, Name, Description, IsAvailableForBooking
    FROM RoomStatus
    ORDER BY RoomStatusId DESC;
END;
GO

CREATE OR ALTER PROCEDURE usp_RoomStatus_GetById
    @RoomStatusId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RoomStatusId, Name, Description, IsAvailableForBooking
    FROM RoomStatus
    WHERE RoomStatusId = @RoomStatusId;
END;
GO

CREATE OR ALTER PROCEDURE usp_RoomStatus_Update
    @RoomStatusId INT,
    @Name NVARCHAR(100),
    @Description NVARCHAR(255),
    @IsAvailableForBooking BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE RoomStatus
        SET Name = @Name,
            Description = @Description,
            IsAvailableForBooking = @IsAvailableForBooking
        WHERE RoomStatusId = @RoomStatusId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_RoomStatus_Delete
    @RoomStatusId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        DELETE FROM RoomStatus
        WHERE RoomStatusId = @RoomStatusId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- =========================================
-- ROOM
-- =========================================

CREATE OR ALTER PROCEDURE usp_Room_Create
    @RoomNumber NVARCHAR(50),
    @RoomTypeId INT,
    @RoomStatusId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF EXISTS (SELECT 1 FROM Room WHERE RoomNumber = @RoomNumber AND IsActive = 1)
            THROW 50005, 'El número de habitación ya existe.', 1;

        INSERT INTO Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId)
        VALUES (@RoomNumber, 1, @RoomTypeId, @RoomStatusId);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewRoomId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Room_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        r.RoomId,
        r.RoomNumber,
        r.IsActive,
        r.RoomTypeId,
        rt.Name AS RoomTypeName,
        r.RoomStatusId,
        rs.Name AS RoomStatusName
    FROM Room r
    INNER JOIN RoomType rt ON r.RoomTypeId = rt.RoomTypeId
    INNER JOIN RoomStatus rs ON r.RoomStatusId = rs.RoomStatusId
    WHERE r.IsActive = 1
      AND rt.IsActive = 1
    ORDER BY r.RoomId DESC;
END;
GO

CREATE OR ALTER PROCEDURE usp_Room_GetById
    @RoomId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        r.RoomId,
        r.RoomNumber,
        r.IsActive,
        r.RoomTypeId,
        rt.Name AS RoomTypeName,
        r.RoomStatusId,
        rs.Name AS RoomStatusName
    FROM Room r
    INNER JOIN RoomType rt ON r.RoomTypeId = rt.RoomTypeId
    INNER JOIN RoomStatus rs ON r.RoomStatusId = rs.RoomStatusId
    WHERE r.RoomId = @RoomId
      AND r.IsActive = 1
      AND rt.IsActive = 1;
END;
GO

CREATE OR ALTER PROCEDURE usp_Room_Update
    @RoomId INT,
    @RoomNumber NVARCHAR(50),
    @RoomTypeId INT,
    @RoomStatusId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM Room WHERE RoomId = @RoomId AND IsActive = 1)
            THROW 50006, 'Habitación no encontrada.', 1;

        IF EXISTS (
            SELECT 1
            FROM Room
            WHERE RoomNumber = @RoomNumber
              AND RoomId <> @RoomId
              AND IsActive = 1
        )
            THROW 50007, 'El número de habitación ya está registrado.', 1;

        UPDATE Room
        SET RoomNumber = @RoomNumber,
            RoomTypeId = @RoomTypeId,
            RoomStatusId = @RoomStatusId
        WHERE RoomId = @RoomId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Room_Delete
    @RoomId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE Room
        SET IsActive = 0
        WHERE RoomId = @RoomId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- =========================================
-- RESERVATION STATUS
-- =========================================

CREATE OR ALTER PROCEDURE usp_ReservationStatus_Create
    @Name NVARCHAR(100),
    @Description NVARCHAR(255),
    @IsFinal BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        INSERT INTO ReservationStatus (Name, Description, IsFinal)
        VALUES (@Name, @Description, @IsFinal);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewReservationStatusId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_ReservationStatus_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ReservationStatusId, Name, Description, IsFinal
    FROM ReservationStatus
    ORDER BY ReservationStatusId DESC;
END;
GO

CREATE OR ALTER PROCEDURE usp_ReservationStatus_GetById
    @ReservationStatusId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ReservationStatusId, Name, Description, IsFinal
    FROM ReservationStatus
    WHERE ReservationStatusId = @ReservationStatusId;
END;
GO

CREATE OR ALTER PROCEDURE usp_ReservationStatus_Update
    @ReservationStatusId INT,
    @Name NVARCHAR(100),
    @Description NVARCHAR(255),
    @IsFinal BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE ReservationStatus
        SET Name = @Name,
            Description = @Description,
            IsFinal = @IsFinal
        WHERE ReservationStatusId = @ReservationStatusId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_ReservationStatus_Delete
    @ReservationStatusId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        DELETE FROM ReservationStatus
        WHERE ReservationStatusId = @ReservationStatusId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- =========================================
-- RESERVATION
-- =========================================

CREATE OR ALTER PROCEDURE usp_Reservation_Create
    @ReservationDate DATETIME,
    @StartDate DATETIME,
    @EndDate DATETIME,
    @CustomerId INT,
    @RoomId INT,
    @ReservationStatusId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF @EndDate <= @StartDate
            THROW 50008, 'La fecha final debe ser mayor que la fecha inicial.', 1;

        IF NOT EXISTS (SELECT 1 FROM Customer WHERE CustomerId = @CustomerId AND IsActive = 1)
            THROW 50009, 'Cliente no encontrado.', 1;

        IF NOT EXISTS (SELECT 1 FROM Room WHERE RoomId = @RoomId AND IsActive = 1)
            THROW 50010, 'Habitación no encontrada.', 1;

        IF EXISTS (
            SELECT 1
            FROM Reservation
            WHERE RoomId = @RoomId
              AND IsActive = 1
              AND ReservationStatusId IN (
                    SELECT ReservationStatusId
                    FROM ReservationStatus
                    WHERE Name NOT IN ('Cancelada', 'Finalizada')
              )
              AND (@StartDate < EndDate AND @EndDate > StartDate)
        )
            THROW 50011, 'La habitación ya está reservada en ese rango de fechas.', 1;

        INSERT INTO Reservation
        (
            ReservationDate,
            StartDate,
            EndDate,
            CustomerId,
            RoomId,
            ReservationStatusId,
            IsActive
        )
        VALUES
        (
            @ReservationDate,
            @StartDate,
            @EndDate,
            @CustomerId,
            @RoomId,
            @ReservationStatusId,
            1
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewReservationId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Reservation_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.ReservationId,
        r.ReservationDate,
        r.StartDate,
        r.EndDate,
        r.CustomerId,
        c.Name + ' ' + c.LastName AS CustomerName,
        r.RoomId,
        rm.RoomNumber,
        r.ReservationStatusId,
        rs.Name AS ReservationStatusName,
        r.IsActive
    FROM Reservation r
    INNER JOIN Customer c ON r.CustomerId = c.CustomerId
    INNER JOIN Room rm ON r.RoomId = rm.RoomId
    INNER JOIN ReservationStatus rs ON r.ReservationStatusId = rs.ReservationStatusId
    WHERE r.IsActive = 1
    ORDER BY r.ReservationId DESC;
END;
GO

CREATE OR ALTER PROCEDURE usp_Reservation_GetById
    @ReservationId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.ReservationId,
        r.ReservationDate,
        r.StartDate,
        r.EndDate,
        r.CustomerId,
        c.Name + ' ' + c.LastName AS CustomerName,
        r.RoomId,
        rm.RoomNumber,
        r.ReservationStatusId,
        rs.Name AS ReservationStatusName,
        r.IsActive
    FROM Reservation r
    INNER JOIN Customer c ON r.CustomerId = c.CustomerId
    INNER JOIN Room rm ON r.RoomId = rm.RoomId
    INNER JOIN ReservationStatus rs ON r.ReservationStatusId = rs.ReservationStatusId
    WHERE r.ReservationId = @ReservationId
      AND r.IsActive = 1;
END;
GO

CREATE OR ALTER PROCEDURE usp_Reservation_Update
    @ReservationId INT,
    @ReservationDate DATETIME,
    @StartDate DATETIME,
    @EndDate DATETIME,
    @CustomerId INT,
    @RoomId INT,
    @ReservationStatusId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF @EndDate <= @StartDate
            THROW 50012, 'La fecha final debe ser mayor que la fecha inicial.', 1;

        IF NOT EXISTS (SELECT 1 FROM Reservation WHERE ReservationId = @ReservationId AND IsActive = 1)
            THROW 50013, 'Reserva no encontrada.', 1;

        IF EXISTS (
            SELECT 1
            FROM Reservation
            WHERE RoomId = @RoomId
              AND ReservationId <> @ReservationId
              AND IsActive = 1
              AND (@StartDate < EndDate AND @EndDate > StartDate)
        )
            THROW 50014, 'Existe conflicto con otra reserva para esa habitación.', 1;

        UPDATE Reservation
        SET ReservationDate = @ReservationDate,
            StartDate = @StartDate,
            EndDate = @EndDate,
            CustomerId = @CustomerId,
            RoomId = @RoomId,
            ReservationStatusId = @ReservationStatusId
        WHERE ReservationId = @ReservationId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Reservation_Delete
    @ReservationId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE Reservation
        SET IsActive = 0
        WHERE ReservationId = @ReservationId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- =========================================
-- SEASON
-- =========================================

CREATE OR ALTER PROCEDURE usp_Season_Create
    @Name NVARCHAR(255),
    @PercentageChange INT,
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF @EndDate < @StartDate
            THROW 50030, 'La fecha final de la temporada debe ser mayor o igual a la inicial.', 1;

        INSERT INTO Season (Name, PercentageChange, StartDate, EndDate, IsActive)
        VALUES (@Name, @PercentageChange, @StartDate, @EndDate, 1);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewSeasonId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Season_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SeasonId, Name, PercentageChange, StartDate, EndDate, IsActive
    FROM Season
    WHERE IsActive = 1
    ORDER BY StartDate ASC, SeasonId DESC;
END;
GO

CREATE OR ALTER PROCEDURE usp_Season_GetById
    @SeasonId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SeasonId, Name, PercentageChange, StartDate, EndDate, IsActive
    FROM Season
    WHERE SeasonId = @SeasonId
      AND IsActive = 1;
END;
GO

CREATE OR ALTER PROCEDURE usp_Season_Update
    @SeasonId INT,
    @Name NVARCHAR(255),
    @PercentageChange INT,
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM Season WHERE SeasonId = @SeasonId AND IsActive = 1)
            THROW 50015, 'Temporada no encontrada.', 1;

        IF @EndDate < @StartDate
            THROW 50031, 'La fecha final de la temporada debe ser mayor o igual a la inicial.', 1;

        UPDATE Season
        SET Name = @Name,
            PercentageChange = @PercentageChange,
            StartDate = @StartDate,
            EndDate = @EndDate
        WHERE SeasonId = @SeasonId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Season_Delete
    @SeasonId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE Season
        SET IsActive = 0
        WHERE SeasonId = @SeasonId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- =========================================
-- PROMOTION
-- =========================================

CREATE OR ALTER PROCEDURE usp_Promotion_Create
    @Name NVARCHAR(255),
    @Discount INT,
    @StartDate DATETIME,
    @EndDate DATETIME,
    @RoomTypeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF @EndDate <= @StartDate
            THROW 50016, 'La fecha final de la promoción debe ser mayor a la inicial.', 1;

        INSERT INTO Promotion (Name, Discount, StartDate, EndDate, RoomTypeId, IsActive)
        VALUES (@Name, @Discount, @StartDate, @EndDate, @RoomTypeId, 1);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewPromotionId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Promotion_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.PromotionId,
        p.Name,
        p.Discount,
        p.StartDate,
        p.EndDate,
        p.RoomTypeId,
        rt.Name AS RoomTypeName,
        p.IsActive
    FROM Promotion p
    INNER JOIN RoomType rt ON p.RoomTypeId = rt.RoomTypeId
    WHERE p.IsActive = 1
      AND rt.IsActive = 1
    ORDER BY p.PromotionId DESC;
END;
GO

CREATE OR ALTER PROCEDURE usp_Promotion_GetById
    @PromotionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.PromotionId,
        p.Name,
        p.Discount,
        p.StartDate,
        p.EndDate,
        p.RoomTypeId,
        rt.Name AS RoomTypeName,
        p.IsActive
    FROM Promotion p
    INNER JOIN RoomType rt ON p.RoomTypeId = rt.RoomTypeId
    WHERE p.PromotionId = @PromotionId
      AND p.IsActive = 1
      AND rt.IsActive = 1;
END;
GO

CREATE OR ALTER PROCEDURE usp_Promotion_Update
    @PromotionId INT,
    @Name NVARCHAR(255),
    @Discount INT,
    @StartDate DATETIME,
    @EndDate DATETIME,
    @RoomTypeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF @EndDate <= @StartDate
            THROW 50017, 'La fecha final de la promoción debe ser mayor a la inicial.', 1;

        IF NOT EXISTS (SELECT 1 FROM Promotion WHERE PromotionId = @PromotionId AND IsActive = 1)
            THROW 50018, 'Promoción no encontrada.', 1;

        UPDATE Promotion
        SET Name = @Name,
            Discount = @Discount,
            StartDate = @StartDate,
            EndDate = @EndDate,
            RoomTypeId = @RoomTypeId
        WHERE PromotionId = @PromotionId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Promotion_Delete
    @PromotionId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE Promotion
        SET IsActive = 0
        WHERE PromotionId = @PromotionId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- =========================================
-- PAYMENT
-- =========================================

CREATE OR ALTER PROCEDURE usp_Payment_Create
    @ReservationId INT,
    @Amount DECIMAL(10,2),
    @PaymentMethod NVARCHAR(50),
    @PaymentDate DATETIME,
    @IsPaid BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF EXISTS (SELECT 1 FROM Payment WHERE ReservationId = @ReservationId AND IsActive = 1)
            THROW 50019, 'Ya existe un pago registrado para esa reserva.', 1;

        INSERT INTO Payment (ReservationId, Amount, PaymentMethod, PaymentDate, IsPaid, IsActive)
        VALUES (@ReservationId, @Amount, @PaymentMethod, @PaymentDate, @IsPaid, 1);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewPaymentId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Payment_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PaymentId,
        ReservationId,
        Amount,
        PaymentMethod,
        PaymentDate,
        IsPaid,
        IsActive
    FROM Payment
    WHERE IsActive = 1
    ORDER BY PaymentId DESC;
END;
GO

CREATE OR ALTER PROCEDURE usp_Payment_GetById
    @PaymentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PaymentId,
        ReservationId,
        Amount,
        PaymentMethod,
        PaymentDate,
        IsPaid,
        IsActive
    FROM Payment
    WHERE PaymentId = @PaymentId
      AND IsActive = 1;
END;
GO

CREATE OR ALTER PROCEDURE usp_Payment_Update
    @PaymentId INT,
    @ReservationId INT,
    @Amount DECIMAL(10,2),
    @PaymentMethod NVARCHAR(50),
    @PaymentDate DATETIME,
    @IsPaid BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM Payment WHERE PaymentId = @PaymentId AND IsActive = 1)
            THROW 50020, 'Pago no encontrado.', 1;

        IF EXISTS (
            SELECT 1
            FROM Payment
            WHERE ReservationId = @ReservationId
              AND PaymentId <> @PaymentId
              AND IsActive = 1
        )
            THROW 50021, 'Otra fila de pago ya usa esa reserva.', 1;

        UPDATE Payment
        SET ReservationId = @ReservationId,
            Amount = @Amount,
            PaymentMethod = @PaymentMethod,
            PaymentDate = @PaymentDate,
            IsPaid = @IsPaid
        WHERE PaymentId = @PaymentId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE usp_Payment_Delete
    @PaymentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE Payment
        SET IsActive = 0
        WHERE PaymentId = @PaymentId
          AND IsActive = 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- =========================================
-- FACILITIES PAGE CONTENT
-- =========================================

CREATE OR ALTER PROCEDURE usp_FacilitiesPageContent_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        p.Title,
        pi.Subtitle,
        pi.Description
    FROM Page p
    LEFT JOIN PageInformation pi ON pi.PageId = p.PageId
    WHERE LOWER(p.Name) = LOWER(N'Facilidades')
    ORDER BY pi.PageInformationId;
END;
GO

CREATE OR ALTER PROCEDURE usp_FacilitiesPageContent_Upsert
    @SectionTitle NVARCHAR(255),
    @SectionTag NVARCHAR(255),
    @DescriptionJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        DECLARE @PageId INT;
        DECLARE @PageInformationId INT;

        SELECT TOP 1 @PageId = PageId
        FROM Page
        WHERE LOWER(Name) = LOWER(N'Facilidades')
        ORDER BY PageId;

        IF @PageId IS NULL
        BEGIN
            INSERT INTO Page (Name, Title, Link)
            VALUES (N'Facilidades', @SectionTitle, N'/about-us#facilidades');

            SET @PageId = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE Page
            SET Title = @SectionTitle,
                Link = N'/about-us#facilidades'
            WHERE PageId = @PageId;
        END

        SELECT TOP 1 @PageInformationId = PageInformationId
        FROM PageInformation
        WHERE PageId = @PageId
        ORDER BY PageInformationId;

        IF @PageInformationId IS NULL
        BEGIN
            INSERT INTO PageInformation (Subtitle, Description, PageId)
            VALUES (@SectionTag, @DescriptionJson, @PageId);
        END
        ELSE
        BEGIN
            UPDATE PageInformation
            SET Subtitle = @SectionTag,
                Description = @DescriptionJson
            WHERE PageInformationId = @PageInformationId;
        END

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- =========================================
-- DATOS SEMILLA PARA PRUEBAS
-- =========================================

DECLARE @FacilitiesContentJson NVARCHAR(MAX) = N'{"sectionTag":"Lo que nos distingue","sectionTitle":"Caracteristicas Principales","highlightTitle":"Ubicacion Privilegiada","highlightDescription":"Situado a solo 45 minutos de San Jose, en las verdes montanas de Cartago, el hotel ofrece vistas panoramicas al Volcan Turrialba y esta rodeado de bosques nubosos y rios cristalinos. Una combinacion unica de accesibilidad y tranquilidad absoluta.","primaryListTitle":"Instalaciones","primaryListItems":["18 habitaciones tematicas","Restaurante ""La Ceiba""","Piscina natural de manantial","Senderos ecologicos (5 km)","Salon de eventos","Spa con plantas locales"],"secondaryListTitle":"Servicios Destacados","secondaryListItems":["Tours al Volcan Turrialba e Irazu","Birdwatching con guias certificados","Talleres de gastronomia tipica","Transporte desde San Jose","Wi-Fi de alta velocidad","Atencion personalizada 24/7"],"serviceCards":[{"title":"18 habitaciones tematicas","description":"Ambientes con personalidad propia, balcones al bosque nuboso y textiles artesanales inspirados en Cartago."},{"title":"Restaurante ""La Ceiba""","description":"Cocina de finca a la mesa, cafe chorreado y menus de temporada que celebran los sabores locales."},{"title":"Piscina natural de manantial","description":"Agua cristalina, temperatura agradable y vistas verdes para recargar energia de forma natural."},{"title":"Senderos ecologicos (5 km)","description":"Rutas senalizadas entre bosque nuboso, ideales para caminatas al amanecer y observacion de flora."},{"title":"Salon de eventos","description":"Espacio versatil con luz natural, perfecto para retiros corporativos, bodas boutique y talleres."},{"title":"Spa con plantas locales","description":"Tratamientos herbales, masajes relajantes y aromaterapia con esencias del bosque costarricense."},{"title":"Tours al Volcan Turrialba e Irazu","description":"Excursiones guiadas para explorar dos volcanes iconicos con logistica y transporte incluidos."},{"title":"Birdwatching con guias certificados","description":"Avistamiento de purrujas y mas de 120 especies con especialistas locales y equipo optico."},{"title":"Talleres de gastronomia tipica","description":"Aprende a preparar tortillas palmeadas, gallo pinto y salsas caseras con cocineras de la zona."},{"title":"Transporte desde San Jose","description":"Traslados seguros puerta a puerta para que llegues sin preocupaciones desde el aeropuerto o la ciudad."},{"title":"Wi-Fi de alta velocidad","description":"Conectividad confiable en habitaciones y areas comunes para trabajar o compartir tu experiencia."},{"title":"Atencion personalizada 24/7","description":"Equipo disponible todo el dia para ayudarte con reservas, recomendaciones y soporte durante tu estadia."}]}';

EXEC usp_FacilitiesPageContent_Upsert
    @SectionTitle = N'Caracteristicas Principales',
    @SectionTag = N'Lo que nos distingue',
    @DescriptionJson = @FacilitiesContentJson;
GO


INSERT INTO RoomType (Name, BasePrice, IsActive)
VALUES
    ('Habitación Doble', 95.00, 1),
    ('Suite Volcán', 135.00, 1),
    ('Villa Familiar', 180.00, 1);
GO

INSERT INTO RoomStatus (Name, Description, IsAvailableForBooking)
VALUES
    ('Disponible', 'Habitación lista para reservar.', 1),
    ('Limpieza', 'Habitación en proceso de limpieza.', 0),
    ('Mantenimiento', 'Habitación fuera de servicio temporalmente.', 0);
GO

INSERT INTO Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId)
VALUES
    ('101', 1, 1, 1),
    ('102', 1, 1, 1),
    ('201', 1, 2, 1),
    ('202', 1, 2, 1),
    ('301', 1, 3, 1);
GO

INSERT INTO ReservationStatus (Name, Description, IsFinal)
VALUES
    ('Pendiente', 'Reserva creada y pendiente de confirmación.', 0),
    ('Confirmada', 'Reserva confirmada por el hotel.', 0),
    ('Finalizada', 'La estadía finalizó.', 1),
    ('Cancelada', 'Reserva cancelada.', 1);
GO

INSERT INTO Customer (Name, LastName, Email, Phone, CreditCard, IsActive)
VALUES
    ('María', 'Jiménez', 'maria.jimenez@demo.com', '8888-1111', '4111111111111111', 1),
    ('Carlos', 'Rodríguez', 'carlos.rodriguez@demo.com', '8888-2222', '5555555555554444', 1);
GO

INSERT INTO Season (Name, PercentageChange, StartDate, EndDate, IsActive)
VALUES
    ('Temporada alta inicio de año 2026', 25, '2026-01-01', '2026-01-31', 1),
    ('Semana Santa 2026', 35, '2026-03-29', '2026-04-05', 1),
    ('Vacaciones de medio año 2026', 25, '2026-07-01', '2026-08-31', 1),
    ('Temporada alta fin de año 2026', 30, '2026-12-01', '2026-12-31', 1);
GO

INSERT INTO Promotion (Name, Discount, StartDate, EndDate, RoomTypeId, IsActive)
VALUES
    ('Escapada Romántica', 25, '2026-04-01', '2026-05-31', 2, 1),
    ('Semana Ecológica', 20, '2026-04-15', '2026-06-30', 1, 1),
    ('Aventura Familiar', 30, '2026-05-01', '2026-07-15', 3, 1);
GO

INSERT INTO Reservation
(
    ReservationDate,
    StartDate,
    EndDate,
    CustomerId,
    RoomId,
    ReservationStatusId,
    IsActive
)
VALUES
    ('2026-04-10T09:00:00', '2026-07-10T15:00:00', '2026-07-13T12:00:00', 1, 1, 2, 1),
    ('2026-04-11T10:30:00', '2026-04-01T15:00:00', '2026-04-04T12:00:00', 2, 5, 2, 1);
GO

INSERT INTO Bill (ReservationId, BasePrice, Discount, SeasonAmount)
VALUES
    (1, 285.00, 0.00, 71.25),
    (2, 540.00, 0.00, 189.00);
GO

INSERT INTO Payment (ReservationId, Amount, PaymentMethod, PaymentDate, IsPaid, IsActive)
VALUES
    (1, 356.25, 'Tarjeta', '2026-04-10T09:15:00', 1, 1),
    (2, 729.00, 'Tarjeta', '2026-04-11T10:45:00', 1, 1);
GO


-- =========================================
-- GETTING THERE PAGE CONTENT
-- =========================================

CREATE OR ALTER PROCEDURE usp_GettingTherePageContent_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        p.Title,
        pi.Subtitle,
        pi.Description
    FROM Page p
    LEFT JOIN PageInformation pi ON pi.PageId = p.PageId
    WHERE LOWER(p.Name) = LOWER(N'Como llegar')
    ORDER BY pi.PageInformationId;
END;
GO

CREATE OR ALTER PROCEDURE usp_GettingTherePageContent_Upsert
    @SectionTitle NVARCHAR(255),
    @SectionTag NVARCHAR(255),
    @DescriptionJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        DECLARE @PageId INT;
        DECLARE @PageInformationId INT;

        SELECT TOP 1 @PageId = PageId
        FROM Page
        WHERE LOWER(Name) = LOWER(N'Como llegar')
        ORDER BY PageId;

        IF @PageId IS NULL
        BEGIN
            INSERT INTO Page (Name, Title, Link)
            VALUES (N'Como llegar', @SectionTitle, N'/about-us#como-llegar');

            SET @PageId = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE Page
            SET Title = @SectionTitle,
                Link = N'/about-us#como-llegar'
            WHERE PageId = @PageId;
        END

        SELECT TOP 1 @PageInformationId = PageInformationId
        FROM PageInformation
        WHERE PageId = @PageId
        ORDER BY PageInformationId;

        IF @PageInformationId IS NULL
        BEGIN
            INSERT INTO PageInformation (Subtitle, Description, PageId)
            VALUES (@SectionTag, @DescriptionJson, @PageId);
        END
        ELSE
        BEGIN
            UPDATE PageInformation
            SET Subtitle = @SectionTag,
                Description = @DescriptionJson
            WHERE PageInformationId = @PageInformationId;
        END

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

DECLARE @GettingThereContentJson NVARCHAR(MAX) = N'{"sectionTag":"Visítanos","sectionTitle":"¿Cómo llegar?","sectionSubtext":"A 45 minutos de San José, en las faldas del Volcán Turrialba.","coordinatesTitle":"Coordenadas","coordinatesDescription":"9.975878207007307° N,83.770258333651° W · Las Purrujas, Cartago.","directionsItems":["Ruta 32 hasta Turrialba, luego desvío a La Pastora.","Transporte privado disponible desde el aeropuerto SJO.","Parqueo gratuito y seguro dentro de la propiedad."],"mapButtonLabel":"Abrir en Google Maps"}';

EXEC usp_GettingTherePageContent_Upsert
    @SectionTitle = N'¿Cómo llegar?',
    @SectionTag = N'Visítanos',
    @DescriptionJson = @GettingThereContentJson;
GO
