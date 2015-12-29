using System;
using System.Collections.Generic;

namespace QuoraLib.DataTypes
{
    public class CaptchaImgStruct
    {
        public string Challenge { get; set; }
        public byte[] Img { get; set; }
    }

    public class CaptchaStruct
    {
        public string Answer { get; set; }
        public string Challenge { get; set; }
        public DateTime Date { get; set; }
    }

    public class CaptchaCollection
    {
        public CaptchaCollection()
        {
            CaptchaItemsList = new List<CaptchaStruct>();
        }

        public List<CaptchaStruct> CaptchaItemsList { get; set; }
    }
}