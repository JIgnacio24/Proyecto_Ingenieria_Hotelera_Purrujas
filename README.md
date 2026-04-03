# Proyecto_Ingenieria_Hotelera_Purrujas

Estructura del Backend:

Api → Application + Infrastructure
Infrastructure → Application + Domain
Application → Domain
Domain → no depende de nadie

Backend/
└── Backend-Ingenieria-Purrujas/
    ├── src/
    │   ├── Backend-Ingenieria-Purrujas.Api/
    │   │   ├── Controllers/
    │   │   ├── Extensions/
    │   │   ├── Middleware/
    │   │   ├── Properties/
    │   │   │   └── launchSettings.json
    │   │   ├── appsettings.json
    │   │   ├── appsettings.Development.json
    │   │   ├── Program.cs
    │   │   ├── Backend-Ingenieria-Purrujas.Api.csproj
    │   │   └── Backend-Ingenieria-Purrujas.http
    │   │
    │   ├── Backend-Ingenieria-Purrujas.Application/
    │   │   ├── Abstractions/
    │   │   ├── Analytics/
    │   │   ├── Auth/
    │   │   ├── Billing/
    │   │   ├── CMS/
    │   │   ├── Customers/
    │   │   ├── Pricing/
    │   │   ├── Reservations/
    │   │   │   ├── Commands/
    │   │   │   ├── Queries/
    │   │   │   └── DTOs/
    │   │   ├── Rooms/
    │   │   └── Backend-Ingenieria-Purrujas.Application.csproj
    │   │
    │   ├── Backend-Ingenieria-Purrujas.Domain/
    │   │   ├── Common/
    │   │   ├── Analytics/
    │   │   ├── Billing/
    │   │   ├── CMS/
    │   │   ├── Customers/
    │   │   │   ├── Entities/
    │   │   │   └── Repositories/
    │   │   ├── Pricing/
    │   │   │   ├── Entities/
    │   │   │   └── Repositories/
    │   │   ├── Reservations/
    │   │   │   ├── Entities/
    │   │   │   ├── Repositories/
    │   │   │   └── ValueObjects/
    │   │   ├── Rooms/
    │   │   │   ├── Entities/
    │   │   │   └── Repositories/
    │   │   └── Backend-Ingenieria-Purrujas.Domain.csproj
    │   │
    │   ├── Backend-Ingenieria-Purrujas.Infrastructure/
    │   │   ├── Files/
    │   │   ├── Identity/
    │   │   ├── Persistence/
    │   │   │   ├── Configurations/
    │   │   │   └── Repositories/
    │   │   ├── Services/
    │   │   └── Backend-Ingenieria-Purrujas.Infrastructure.csproj
    │   │
    │   └── tests/
    │       ├── Backend-Ingenieria-Purrujas.Domain.Tests/
    │       └── Backend-Ingenieria-Purrujas.Application.Tests/
    │
    └── Backend-Ingenieria-Purrujas.sln


Scrip de la Base de datos:
CREATE DATABASE Ingenieria_Purrujas_BD;
GO

USE Ingenieria_Purrujas_BD;
GO

CREATE TABLE Customer (
    CustomerId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255),
    LastName NVARCHAR(255),
    Email NVARCHAR(255) UNIQUE,
    Phone NVARCHAR(30) NULL,
    CreditCard NVARCHAR(20)
);

CREATE TABLE RoomType (
    RoomTypeId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255),
    BasePrice DECIMAL(10,2)
);

CREATE TABLE RoomStatus (
    RoomStatusId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100),
    Description NVARCHAR(255),
    IsAvailableForBooking BIT
);

CREATE TABLE Room (
    RoomId INT PRIMARY KEY IDENTITY,
    RoomNumber NVARCHAR(50) UNIQUE,
    IsActive BIT,
    RoomTypeId INT,
    RoomStatusId INT,
    CONSTRAINT FKRoomTypeRoom FOREIGN KEY (RoomTypeId) REFERENCES RoomType(RoomTypeId),
    CONSTRAINT FKRoomStatus FOREIGN KEY (RoomStatusId) REFERENCES RoomStatus(RoomStatusId)
);

CREATE TABLE ReservationStatus (
    ReservationStatusId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100),
    Description NVARCHAR(255),
    IsFinal BIT
);

CREATE TABLE Reservation (
    ReservationId INT PRIMARY KEY IDENTITY,
	ReservationDate DATETIME,
    StartDate DATETIME,
    EndDate DATETIME,
    CustomerId INT,
    RoomId INT,
    ReservationStatusId INT,
    CONSTRAINT FKCustomerReservation FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId),
    CONSTRAINT FKRoomReservation FOREIGN KEY (RoomId) REFERENCES Room(RoomId),
    CONSTRAINT FKReservationStatus FOREIGN KEY (ReservationStatusId) REFERENCES ReservationStatus(ReservationStatusId),
    CONSTRAINT CK_Reservation_Dates CHECK (EndDate > StartDate)
);

CREATE TABLE Bill (
    BillId INT PRIMARY KEY IDENTITY,
    ReservationId INT unique,
    BasePrice DECIMAL(10,2),
    Discount DECIMAL(10,2),
    SeasonAmount DECIMAL(10,2),
    CONSTRAINT FKReservationBill FOREIGN KEY (ReservationId) REFERENCES Reservation(ReservationId)
);

CREATE TABLE Payment (
    PaymentId INT PRIMARY KEY IDENTITY,
    ReservationId INT UNIQUE, -- 1 pago por reserva
    Amount DECIMAL(10,2) NOT NULL,
    PaymentMethod NVARCHAR(50), -- 'CASH', 'CARD', etc.
    PaymentDate DATETIME,
    IsPaid BIT NOT NULL DEFAULT 0,
    CONSTRAINT FKPaymentReservation FOREIGN KEY (ReservationId) REFERENCES Reservation(ReservationId)
);

CREATE TABLE Season (
    SeasonId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255),
    PercentageChange INT,
    IsActive BIT
);

CREATE TABLE Promotion (
    PromotionId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255),
    Discount INT,
    StartDate DATETIME,
    EndDate DATETIME,
	RoomTypeId INT,
	CONSTRAINT FKPromotionRoomType FOREIGN KEY (RoomTypeId) REFERENCES RoomType(RoomTypeId)
);

CREATE TABLE Feature (
    FeatureId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255)
);

CREATE TABLE Image (
    ImageId INT PRIMARY KEY IDENTITY,
    ImageURL NVARCHAR(500)
);

CREATE TABLE Advertising (
    AdvertisingId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255),
    Link NVARCHAR(500)
);

CREATE TABLE Page (
    PageId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255),
    Title NVARCHAR(255),
    Link NVARCHAR(500)
);

CREATE TABLE PageInformation (
    PageInformationId INT PRIMARY KEY IDENTITY,
    Subtitle NVARCHAR(255),
    Description NVARCHAR(MAX),
	PageId INT,
	CONSTRAINT FKPagePageInformation FOREIGN KEY (PageId) REFERENCES Page(PageId)
);

CREATE TABLE PageInformationImage (
    PageInformationImageId INT PRIMARY KEY IDENTITY,
    Description NVARCHAR(1000),
    ImageId INT,
    PageInformationId INT,
	CONSTRAINT FKPageInformationImagePageId FOREIGN KEY (PageInformationId) REFERENCES PageInformation(PageInformationId),
	CONSTRAINT FKPageInformationImageId FOREIGN KEY (ImageId) REFERENCES Image(ImageId)
);


---------------

CREATE TABLE RoomTypeFeature (
    RoomTypeFeatureId INT PRIMARY KEY IDENTITY,
    FeatureId INT,
    RoomTypeId INT,
	CONSTRAINT RoomTypeFeatureRoomId FOREIGN KEY (RoomTypeId) REFERENCES RoomType(RoomTypeId),
	CONSTRAINT RoomTypeFeatureId FOREIGN KEY (FeatureId) REFERENCES Feature(FeatureId)
);

CREATE TABLE RoomTypeImage (
    RoomTypeImageId INT PRIMARY KEY IDENTITY,
    ImageId INT,
    RoomTypeId INT,
    CONSTRAINT FKRoomTyeRoomImageRoomId FOREIGN KEY (RoomTypeId) REFERENCES RoomType(RoomTypeId),
	CONSTRAINT FKRoomTyeRoomImageId FOREIGN KEY (ImageId) REFERENCES Image(ImageId)
);

CREATE TABLE AdvertisingImage (
    AdvertisingImageId INT PRIMARY KEY IDENTITY,
    AdvertisingId INT,
	ImageId INT,
	CONSTRAINT FKImageAdvertisingId FOREIGN KEY (AdvertisingId) REFERENCES Advertising(AdvertisingId),
	CONSTRAINT FKImageAdvertisingImageId FOREIGN KEY (ImageId) REFERENCES Image(ImageId)
);

CREATE TABLE ContactInformation(
    ContactInformationId INT PRIMARY KEY IDENTITY,
    Email NVARCHAR(255),
    Phone NVARCHAR(50),
    Address NVARCHAR(255),
    PageId INT,
    CONSTRAINT FKPageContactInformation FOREIGN KEY (PageId) REFERENCES Page(PageId)
);

-- ============================================
-- ANALÍTICA Y PREDICCIÓN
-- ============================================
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