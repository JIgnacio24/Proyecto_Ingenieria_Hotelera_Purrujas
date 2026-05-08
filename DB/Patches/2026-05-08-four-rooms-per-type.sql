-- Patch: 4 habitaciones por tipo de habitación
-- Agrega las habitaciones faltantes para llegar a 4 por tipo
-- Doble (RoomTypeId=1): agrega 103, 104
-- Suite Volcán (RoomTypeId=2): agrega 203, 204
-- Villa Familiar (RoomTypeId=3): agrega 302, 303, 304
-- Estado inicial: Disponible (RoomStatusId=1)

INSERT INTO Room (RoomNumber, IsActive, RoomTypeId, RoomStatusId)
VALUES
    ('103', 1, 1, 1),
    ('104', 1, 1, 1),
    ('203', 1, 2, 1),
    ('204', 1, 2, 1),
    ('302', 1, 3, 1),
    ('303', 1, 3, 1),
    ('304', 1, 3, 1);
GO
