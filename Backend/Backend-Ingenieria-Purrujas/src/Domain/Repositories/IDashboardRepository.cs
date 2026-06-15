using Backend_Ingenieria_Purrujas.Domain.Dashboard;

namespace Backend_Ingenieria_Purrujas.Domain.Repositories;

public interface IDashboardRepository
{
    Task<DashboardMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
}
