using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly string _connectionString;

    public ReservationRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<int> CreateAsync(Reservation reservation, Bill bill, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var transaction = conn.BeginTransaction();

        try
        {
            // Create Reservation
            await using var resCmd = new SqlCommand("usp_Reservation_Create", conn, transaction)
            {
                CommandType = CommandType.StoredProcedure
            };
            resCmd.Parameters.AddWithValue("@ReservationDate", reservation.ReservationDate);
            resCmd.Parameters.AddWithValue("@StartDate", reservation.StartDate.ToDateTime(TimeOnly.MinValue));
            resCmd.Parameters.AddWithValue("@EndDate", reservation.EndDate.ToDateTime(TimeOnly.MinValue));
            resCmd.Parameters.AddWithValue("@CustomerId", reservation.CustomerId);
            resCmd.Parameters.AddWithValue("@RoomId", reservation.RoomId);
            resCmd.Parameters.AddWithValue("@ReservationStatusId", reservation.ReservationStatusId);

            var scalar = await resCmd.ExecuteScalarAsync(cancellationToken);
            var reservationId = Convert.ToInt32(scalar);

            // Create Bill
            const string billSql = """
                INSERT INTO Bill (ReservationId, BasePrice, Discount, SeasonAmount)
                VALUES (@ReservationId, @BasePrice, @Discount, @SeasonAmount)
                """;

            await using var billCmd = new SqlCommand(billSql, conn, transaction);
            billCmd.Parameters.AddWithValue("@ReservationId", reservationId);
            billCmd.Parameters.AddWithValue("@BasePrice", bill.BasePrice);
            billCmd.Parameters.AddWithValue("@Discount", bill.Discount);
            billCmd.Parameters.AddWithValue("@SeasonAmount", bill.SeasonAmount);
            await billCmd.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return reservationId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<ReservationDetail>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                r.ReservationId,
                r.ReservationDate,
                CAST(r.StartDate AS DATE)         AS StartDate,
                CAST(r.EndDate   AS DATE)         AS EndDate,
                r.CustomerId,
                c.Name                            AS CustomerName,
                c.LastName                        AS CustomerLastName,
                c.Email                           AS CustomerEmail,
                r.RoomId,
                rm.RoomNumber,
                rt.Name                           AS RoomTypeName,
                ISNULL(b.BasePrice, 0)            AS BasePrice,
                ISNULL(b.Discount, 0)             AS Discount,
                ISNULL(b.SeasonAmount, 0)         AS SeasonAmount,
                r.ReservationStatusId,
                rs.Name                           AS ReservationStatusName
            FROM Reservation r
            INNER JOIN Customer          c  ON c.CustomerId           = r.CustomerId
            INNER JOIN Room              rm ON rm.RoomId              = r.RoomId
            INNER JOIN RoomType          rt ON rt.RoomTypeId          = rm.RoomTypeId
            INNER JOIN ReservationStatus rs ON rs.ReservationStatusId = r.ReservationStatusId
            LEFT  JOIN Bill              b  ON b.ReservationId        = r.ReservationId
            WHERE r.IsActive = 1
            ORDER BY r.ReservationDate DESC
            """;

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var results = new List<ReservationDetail>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(ReadDetail(reader));
        }
        return results.AsReadOnly();
    }

    public async Task<ReservationDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                r.ReservationId,
                r.ReservationDate,
                CAST(r.StartDate AS DATE)         AS StartDate,
                CAST(r.EndDate   AS DATE)         AS EndDate,
                r.CustomerId,
                c.Name                            AS CustomerName,
                c.LastName                        AS CustomerLastName,
                c.Email                           AS CustomerEmail,
                r.RoomId,
                rm.RoomNumber,
                rt.Name                           AS RoomTypeName,
                ISNULL(b.BasePrice, 0)            AS BasePrice,
                ISNULL(b.Discount, 0)             AS Discount,
                ISNULL(b.SeasonAmount, 0)         AS SeasonAmount,
                r.ReservationStatusId,
                rs.Name                           AS ReservationStatusName
            FROM Reservation r
            INNER JOIN Customer          c  ON c.CustomerId           = r.CustomerId
            INNER JOIN Room              rm ON rm.RoomId              = r.RoomId
            INNER JOIN RoomType          rt ON rt.RoomTypeId          = rm.RoomTypeId
            INNER JOIN ReservationStatus rs ON rs.ReservationStatusId = r.ReservationStatusId
            LEFT  JOIN Bill              b  ON b.ReservationId        = r.ReservationId
            WHERE r.ReservationId = @Id
              AND r.IsActive      = 1
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDetail(reader) : null;
    }

    public async Task UpdateStatusAsync(int id, int statusId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE Reservation
            SET ReservationStatusId = @StatusId
            WHERE ReservationId = @Id AND IsActive = 1
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@StatusId", statusId);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int?> GetStatusIdByNameAsync(string statusName, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = "SELECT ReservationStatusId FROM ReservationStatus WHERE Name = @Name";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Name", statusName);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is int id ? id : null;
    }

    public async Task UpdateAsync(int id, DateTime reservationDate, DateOnly startDate, DateOnly endDate, int roomId, int customerId, int statusId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand("usp_Reservation_Update", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@ReservationId", id);
        cmd.Parameters.AddWithValue("@ReservationDate", reservationDate);
        cmd.Parameters.AddWithValue("@StartDate", startDate.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@EndDate", endDate.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@CustomerId", customerId);
        cmd.Parameters.AddWithValue("@RoomId", roomId);
        cmd.Parameters.AddWithValue("@ReservationStatusId", statusId);

        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 50014)
        {
            throw new InvalidOperationException("Existe un conflicto de disponibilidad para la habitación y fechas seleccionadas.");
        }
    }

    public async Task UpdateBillAsync(int reservationId, decimal basePrice, decimal seasonAmount, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE Bill
            SET BasePrice = @BasePrice, SeasonAmount = @SeasonAmount
            WHERE ReservationId = @ReservationId
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ReservationId", reservationId);
        cmd.Parameters.AddWithValue("@BasePrice", basePrice);
        cmd.Parameters.AddWithValue("@SeasonAmount", seasonAmount);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE Reservation
            SET IsActive = 0
            WHERE ReservationId = @Id AND IsActive = 1
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReservationDetail>> GetDeletedAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                r.ReservationId,
                r.ReservationDate,
                CAST(r.StartDate AS DATE)         AS StartDate,
                CAST(r.EndDate   AS DATE)         AS EndDate,
                r.CustomerId,
                c.Name                            AS CustomerName,
                c.LastName                        AS CustomerLastName,
                c.Email                           AS CustomerEmail,
                r.RoomId,
                rm.RoomNumber,
                rt.Name                           AS RoomTypeName,
                ISNULL(b.BasePrice, 0)            AS BasePrice,
                ISNULL(b.Discount, 0)             AS Discount,
                ISNULL(b.SeasonAmount, 0)         AS SeasonAmount,
                r.ReservationStatusId,
                rs.Name                           AS ReservationStatusName
            FROM Reservation r
            INNER JOIN Customer          c  ON c.CustomerId           = r.CustomerId
            INNER JOIN Room              rm ON rm.RoomId              = r.RoomId
            INNER JOIN RoomType          rt ON rt.RoomTypeId          = rm.RoomTypeId
            INNER JOIN ReservationStatus rs ON rs.ReservationStatusId = r.ReservationStatusId
            LEFT  JOIN Bill              b  ON b.ReservationId        = r.ReservationId
            WHERE r.IsActive = 0
            ORDER BY r.ReservationDate DESC
            """;

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var results = new List<ReservationDetail>();
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadDetail(reader));

        return results.AsReadOnly();
    }

    private static ReservationDetail ReadDetail(SqlDataReader reader) => new()
    {
        ReservationId         = reader.GetInt32(reader.GetOrdinal("ReservationId")),
        ReservationDate       = reader.GetDateTime(reader.GetOrdinal("ReservationDate")),
        StartDate             = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("StartDate"))),
        EndDate               = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("EndDate"))),
        CustomerId            = reader.GetInt32(reader.GetOrdinal("CustomerId")),
        CustomerName          = reader.GetString(reader.GetOrdinal("CustomerName")),
        CustomerLastName      = reader.GetString(reader.GetOrdinal("CustomerLastName")),
        CustomerEmail         = reader.GetString(reader.GetOrdinal("CustomerEmail")),
        RoomId                = reader.GetInt32(reader.GetOrdinal("RoomId")),
        RoomNumber            = reader.GetString(reader.GetOrdinal("RoomNumber")),
        RoomTypeName          = reader.GetString(reader.GetOrdinal("RoomTypeName")),
        BasePrice             = reader.GetDecimal(reader.GetOrdinal("BasePrice")),
        Discount              = reader.GetDecimal(reader.GetOrdinal("Discount")),
        SeasonAmount          = reader.GetDecimal(reader.GetOrdinal("SeasonAmount")),
        ReservationStatusId   = reader.GetInt32(reader.GetOrdinal("ReservationStatusId")),
        ReservationStatusName = reader.GetString(reader.GetOrdinal("ReservationStatusName"))
    };
}
