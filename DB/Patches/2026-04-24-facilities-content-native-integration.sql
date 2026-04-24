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

DECLARE @FacilitiesContentJson NVARCHAR(MAX) = N'{"sectionTag":"Lo que nos distingue","sectionTitle":"Caracteristicas Principales","highlightTitle":"Ubicacion Privilegiada","highlightDescription":"Situado a solo 45 minutos de San Jose, en las verdes montanas de Cartago, el hotel ofrece vistas panoramicas al Volcan Turrialba y esta rodeado de bosques nubosos y rios cristalinos. Una combinacion unica de accesibilidad y tranquilidad absoluta.","primaryListTitle":"Instalaciones","primaryListItems":["18 habitaciones tematicas","Restaurante ""La Ceiba""","Piscina natural de manantial","Senderos ecologicos (5 km)","Salon de eventos","Spa con plantas locales"],"secondaryListTitle":"Servicios Destacados","secondaryListItems":["Tours al Volcan Turrialba e Irazu","Birdwatching con guias certificados","Talleres de gastronomia tipica","Transporte desde San Jose","Wi-Fi de alta velocidad","Atencion personalizada 24/7"],"serviceCards":[{"title":"18 habitaciones tematicas","description":"Ambientes con personalidad propia, balcones al bosque nuboso y textiles artesanales inspirados en Cartago."},{"title":"Restaurante ""La Ceiba""","description":"Cocina de finca a la mesa, cafe chorreado y menus de temporada que celebran los sabores locales."},{"title":"Piscina natural de manantial","description":"Agua cristalina, temperatura agradable y vistas verdes para recargar energia de forma natural."},{"title":"Senderos ecologicos (5 km)","description":"Rutas senalizadas entre bosque nuboso, ideales para caminatas al amanecer y observacion de flora."},{"title":"Salon de eventos","description":"Espacio versatil con luz natural, perfecto para retiros corporativos, bodas boutique y talleres."},{"title":"Spa con plantas locales","description":"Tratamientos herbales, masajes relajantes y aromaterapia con esencias del bosque costarricense."},{"title":"Tours al Volcan Turrialba e Irazu","description":"Excursiones guiadas para explorar dos volcanes iconicos con logistica y transporte incluidos."},{"title":"Birdwatching con guias certificados","description":"Avistamiento de purrujas y mas de 120 especies con especialistas locales y equipo optico."},{"title":"Talleres de gastronomia tipica","description":"Aprende a preparar tortillas palmeadas, gallo pinto y salsas caseras con cocineras de la zona."},{"title":"Transporte desde San Jose","description":"Traslados seguros puerta a puerta para que llegues sin preocupaciones desde el aeropuerto o la ciudad."},{"title":"Wi-Fi de alta velocidad","description":"Conectividad confiable en habitaciones y areas comunes para trabajar o compartir tu experiencia."},{"title":"Atencion personalizada 24/7","description":"Equipo disponible todo el dia para ayudarte con reservas, recomendaciones y soporte durante tu estadia."}]}';

EXEC usp_FacilitiesPageContent_Upsert
    @SectionTitle = N'Caracteristicas Principales',
    @SectionTag = N'Lo que nos distingue',
    @DescriptionJson = @FacilitiesContentJson;
GO
