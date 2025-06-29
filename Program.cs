using CommandLine;
using CommandLine.Text;
using DSCS_MBE_Tool;
using DSCS_MBE_Tool.Strucs;
using DSCSTools;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DSCSTools
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public class GlobalOptions
    {
        [Option('v', "verbose", Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [Option('t', "Multithread", Default = "true", HelpText = "Disables MultiThreading.")]
        public string Multithreading { get; set; }

        [Option(Default = false, HelpText = "Disables the progress bar.")]
        public bool DisableProgressBar { get; set; }

        [Option('m', "isPatch", Default = "true", HelpText = "Determines whether or not the MBE is extracted/packed as a patch. Valid Options: <true|false>")]
        public string isPatch { get; set; }
    }
    


    [Verb("mbeextract", HelpText = "Extract a .mbe file or a directory of them into YAML.")]
    public class ExtractOptions : GlobalOptions
    {
        [Value(0, MetaName = "source", Required = true, HelpText = "Source file or directory to extract from.")]
        public string Source { get; set; } = string.Empty;
        [Value(1, MetaName = "target", Required = false, HelpText = "Target folder to extract files into. Defaults to ./Converted")]
        public string? TargetFolder { get; set; }

        [Option('l', "lang", Required = false, HelpText = "Select languages to output to.")]
        public IEnumerable<string>? Lang { get; set; }
    }


    [Verb("mbepack", HelpText = "Repack a YAML file or directory of YAML files into a .mbe file.")]
    public class PackOptions : GlobalOptions
    {
        [Value(0, Required = true, HelpText = "Source file or directory.")]
        public string Source { get; set; } = string.Empty;
        [Value(1, Required = false, HelpText = "Target file to create the .mbe file.")]
        public string? TargetFolder { get; set; }
    }


    public class Program
    {
        protected MBETable? MBETable;
        private static MBETable? mbeTable;


        static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "help")
            {
                PrintUsage();
                return 0;
            }
            Global.SetGlobalTask(Parser.Default.ParseArguments<GlobalOptions>(args)
                .WithParsedAsync(async options =>
                {
                    Global.Verbose = options.Verbose;
                    Global.Multithreading = bool.Parse(options.Multithreading);
                    Global.IsPatch = bool.Parse(options.isPatch);
                    Global.DisableProgressBar = options.DisableProgressBar;

                    // Return a completed task to satisfy the lambda's return type  
                    await Task.CompletedTask;
                })
            ); // Ensure the async operation completes  

            if (Global.GlobalTask.IsFaulted)
            {

                throw Global.GlobalTask.Exception;
            }
            var timer = Stopwatch.StartNew();
            int retcode = Parser.Default.ParseArguments<ExtractOptions, PackOptions>(args)
                .MapResult(
                    (ExtractOptions opts) => ExtractMBE(opts),
                    (PackOptions opts) => PackMBE(opts),
                    errs => 1);
            timer.Stop();
            TimeSpan timeTaken = timer.Elapsed;
            Console.WriteLine($"Execution time: {timeTaken.TotalNanoseconds / 1000000.0} |{timeTaken.TotalNanoseconds}ns");
            return retcode;
        }


        static int ExtractMBE(ExtractOptions options)
        {

            if (options.Lang != null)
            {
                if(options.Lang.Count() != 0)
                    options.Lang = [.. options.Lang.Select(lang => lang.ToLowerInvariant())];
                else
                    options.Lang = ["eng", "jpn"]; // Default languages if none specified
            }

            options.TargetFolder ??= string.Empty;

            // Use WriteVerbose extension with color instead of direct Console calls.
            $"[INFO] Input Path: {options.Source}".WriteVerbose(ConsoleColor.Yellow);

            $"[STEP 1] Validating input path... {options.Source}".WriteVerbose(ConsoleColor.Green);

            if (!Directory.Exists(options.Source) && (!File.Exists(options.Source) || !Path.GetExtension(options.Source).Equals(".mbe", StringComparison.CurrentCultureIgnoreCase)))
                throw new FileNotFoundException("Error: Source must be a valid .mbe file or a directory containing .mbe files.");

            if (string.IsNullOrEmpty(options.TargetFolder))
            {
                options.TargetFolder = Path.GetDirectoryName(options.Source) + "\\Converted";

            } else if (File.Exists(options.TargetFolder))
                throw new ArgumentException($"Error: Target path \"{options.TargetFolder}\" is a file, must be a directory.");

            if (Path.GetFullPath(options.Source) == Path.GetFullPath(options.TargetFolder))
                throw new ArgumentException("Error: input and output paths must be different!");

            Directory.CreateDirectory(options.TargetFolder);

            $"[STEP 2] Paths validated successfully.".WriteVerbose(ConsoleColor.Green);

            if (Directory.Exists(options.Source))
            {
                $"[DIRECTORY] Processing directory: {options.Source}".WriteLineColored(ConsoleColor.Blue);
                string[] files = Directory.GetFiles(options.Source);

                ConsoleProgress.ProgressBar progressBar = new(files.Length);
                int processedCount = 0;

                if (Global.Multithreading)
                {
                    Parallel.ForEach(files, file =>
                    {
                        // Use a local variable to avoid race conditions
                        $"[FILE] Processing file: {file}".WriteVerbose(ConsoleColor.Magenta);
                        ExtractSingleMBEFile(file, options);

                        int count = Interlocked.Increment(ref processedCount);
                        progressBar.Report(count);
                    });
                }
                else
                {
                    foreach (var file in files)
                    {
                        $"[FILE] Processing file: {file}".WriteVerbose(ConsoleColor.Magenta);
                        ExtractSingleMBEFile(file, options);

                        processedCount++;
                        progressBar.Report(processedCount);
                    }
                }
            }
            else if (File.Exists(options.Source))
            {
                $"[FILE] Processing single file: {options.Source}".WriteLineColored(ConsoleColor.Blue);
                ExtractSingleMBEFile(options);
            }
            else
            {
                $"[ERROR] The source path is neither a directory nor a file: {options.Source}".WriteLineColored(ConsoleColor.Red);
                throw new ArgumentException("Error: input is neither directory nor file.");
            }
            $"[INFO] Extracting completed successfully. Output written to \"{options.TargetFolder}\"".WriteLineColored(ConsoleColor.Green);
            Console.WriteLine("Done");

            return 0;
        }

        private static void ExtractSingleMBEFile(ExtractOptions options)
        {
            MBE.Reader mbeReader = new(options.Source);
            MBE.File mbeFile = mbeReader.Read();
            string yaml = MBE.Converter.ToYaml(mbeFile, options.Source, bool.Parse(options.isPatch), [.. options.Lang!]);
            var outputPath = Path.Combine(options.TargetFolder!, $"./{Path.GetFileNameWithoutExtension(options.Source)}.yaml");
            using var output = new StreamWriter(outputPath);
            output.Write(yaml);
        }
        private static void ExtractSingleMBEFile(string source, ExtractOptions options)
        {
            MBE.Reader mbeReader = new(source);
            MBE.File mbeFile = mbeReader.Read();

            string yaml = MBE.Converter.ToYaml(mbeFile, source, bool.Parse(options.isPatch), [.. options.Lang!]);

            var outputPath = Path.Combine(options.TargetFolder!, $"./{Path.GetFileNameWithoutExtension(source)}.yaml");
            using var output = new StreamWriter(outputPath);

            output.Write(yaml);
        }

        static int PackMBE(PackOptions options)
        {
            if (string.IsNullOrEmpty(options.TargetFolder))
            {
                options.TargetFolder = Path.GetDirectoryName(options.Source) + "\\Converted";
                Directory.CreateDirectory(options.TargetFolder);
            }
            $"[INFO] Output folder: {options.TargetFolder}".WriteVerbose(ConsoleColor.Green);
            if (Directory.Exists(options.Source))
            {
                $"[DIRECTORY] Processing directory: {options.Source}".WriteLineColored(ConsoleColor.Blue);
                string[] files = Directory.GetFiles(options.Source);
                ConsoleProgress.ProgressBar progressBar = new(files.Length);
                int processedCount = 0;
                if (Global.Multithreading)
                {
                    Parallel.ForEach(files, file =>
                    {
                        // Use a local variable to avoid race conditions
                        $"[FILE] Processing file: {file}".WriteVerbose(ConsoleColor.Magenta);
                        PackSingleMBEFile(file, options);
                        int count = Interlocked.Increment(ref processedCount);
                        progressBar.Report(count);
                    });
                }
                else
                {
                    foreach (var file in files)
                    {
                        $"[FILE] Processing file: {file}".WriteVerbose(ConsoleColor.Magenta);
                        PackSingleMBEFile(file, options);
                        processedCount++;
                        progressBar.Report(processedCount);
                    }
                }
            }
            else if (File.Exists(options.Source))
            {
                $"[FILE] Processing single file: {options.Source}".WriteLineColored(ConsoleColor.Blue);
                PackSingleMBEFile(options);
                //mbeTable = EXPA.ParseYAML(options.Source);
                //EXPA.PackMBETable(mbeTable, options.Source, options.TargetFolder);
            }
            else
            {
                $"[ERROR] The source path is neither a directory nor a file: {options.Source}".WriteLineColored(ConsoleColor.Red);
                return 1;
            }
            $"[INFO] Packing completed successfully. Output written to {options.TargetFolder}".WriteLineColored(ConsoleColor.Green);
            Console.WriteLine("Done");
            return 0;
        }
        private static void PackSingleMBEFile(string source, PackOptions options)
        {
            MBE.File mbeFile = MBE.Converter.FromYamlFile(source, bool.Parse(options.isPatch));
            MBE.Writer mbeWriter = new(mbeFile, source);
            mbeWriter.Write(options.TargetFolder!);
        }
        private static void PackSingleMBEFile(PackOptions options)
        {
            MBE.File mbeFile = MBE.Converter.FromYamlFile(options.Source, bool.Parse(options.isPatch));
            MBE.Writer mbeWriter = new(mbeFile, options.Source);
            mbeWriter.Write(options.TargetFolder!);
        }

        public static void PrintUsage()
        {

            @"Yaml-MBE_Tool - Digimon Story Cyber Sleuth MBE Tool

USAGE:
  DSCS_MBE_Tool <command> [options]

COMMANDS:
  mbeextract    Extract .mbe file(s) to YAML format
  mbepack       Pack YAML file(s) back to .mbe format

GLOBAL OPTIONS:
  -v, --verbose              Enable verbose output
  -t, --Multithread <bool>   Enable/disable multithreading (default: true)
  --DisableProgressBar       Disable the progress bar
  -m, --isPatch <bool>       Extract/pack as patch format (default: true)
  --help                     Display help information
  --version                  Display version information

EXTRACT COMMAND:
  DSCSTools mbeextract <source> [target] [options]

  ARGUMENTS:
    source                   Source .mbe file or directory containing .mbe files
    target                   Target directory for extracted YAML files (optional, defaults to ./Converted)

  OPTIONS:
    -l, --lang <languages>   Specify languages to extract. Accepts ISO 639 language codes & ISO 3166 Country Codes (default: eng, jpn)

PACK COMMAND:
  DSCSTools mbepack <source> [target] [options]

  ARGUMENTS:
    source                   Source YAML file or directory containing YAML files
    target                   Target directory for packed .mbe files (optional, defaults to ./Converted)

EXAMPLES:
  DSCSTools mbeextract message.mbe
  DSCSTools mbeextract ./mbe_files/ ./extracted/
  DSCSTools mbeextract message.mbe -l jpn,eng
  DSCSTools mbepack message.yaml ./output/
  DSCSTools mbepack message.yaml".WriteLineColored(ConsoleColor.White);
        }
    }
}