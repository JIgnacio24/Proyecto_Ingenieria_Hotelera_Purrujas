using System.Data;
using System.Globalization;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Text;

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
        int? excludeReservationId = null,
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

        var rooms = await ReadAvailableRoomsAsync(
            connection,
            startDate,
            endDate,
            roomTypeId,
            excludeReservationId,
            seasons,
            cancellationToken);

        var roomTypeName = "Todos los tipos";
        if (roomTypeId.HasValue)
        {
            var roomTypes = await ReadRoomTypesAsync(connection, cancellationToken);
            roomTypeName = roomTypes.FirstOrDefault(type => type.RoomTypeId == roomTypeId.Value)?.Name ?? roomTypeName;
        }

        var roomTypeAvailability = rooms.Count == 0
            ? await ReadRoomTypeAvailabilityAsync(connection, startDate, endDate, excludeReservationId, cancellationToken)
            : [];

        var suggestedDateRanges = rooms.Count == 0
            ? await ReadNearbyDateSuggestionsAsync(connection, startDate, endDate, roomTypeId, excludeReservationId, cancellationToken)
            : [];

        return new RoomAvailabilitySearchResult
        {
            StartDate = startDate,
            EndDate = endDate,
            RoomTypeId = roomTypeId,
            RoomTypeName = roomTypeName,
            AvailableRooms = rooms.Count,
            Rooms = rooms,
            RoomTypeAvailability = roomTypeAvailability,
            SuggestedDateRanges = suggestedDateRanges
        };
    }

    private async Task<List<RoomAvailabilityItem>> ReadAvailableRoomsAsync(
        SqlConnection connection,
        DateOnly startDate,
        DateOnly endDate,
        int? roomTypeId,
        int? excludeReservationId,
        IReadOnlyCollection<Season> seasons,
        CancellationToken cancellationToken)
    {
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
                      AND (@ExcludeReservationId IS NULL OR reservation.ReservationId <> @ExcludeReservationId)
              )
            ORDER BY rt.Name, r.RoomNumber;
            """;

        await using var command = new SqlCommand(query, connection);
        command.CommandType = CommandType.Text;
        command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@RoomTypeId", SqlDbType.Int).Value = roomTypeId.HasValue ? roomTypeId.Value : DBNull.Value;
        command.Parameters.Add("@ExcludeReservationId", SqlDbType.Int).Value = excludeReservationId.HasValue ? excludeReservationId.Value : DBNull.Value;

        var rooms = new List<RoomAvailabilityItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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

        return rooms;
    }

    private static async Task<List<RoomTypeAvailabilityItem>> ReadRoomTypeAvailabilityAsync(
        SqlConnection connection,
        DateOnly startDate,
        DateOnly endDate,
        int? excludeReservationId,
        CancellationToken cancellationToken)
    {
        const string query = """
            ;WITH AvailableRooms AS (
                SELECT
                    r.RoomTypeId,
                    COUNT(*) AS AvailableRooms
                FROM dbo.Room r
                INNER JOIN dbo.RoomType rt ON r.RoomTypeId = rt.RoomTypeId
                INNER JOIN dbo.RoomStatus rs ON r.RoomStatusId = rs.RoomStatusId
                WHERE r.IsActive = 1
                  AND rt.IsActive = 1
                  AND rs.IsAvailableForBooking = 1
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
                          AND (@ExcludeReservationId IS NULL OR reservation.ReservationId <> @ExcludeReservationId)
                  )
                GROUP BY r.RoomTypeId
            )
            SELECT
                rt.RoomTypeId,
                rt.Name AS RoomTypeName,
                COALESCE(ar.AvailableRooms, 0) AS AvailableRooms
            FROM dbo.RoomType rt
            LEFT JOIN AvailableRooms ar ON ar.RoomTypeId = rt.RoomTypeId
            WHERE rt.IsActive = 1
            ORDER BY rt.Name;
            """;

        await using var command = new SqlCommand(query, connection);
        command.CommandType = CommandType.Text;
        command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@ExcludeReservationId", SqlDbType.Int).Value = excludeReservationId.HasValue ? excludeReservationId.Value : DBNull.Value;

        var roomTypes = new List<RoomTypeAvailabilityItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roomTypes.Add(new RoomTypeAvailabilityItem
            {
                RoomTypeId = reader.GetInt32(0),
                RoomTypeName = reader.GetString(1),
                AvailableRooms = reader.GetInt32(2)
            });
        }

        return DeduplicateRoomTypeAvailability(roomTypes);
    }

    private async Task<List<RoomAvailabilityDateSuggestion>> ReadNearbyDateSuggestionsAsync(
        SqlConnection connection,
        DateOnly startDate,
        DateOnly endDate,
        int? roomTypeId,
        int? excludeReservationId,
        CancellationToken cancellationToken)
    {
        var suggestedRanges = new List<RoomAvailabilityDateSuggestion>();
        var stayLength = endDate.DayNumber - startDate.DayNumber;

        foreach (var offset in BuildNearbyOffsets())
        {
            var candidateStart = startDate.AddDays(offset);
            var candidateEnd = candidateStart.AddDays(stayLength);

            if (candidateEnd <= candidateStart)
            {
                continue;
            }

            var availableRooms = await CountAvailableRoomsAsync(
                connection,
                candidateStart,
                candidateEnd,
                roomTypeId,
                excludeReservationId,
                cancellationToken);

            if (availableRooms == 0)
            {
                continue;
            }

            suggestedRanges.Add(new RoomAvailabilityDateSuggestion
            {
                StartDate = candidateStart,
                EndDate = candidateEnd,
                AvailableRooms = availableRooms
            });

            if (suggestedRanges.Count == 3)
            {
                break;
            }
        }

        return suggestedRanges;
    }

    private static IEnumerable<int> BuildNearbyOffsets()
    {
        const int radius = 7;

        for (var step = 1; step <= radius; step++)
        {
            yield return step;
            yield return -step;
        }
    }

    private static async Task<int> CountAvailableRoomsAsync(
        SqlConnection connection,
        DateOnly startDate,
        DateOnly endDate,
        int? roomTypeId,
        int? excludeReservationId,
        CancellationToken cancellationToken)
    {
        const string query = """
            SELECT COUNT(*)
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
                      AND (@ExcludeReservationId IS NULL OR reservation.ReservationId <> @ExcludeReservationId)
              );
            """;

        await using var command = new SqlCommand(query, connection);
        command.CommandType = CommandType.Text;
        command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@RoomTypeId", SqlDbType.Int).Value = roomTypeId.HasValue ? roomTypeId.Value : DBNull.Value;
        command.Parameters.Add("@ExcludeReservationId", SqlDbType.Int).Value = excludeReservationId.HasValue ? excludeReservationId.Value : DBNull.Value;

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is int count ? count : Convert.ToInt32(scalar);
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

        return DeduplicateRoomTypeOptions(roomTypes);
    }

    private static List<RoomTypeOption> DeduplicateRoomTypeOptions(IEnumerable<RoomTypeOption> roomTypes)
    {
        return roomTypes
            .GroupBy(roomType => NormalizeRoomTypeKey(roomType.Name))
            .Select(group => group
                .OrderBy(roomType => HasAccents(roomType.Name) ? 0 : 1)
                .ThenBy(roomType => roomType.RoomTypeId)
                .First())
            .OrderBy(roomType => roomType.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static List<RoomTypeAvailabilityItem> DeduplicateRoomTypeAvailability(IEnumerable<RoomTypeAvailabilityItem> roomTypes)
    {
        return roomTypes
            .GroupBy(roomType => NormalizeRoomTypeKey(roomType.RoomTypeName))
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(roomType => HasAccents(roomType.RoomTypeName) ? 0 : 1)
                    .ThenBy(roomType => roomType.RoomTypeId)
                    .ToList();

                return new RoomTypeAvailabilityItem
                {
                    RoomTypeId = ordered[0].RoomTypeId,
                    RoomTypeName = ordered[0].RoomTypeName,
                    AvailableRooms = ordered.Sum(roomType => roomType.AvailableRooms)
                };
            })
            .OrderBy(roomType => roomType.RoomTypeName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string NormalizeRoomTypeKey(string value)
    {
        return StripDiacritics(value).ToLowerInvariant();
    }

    private static string StripDiacritics(string value)
    {
        var trimmed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(trimmed.Length);

        foreach (var character in trimmed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool HasAccents(string value)
    {
        return !string.Equals(value.Trim(), StripDiacritics(value), StringComparison.Ordinal);
    }

    private void EnsureConnectionString()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("No se configuró la conexión a la base de datos.");
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
