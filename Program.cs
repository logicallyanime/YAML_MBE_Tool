using System;
using System.IO;
using DSCS_MBE_Tool;
using DSCS_MBE_Tool.Strucs;
using DSCSTools.MBE;
using CommandLine;

namespace DSCSTools
{
    class Program
    {
        [Verb("mbeextract", HelpText = "Extract a .mbe file or a directory of them into YAML.")]
        public class ExtractOptions
        {
            [Value(0, MetaName = "source", Required = true, HelpText = "Source file or directory to extract from.")]
            public string Source { get; set; } = string.Empty;
            [Value(1, MetaName = "source", Required = false, HelpText = "Target folder to extract files into. Defaults to ./Converted")]
            public string? TargetFolder { get; set; }
        }
        [Verb("mbepack", HelpText = "Repack a YAML file or directory of YAML files into a .mbe file.")]
        public class PackOptions
        {
            [Value(0, Required = true, HelpText = "Source file or directory.")]
            public string Source { get; set; } = string.Empty;
            [Value(1, Required = false, HelpText = "Target file to create the .mbe file.")]
            public string? TargetFolder { get; set; }
        }

        // Constants for version and repository information
        private const string VERSION = "1.1.0";
        private const string REPO_URL = "https://github.com/SydMontague/DSCSTools/";
        protected MBETable? MBETable;
        private static MBETable mbeTable;

        // Prints usage instructions for the tool
        private static void PrintUsage()
        {
            Console.WriteLine($"DSCSTools v{VERSION} C# Port | {REPO_URL}");
            Console.WriteLine("Modes:");
            Console.WriteLine("    --mbeextract <source> <targetFolder>");
            Console.WriteLine("        Extracts a .mbe file or a directory of them into YAML,");
            Console.WriteLine("        as long as its structure is defined in the structure.json file.");
            Console.WriteLine("    --mbepack <sourceFolder> <targetFile>");
            Console.WriteLine("        Repacks an .mbe folder containing YAML files back into a .mbe file");
            Console.WriteLine("        as long as its structure is found and defined in the structure.json file.");
            //Console.WriteLine("    --mbepatch <sourceFile> <patchFile> <targetFile>");
            //Console.WriteLine("        Applies a YAML patch file to an existing .mbe file.");
            Console.WriteLine("    --mbeparseTest");
            Console.WriteLine("        Parses an .mbe file and prints the contents to the console for testing purposes.");
        }

        static int Main(string[] args)
        {
            //try
            //{
            //    // Check if we have enough arguments
            //    if (args.Length < 1)
            //    {
            //        PrintUsage();
            //        return 0;
            //    }

            //    string? sourcePath = null;
            //    string? targetPath = null;

            //    // Get the command and paths
            //    string command = args[0];
            //    if(string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath))
            //    {
            //        sourcePath = Path.GetFullPath(args[1]);
            //        targetPath = Path.GetFullPath(args[2]) ?? null;
            //    }

            //    // Validate source path exists
            //    if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            //    {
            //        throw new ArgumentException("Error: input path does not exist.");
            //    }

            //    // Process commands
            //    switch (command)
            //    {
            //        case "--mbeextract":
            //            EXPA.ExtractMBE(sourcePath, targetPath);
            //            Console.WriteLine("Done");
            //            break;

            //        case "--mbepack":
            //            mbeTable = EXPA.ParseYAML(sourcePath);
            //            EXPA.PackMBE(sourcePath, targetPath);
            //            Console.WriteLine("Done");
            //            break;

            //        //case "--mbepatch":
            //        //    EXPA.PatchMBE(Path.GetFullPath(args[1]),Path.GetFullPath(args[2]), Path.GetFullPath(args[3]));
            //        //    Console.WriteLine("Done");
            //        //    break;

            //        case "--mbeparseTest":
            //            mbeTable = EXPA.ParseYAML(sourcePath);
            //            EXPA.PackMBETable(mbeTable, sourcePath, targetPath);
            //            break;

            //        default:
            //            PrintUsage();
            //            break;
            //    }

            //    return 0;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //    return 1;
            //}
            return Parser.Default.ParseArguments<ExtractOptions, PackOptions>(args)
                .MapResult(
                    (ExtractOptions opts) => ExtractMBE(opts),
                    (PackOptions opts) => PackMBE(opts),
                    errs => 1);
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
            if (Directory.Exists(options.Source))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"[DIRECTORY] Processing directory: {options.Source}");
                Console.ResetColor();
                string[] files = Directory.GetFiles(options.Source);
                ConsoleProgress.ProgressBar progressBar = new(files.Length);
                int processedCount = 0;
                foreach (var file in files)
                {
                    mbeTable = EXPA.ParseYAML(file);
                    EXPA.PackMBETable(mbeTable, file, options.TargetFolder);
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"[FILE] Processed file: {file}");
                    Console.ResetColor();
                    processedCount++;
                    progressBar.Report(processedCount);
                }
            }
            else if (File.Exists(options.Source))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"[FILE] Processing single file: {options.Source}");
                Console.ResetColor();
                mbeTable = EXPA.ParseYAML(options.Source);
                EXPA.PackMBETable(mbeTable, options.Source, options.TargetFolder);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] The source path is neither a directory nor a file: {options.Source}");
                Console.ResetColor();
                return 1;
            }
            Console.WriteLine("Done");
            return 0;
        }
    }
}