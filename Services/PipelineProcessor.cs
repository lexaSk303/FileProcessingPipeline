using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using FileProcessingPipeline.Models;

namespace FileProcessingPipeline.Services;

public static class PipelineProcessor
{
    public static async Task<AnalysisReport> ProcessAsync(IEnumerable<FileTask> tasks, int workerCount)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        if (workerCount < 1) throw new ArgumentOutOfRangeException(nameof(workerCount));

        var taskList = tasks.ToList();
        var stopwatch = Stopwatch.StartNew();

        // TODO 1
        var options = new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait };
        var channel = Channel.CreateBounded<FileTask>(options);

        var results = new ConcurrentBag<FileAnalysisResult>();
        var errors = new ConcurrentBag<FileProcessingError>();
        var workerStats = new ConcurrentDictionary<int, int>();

        // TODO 2
        var producerTask = Task.Run(async () =>
        {
            foreach (var task in taskList)
            {
                await channel.Writer.WriteAsync(task);
            }
            channel.Writer.TryComplete();
        });

        // TODO 3
        var workers = new List<Task>();
        for (int i = 1; i <= workerCount; i++)
        {
            int workerId = i;
            workers.Add(Task.Run(async () =>
            {
                await foreach (var task in channel.Reader.ReadAllAsync())
                {
                    await ProcessSingleTaskAsync(task, workerId, results, errors, workerStats);
                }
            }));
        }

        // TODO 4 
        await Task.WhenAll(workers);
        stopwatch.Stop();

        // TODO 5
        return ReportService.Create(
            $"Pipeline ({workerCount} workers)",
            taskList.Count,
            results.ToList(),
            errors.ToList(),
            workerCount,
            stopwatch.ElapsedMilliseconds,
            workerStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        );
    }

    // П1, Задание 3 и П2, Задание 2

    private static async Task ProcessSingleTaskAsync(
        FileTask task,
        int workerId,
        ConcurrentBag<FileAnalysisResult> results,
        ConcurrentBag<FileProcessingError> errors,
        ConcurrentDictionary<int, int> workerStats)
    {
        try
        {
            var result = await FileAnalyzer.AnalyzeAsync(task, workerId);
            results.Add(result);

            // П2, Задание 3
            workerStats.AddOrUpdate(workerId, 1, (key, oldValue) => oldValue + 1);

            Console.WriteLine($"[OK] Worker {workerId}: {result.FileName}");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is FileNotFoundException)
        {
            // П2, Задание 2
            errors.Add(new FileProcessingError(
                task.Id,
                Path.GetFileName(task.FilePath),
                ex.GetType().Name,
                ex.Message));

            Console.WriteLine($"[ERR] Worker {workerId}: {Path.GetFileName(task.FilePath)} - {ex.Message}");
        }
    }
}