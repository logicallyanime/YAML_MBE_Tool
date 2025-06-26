using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;
using System.Reflection;

namespace DSCS_MBE_Tool
{
    public static class NameDB
    {
        public static List<SpeakerName> Names { get; set; } = new List<SpeakerName>();
        static NameDB()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();
            using (var stream = thisAssembly.GetManifestResourceStream("DSCS_MBE_Tool.nameDB.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    Names = (JsonConvert.DeserializeObject<Dictionary<String, List<SpeakerName>>>(json, Converter.Settings)
                        ?? throw new InvalidOperationException("Failed to deserialize nameDB.json"))["Names"];
                }
            }
        }

        public static SpeakerName? GetName(int id)
        {
            var spkr = Names.Find(x => x.Id == id);
            return spkr;
        }
        public class SpeakerName
        {

            [JsonProperty("ID")]
            public int Id { get; set; }

            [JsonProperty("jpn")]
            public string? jpn { get; set; }

            [JsonProperty("eng")]
            public string? eng { get; set; }

            [JsonProperty("chn")]
            public string? chn { get; set; }

            [JsonProperty("engc")]
            public string? engc { get; set; }

            [JsonProperty("kor")]
            public string? kor { get; set; }

            [JsonProperty("ger")]
            public string? ger { get; set; }

        }
    }


    public static class VoiceDb
    {
        public static Dictionary<string, string>? Lexicon { get; set; }

        public static Dictionary<string, Dictionary<string, string>>? Scenes { get; set; }


        static VoiceDb()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();
            using (var stream = thisAssembly.GetManifestResourceStream("DSCS_MBE_Tool.voiceDB.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    VoiceDbInstance dbInstance = JsonConvert.DeserializeObject<VoiceDbInstance>(json, Converter.Settings)
                           ?? throw new InvalidOperationException("Failed to deserialize voiceDB.json");
                    Lexicon = dbInstance.Lexicon ?? throw new InvalidOperationException("Lexicon is null in voiceDB.json");
                    Scenes = dbInstance.Scenes ?? throw new InvalidOperationException("Scenes is null in voiceDB.json");
                    
                }
            }
        }
        public static string? GetVoiceFile(string id)
        {
            if (Lexicon == null || !Lexicon.ContainsKey(id))
            {
                return null;
            }
            return Lexicon[id];
        }
        public static string? GetVoiceFile(string scene,string id)
        {
            if (Scenes == null || !Scenes.ContainsKey(scene) || !Scenes[scene].ContainsKey(id))
            {
                return null;
            }
            return Scenes[scene][id];
        }

        public class VoiceDbInstance
        {
            [JsonProperty("Lexicon")]
            public Dictionary<string, string>? Lexicon { get; set; }

            [JsonProperty("Scenes")]
            public Dictionary<string, Dictionary<string, string>>? Scenes { get; set; }
        }


    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    public static class Utils
    {
        public static string NormalizeType(Type T)
        {
            return T.Name.ToLower() switch
            {
                "int16" => "short",
                "int32" => "int",
                "int64" => "long",
                _ => T.Name.ToLower(),
            };
        }
    }

    public static class Global
    {
        public static string GetVersion()
        {
            var version = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            return version ?? "Null"
                        .Split("+").First() ;
        }
        public static string RepoUrl { get; set; } = "";

        private static bool verbose = false;
        public static bool Verbose
        {
            get
            {
                if (!IsGlobalTaskCompleted)
                {
                    GlobalTask.Wait();
                }
                return verbose;
            }
            set
            {
                verbose = value;
                if (verbose)
                {
                    System.Console.WriteLine($"Verbose mode is enabled. Version: {GetVersion()}");
                }
            }
        }
        private static bool multithreading = true;
        public static bool Multithreading
        {
            get
            {
                if (!IsGlobalTaskCompleted)
                {
                    GlobalTask.Wait();
                }
                return multithreading;
            }
            set => multithreading = value;
        }

        private static bool disableProgressBar = false;
        public static bool DisableProgressBar
        {
            get
            {
                if (!IsGlobalTaskCompleted)
                {
                    GlobalTask.Wait();
                }
                return disableProgressBar;
            }
            set => disableProgressBar = value;
        }

        private static bool isPatch = true;
        public static bool IsPatch
        {
            get
            {
                if (!IsGlobalTaskCompleted)
                {
                    GlobalTask.Wait();
                }
                return isPatch;
            }
            set => isPatch = value;
        }

        public static Task GlobalTask { get; set; } = Task.CompletedTask;

        public static void SetGlobalTask(Task task)
        {
            GlobalTask = task;
        }

        private static bool IsGlobalTaskCompleted => GlobalTask.IsCompleted;

    }

    public static class ConsoleExtensions
    {
        public static void WriteLineColored(this string message, ConsoleColor color)
        {
            var originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = originalColor;
        }
        public static void WriteVerbose(this string message)
        {
            if(Global.Verbose) System.Console.WriteLine(message);
        }

        public static void WriteVerbose(this string message, ConsoleColor color)
        {
            if (Global.Verbose) WriteLineColored(message, ConsoleColor.DarkGray);
        }
    }

}

