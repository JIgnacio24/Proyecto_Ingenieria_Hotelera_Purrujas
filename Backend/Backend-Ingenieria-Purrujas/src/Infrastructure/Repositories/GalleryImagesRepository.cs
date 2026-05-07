using System.Data;
using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Backend_Ingenieria_Purrujas.Infrastructure.Repositories;

public sealed class GalleryImagesRepository : IGalleryImagesRepository
{
    private readonly string _connectionString;

    public GalleryImagesRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<IReadOnlyList<GalleryImage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var images = new List<GalleryImage>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string query = """
            SELECT Id, Name, Src, Alt, Caption, Category, IsActive
            FROM dbo.GalleryImages
            WHERE IsActive = 1
            ORDER BY Id;
        """;

        await using var command = new SqlCommand(query, connection);
        command.CommandType = CommandType.Text;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            images.Add(ReadGalleryImage(reader));
        }

        return images;
    }

    public async Task<GalleryImage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string query = """
            SELECT Id, Name, Src, Alt, Caption, Category, IsActive
            FROM dbo.GalleryImages
            WHERE Id = @Id;
        """;

        await using var command = new SqlCommand(query, connection);
        command.CommandType = CommandType.Text;
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadGalleryImage(reader);
    }

    public async Task<GalleryImage> UpdateAsync(GalleryImage image, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string query = """
            UPDATE dbo.GalleryImages
            SET Name = @Name,
                Src = @Src,
                Alt = @Alt,
                Caption = @Caption,
                Category = @Category,
                IsActive = @IsActive
            WHERE Id = @Id;

            SELECT Id, Name, Src, Alt, Caption, Category, IsActive
            FROM dbo.GalleryImages
            WHERE Id = @Id;
        """;

        await using var command = new SqlCommand(query, connection);
        command.CommandType = CommandType.Text;
        command.Parameters.Add("@Id", SqlDbType.Int).Value = image.Id;
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = image.Name;
        command.Parameters.Add("@Src", SqlDbType.NVarChar, 500).Value = image.Src;
        command.Parameters.Add("@Alt", SqlDbType.NVarChar, 255).Value = image.Alt;
        command.Parameters.Add("@Caption", SqlDbType.NVarChar, 255).Value = image.Caption;
        command.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = image.Category;
        command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = image.IsActive;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("No fue posible actualizar la imagen de galeria.");
        }

        return ReadGalleryImage(reader);
    }

    private static GalleryImage ReadGalleryImage(SqlDataReader reader)
    {
        var id = reader.GetInt32(0);
        var category = reader.GetString(5);

        // El registro semilla 3 corresponde al fondo del hero del Home.
        // Se normaliza al leer para recuperar instalaciones donde fue guardado como "hotel".
        if (id == 3)
        {
            category = "fondo";
        }

        return new GalleryImage
        {
            Id = id,
            Name = reader.GetString(1),
            Src = reader.GetString(2),
            Alt = reader.GetString(3),
            Caption = reader.GetString(4),
            Category = category,
            IsActive = reader.GetBoolean(6)
        };
    }
}
