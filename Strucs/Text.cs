using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace DSCS_MBE_Tool.Strucs
{
    public class Language
    {
        public string? Japanese { get; set; }
        public string? English { get; set; }
        public string? Chinese { get; set; }
        public string? EnglishCensored { get; set; }
        public string? Korean { get; set; }
        public string? German { get; set; }
        public Language(string? japanese, string? english, string? chinese, string? englishCensored, string? korean, string? german)
        {
            Japanese = japanese;
            English = english;
            Chinese = chinese;
            EnglishCensored = englishCensored;
            Korean = korean;
            German = german;
        }
        public Language(Dictionary<string, string> msg)
        {
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
                    Japanese = kvp.Value;
                else if (enKeys.Contains(key))
                    English = kvp.Value;
                else if (zhKeys.Contains(key))
                    Chinese = kvp.Value;
                else if (engcKeys.Contains(key))
                    EnglishCensored = kvp.Value;
                else if (koKeys.Contains(key))
                    Korean = kvp.Value;
                else if (deKeys.Contains(key))
                    German = kvp.Value;
            }
        }

        public Dictionary<string, string> GetLangHash(List<string> langOpts)
        {
            if (langOpts != null && langOpts.Count != 0)
            {

                if (langOpts.Contains("all", StringComparer.OrdinalIgnoreCase))
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
                .Where(kvp => langOpts.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
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
    public class Text : IMBEClass
    {

        public Int32 ID { get; set; }
        public string? Japanese { get; set; }
        public string? English { get; set; }
        public string? Chinese { get; set; }
        public string? EnglishCensored { get; set; }
        public string? Korean { get; set; }
        public string? German { get; set; }

        public dynamic ToPatch(List<string> lang, string source = "")
        {
            PatchText patchText = new PatchText
            {
                id = ID.ToString(),
                msg = new Language(Japanese, English, Chinese, EnglishCensored, Korean, German).GetLangHash(lang)
            };
            return patchText;
        }
    }

    public class PatchText: IMBEClass
    {
        public required string id { get; set; }
        public required Dictionary<string, string> msg { get; set; }

        public static explicit operator Text(PatchText patchText)
        {
            Language msgLanguage = new(patchText.msg);


            Text text = new()
            {
                ID = int.Parse(patchText.id),
                Japanese = msgLanguage.Japanese,
                English = msgLanguage.English,
                Chinese = msgLanguage.Chinese,
                EnglishCensored = msgLanguage.EnglishCensored,
                Korean = msgLanguage.Korean,
                German = msgLanguage.German
            };
            return text;
        }

        public dynamic ToPatch(List<string> lang, string source = "")
        {
            throw new NotImplementedException("ToPatch method is not implemented for PatchMessage class.");
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

        public dynamic ToPatch(List<string> lang, string source = "") {

            if(string.IsNullOrEmpty(source))
            {
                throw new ArgumentException("Source file path cannot be null or empty.", nameof(source));
            }


            string? voiceFile = VoiceDb.GetVoiceFile(Path.GetFileNameWithoutExtension(source), ID.ToString()) ?? "undefined";

            string? name = NameDB.GetName(Speaker)?.eng ?? "undefined";


            PatchMessage patchMessage = new()
            {
                id = ID.ToString(),
                speakerId = Speaker,
                voiceFn = voiceFile,
                name = name,
                msg = new Language(Japanese, English, Chinese, EnglishCensored, Korean, German).GetLangHash(lang)
            };
            return patchMessage;
        }
    }

    public class PatchMessage : IMBEClass
    {
        required
        public string id
        { get; set; }

        required
        public int speakerId
        { get; set; }

        public string? voiceFn { get; set; }

        required
        public string name
        { get; set; }

        required
        public Dictionary<string, string> msg
        { get; set; }


        public static explicit operator Message(PatchMessage patchMessage)
        {
            Language msgLanguage = new(patchMessage.msg);


            Message message = new()
            {
                ID = int.Parse(patchMessage.id),
                Speaker = patchMessage.speakerId,
                Japanese = msgLanguage.Japanese,
                English = msgLanguage.English,
                Chinese = msgLanguage.Chinese,
                EnglishCensored = msgLanguage.EnglishCensored,
                Korean = msgLanguage.Korean,
                German = msgLanguage.German
            };
            return message;
        }

        public dynamic ToPatch(List<string> lang, string source = "")
        {
            throw new NotImplementedException("ToPatch method is not implemented for PatchMessage class.");
        }
    }
}
