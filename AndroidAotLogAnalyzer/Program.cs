using System.CodeDom.Compiler;
using CommandLine;
using CommandLine.Text;

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
                    var inputFilePath = options.InputFilePath;
                    Console.WriteLine($"Input file: {inputFilePath}");
                    if (!File.Exists(inputFilePath))
                    {
                        Console.Error.WriteLine("Input file does not exist");
                        Environment.Exit(1);
                        return;
                    }

                    var outputFilePath = options.OutputFilePath;
                    if (!string.IsNullOrWhiteSpace(outputFilePath))
                        Console.WriteLine($"Output file: {outputFilePath}");
                    else
                    {
                        outputFilePath = inputFilePath + ".aot-stat.txt";
                        Console.WriteLine($"Output file (auto selected): {outputFilePath}");
                    }

                    if (File.Exists(outputFilePath))
                    {
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Output file exists and will be overriden");
                        Console.ForegroundColor = color;
                        File.Delete(outputFilePath);
                    }

                    AotLogDataOverview overview;
                    using (var inputStream = File.Open(inputFilePath, FileMode.Open))
                        overview = new LogFileReader().Process(inputStream);
                    using (var outputStream = File.Open(outputFilePath, FileMode.CreateNew))
                    using (var streamWriter = new StreamWriter(outputStream))
                    using (var streamWriter2 = new IndentedTextWriter(streamWriter))
                        WriteResult(streamWriter2, overview).Wait();
                    
                    Console.WriteLine("Done");
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
                await streamWriter.WriteLineAsync(type.Name).ConfigureAwait(false);;
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