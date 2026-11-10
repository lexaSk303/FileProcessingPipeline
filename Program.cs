using System.Text.Json;
using FileProcessingPipeline.Models;
using FileProcessingPipeline.Services;

namespace FileProcessingPipeline;

internal class Program
{
    private static async Task Main()
    {
        Console.WriteLine("================================");
        Console.WriteLine("     FILE PROCESSING PIPELINE");
        Console.WriteLine("================================");

        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDirectory);

        // П2, Задание 2
        var tasks = new List<FileTask>
        {
            new(1, Path.Combine(dataDirectory, "file1.txt")),
            new(2, Path.Combine(dataDirectory, "file2.txt")),
            new(3, Path.Combine(dataDirectory, "missing_file.txt")),
            new(4, Path.Combine(dataDirectory, "file3.txt")),
            new(5, Path.Combine(dataDirectory, "file4.txt"))
        };

        Console.WriteLine($"Найдено заданий: {tasks.Count}\n");
        Console.WriteLine("1. Последовательная обработка");
        Console.WriteLine("2. Параллельная обработка (Pipeline)");
        Console.Write("Выберите режим: ");

        string? choice = Console.ReadLine();
        AnalysisReport report;

        switch (choice)
        {
            case "1":
                report = await SequentialProcessor.ProcessAsync(tasks);
                break;
            case "2":
                Console.Write("Количество обработчиков (1, 2, 4, 8): ");
                if (!int.TryParse(Console.ReadLine(), out int workerCount) || workerCount < 1)
                {
                    Console.WriteLine("Некорректное значение. Использую 2.");
                    workerCount = 2;
                }
                report = await PipelineProcessor.ProcessAsync(tasks, workerCount);
                break;
            default:
                Console.WriteLine("Неизвестный режим.");
                return;
        }

        PrintReport(report);
        await SaveReportAsync(report);
        PrintAnswersToQuestions(); // П2, Задание 4 
    }

    private static void PrintReport(AnalysisReport report)
    {
        Console.WriteLine("\n========== ОТЧЁТ ==========");
        Console.WriteLine($"Режим: {report.Mode}");
        Console.WriteLine($"Всего файлов: {report.TotalFiles}");
        Console.WriteLine($"Успешно: {report.SuccessfulFiles}");
        Console.WriteLine($"Ошибок: {report.FailedFiles}");
        Console.WriteLine($"Время: {report.ProcessingTimeMs} мс");

        // П2, Задание 3
        Console.WriteLine("\n--- СТАТИСТИКА WORKERS ---");
        foreach (var stat in report.WorkerStatistics.OrderBy(x => x.Key))
        {
            Console.WriteLine($"Worker {stat.Key}: {stat.Value} заданий");
        }
        Console.WriteLine("--------------------------");

        if (report.Errors.Count > 0)
        {
            Console.WriteLine("\nОШИБКИ:");
            foreach (var error in report.Errors)
            {
                Console.WriteLine($"  {error.FileName}: {error.ErrorType} - {error.Message}");
            }
        }
    }

    private static async Task SaveReportAsync(AnalysisReport report)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(report, options);
        string path = Path.Combine(AppContext.BaseDirectory, "report.json");
        await File.WriteAllTextAsync(path, json);
        Console.WriteLine($"\nОтчёт сохранён: {path}");
    }

    // П2, Задание 4:
    private static void PrintAnswersToQuestions()
    {
        Console.WriteLine("\n========== Ответы П2, Задание 4 ==========");
        Console.WriteLine("1. Почему параллельная обработка с 1 исполнителем может быть медленнее?");
        Console.WriteLine("   Из-за накладных расходов на создание канала, задач и переключение контекста.");
        Console.WriteLine("2. Почему увеличение обработчиков не всегда ускоряет программу?");
        Console.WriteLine("   Ограничения CPU, диска или блокировки на уровне ОС.");
        Console.WriteLine("3. Как объём данных влияет на производительность?");
        Console.WriteLine("   На малых данных выгоднее Sequential. На больших Parallel окупает накладные расходы.");
        Console.WriteLine("4. Почему важно контролировать количество операций?");
        Console.WriteLine("   Чтобы избежать переполнения памяти и истощения ресурсов.");
        Console.WriteLine("=======================================================");
    }
}