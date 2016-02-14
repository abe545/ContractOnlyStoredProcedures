using System.IO;
using System.Linq;

namespace CodeOnlyStoredProcedure.Tests
{
    internal class SmokeDb
    {
        public static readonly string Connection = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={ Path.GetDirectoryName(typeof(SmokeDb).Assembly.CodeBase.Substring(@"file:\\\".Length)).Split('\\').Reverse().Skip(3).Reverse().Aggregate((s1, s2) => s1 + '\\' + s2) }\SmokeDb.mdf;Integrated Security=True;Connect Timeout=30";
    }
}
