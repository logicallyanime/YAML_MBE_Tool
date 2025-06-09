using System;
using System.IO;
using DSCSTools.MBE;

namespace DSCSTools
{
    class Program
    {
        // Constants for version and repository information
        private const string VERSION = "1.1.0";
        private const string REPO_URL = "https://github.com/SydMontague/DSCSTools/";

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
            Console.WriteLine("    --mbepatch <sourceFile> <patchFile> <targetFile>");
            Console.WriteLine("        Applies a YAML patch file to an existing .mbe file.");
        }

        static int Main(string[] args)
        {
            try
            {
                // Check if we have enough arguments
                if (args.Length < 3)
                {
                    PrintUsage();
                    return 0;
                }

                // Get the command and paths
                string command = args[0];
                string sourcePath = Path.GetFullPath(args[1]);
                string targetPath = Path.GetFullPath(args[2]);

                // Validate source path exists
                if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
                {
                    throw new ArgumentException("Error: input path does not exist.");
                }

                // Process commands
                switch (command)
                {
                    case "--mbeextract":
                        EXPA.ExtractMBE(sourcePath, targetPath);
                        Console.WriteLine("Done");
                        break;

                    case "--mbepack":
                        EXPA.PackMBE(sourcePath, targetPath);
                        Console.WriteLine("Done");
                        break;

                    case "--mbepatch":
                        EXPA.PatchMBE(Path.GetFullPath(args[1]),Path.GetFullPath(args[2]), Path.GetFullPath(args[3]));
                        Console.WriteLine("Done");
                        break;

                    default:
                        PrintUsage();
                        break;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
        }
    }
}