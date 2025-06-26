using CommandLine;
using DSCS_MBE_Tool;
using DSCS_MBE_Tool.Strucs;
using DSCSTools.MBE;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
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
        public string isPatch {  get; set; }
    }
    [Verb("mbeextract", HelpText = "Extract a .mbe file or a directory of them into YAML.")]
    public class ExtractOptions : GlobalOptions
    {
        [Value(0, MetaName = "source", Required = true, HelpText = "Source file or directory to extract from.")]
        public string Source { get; set; } = string.Empty;
        [Value(1, MetaName = "source", Required = false, HelpText = "Target folder to extract files into. Defaults to ./Converted")]
        public string? TargetFolder { get; set; }
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
            Global.SetGlobalTask(Parser.Default.ParseArguments<GlobalOptions>(args)
                .WithParsedAsync(async options =>
                {
                    Global.Verbose = options.Verbose;
                    Global.Multithreading = bool.Parse(options.Multithreading);
                    Global.IsPatch = bool.Parse(options.isPatch);
                    Global.DisableProgressBar = options.DisableProgressBar;

                    if (Global.Verbose)
                    {
                        Console.WriteLine($"Verbose mode is enabled. Version: {Global.Version}");
                    }

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
            if (string.IsNullOrEmpty(options.TargetFolder))
            {
                options.TargetFolder = Path.GetDirectoryName(options.Source) + "\\Converted";

            }
            Directory.CreateDirectory(options.TargetFolder);
            EXPA.ExtractMBE(options.Source, options.TargetFolder);
            Console.WriteLine("Done");
            
            return 0;
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
                $"[DIRECTORY] Processing directory: {options.Source}".WriteVerbose(ConsoleColor.Blue);
                string[] files = Directory.GetFiles(options.Source);
                ConsoleProgress.ProgressBar progressBar = new(files.Length);
                int processedCount = 0;
                if (Global.Multithreading)
                {
                    Parallel.ForEach(files, file =>
                    {
                        // Use a local variable to avoid race conditions
                        $"[FILE] Processing file: {file}".WriteVerbose(ConsoleColor.Magenta);
                        MBETable localTable = EXPA.ParseYAML(file);
                        EXPA.PackMBETable(localTable, file, options.TargetFolder);
                        int count = Interlocked.Increment(ref processedCount);
                        progressBar.Report(count);
                    });
                }
                else
                {
                    foreach (var file in files)
                    {
                        $"[FILE] Processing file: {file}".WriteVerbose(ConsoleColor.Magenta);
                        mbeTable = EXPA.ParseYAML(file);
                        EXPA.PackMBETable(mbeTable, file, options.TargetFolder);
                        processedCount++;
                        progressBar.Report(processedCount);
                    }
                }
            }
            else if (File.Exists(options.Source))
            {
                $"[FILE] Processing single file: {options.Source}".WriteVerbose(ConsoleColor.Blue);
                mbeTable = EXPA.ParseYAML(options.Source);
                EXPA.PackMBETable(mbeTable, options.Source, options.TargetFolder);
            }
            else
            {
                $"[ERROR] The source path is neither a directory nor a file: {options.Source}".WriteVerbose(ConsoleColor.Red);
                return 1;
            }
            $"[INFO] Packing completed successfully. Output written to {options.TargetFolder}".WriteLineColored(ConsoleColor.Green);
            Console.WriteLine("Done");
            return 0;
        }
    }
}