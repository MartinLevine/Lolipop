using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lolipop;

namespace LolipopTest
{
    public class Student : LolipopDataHelper
    {
        [LolipopColumnAttr("PRIMARY KEY")]
        public int StuID { get; set; }
        public int StuGrade { get; set; }
        public bool StuSex { get; set; }
        public string StuName { get; set; }
        public string StuClass { get; set; }
        /**
        * if you want nominate a different file to save your data
        * public Student():base("d://test.db")
        * {
        * ...
        * }
        */
        public override string ToString()
        {
            return string.Format("学生号：{0}\t年段：{1}\t性别：{2}\t姓名：{3}\t班级：{4}\t", StuID, StuGrade, StuSex, StuName, StuClass);
        }
    }
}
