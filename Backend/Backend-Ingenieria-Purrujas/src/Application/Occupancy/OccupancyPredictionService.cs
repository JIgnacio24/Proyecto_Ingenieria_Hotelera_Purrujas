using Backend_Ingenieria_Purrujas.Domain.Entities;
using Backend_Ingenieria_Purrujas.Domain.Repositories;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Backend_Ingenieria_Purrujas.Application.Occupancy;

public class OccupancyPredictionService : IOccupancyPredictionService
{
    private readonly IOccupancyRepository _repository;

    public OccupancyPredictionService(IOccupancyRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<OccupancyRecord>> GetHistoryAsync() =>
        _repository.GetHistoryAsync();

    public async Task<IEnumerable<OccupancyPrediction>> PredictAsync()
    {
        var history = (await _repository.GetHistoryAsync()).ToList();

        if (history.Count == 0)
            return [];

        var mlContext = new MLContext(seed: 42);

        var trainingData = mlContext.Data.LoadFromEnumerable(
            history.Select(r => new TrainingData
            {
                Year = r.Year,
                Month = r.Month,
                OccupancyPercentage = r.OccupancyPercentage
            }));

        var pipeline = mlContext.Transforms
            .Concatenate("Features", nameof(TrainingData.Year), nameof(TrainingData.Month))
            .Append(mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(mlContext.Regression.Trainers.Sdca(
                labelColumnName: "Label",
                featureColumnName: "Features",
                maximumNumberOfIterations: 200));

        var model = pipeline.Fit(trainingData);
        var engine = mlContext.Model.CreatePredictionEngine<TrainingData, PredictionOutput>(model);

        var predictions = new List<OccupancyPrediction>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var next = new DateOnly(today.Year, today.Month, 1).AddMonths(1);

        for (int i = 0; i < 6; i++)
        {
            var output = engine.Predict(new TrainingData { Year = next.Year, Month = next.Month });
            predictions.Add(new OccupancyPrediction
            {
                Year = next.Year,
                Month = next.Month,
                OccupancyPercentage = Math.Clamp(output.Score, 0f, 100f)
            });
            next = next.AddMonths(1);
        }

        return predictions;
    }

    // Clases internas para ML.NET — no forman parte del dominio público
    private sealed class TrainingData
    {
        public float Year { get; set; }
        public float Month { get; set; }

        [ColumnName("Label")]
        public float OccupancyPercentage { get; set; }
    }

    private sealed class PredictionOutput
    {
        [ColumnName("Score")]
        public float Score { get; set; }
    }
}
