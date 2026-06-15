using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IOccupancyRepository
{
    Task<IEnumerable<OccupancyRecord>> GetHistoryAsync();
    Task<bool> HasDataAsync();
}
