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

select* from AdminUser

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
select * from season
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
            THROW 50013, 'La contraseña debe tener al menos 8 caracteres.', 1;

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
            VALUES (N'Facilidades', @SectionTitle, N'/#facilidades');

            SET @PageId = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE Page
            SET Title = @SectionTitle,
                Link = N'/#facilidades'
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

DECLARE @FacilitiesContentJson NVARCHAR(MAX) = N'{
    "sectionTag":"Lo que nos distingue",
    "sectionTitle":"Características Principales",
    "highlightTitle":"Ubicación Privilegiada",
    "highlightDescription":"Situado a 2 horas de San José, en las verdes montañas de Cartago, el hotel ofrece vistas panorámicas al Volcán Turrialba y está rodeado de bosques nubosos 
                            y ríos cristalinos. Una combinación única de accesibilidad y tranquilidad absoluta.",
    
    "primaryListTitle":"Instalaciones",
    "primaryListItems":[
        "18 habitaciones temáticas",
        "Restaurante ""La Ceiba""",
        "Piscina natural de manantial",
        "Senderos ecológicos (5 km)",
        "Salón de eventos",
        "Spa con plantas locales"
    ],

    "secondaryListTitle":"Servicios Destacados",
    "secondaryListItems":[
        "Tours al Volcán Turrialba e Irazú",
        "Birdwatching con guías certificados",
        "Talleres de gastronomía típica",
        "Transporte desde San José",
        "Wi-Fi de alta velocidad",
        "Atención personalizada 24/7"
    ],

    "serviceCards":[
        {
            "title":"18 habitaciones temáticas",
            "description":"Ambientes con personalidad propia, balcones al bosque nuboso y textiles artesanales inspirados en Cartago."
        },
        {
            "title":"Restaurante ""La Ceiba""",
            "description":"Cocina de finca a la mesa, café chorreado y menús de temporada que celebran los sabores locales."
        },
        {
            "title":"Piscina natural de manantial",
            "description":"Agua cristalina, temperatura agradable y vistas verdes para recargar energía de forma natural."
        },
        {
            "title":"Senderos ecológicos (5 km)",
            "description":"Rutas señalizadas entre bosque nuboso, ideales para caminatas al amanecer y observación de flora."
        },
        {
            "title":"Salón de eventos",
            "description":"Espacio versátil con luz natural, perfecto para retiros corporativos, bodas boutique y talleres."
        },
        {
            "title":"Spa con plantas locales",
            "description":"Tratamientos herbales, masajes relajantes y aromaterapia con esencias del bosque costarricense."
        },
        {
            "title":"Tours al Volcán Turrialba e Irazú",
            "description":"Excursiones guiadas para explorar dos volcanes icónicos con logística y transporte incluidos."
        },
        {
            "title":"Birdwatching con guías certificados",
            "description":"Avistamiento de purrujas y más de 120 especies con especialistas locales y equipo óptico."
        },
        {
            "title":"Talleres de gastronomía típica",
            "description":"Aprende a preparar tortillas palmeadas, gallo pinto y salsas caseras con cocineras de la zona."
        },
        {
            "title":"Transporte desde San José",
            "description":"Traslados seguros puerta a puerta para que llegues sin preocupaciones desde el aeropuerto o la ciudad."
        },
        {
            "title":"Wi-Fi de alta velocidad",
            "description":"Conectividad confiable en habitaciones y áreas comunes para trabajar o compartir tu experiencia."
        },
        {
            "title":"Atención personalizada 24/7",
            "description":"Equipo disponible todo el día para ayudarte con reservas, recomendaciones y soporte durante tu estadía."
        }
    ]
}';

EXEC usp_FacilitiesPageContent_Upsert
    @SectionTitle = N'Características Principales',
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

--Patches
USE Ingenieria_Purrujas_BD;
GO

-- Non-destructive patch for Facilities content persistence.

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
            VALUES (N'Facilidades', @SectionTitle, N'/#facilidades');

            SET @PageId = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE Page
            SET Title = @SectionTitle,
                Link = N'/#facilidades'
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

DECLARE @FacilitiesContentJson NVARCHAR(MAX) = N'
{
    "sectionTag":"Lo que nos distingue",
    "sectionTitle":"Características Principales",
    "highlightTitle":"Ubicación Privilegiada",
    "highlightDescription":"Situado a solo 45 minutos de San José, en las verdes montañas de Cartago, el hotel ofrece vistas panorámicas al Volcán Turrialba y está rodeado de bosques nubosos y ríos cristalinos. Una combinación única de accesibilidad y tranquilidad absoluta.",

    "primaryListTitle":"Instalaciones",
    "primaryListItems":
    [
        "18 habitaciones temáticas",
        "Restaurante ""La Ceiba""",
        "Piscina natural de manantial",
        "Senderos ecológicos (5 km)",
        "Salón de eventos",
        "Spa con plantas locales"
    ],

    "secondaryListTitle":"Servicios Destacados",
    "secondaryListItems":
    [
        "Tours al Volcán Turrialba e Irazú",
        "Birdwatching con guías certificados",
        "Talleres de gastronomía típica",
        "Transporte desde San José",
        "Wi-Fi de alta velocidad",
        "Atención personalizada 24/7"
    ],

    "serviceCards":
    [
        {
            "title":"18 habitaciones temáticas",
            "description":"Ambientes con personalidad propia, balcones al bosque nuboso y textiles artesanales inspirados en Cartago."
        },
        {
            "title":"Restaurante ""La Ceiba""",
            "description":"Cocina de finca a la mesa, café chorreado y menús de temporada que celebran los sabores locales."
        },
        {
            "title":"Piscina natural de manantial",
            "description":"Agua cristalina, temperatura agradable y vistas verdes para recargar energía de forma natural."
        },
        {
            "title":"Senderos ecológicos (5 km)",
            "description":"Rutas señalizadas entre bosque nuboso, ideales para caminatas al amanecer y observación de flora."
        },
        {
            "title":"Salón de eventos",
            "description":"Espacio versátil con luz natural, perfecto para retiros corporativos, bodas boutique y talleres."
        },
        {
            "title":"Spa con plantas locales",
            "description":"Tratamientos herbales, masajes relajantes y aromaterapia con esencias del bosque costarricense."
        },
        {
            "title":"Tours al Volcán Turrialba e Irazú",
            "description":"Excursiones guiadas para explorar dos volcanes icónicos con logística y transporte incluidos."
        },
        {
            "title":"Birdwatching con guías certificados",
            "description":"Avistamiento de purrujas y más de 120 especies con especialistas locales y equipo óptico."
        },
        {
            "title":"Talleres de gastronomía típica",
            "description":"Aprende a preparar tortillas palmeadas, gallo pinto y salsas caseras con cocineras de la zona."
        },
        {
            "title":"Transporte desde San José",
            "description":"Traslados seguros puerta a puerta para que llegues sin preocupaciones desde el aeropuerto o la ciudad."
        },
        {
            "title":"Wi-Fi de alta velocidad",
            "description":"Conectividad confiable en habitaciones y áreas comunes para trabajar o compartir tu experiencia."
        },
        {
            "title":"Atención personalizada 24/7",
            "description":"Equipo disponible todo el día para ayudarte con reservas, recomendaciones y soporte durante tu estadía."
        }
    ]
}';

EXEC usp_FacilitiesPageContent_Upsert
    @SectionTitle = N'Características Principales',
    @SectionTag = N'Lo que nos distingue',
    @DescriptionJson = @FacilitiesContentJson;
GO

-- Crear la tabla AboutUsPageContent si no existe.
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AboutUsPageContent')
BEGIN
    CREATE TABLE AboutUsPageContent (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ContentJson NVARCHAR(MAX) NOT NULL DEFAULT '{}',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END;
GO

-- Procedimiento almacenado para obtener el contenido de "Sobre Nosotros".
CREATE OR ALTER PROCEDURE usp_AboutUsPageContent_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        ISNULL(ContentJson, '{}') AS ContentJson
    FROM AboutUsPageContent
    ORDER BY CreatedAt DESC;
END;
GO

-- Procedimiento almacenado para insertar o actualizar el contenido de "Sobre Nosotros".
CREATE OR ALTER PROCEDURE usp_AboutUsPageContent_Upsert
    @ContentJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM AboutUsPageContent)
    BEGIN
        UPDATE AboutUsPageContent
        SET ContentJson = @ContentJson,
            UpdatedAt = GETUTCDATE()
        WHERE Id = (SELECT TOP 1 Id FROM AboutUsPageContent ORDER BY CreatedAt DESC);
    END
    ELSE
    BEGIN
        INSERT INTO AboutUsPageContent (ContentJson, CreatedAt, UpdatedAt)
        VALUES (@ContentJson, GETUTCDATE(), GETUTCDATE());
    END
END;
GO
---------------------------------------------
-- Datos iniciales del about us
----------------------------------------------
DECLARE @AboutUsContentJson NVARCHAR(MAX) = N'
{
    "historyTag":"Desde 2005",
    "historyTitle":"Nuestra Historia",

    "historyDescription":"Hotel Las Purrujas nació en el año 2005 en el corazón de los Andes costarricenses, específicamente en las faldas del Volcán Turrialba, en la provincia de Cartago. 
                          Su nombre rinde homenaje a las purrujas, pequeñas aves endémicas de la región que simbolizan la vida silvestre y la conexión profunda con la naturaleza.
                          \n\nFundado por la familia Vargas Montoya, el hotel comenzó como una pequeña posada de cuatro habitaciones con el sueño de ofrecer una experiencia auténtica del 
                          campo costarricense. Con los años y gracias al turismo ecológico, se convirtió en un referente del ecoturismo en la zona central de Costa Rica.",

    "historyTimelineStartYear":"2005",

    "historyMilestones":
    [
        "Fundación con 4 habitaciones",
        "Expansión del restaurante La Ceiba",
        "18 habitaciones temáticas",
        "Referente de ecoturismo en Cartago"
    ],

    "historyTimelineEndLabel":"Hoy",

    "teamTag":"Nuestra gente",
    "teamTitle":"Equipo & Filosofía",

    "collaboratorsCount":30,
    "localTalentPercentage":90,
    "experienceYears":20,

    "collaboratorsLabel":"Colaboradores",
    "localTalentLabel":"Talento local de Cartago",
    "experienceLabel":"Años de experiencia",

    "directorName":"Andrea Vargas",
    "directorTitle":"Directora General",

    "directorBiography":"Hija de los fundadores y graduada en Administración Hotelera de la Universidad de Costa Rica, Andrea lidera el hotel con una visión moderna sin perder la esencia 
                         familiar que lo caracteriza. Bajo su dirección, Las Purrujas ha crecido como referente de ecoturismo responsable en la región.",

    "philosophyTitle":"Nuestra Filosofía",

    "philosophyDescription":"En Las Purrujas no solo ofrecemos una cama y un desayuno; ofrecemos una experiencia de vida. Cada detalle, desde la decoración artesanal hasta el menú del 
                             restaurante, está pensado para que el huésped se lleve consigo un pedazo auténtico de Costa Rica. Creemos que el turismo puede y debe ser un motor de desarrollo 
                             local, por eso reinvertimos parte de nuestros ingresos en programas educativos y ambientales para la comunidad.",

    "philosophyQuote":"Donde la naturaleza te abraza y Costa Rica te enamora.",

    "mvvTag":"Quiénes somos",
    "mvvTitle":"Misión, Visión & Valores",

    "missionTitle":"Misión",

    "mission":"Brindar a nuestros huéspedes una experiencia de hospedaje auténtica, cálida y sostenible, conectándolos con la riqueza natural y cultural de Costa Rica, a través de un 
               servicio personalizado y comprometido con el bienestar de la comunidad local y el medio ambiente.",

    "visionTitle":"Visión",

    "vision":"Ser reconocidos como el principal destino de ecoturismo en la región de Cartago para el año 2030, liderando un modelo de turismo responsable que inspire a otras empresas 
              hoteleras a adoptar prácticas sostenibles.",

    "valuesTitle":"Valores",

    "values":
    [
        "Sostenibilidad",
        "Calidez humana",
        "Compromiso comunitario",
        "Respeto por la naturaleza",
        "Excelencia en el servicio"
    ],

    "galleryTag":"Inspírate",
    "galleryTitle":"Galería",

    "gallerySubtext":"Descubre las instalaciones del hotel y los maravillosos lugares que te rodean para planificar tu itinerario perfecto."
}';
EXEC usp_AboutUsPageContent_Upsert
    @ContentJson = @AboutUsContentJson;
GO

-------------------------------
-- Galeria de imagenes
-------------------------------
IF OBJECT_ID('dbo.GalleryImages', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.GalleryImages;
END;
GO

CREATE TABLE dbo.GalleryImages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Src NVARCHAR(500) NOT NULL,
    Alt NVARCHAR(255) NOT NULL,
    Caption NVARCHAR(255) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

INSERT INTO dbo.GalleryImages
(Name, Src, Alt, Caption, Category)
VALUES
('atencion_personalizada.png', '/uploads/gallery/atencion_personalizada.png', 'Atención personalizada en el hotel', 'Atención personalizada', 'hotel'),
('avistamiento_aves.png', '/uploads/gallery/avistamiento_aves.png', 'Avistamiento de aves en los alrededores', 'Avistamiento de aves', 'lugares'),
('foto_fondo.png', '/uploads/gallery/foto_fondo.png', 'Hotel Las Purrujas', 'Hotel Las Purrujas', 'fondo'),
('gastronomia_tipica.png', '/uploads/gallery/gastronomia_tipica.png', 'Gastronomía típica costarricense', 'Gastronomía típica', 'hotel'),
('habitacion_doble.png', '/uploads/gallery/habitacion_doble.png', 'Habitación doble', 'Habitación doble', 'hotel'),
('habitacion_doble_2.png', '/uploads/gallery/habitacion_doble_2.png', 'Habitación doble con vista', 'Habitación doble · vista balcón', 'hotel'),
('habitacion_doble_3.png', '/uploads/gallery/habitacion_doble_3.png', 'Habitación doble adicional', 'Habitación doble adicional', 'hotel'),
('internet.png', '/uploads/gallery/internet.png', 'Internet de alta velocidad en el hotel', 'Wi-Fi de alta velocidad', 'hotel'),
('piscinas_naturales.png', '/uploads/gallery/piscinas_naturales.png', 'Piscinas naturales del hotel', 'Piscinas naturales', 'hotel'),
('restaurante_la_ceiba.png', '/uploads/gallery/restaurante_la_ceiba.png', 'Restaurante La Ceiba', 'Restaurante La Ceiba', 'hotel'),
('salon_eventos.png', '/uploads/gallery/salon_eventos.png', 'Salón de eventos rodeado de naturaleza', 'Salón de eventos', 'hotel'),
('senderismo_volcan.png', '/uploads/gallery/senderismo_volcan.png', 'Senderismo en el volcán Turrialba', 'Senderismo en el volcán', 'lugares'),
('senderos.png', '/uploads/gallery/senderos.png', 'Senderos ecológicos de la zona', 'Senderos ecológicos', 'lugares'),
('spa.png', '/uploads/gallery/spa.png', 'Spa con plantas locales', 'Spa y bienestar', 'hotel'),
('transporte.png', '/uploads/gallery/transporte.png', 'Transporte privado desde San José', 'Transporte privado', 'hotel'),
('villa_familiar.png', '/uploads/gallery/villa_familiar.png', 'Villa familiar', 'Villa familiar', 'hotel'),
('vista_balcon_noche.png', '/uploads/gallery/vista_balcon_noche.png', 'Vista nocturna desde el balcón', 'Vista desde el balcón de noche', 'hotel'),
('vista_balcon.png', '/uploads/gallery/vista_balcon.png', 'Vista desde el balcón', 'Vista desde el balcón', 'hotel');
GO

-------------------------
-- Home
-------------------------
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

------------------------------------
-- Disponibilidad de habitaciones
-------------------------------------
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
    VALUES (N'Reserva', N'Demo', N'disponibilidad.demo@laspurrujas.local', N'0000-0000', N'0000000000000000', 1);
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

-------------------------------------------------------
-- Room availability admin
-------------------------------------------------------
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

----------------------------------------------
-- four rooms per type
----------------------------------------------
-- Patch: 4 habitaciones por tipo de habitación
-- Agrega las habitaciones faltantes para llegar a 4 por tipo
-- Doble (RoomTypeId=1): agrega 103, 104
-- Suite Volcán (RoomTypeId=2): agrega 203, 204
-- Villa Familiar (RoomTypeId=3): agrega 302, 303, 304
-- Estado inicial: Disponible (RoomStatusId=1)

IF NOT EXISTS (SELECT 1 FROM Room WHERE RoomNumber = N'104')
    INSERT INTO Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId)
    VALUES (N'104', 1, 1, 1);

IF NOT EXISTS (SELECT 1 FROM Room WHERE RoomNumber = N'203')
    INSERT INTO Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId)
    VALUES (N'203', 1, 2, 1);

IF NOT EXISTS (SELECT 1 FROM Room WHERE RoomNumber = N'204')
    INSERT INTO Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId)
    VALUES (N'204', 1, 2, 1);

IF NOT EXISTS (SELECT 1 FROM Room WHERE RoomNumber = N'303')
    INSERT INTO Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId)
    VALUES (N'303', 1, 3, 1);

IF NOT EXISTS (SELECT 1 FROM Room WHERE RoomNumber = N'304')
    INSERT INTO Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId)
    VALUES (N'304', 1, 3, 1);
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