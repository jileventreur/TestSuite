using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using PhenoWareCommon;

namespace TestSuite
{
    class Program
    {
        static void Main(string[] args)
        {
            int x = 7;
            Expression<Func<bool>> expr = () => x > 5;

            Test sup5Test = (() => x > 5, "Cool cool cool");
            Test inf10Test = (() => x < 10, "Well well well");
            Test combined = sup5Test.And(inf10Test, Test.MessageCombineMode.Concat);
            var sup5TestResBefore = sup5Test.Exec();
            var inf10ResBefore = inf10Test.Exec();
            var combineResBefore = combined.Exec();
            x = 10;
            var sup5TestResAfter = sup5Test.Exec();
            var inf10ResAfter = inf10Test.Exec();
            var combineResAfter = combined.Exec();
            
        }
    }
}
