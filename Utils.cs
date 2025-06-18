using DSCS_MBE_Tool.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSCS_MBE_Tool
{

    public static class NameDB
    {
        public static List<SpeakerName> Names { get; set; } = new List<SpeakerName>();
        static NameDB()
        {
            string json = File.ReadAllText("nameDB.json");
            Names = JsonConvert.DeserializeObject<Dictionary<String, List<SpeakerName>>>(json, Converter.Settings)["Names"]
                ?? throw new InvalidOperationException("Failed to deserialize nameDB.json");
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
            string json = File.ReadAllText("voiceDB.json");
            VoiceDbInstance dbInstance = JsonConvert.DeserializeObject<VoiceDbInstance>(json, Converter.Settings)
                   ?? throw new InvalidOperationException("Failed to deserialize voiceDB.json");
            Lexicon = dbInstance.Lexicon ?? throw new InvalidOperationException("Lexicon is null in voiceDB.json");
            Scenes = dbInstance.Scenes ?? throw new InvalidOperationException("Scenes is null in voiceDB.json");
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

}
