
using Microsoft.SqlServer.Management.Smo;

namespace DocDB.Tests
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var dt = new DataType(SqlDataType.Xml);
            Console.WriteLine("==> " + dt.PrettyName());
        }
    }
}
