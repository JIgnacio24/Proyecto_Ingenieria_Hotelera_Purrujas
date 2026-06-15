using Backend_Ingenieria_Purrujas.Domain.Entities;

namespace Backend_Ingenieria_Purrujas.Application.Occupancy;

public interface IOccupancyPredictionService
{
    Task<IEnumerable<OccupancyPrediction>> PredictAsync();
    Task<IEnumerable<OccupancyRecord>> GetHistoryAsync();
}
