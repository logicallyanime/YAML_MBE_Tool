using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Globalization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.ObjectFactories;

namespace DSCSTools.MBE
{
    public class CustomSerializer
    {
        public int msgID { get; set; }
        [YamlIgnore]
        public string? SpeakerName { get; set; }
        public string? jpn { get; set; }
        public string? eng { get; set; }
        public string? engDub { get; set; }

        
    }
}