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
