using System;
using System.Collections.Generic;
using System.Linq;
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
                msg = new Dictionary<string, string>
                {
                    { "jp", Japanese ?? "" },
                    { "eng", English ?? "" }
                }
            };
            return patchText;
        }

    }

    public class PatchText: IMBEClass
    {
        public required string id { get; set; }
        public required Dictionary<string, string> msg { get; set; }
        public Text ToText()
        {
            Text text = new Text
            {
                ID = int.Parse(id),
                Japanese = msg.ContainsKey("jp") ? msg["jp"] : "",
                English = msg.ContainsKey("eng") ? msg["eng"] : "",
                Chinese = msg.ContainsKey("chi") ? msg["chi"] : "",
                EnglishCensored = msg.ContainsKey("eng_censored") ? msg["eng_censored"] : "",
                Korean = msg.ContainsKey("kor") ? msg["kor"] : "",
                German = msg.ContainsKey("ger") ? msg["ger"] : ""
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
                msg = new Dictionary<string, string>
                {
                    { "jp", Japanese ?? "" },
                    { "eng", English ?? "" }
                }
            };
            return patchMessage;
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
            Message message = new Message
            {
                ID = int.Parse(id),
                Speaker = speakerId,
                English = msg.ContainsKey("eng") ? msg["eng"] : ""
            };
            return message;
        }
    }
}
