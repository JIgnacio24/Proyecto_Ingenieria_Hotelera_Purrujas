using System.Data;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public sealed class RoomAvailabilityRepository : IRoomAvailabilityRepository
{
    private readonly string _connectionString;
    private readonly ISeasonRepository _seasonRepository;

    public RoomAvailabilityRepository(
        IConfiguration configuration,
        ISeasonRepository seasonRepository)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _seasonRepository = seasonRepository;
    }

    public async Task<RoomAvailabilitySummary> GetTodayAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        EnsureConnectionString();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var rooms = await ReadTodayRoomsAsync(connection, date, cancellationToken);
        var roomTypes = await ReadRoomTypesAsync(connection, cancellationToken);

        return new RoomAvailabilitySummary
        {
            Date = date,
            TotalRooms = rooms.Count,
            AvailableRooms = rooms.Count(room => room.IsAvailable),
            OccupiedRooms = rooms.Count(room => room.StatusName.Equals("Ocupada", StringComparison.OrdinalIgnoreCase)),
            OutOfServiceRooms = rooms.Count(room => !room.IsAvailable && !room.StatusName.Equals("Ocupada", StringComparison.OrdinalIgnoreCase)),
            Rooms = rooms,
            RoomTypes = roomTypes
        };
    }

    public async Task<RoomAvailabilitySearchResult> SearchAsync(
        DateOnly startDate,
        DateOnly endDate,
        int? roomTypeId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnectionString();

        if (endDate <= startDate)
        {
            throw new ArgumentException("La fecha de salida debe ser posterior a la fecha de llegada.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var seasons = await _seasonRepository.GetActiveAsync(cancellationToken);

        const string query = """
            SELECT
                r.RoomId,
                r.RoomNumber,
                rt.Name AS RoomTypeName,
                rs.Name AS OperationalStatus,
                rt.BasePrice
            FROM dbo.Room r
            INNER JOIN dbo.RoomType rt ON r.RoomTypeId = rt.RoomTypeId
            INNER JOIN dbo.RoomStatus rs ON r.RoomStatusId = rs.RoomStatusId
            WHERE r.IsActive = 1
              AND rt.IsActive = 1
              AND rs.IsAvailableForBooking = 1
              AND (@RoomTypeId IS NULL OR r.RoomTypeId = @RoomTypeId)
              AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.Reservation reservation
                    INNER JOIN dbo.ReservationStatus reservationStatus
                        ON reservation.ReservationStatusId = reservationStatus.ReservationStatusId
                    WHERE reservation.RoomId = r.RoomId
                      AND reservation.IsActive = 1
                      AND reservationStatus.IsFinal = 0
                      AND reservation.StartDate < @EndDate
                      AND reservation.EndDate > @StartDate
              )
            ORDER BY rt.Name, r.RoomNumber;
            """;

        await using var command = new SqlCommand(query, connection);
        command.CommandType = CommandType.Text;
        command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@RoomTypeId", SqlDbType.Int).Value = roomTypeId.HasValue ? roomTypeId.Value : DBNull.Value;

        var rooms = new List<RoomAvailabilityItem>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var room = new RoomAvailabilityItem
                {
                    RoomId = reader.GetInt32(0),
                    RoomNumber = reader.GetString(1),
                    RoomTypeName = reader.GetString(2),
                    OperationalStatus = reader.GetString(3),
                    BasePricePerNight = reader.GetDecimal(4)
                };

                var quoteBreakdown = BuildQuoteBreakdown(startDate, endDate, room.BasePricePerNight, seasons);
                room.NightsTotal = quoteBreakdown.TotalNights;
                room.HighSeasonNights = quoteBreakdown.HighSeasonNights;
                room.LowSeasonNights = quoteBreakdown.LowSeasonNights;
                room.HighestSeasonMultiplier = quoteBreakdown.HighestMultiplier;
                room.TotalUsd = decimal.Round(quoteBreakdown.TotalUsd, 2);

                rooms.Add(room);
            }
        }

        var roomTypeName = "Todos los tipos";
        if (roomTypeId.HasValue)
        {
            var roomTypes = await ReadRoomTypesAsync(connection, cancellationToken);
            roomTypeName = roomTypes.FirstOrDefault(type => type.RoomTypeId == roomTypeId.Value)?.Name ?? roomTypeName;
        }

        return new RoomAvailabilitySearchResult
        {
            StartDate = startDate,
            EndDate = endDate,
            RoomTypeId = roomTypeId,
            RoomTypeName = roomTypeName,
            AvailableRooms = rooms.Count,
            Rooms = rooms
        };
    }

    private static async Task<List<RoomStatusTodayItem>> ReadTodayRoomsAsync(
        SqlConnection connection,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        const string query = """
            SELECT
                r.RoomId,
                r.RoomNumber,
                rt.Name AS RoomTypeName,
                CASE
                    WHEN currentReservation.ReservationId IS NOT NULL THEN N'Ocupada'
                    ELSE rs.Name
                END AS StatusName,
                CASE
                    WHEN currentReservation.ReservationId IS NULL AND rs.IsAvailableForBooking = 1 THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                END AS IsAvailable,
                currentReservation.CustomerName
            FROM dbo.Room r
            INNER JOIN dbo.RoomType rt ON r.RoomTypeId = rt.RoomTypeId
            INNER JOIN dbo.RoomStatus rs ON r.RoomStatusId = rs.RoomStatusId
            OUTER APPLY (
                SELECT TOP 1
                    reservation.ReservationId,
                    customer.Name + N' ' + customer.LastName AS CustomerName
                FROM dbo.Reservation reservation
                INNER JOIN dbo.ReservationStatus reservationStatus
                    ON reservation.ReservationStatusId = reservationStatus.ReservationStatusId
                INNER JOIN dbo.Customer customer
                    ON reservation.CustomerId = customer.CustomerId
                WHERE reservation.RoomId = r.RoomId
                  AND reservation.IsActive = 1
                  AND reservationStatus.IsFinal = 0
                  AND reservation.StartDate < @NextDate
                  AND reservation.EndDate > @CurrentDate
                ORDER BY reservation.StartDate DESC
            ) currentReservation
            WHERE r.IsActive = 1
              AND rt.IsActive = 1
            ORDER BY r.RoomNumber;
            """;

        await using var command = new SqlCommand(query, connection);
        command.CommandType = CommandType.Text;
        command.Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = date.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@NextDate", SqlDbType.DateTime).Value = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var rooms = new List<RoomStatusTodayItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rooms.Add(new RoomStatusTodayItem
            {
                RoomId = reader.GetInt32(0),
                RoomNumber = reader.GetString(1),
                RoomTypeName = reader.GetString(2),
                StatusName = reader.GetString(3),
                IsAvailable = reader.GetBoolean(4),
                CurrentGuest = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return rooms;
    }

    private static async Task<List<RoomTypeOption>> ReadRoomTypesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string query = """
            SELECT RoomTypeId, Name
            FROM dbo.RoomType
            WHERE IsActive = 1
            ORDER BY Name;
            """;

        await using var command = new SqlCommand(query, connection);
        command.CommandType = CommandType.Text;

        var roomTypes = new List<RoomTypeOption>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roomTypes.Add(new RoomTypeOption
            {
                RoomTypeId = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return roomTypes;
    }

    private void EnsureConnectionString()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("No se configuro la conexion a la base de datos.");
        }
    }

    private static QuoteBreakdown BuildQuoteBreakdown(
        DateOnly start,
        DateOnly end,
        decimal basePrice,
        IReadOnlyCollection<Season> seasons)
    {
        var totalNights = 0;
        var highSeasonNights = 0;
        var lowSeasonNights = 0;
        var totalUsd = 0m;
        var highestMultiplier = 1m;

        var cursor = start;
        while (cursor < end)
        {
            totalNights++;

            var multiplier = ResolveMultiplier(cursor, seasons);
            totalUsd += basePrice * multiplier;

            if (multiplier > 1m)
            {
                highSeasonNights++;
            }
            else
            {
                lowSeasonNights++;
            }

            if (multiplier > highestMultiplier)
            {
                highestMultiplier = multiplier;
            }

            cursor = cursor.AddDays(1);
        }

        return new QuoteBreakdown(totalNights, highSeasonNights, lowSeasonNights, totalUsd, highestMultiplier);
    }

    private static decimal ResolveMultiplier(DateOnly date, IReadOnlyCollection<Season> seasons)
    {
        var matchedSeason = seasons
            .Where(season => date >= season.StartDate && date <= season.EndDate)
            .OrderByDescending(season => season.PercentageChange)
            .FirstOrDefault();

        return matchedSeason is null
            ? 1m
            : 1m + (matchedSeason.PercentageChange / 100m);
    }

    private sealed record QuoteBreakdown(
        int TotalNights,
        int HighSeasonNights,
        int LowSeasonNights,
        decimal TotalUsd,
        decimal HighestMultiplier
    );
}
