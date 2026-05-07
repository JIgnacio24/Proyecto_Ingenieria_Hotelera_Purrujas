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

DECLARE @AboutUsContentJson NVARCHAR(MAX) = N'{
"historyTag":"Desde 2005",
"historyTitle":"Nuestra Historia",
"historyDescription":"Hotel Las Purrujas nacio en el ano 2005 en el corazon de los Andes costarricenses, especificamente en las faldas del Volcan Turrialba, 
    en la provincia de Cartago. Su nombre rinde homenaje a las purrujas, pequenas aves endemicas de la region que simbolizan la vida silvestre y la conexion profunda 
    con la naturaleza.\n\nFundado por la familia Vargas Montoya, el hotel comenzo como una pequena posada de cuatro habitaciones con el sueno de ofrecer una experiencia 
    autentica del campo costarricense. Con los anos y gracias al turismo ecologico, se convirtio en un referente del ecoturismo en la zona central de Costa Rica.",
"historyTimelineStartYear":
"2005",
"historyMilestones":
    ["Fundacion con 4 habitaciones",
    "Expansion del restaurante La Ceiba",
    "18 habitaciones tematicas",
    "Referente de ecoturismo en Cartago"],
"historyTimelineEndLabel":"Hoy",
"teamTag":"Nuestra gente",
"teamTitle":"Equipo & Filosofia",
"collaboratorsCount":30,
"localTalentPercentage":90,
"experienceYears":20,
"directorName":"Andrea Vargas",
"directorTitle":"Directora General",
"directorBiography":"Hija de los fundadores y graduada en Administracion Hotelera de la Universidad de Costa Rica, Andrea lidera el hotel con una vision moderna 
sin perder la esencia familiar que lo caracteriza. Bajo su direccion, Las Purrujas ha crecido como referente de ecoturismo responsable en la region.",
"philosophyTitle":"Nuestra Filosofia",
"philosophyDescription":"En Las Purrujas no solo ofrecemos una cama y un desayuno; ofrecemos una experiencia de vida. Cada detalle, desde la decoracion artesanal hasta 
    el menu del restaurante, esta pensado para que el huesped se lleve consigo un pedazo autentico de Costa Rica. Creemos que el turismo puede y debe ser un motor de desarrollo local, 
    por eso reinvertimos parte de nuestros ingresos en programas educativos y ambientales para la comunidad.",
"philosophyQuote":"Donde la naturaleza te abraza y Costa Rica te enamora.",
"mvvTag":"Quienes somos",
"mvvTitle":"Mision, Vision & Valores",
"missionTitle":"Mision",
"mission":"Brindar a nuestros huespedes una experiencia de hospedaje autentica, calida y sostenible, conectandolos con la riqueza natural y cultural de Costa Rica, a traves de 
    un servicio personalizado y comprometido con el bienestar de la comunidad local y el medio ambiente.",
"visionTitle":"Vision",
"vision":"Ser reconocidos como el principal destino de ecoturismo en la region de Cartago para el ano 2030, liderando un modelo de turismo responsable que inspire a otras empresas 
    hoteleras a adoptar practicas sostenibles.",
"valuesTitle":"Valores",
"values":[
    "Sostenibilidad",
    "Calidez humana",
    "Compromiso comunitario",
    "Respeto por la naturaleza",
    "Excelencia en el servicio"],
"galleryTag":"Inspirate",
"galleryTitle":"Galeria",
"gallerySubtext":"Descubre las instalaciones del hotel y los maravillosos lugares que te rodean para planificar tu itinerario perfecto."}';

EXEC usp_AboutUsPageContent_Upsert
    @ContentJson = @AboutUsContentJson;
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