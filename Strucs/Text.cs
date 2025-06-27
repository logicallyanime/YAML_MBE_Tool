using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace DSCS_MBE_Tool.Strucs
{
    public class Text : IMBEClass
    {

        public Int32 ID { get; set; }
        public string? Japanese { get; set; }
        public string? English { get; set; }
        public string? Chinese { get; set; }
        public string? EnglishCensored { get; set; }
        public string? Korean { get; set; }
        public string? German { get; set; }

        public PatchText ToPatch(string filename)
        {
            PatchText patchText = new PatchText
            {
                id = ID.ToString(),
                msg = GetLangHash()
            };
            return patchText;
        }

        public Dictionary<string, string> GetLangHash()
        {

            if(Global.ExportLanguages != null && Global.ExportLanguages.Count != 0)
            {

                if (Global.ExportLanguages.Contains("all", StringComparer.OrdinalIgnoreCase))
                {
                    return new Dictionary<string, string>
                    {
                        {"jpn", Japanese ?? ""},
                        {"eng", English ?? ""},
                        {"zho", Chinese ?? ""},
                        {"engc", EnglishCensored ?? ""},
                        {"kor", Korean ?? ""},
                        {"ger", German ?? ""}
                    };
                }
                    return new Dictionary<string, string>
                    {
                        {"jp", Japanese ?? ""},
                        {"ja", Japanese ?? ""},
                        {"jpn", Japanese ?? ""},
                        {"us", English ?? ""},
                        {"usa", English ?? ""},
                        {"en", English ?? ""},
                        {"eng", English ?? ""},
                        {"cn", Chinese ?? ""},
                        {"chn", Chinese ?? ""},
                        {"zh", Chinese ?? ""},
                        {"zho", Chinese ?? ""},
                        {"cdo", Chinese ?? ""},
                        {"cjy", Chinese ?? ""},
                        {"cmn", Chinese ?? ""},
                        {"cnp", Chinese ?? ""},
                        {"csp", Chinese ?? ""},
                        {"czh", Chinese ?? ""},
                        {"czo", Chinese ?? ""},
                        {"gan", Chinese ?? ""},
                        {"hak", Chinese ?? ""},
                        {"hnm", Chinese ?? ""},
                        {"hsn", Chinese ?? ""},
                        {"luh", Chinese ?? ""},
                        {"lzh", Chinese ?? ""},
                        {"mnp", Chinese ?? ""},
                        {"nan", Chinese ?? ""},
                        {"sjc", Chinese ?? ""},
                        {"wuu", Chinese ?? ""},
                        {"yue", Chinese ?? ""},
                        {"dng", Chinese ?? ""},
                        {"engc", EnglishCensored ?? ""},
                        {"eng_censored", EnglishCensored ?? ""},
                        {"kr", Korean ?? ""},
                        {"ko", Korean ?? ""},
                        {"kor", Korean ?? ""},
                        {"ger", German ?? ""},
                        {"de", German ?? ""},
                        {"deu", German ?? ""}
                    }
                    .Where(kvp => Global.ExportLanguages.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            else
            {
                return new Dictionary<string, string>
                {
                    {"jpn", Japanese ?? ""},
                    {"eng", English ?? ""}
                };
            }

            throw new UnauthorizedAccessException();
        }

    }

    public class PatchText: IMBEClass
    {
        public required string id { get; set; }
        public required Dictionary<string, string> msg { get; set; }


        public Text ToText()
        {
            string jpn = "";
            string eng = "";
            string zho = "";
            string engc = "";
            string kor = "";
            string ger = "";

            foreach (var kvp in msg)
            {
                var jpKeys = new HashSet<string> { "ja", "jp", "jpn" };
                var enKeys = new HashSet<string> { "us", "usa", "en", "eng" };
                var zhKeys = new HashSet<string> { "cn", "chn", "zh", "zho", "cdo", "cjy", "cmn", "cnp", "csp", "czh", "czo", "gan", "hak", "hnm", "hsn", "luh", "lzh", "mnp", "nan", "sjc", "wuu", "yue", "dng" };
                var engcKeys = new HashSet<string> { "engc", "eng_censored" };
                var koKeys = new HashSet<string> { "kor", "ko", "kp" };
                var deKeys = new HashSet<string> { "ger", "de", "deu" };

                var key = kvp.Key.ToLowerInvariant();
                if (jpKeys.Contains(key))
                    jpn = kvp.Value;
                else if (enKeys.Contains(key))
                    eng = kvp.Value;
                else if (zhKeys.Contains(key))
                    zho = kvp.Value;
                else if (engcKeys.Contains(key))
                    engc = kvp.Value;
                else if (koKeys.Contains(key))
                    kor = kvp.Value;
                else if (deKeys.Contains(key))
                    ger = kvp.Value;
            }
            Text text = new Text
            {
                ID = int.Parse(id),
                Japanese = jpn,
                English = eng,
                Chinese = zho,
                EnglishCensored = engc,
                Korean = kor,
                German = ger
            };
            return text;
        }
    }

    public class Message : IMBEClass
    {
        public int ID { get; set; }
        public int Speaker { get; set; }
        public string? Japanese { get; set; }
        public string? English { get; set; }
        public string? Chinese { get; set; }
        public string? EnglishCensored { get; set; }
        public string? Korean { get; set; }
        public string? German { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>
            {
                { "ID", ID.ToString() },
                { "Speaker", Speaker.ToString() },
                { "Japanese", Japanese ?? "" },
                { "English", English ?? "" },
                { "Chinese", Chinese ?? "" },
                { "EnglishCensored", EnglishCensored ?? "" },
                { "Korean", Korean ?? "" },
                { "German", German ?? "" }
            };
            return dict;

        }

        public PatchMessage ToPatch(string filename) {


            string? voiceFile = VoiceDb.GetVoiceFile(Path.GetFileNameWithoutExtension(filename), ID.ToString()) ?? "undefined";

            string? name = NameDB.GetName(Speaker)?.eng ?? "undefined";


            PatchMessage patchMessage = new PatchMessage
            {
                id = ID.ToString(),
                speakerId = Speaker,
                voiceFn = voiceFile,
                name = name,
                msg = GetLangHash()
            };
            return patchMessage;
        }

        public Dictionary<string, string> GetLangHash()
        {

            if(Global.ExportLanguages != null && Global.ExportLanguages.Count != 0)
            {

                if (Global.ExportLanguages.Contains("all", StringComparer.OrdinalIgnoreCase))
                {
                    return new Dictionary<string, string>
                    {
                        {"jpn", Japanese ?? ""},
                        {"eng", English ?? ""},
                        {"zho", Chinese ?? ""},
                        {"engc", EnglishCensored ?? ""},
                        {"kor", Korean ?? ""},
                        {"ger", German ?? ""}
                    };
                }
                    return new Dictionary<string, string>
                    {
                        {"jp", Japanese ?? ""},
                        {"ja", Japanese ?? ""},
                        {"jpn", Japanese ?? ""},
                        {"us", English ?? ""},
                        {"usa", English ?? ""},
                        {"en", English ?? ""},
                        {"eng", English ?? ""},
                        {"cn", Chinese ?? ""},
                        {"chn", Chinese ?? ""},
                        {"zh", Chinese ?? ""},
                        {"zho", Chinese ?? ""},
                        {"cdo", Chinese ?? ""},
                        {"cjy", Chinese ?? ""},
                        {"cmn", Chinese ?? ""},
                        {"cnp", Chinese ?? ""},
                        {"csp", Chinese ?? ""},
                        {"czh", Chinese ?? ""},
                        {"czo", Chinese ?? ""},
                        {"gan", Chinese ?? ""},
                        {"hak", Chinese ?? ""},
                        {"hnm", Chinese ?? ""},
                        {"hsn", Chinese ?? ""},
                        {"luh", Chinese ?? ""},
                        {"lzh", Chinese ?? ""},
                        {"mnp", Chinese ?? ""},
                        {"nan", Chinese ?? ""},
                        {"sjc", Chinese ?? ""},
                        {"wuu", Chinese ?? ""},
                        {"yue", Chinese ?? ""},
                        {"dng", Chinese ?? ""},
                        {"engc", EnglishCensored ?? ""},
                        {"eng_censored", EnglishCensored ?? ""},
                        {"kr", Korean ?? ""},
                        {"ko", Korean ?? ""},
                        {"kor", Korean ?? ""},
                        {"ger", German ?? ""},
                        {"de", German ?? ""},
                        {"deu", German ?? ""}
                    }
                    .Where(kvp => Global.ExportLanguages.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            else
            {
                return new Dictionary<string, string>
                {
                    {"jpn", Japanese ?? ""},
                    {"eng", English ?? ""}
                };
            }

            throw new UnauthorizedAccessException();
        }
    }
    public class PatchMessage: IMBEClass
    {
        required
        public string id { get; set; }

        required
        public int speakerId { get; set; }

        public string? voiceFn { get; set; }

        required
        public string name { get; set; }

        required
        public Dictionary<string, string> msg { get; set; }


        public Message ToMessage()
        {
            string jpn = "";
            string eng = "";
            string zho = "";
            string engc = "";
            string kor = "";
            string ger = "";

            foreach (var kvp in msg)
            {
                var jpKeys = new HashSet<string> { "ja", "jp", "jpn" };
                var enKeys = new HashSet<string> { "us", "usa", "en", "eng" };
                var zhKeys = new HashSet<string> { "cn", "chn", "zh", "zho", "cdo", "cjy", "cmn", "cnp", "csp", "czh", "czo", "gan", "hak", "hnm", "hsn", "luh", "lzh", "mnp", "nan", "sjc", "wuu", "yue", "dng" };
                var engcKeys = new HashSet<string> { "engc", "eng_censored" };
                var koKeys = new HashSet<string> { "kor", "ko", "kp" };
                var deKeys = new HashSet<string> { "ger", "de", "deu" };

                var key = kvp.Key.ToLowerInvariant();
                if (jpKeys.Contains(key))
                    jpn = kvp.Value;
                else if (enKeys.Contains(key))
                    eng = kvp.Value;
                else if (zhKeys.Contains(key))
                    zho = kvp.Value;
                else if (engcKeys.Contains(key))
                    engc = kvp.Value;
                else if (koKeys.Contains(key))
                    kor = kvp.Value;
                else if (deKeys.Contains(key))
                    ger = kvp.Value;
            }

            Message message = new Message
            {
                ID = int.Parse(id),
                Speaker = speakerId,
                Japanese = jpn,
                English = eng,
                Chinese = zho,
                EnglishCensored = engc,
                Korean = kor,
                German = ger
            };
            return message;
        }
    }
}
