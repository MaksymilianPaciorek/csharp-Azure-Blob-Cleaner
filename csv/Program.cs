using Azure.Storage.Blobs;

namespace AzureBlobCleaner
{
    class Program
    {
        private static string? connectionString;
        private const string csvFilePath = "identifiers.csv";
        private static BlobServiceClient? blobServiceClient;

        // SETTINGS
        private const int MaxDegreeOfParallelism = 1; // Max. threads 
        private const int MaxLinesPerBatch = 250; // Max. Lines per batch (Choose for how much lines your .csv should be cut.
                                                  // For example 500 means that your .csv will be cut every 500 so it won't take too much memory.)

        private static int totalLines = 0;
        private static int processedLines = 0;
        private static object lockObject = new object();

        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Info: Trwa ładowanie pliku CSV...");
            Console.ResetColor();

            var credentialsLoader = new CredentialsLoader();
            connectionString = credentialsLoader.GetAzureConnectionString();
            blobServiceClient = new BlobServiceClient(connectionString);

            var lines = File.ReadLines(csvFilePath).ToList();
            totalLines = lines.Count;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Info: Trwa Przetwarzanie danych...\n");
            Console.ResetColor();

            await ProcessCsvFileInBatchesAsync(lines);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nInfo: Usuwanie zostało pomyślnie zakończone");
            Console.ResetColor();
        }

        static async Task ProcessCsvFileInBatchesAsync(List<string> lines)
        {
            int batchCount = (int)Math.Ceiling((double)lines.Count / MaxLinesPerBatch);

            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                var batch = lines.Skip(batchIndex * MaxLinesPerBatch).Take(MaxLinesPerBatch).ToList();

                await ProcessBatchAsync(batch);
            }
        }

        static async Task ProcessBatchAsync(List<string> batch)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism
            };

            await Task.WhenAll(batch.Select(async (line, index) =>
            {
                var fields = line.Split(',');

                if (fields.Length == 2)
                {
                    var personId = fields[0].Trim();
                    var photoId = fields[1].Trim();

                    if (!string.IsNullOrEmpty(personId) && !string.IsNullOrEmpty(photoId))
                    {
                        await DeleteBlobAsync(personId, photoId);
                    }
                }

                Interlocked.Increment(ref processedLines);
                ReportProgress();
            }));
        }

        static async Task DeleteBlobAsync(string containerName, string blobName)
        {
            try
            {
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                if (await containerClient.ExistsAsync())
                {
                    var blobClient = containerClient.GetBlobClient(blobName);
                    if (await blobClient.ExistsAsync())
                    {
                        await blobClient.DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Błąd przy usuwaniu blobu: {ex.Message}");
                Console.ResetColor();
            }
        }

        // Progress Bar    

        static void ReportProgress()
        {
            double percentage = (double)processedLines / totalLines * 100;

            lock (lockObject)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                string progressBar = FormatProgressBar(percentage);
            }
        }

        static string FormatProgressBar(double percentage)
        {
            const int progressBarWidth = 30; 

            int greenLength = (int)(percentage / 100 * progressBarWidth); 
            int darkGreenLength = 1;
            int redLength = progressBarWidth - greenLength - darkGreenLength; 

            if (greenLength < 0) greenLength = 0;
            if (darkGreenLength < 0) darkGreenLength = 0;
            if (redLength < 0) redLength = 0;

            string green = new string('#', greenLength);
            string darkGreen = new string('#', darkGreenLength);
            string red = new string('#', redLength);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"{processedLines} / {totalLines} ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(green); 
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(darkGreen); 
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(red); 
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] ");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"{percentage:0.0}%");
            Console.ResetColor();

            return $"{processedLines} / {totalLines} [{green}{darkGreen}{red}] {percentage:0.0}%";
        }

    }
}
