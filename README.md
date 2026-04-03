# Proyecto_Ingenieria_Hotelera_Purrujas

Estructura del Backend:

Api → Application + Infrastructure
Infrastructure → Application + Domain
Application → Domain
Domain → no depende de nadie

LasPurrujas.sln
src/
  LasPurrujas.Api/
    Controllers/
      ReservationsController.cs
      RoomsController.cs
      CmsController.cs
    Extensions/
      ServiceCollectionExtensions.cs
    Middleware/
    Program.cs
    appsettings.json

  LasPurrujas.Application/
    Abstractions/
      Persistence/
        IUnitOfWork.cs
      Services/
        IDateTimeProvider.cs
    Reservations/
      Commands/
        CreateReservation/
          CreateReservationCommand.cs
          CreateReservationHandler.cs
      Queries/
        GetReservationById/
          GetReservationByIdQuery.cs
          GetReservationByIdHandler.cs
      DTOs/
        ReservationDto.cs
    Rooms/
      Queries/
    Customers/
    Pricing/
    Cms/
    Common/

  LasPurrujas.Domain/
    Common/
      Entity.cs
      AggregateRoot.cs
      Result.cs
      DomainException.cs
    Reservations/
      Entities/
        Reservation.cs
        ReservationRoom.cs
      Repositories/
        IReservationRepository.cs
      ValueObjects/
        DateRange.cs
    Customers/
      Entities/
        Customer.cs
      Repositories/
        ICustomerRepository.cs
    Rooms/
      Entities/
        Room.cs
        RoomType.cs
      Repositories/
        IRoomRepository.cs
    Pricing/
      Entities/
        RatePlan.cs
      Repositories/
        IRatePlanRepository.cs

  LasPurrujas.Infrastructure/
    Persistence/
      LasPurrujasDbContext.cs
      Configurations/
        ReservationConfiguration.cs
        ReservationRoomConfiguration.cs
        CustomerConfiguration.cs
        RoomConfiguration.cs
        RatePlanConfiguration.cs
      Repositories/
        ReservationRepository.cs
        CustomerRepository.cs
        RoomRepository.cs
        RatePlanRepository.cs
      UnitOfWork.cs
    Services/
      DateTimeProvider.cs
    DependencyInjection.cs

tests/
  LasPurrujas.Domain.Tests/
  LasPurrujas.Application.Tests/


Scrip de la Base de datos:
-- ============================================
-- BASE DE DATOS
-- ============================================
CREATE DATABASE Ingenieria_Purrujas_BD;
GO

USE Ingenieria_Purrujas_BD;
GO

-- ============================================
-- TABLAS DE CATÁLOGO
-- ============================================

CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE ReservationStatuses (
    ReservationStatusId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    IsFinal BIT NOT NULL DEFAULT 0
);
GO

CREATE TABLE RoomStatuses (
    RoomStatusId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    IsAvailableForBooking BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE PaymentMethods (
    PaymentMethodId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE PaymentStatuses (
    PaymentStatusId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    IsFinal BIT NOT NULL DEFAULT 0
);
GO

CREATE TABLE MediaTypes (
    MediaTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);
GO

CREATE TABLE SectionTypes (
    SectionTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);
GO

CREATE TABLE PromotionTypes (
    PromotionTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);
GO

-- ============================================
-- SEGURIDAD Y ADMINISTRACIÓN
-- ============================================

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    RoleId INT NOT NULL,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    Phone NVARCHAR(30) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    LastLoginAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Users_Roles
        FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

CREATE TABLE UserSessions (
    UserSessionId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    TokenIdentifier NVARCHAR(255) NOT NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    StartedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    RevokedAt DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_UserSessions_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

CREATE TABLE AuditLogs (
    AuditLogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    EntityName NVARCHAR(150) NOT NULL,
    EntityId NVARCHAR(100) NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_AuditLogs_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

-- ============================================
-- CLIENTES Y HUÉSPEDES
-- ============================================

CREATE TABLE Customers (
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(30) NULL,
    IdentificationType NVARCHAR(50) NULL,
    IdentificationNumber NVARCHAR(100) NULL,
    BirthDate DATE NULL,
    Notes NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE CustomerAddresses (
    CustomerAddressId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    Country NVARCHAR(100) NULL,
    Province NVARCHAR(100) NULL,
    City NVARCHAR(100) NULL,
    District NVARCHAR(100) NULL,
    AddressLine NVARCHAR(255) NULL,
    PostalCode NVARCHAR(20) NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_CustomerAddresses_Customers
        FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
        ON DELETE CASCADE
);
GO

CREATE TABLE Guests (
    GuestId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    IdentificationType NVARCHAR(50) NULL,
    IdentificationNumber NVARCHAR(100) NULL,
    BirthDate DATE NULL,
    Email NVARCHAR(150) NULL,
    Phone NVARCHAR(30) NULL,
    Notes NVARCHAR(MAX) NULL
);
GO

-- ============================================
-- HABITACIONES
-- ============================================

CREATE TABLE RoomTypes (
    RoomTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(MAX) NULL,
    Capacity INT NOT NULL,
    BasePrice DECIMAL(12,2) NOT NULL,
    MaxAdults INT NOT NULL,
    MaxChildren INT NOT NULL,
    AreaM2 DECIMAL(10,2) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT CK_RoomTypes_Capacity CHECK (Capacity > 0),
    CONSTRAINT CK_RoomTypes_BasePrice CHECK (BasePrice >= 0),
    CONSTRAINT CK_RoomTypes_MaxAdults CHECK (MaxAdults >= 0),
    CONSTRAINT CK_RoomTypes_MaxChildren CHECK (MaxChildren >= 0)
);
GO

CREATE TABLE Rooms (
    RoomId INT IDENTITY(1,1) PRIMARY KEY,
    RoomTypeId INT NOT NULL,
    RoomStatusId INT NOT NULL,
    RoomNumber NVARCHAR(20) NOT NULL UNIQUE,
    Floor INT NULL,
    Description NVARCHAR(MAX) NULL,
    CurrentPrice DECIMAL(12,2) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Rooms_RoomTypes
        FOREIGN KEY (RoomTypeId) REFERENCES RoomTypes(RoomTypeId),
    CONSTRAINT FK_Rooms_RoomStatuses
        FOREIGN KEY (RoomStatusId) REFERENCES RoomStatuses(RoomStatusId),
    CONSTRAINT CK_Rooms_CurrentPrice CHECK (CurrentPrice >= 0)
);
GO

CREATE TABLE Features (
    FeatureId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    Icon NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE RoomTypeFeatures (
    RoomTypeFeatureId INT IDENTITY(1,1) PRIMARY KEY,
    RoomTypeId INT NOT NULL,
    FeatureId INT NOT NULL,

    CONSTRAINT FK_RoomTypeFeatures_RoomTypes
        FOREIGN KEY (RoomTypeId) REFERENCES RoomTypes(RoomTypeId)
        ON DELETE CASCADE,
    CONSTRAINT FK_RoomTypeFeatures_Features
        FOREIGN KEY (FeatureId) REFERENCES Features(FeatureId)
        ON DELETE CASCADE,
    CONSTRAINT UQ_RoomTypeFeatures UNIQUE (RoomTypeId, FeatureId)
);
GO

CREATE TABLE MaintenanceBlocks (
    MaintenanceBlockId INT IDENTITY(1,1) PRIMARY KEY,
    RoomId INT NOT NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    Reason NVARCHAR(255) NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedByUserId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_MaintenanceBlocks_Rooms
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    CONSTRAINT FK_MaintenanceBlocks_Users
        FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    CONSTRAINT CK_MaintenanceBlocks_Dates CHECK (EndDate > StartDate)
);
GO

CREATE TABLE RoomStatusHistories (
    RoomStatusHistoryId BIGINT IDENTITY(1,1) PRIMARY KEY,
    RoomId INT NOT NULL,
    RoomStatusId INT NOT NULL,
    ChangedByUserId INT NULL,
    Reason NVARCHAR(255) NULL,
    ChangedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_RoomStatusHistories_Rooms
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    CONSTRAINT FK_RoomStatusHistories_RoomStatuses
        FOREIGN KEY (RoomStatusId) REFERENCES RoomStatuses(RoomStatusId),
    CONSTRAINT FK_RoomStatusHistories_Users
        FOREIGN KEY (ChangedByUserId) REFERENCES Users(UserId)
);
GO

-- ============================================
-- TEMPORADAS, TARIFAS Y PROMOCIONES
-- ============================================

CREATE TABLE Seasons (
    SeasonId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    PercentageChange DECIMAL(8,2) NOT NULL DEFAULT 0,
    Priority INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    Description NVARCHAR(255) NULL,

    CONSTRAINT CK_Seasons_Dates CHECK (EndDate >= StartDate)
);
GO

CREATE TABLE RatePlans (
    RatePlanId INT IDENTITY(1,1) PRIMARY KEY,
    RoomTypeId INT NOT NULL,
    SeasonId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    PricePerNight DECIMAL(12,2) NOT NULL,
    MinNights INT NOT NULL DEFAULT 1,
    MaxGuests INT NOT NULL DEFAULT 1,
    ValidFrom DATE NOT NULL,
    ValidTo DATE NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_RatePlans_RoomTypes
        FOREIGN KEY (RoomTypeId) REFERENCES RoomTypes(RoomTypeId),
    CONSTRAINT FK_RatePlans_Seasons
        FOREIGN KEY (SeasonId) REFERENCES Seasons(SeasonId),
    CONSTRAINT CK_RatePlans_Price CHECK (PricePerNight >= 0),
    CONSTRAINT CK_RatePlans_MinNights CHECK (MinNights > 0),
    CONSTRAINT CK_RatePlans_MaxGuests CHECK (MaxGuests > 0),
    CONSTRAINT CK_RatePlans_Dates CHECK (ValidTo >= ValidFrom)
);
GO

CREATE TABLE Promotions (
    PromotionId INT IDENTITY(1,1) PRIMARY KEY,
    PromotionTypeId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    DiscountPercentage DECIMAL(8,2) NULL,
    DiscountAmount DECIMAL(12,2) NULL,
    PromoCode NVARCHAR(50) NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Priority INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Promotions_PromotionTypes
        FOREIGN KEY (PromotionTypeId) REFERENCES PromotionTypes(PromotionTypeId),
    CONSTRAINT CK_Promotions_Dates CHECK (EndDate >= StartDate),
    CONSTRAINT CK_Promotions_DiscountPercentage CHECK (
        DiscountPercentage IS NULL OR (DiscountPercentage >= 0 AND DiscountPercentage <= 100)
    ),
    CONSTRAINT CK_Promotions_DiscountAmount CHECK (
        DiscountAmount IS NULL OR DiscountAmount >= 0
    )
);
GO

CREATE TABLE PromotionRules (
    PromotionRuleId INT IDENTITY(1,1) PRIMARY KEY,
    PromotionId INT NOT NULL,
    RoomTypeId INT NULL,
    SeasonId INT NULL,
    MinNights INT NOT NULL DEFAULT 1,
    MinRooms INT NOT NULL DEFAULT 1,
    AppliesToAllRoomTypes BIT NOT NULL DEFAULT 0,
    AppliesToAllSeasons BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_PromotionRules_Promotions
        FOREIGN KEY (PromotionId) REFERENCES Promotions(PromotionId)
        ON DELETE CASCADE,
    CONSTRAINT FK_PromotionRules_RoomTypes
        FOREIGN KEY (RoomTypeId) REFERENCES RoomTypes(RoomTypeId),
    CONSTRAINT FK_PromotionRules_Seasons
        FOREIGN KEY (SeasonId) REFERENCES Seasons(SeasonId),
    CONSTRAINT CK_PromotionRules_MinNights CHECK (MinNights > 0),
    CONSTRAINT CK_PromotionRules_MinRooms CHECK (MinRooms > 0)
);
GO

-- ============================================
-- RESERVAS
-- ============================================

CREATE TABLE Reservations (
    ReservationId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    ReservationStatusId INT NOT NULL,
    ReservationCode NVARCHAR(50) NOT NULL UNIQUE,
    CheckInDate DATE NOT NULL,
    CheckOutDate DATE NOT NULL,
    Adults INT NOT NULL,
    Children INT NOT NULL DEFAULT 0,
    SpecialRequests NVARCHAR(MAX) NULL,
    Subtotal DECIMAL(12,2) NOT NULL DEFAULT 0,
    DiscountTotal DECIMAL(12,2) NOT NULL DEFAULT 0,
    TaxAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Reservations_Customers
        FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    CONSTRAINT FK_Reservations_ReservationStatuses
        FOREIGN KEY (ReservationStatusId) REFERENCES ReservationStatuses(ReservationStatusId),
    CONSTRAINT CK_Reservations_Dates CHECK (CheckOutDate > CheckInDate),
    CONSTRAINT CK_Reservations_Adults CHECK (Adults > 0),
    CONSTRAINT CK_Reservations_Children CHECK (Children >= 0),
    CONSTRAINT CK_Reservations_Subtotal CHECK (Subtotal >= 0),
    CONSTRAINT CK_Reservations_DiscountTotal CHECK (DiscountTotal >= 0),
    CONSTRAINT CK_Reservations_TaxAmount CHECK (TaxAmount >= 0),
    CONSTRAINT CK_Reservations_TotalAmount CHECK (TotalAmount >= 0)
);
GO

CREATE TABLE ReservationRooms (
    ReservationRoomId INT IDENTITY(1,1) PRIMARY KEY,
    ReservationId INT NOT NULL,
    RoomId INT NOT NULL,
    RatePlanId INT NOT NULL,
    PromotionId INT NULL,
    PricePerNight DECIMAL(12,2) NOT NULL,
    Nights INT NOT NULL,
    Subtotal DECIMAL(12,2) NOT NULL,
    DiscountAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(12,2) NOT NULL,

    CONSTRAINT FK_ReservationRooms_Reservations
        FOREIGN KEY (ReservationId) REFERENCES Reservations(ReservationId)
        ON DELETE CASCADE,
    CONSTRAINT FK_ReservationRooms_Rooms
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
    CONSTRAINT FK_ReservationRooms_RatePlans
        FOREIGN KEY (RatePlanId) REFERENCES RatePlans(RatePlanId),
    CONSTRAINT FK_ReservationRooms_Promotions
        FOREIGN KEY (PromotionId) REFERENCES Promotions(PromotionId),
    CONSTRAINT CK_ReservationRooms_Price CHECK (PricePerNight >= 0),
    CONSTRAINT CK_ReservationRooms_Nights CHECK (Nights > 0),
    CONSTRAINT CK_ReservationRooms_Subtotal CHECK (Subtotal >= 0),
    CONSTRAINT CK_ReservationRooms_DiscountAmount CHECK (DiscountAmount >= 0),
    CONSTRAINT CK_ReservationRooms_TotalAmount CHECK (TotalAmount >= 0)
);
GO

CREATE TABLE ReservationGuests (
    ReservationGuestId INT IDENTITY(1,1) PRIMARY KEY,
    ReservationId INT NOT NULL,
    GuestId INT NOT NULL,
    IsPrimaryGuest BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_ReservationGuests_Reservations
        FOREIGN KEY (ReservationId) REFERENCES Reservations(ReservationId)
        ON DELETE CASCADE,
    CONSTRAINT FK_ReservationGuests_Guests
        FOREIGN KEY (GuestId) REFERENCES Guests(GuestId),
    CONSTRAINT UQ_ReservationGuests UNIQUE (ReservationId, GuestId)
);
GO

CREATE TABLE ReservationEvents (
    ReservationEventId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ReservationId INT NOT NULL,
    UserId INT NULL,
    ReservationStatusId INT NULL,
    EventType NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_ReservationEvents_Reservations
        FOREIGN KEY (ReservationId) REFERENCES Reservations(ReservationId)
        ON DELETE CASCADE,
    CONSTRAINT FK_ReservationEvents_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_ReservationEvents_ReservationStatuses
        FOREIGN KEY (ReservationStatusId) REFERENCES ReservationStatuses(ReservationStatusId)
);
GO

-- ============================================
-- FACTURACIÓN Y PAGOS
-- ============================================

CREATE TABLE Bills (
    BillId INT IDENTITY(1,1) PRIMARY KEY,
    ReservationId INT NOT NULL UNIQUE,
    BillNumber NVARCHAR(50) NOT NULL UNIQUE,
    BillingName NVARCHAR(150) NOT NULL,
    BillingIdentification NVARCHAR(100) NULL,
    IssueDate DATETIME2 NOT NULL,
    BaseAmount DECIMAL(12,2) NOT NULL,
    DiscountAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    TaxAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(12,2) NOT NULL,
    Notes NVARCHAR(MAX) NULL,

    CONSTRAINT FK_Bills_Reservations
        FOREIGN KEY (ReservationId) REFERENCES Reservations(ReservationId),
    CONSTRAINT CK_Bills_BaseAmount CHECK (BaseAmount >= 0),
    CONSTRAINT CK_Bills_DiscountAmount CHECK (DiscountAmount >= 0),
    CONSTRAINT CK_Bills_TaxAmount CHECK (TaxAmount >= 0),
    CONSTRAINT CK_Bills_TotalAmount CHECK (TotalAmount >= 0)
);
GO

CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    ReservationId INT NOT NULL,
    BillId INT NULL,
    PaymentMethodId INT NOT NULL,
    PaymentStatusId INT NOT NULL,
    ReferenceNumber NVARCHAR(100) NULL,
    Amount DECIMAL(12,2) NOT NULL,
    Currency NVARCHAR(10) NOT NULL DEFAULT 'CRC',
    PaidAt DATETIME2 NULL,
    Notes NVARCHAR(MAX) NULL,
    RegisteredByUserId INT NULL,

    CONSTRAINT FK_Payments_Reservations
        FOREIGN KEY (ReservationId) REFERENCES Reservations(ReservationId),
    CONSTRAINT FK_Payments_Bills
        FOREIGN KEY (BillId) REFERENCES Bills(BillId),
    CONSTRAINT FK_Payments_PaymentMethods
        FOREIGN KEY (PaymentMethodId) REFERENCES PaymentMethods(PaymentMethodId),
    CONSTRAINT FK_Payments_PaymentStatuses
        FOREIGN KEY (PaymentStatusId) REFERENCES PaymentStatuses(PaymentStatusId),
    CONSTRAINT FK_Payments_Users
        FOREIGN KEY (RegisteredByUserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Payments_Amount CHECK (Amount >= 0)
);
GO

-- ============================================
-- CMS Y SITIO PÚBLICO
-- ============================================

CREATE TABLE Pages (
    PageId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NOT NULL UNIQUE,
    Title NVARCHAR(150) NOT NULL,
    MetaTitle NVARCHAR(150) NULL,
    MetaDescription NVARCHAR(255) NULL,
    IsPublished BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE ContentSections (
    ContentSectionId INT IDENTITY(1,1) PRIMARY KEY,
    PageId INT NOT NULL,
    SectionTypeId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Title NVARCHAR(150) NULL,
    Subtitle NVARCHAR(255) NULL,
    Body NVARCHAR(MAX) NULL,
    DisplayOrder INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_ContentSections_Pages
        FOREIGN KEY (PageId) REFERENCES Pages(PageId)
        ON DELETE CASCADE,
    CONSTRAINT FK_ContentSections_SectionTypes
        FOREIGN KEY (SectionTypeId) REFERENCES SectionTypes(SectionTypeId)
);
GO

CREATE TABLE MediaAssets (
    MediaAssetId INT IDENTITY(1,1) PRIMARY KEY,
    MediaTypeId INT NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FileUrl NVARCHAR(500) NOT NULL,
    AltText NVARCHAR(255) NULL,
    Caption NVARCHAR(255) NULL,
    Width INT NULL,
    Height INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    UploadedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_MediaAssets_MediaTypes
        FOREIGN KEY (MediaTypeId) REFERENCES MediaTypes(MediaTypeId),
    CONSTRAINT CK_MediaAssets_Width CHECK (Width IS NULL OR Width > 0),
    CONSTRAINT CK_MediaAssets_Height CHECK (Height IS NULL OR Height > 0)
);
GO

CREATE TABLE ContentSectionMedia (
    ContentSectionMediaId INT IDENTITY(1,1) PRIMARY KEY,
    ContentSectionId INT NOT NULL,
    MediaAssetId INT NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 1,
    IsPrimary BIT NOT NULL DEFAULT 0,

    CONSTRAINT FK_ContentSectionMedia_ContentSections
        FOREIGN KEY (ContentSectionId) REFERENCES ContentSections(ContentSectionId)
        ON DELETE CASCADE,
    CONSTRAINT FK_ContentSectionMedia_MediaAssets
        FOREIGN KEY (MediaAssetId) REFERENCES MediaAssets(MediaAssetId)
        ON DELETE CASCADE,
    CONSTRAINT UQ_ContentSectionMedia UNIQUE (ContentSectionId, MediaAssetId)
);
GO

CREATE TABLE Advertisings (
    AdvertisingId INT IDENTITY(1,1) PRIMARY KEY,
    MediaAssetId INT NOT NULL,
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    RedirectUrl NVARCHAR(500) NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    Priority INT NOT NULL DEFAULT 1,

    CONSTRAINT FK_Advertisings_MediaAssets
        FOREIGN KEY (MediaAssetId) REFERENCES MediaAssets(MediaAssetId),
    CONSTRAINT CK_Advertisings_Dates CHECK (EndDate >= StartDate)
);
GO

CREATE TABLE ContactInfos (
    ContactInfoId INT IDENTITY(1,1) PRIMARY KEY,
    PhonePrimary NVARCHAR(30) NULL,
    PhoneSecondary NVARCHAR(30) NULL,
    EmailPrimary NVARCHAR(150) NULL,
    EmailSecondary NVARCHAR(150) NULL,
    Whatsapp NVARCHAR(30) NULL,
    BusinessHours NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE TABLE MapLocations (
    MapLocationId INT IDENTITY(1,1) PRIMARY KEY,
    PageId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Latitude DECIMAL(10,7) NOT NULL,
    Longitude DECIMAL(10,7) NOT NULL,
    AddressText NVARCHAR(255) NULL,
    ReferencePoints NVARCHAR(MAX) NULL,
    GoogleMapsUrl NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_MapLocations_Pages
        FOREIGN KEY (PageId) REFERENCES Pages(PageId)
        ON DELETE CASCADE
);
GO

CREATE TABLE SocialLinks (
    SocialLinkId INT IDENTITY(1,1) PRIMARY KEY,
    Platform NVARCHAR(100) NOT NULL,
    Url NVARCHAR(500) NOT NULL,
    Icon NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL DEFAULT 1
);
GO

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

-- ============================================
-- ÍNDICES ÚTILES
-- ============================================

CREATE INDEX IX_Users_RoleId ON Users(RoleId);
CREATE INDEX IX_UserSessions_UserId ON UserSessions(UserId);
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_EntityName_EntityId ON AuditLogs(EntityName, EntityId);

CREATE INDEX IX_CustomerAddresses_CustomerId ON CustomerAddresses(CustomerId);

CREATE INDEX IX_Rooms_RoomTypeId ON Rooms(RoomTypeId);
CREATE INDEX IX_Rooms_RoomStatusId ON Rooms(RoomStatusId);
CREATE INDEX IX_MaintenanceBlocks_RoomId ON MaintenanceBlocks(RoomId);
CREATE INDEX IX_RoomStatusHistories_RoomId ON RoomStatusHistories(RoomId);

CREATE INDEX IX_RatePlans_RoomTypeId ON RatePlans(RoomTypeId);
CREATE INDEX IX_RatePlans_SeasonId ON RatePlans(SeasonId);
CREATE INDEX IX_Promotions_PromotionTypeId ON Promotions(PromotionTypeId);
CREATE INDEX IX_PromotionRules_PromotionId ON PromotionRules(PromotionId);

CREATE INDEX IX_Reservations_CustomerId ON Reservations(CustomerId);
CREATE INDEX IX_Reservations_ReservationStatusId ON Reservations(ReservationStatusId);
CREATE INDEX IX_Reservations_CheckInDate_CheckOutDate ON Reservations(CheckInDate, CheckOutDate);

CREATE INDEX IX_ReservationRooms_ReservationId ON ReservationRooms(ReservationId);
CREATE INDEX IX_ReservationRooms_RoomId ON ReservationRooms(RoomId);
CREATE INDEX IX_ReservationRooms_RatePlanId ON ReservationRooms(RatePlanId);

CREATE INDEX IX_ReservationGuests_ReservationId ON ReservationGuests(ReservationId);
CREATE INDEX IX_ReservationEvents_ReservationId ON ReservationEvents(ReservationId);

CREATE INDEX IX_Bills_ReservationId ON Bills(ReservationId);
CREATE INDEX IX_Payments_ReservationId ON Payments(ReservationId);
CREATE INDEX IX_Payments_BillId ON Payments(BillId);

CREATE INDEX IX_ContentSections_PageId ON ContentSections(PageId);
CREATE INDEX IX_ContentSectionMedia_ContentSectionId ON ContentSectionMedia(ContentSectionId);
CREATE INDEX IX_Advertisings_MediaAssetId ON Advertisings(MediaAssetId);
CREATE INDEX IX_MapLocations_PageId ON MapLocations(PageId);

-- ============================================
-- DATOS SEMILLA BÁSICOS
-- ============================================

INSERT INTO Roles (Name, Description) VALUES
('ADMIN', 'Administrador del sistema'),
('EDITOR', 'Editor de contenido');
GO

INSERT INTO ReservationStatuses (Name, Description, IsFinal) VALUES
('PENDING', 'Reserva pendiente', 0),
('CONFIRMED', 'Reserva confirmada', 0),
('CANCELLED', 'Reserva cancelada', 1),
('COMPLETED', 'Reserva finalizada', 1);
GO

INSERT INTO RoomStatuses (Name, Description, IsAvailableForBooking) VALUES
('AVAILABLE', 'Disponible para reservar', 1),
('OCCUPIED', 'Habitación ocupada', 0),
('CLEANING', 'En limpieza', 0),
('MAINTENANCE', 'En mantenimiento', 0),
('OUT_OF_SERVICE', 'Fuera de servicio', 0);
GO

INSERT INTO PaymentMethods (Name, Description, IsActive) VALUES
('CASH', 'Pago en efectivo', 1),
('CARD', 'Tarjeta', 1),
('TRANSFER', 'Transferencia bancaria', 1);
GO

INSERT INTO PaymentStatuses (Name, Description, IsFinal) VALUES
('PENDING', 'Pago pendiente', 0),
('PAID', 'Pago realizado', 1),
('FAILED', 'Pago fallido', 1),
('REFUNDED', 'Pago reembolsado', 1);
GO

INSERT INTO MediaTypes (Name, Description) VALUES
('IMAGE', 'Imagen'),
('VIDEO', 'Video'),
('BANNER', 'Banner');
GO

INSERT INTO SectionTypes (Name, Description) VALUES
('HERO', 'Sección principal'),
('TEXT', 'Texto informativo'),
('GALLERY', 'Galería'),
('CONTACT', 'Contacto'),
('MAP', 'Mapa'),
('PROMOTION', 'Promociones');
GO

INSERT INTO PromotionTypes (Name, Description) VALUES
('PERCENTAGE', 'Descuento porcentual'),
('FIXED_AMOUNT', 'Descuento por monto fijo'),
('SEASONAL', 'Promoción de temporada');
GO