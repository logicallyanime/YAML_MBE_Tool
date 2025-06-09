using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSCS_MBE_Tool.Strucs
{
    public class Text
    {

        public int ID { get; set; }
        public string? Japanese { get; set; }
        public string? English { get; set; }
        public string? Chinese { get; set; }
        public string? EnglishCensored { get; set; }
        public string? Korean { get; set; }
        public string? German { get; set; }

    }

    public class Message : Text
    {
        public int Speaker { get; set; }
    }
}
