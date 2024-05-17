using System.CodeDom.Compiler;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;

namespace AndroidAotLogAnalyzer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<Options>(args);
            parserResult
                .WithParsed(options =>
                {
                    var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder
                            .ClearProviders()
                            .AddConsole();
                    });

                    var logger = loggerFactory.CreateLogger<Program>();

                    var inputFilePath = options.InputFilePath;
                    logger.LogInformation("Input file: {InputFilePath}", inputFilePath);
                    if (!File.Exists(inputFilePath))
                    {
                        logger.LogError("Input file does not exist");
                        Environment.Exit(1);
                        return;
                    }

                    var outputFilePath = options.OutputFilePath;
                    if (!string.IsNullOrWhiteSpace(outputFilePath))
                        logger.LogInformation("Output file: {OutputFilePath}", outputFilePath);
                    else
                    {
                        outputFilePath = inputFilePath + ".aot-stat.txt";
                        logger.LogInformation("Output file (auto selected): {OutputFilePath}", outputFilePath);
                    }

                    if (File.Exists(outputFilePath))
                    {
                        logger.LogWarning("Output file exists and will be overriden");
                        File.Delete(outputFilePath);
                    }

                    AotLogDataOverview overview;
                    using (var inputStream = File.Open(inputFilePath, FileMode.Open))
                        overview = new LogFileReader().Process(inputStream);
                    using (var outputStream = File.Open(outputFilePath, FileMode.CreateNew))
                    using (var streamWriter = new StreamWriter(outputStream))
                    using (var streamWriter2 = new IndentedTextWriter(streamWriter))
                        WriteResult(streamWriter2, overview).Wait();
                    
                    logger.LogInformation("Done");
                })
                .WithNotParsed(errors =>
                {
                    // Display help or error messages to the user
                    var helpText = HelpText.AutoBuild(parserResult);
                    foreach (var error in errors)
                    {
                        if (error is HelpRequestedError || error is VersionRequestedError)
                            Console.WriteLine(helpText);
                        else
                            Console.WriteLine($"Error: {error}");
                    }
                });
        }

        private static async Task WriteResult(IndentedTextWriter streamWriter, AotLogDataOverview overview)
        {
            await streamWriter.WriteLineAsync("OVERALL AOT method log records: " + overview.Total);
            await streamWriter.WriteLineAsync().ConfigureAwait(false);
            await Write(streamWriter, overview.NotFound);
            await streamWriter.WriteLineAsync().ConfigureAwait(false);
            await Write(streamWriter, overview.Found);
        }

        private static async Task Write(IndentedTextWriter streamWriter, AotLogData aotLogData)
        {
            await streamWriter.WriteLineAsync(aotLogData.Title).ConfigureAwait(false);
            await streamWriter.WriteLineAsync().ConfigureAwait(false);
            //streamWriter.Indent++;
            foreach (var type in aotLogData.Types.OrderBy(c => c.Name))
            {
                await streamWriter.WriteLineAsync(type.DisplayName).ConfigureAwait(false);;
                streamWriter.Indent++;
                foreach (var method in type.Methods)
                    await streamWriter.WriteLineAsync(method).ConfigureAwait(false);
                streamWriter.Indent--;
            }
            //streamWriter.Indent--;
        }
    }
}

public class Options
{
    [Option('i', "input", Required = true, HelpText = "Input file path.")]
    public string InputFilePath { get; set; }

    [Option('o', "output", Required = false, HelpText = "Output file path. If it's not specified, output file name is constructed from input file name with adding `.aot-stat.txt` ext.")]
    public string OutputFilePath { get; set; }
}