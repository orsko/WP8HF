using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WP8HF.DataModel
{
    public class QuestionList : DataContext
    {
        public static string DbConnStr = "Data Source=isostore:/questions.sdf";

        public Table<QuestionItem> QuestionItems;

        public QuestionList(string connStr) : base(connStr)
        {

        }

        public QuestionList() : base(DbConnStr)
        {

        }
    }
}
