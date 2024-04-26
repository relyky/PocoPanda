using Microsoft.Extensions.Configuration;
using Vista.DbPanda;

namespace TestPanda;

[TestClass]
public class UnitTest1 : TestBase
{
  [TestMethod("同步連接DB")]
  public void TestMethod1()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();
  }

  [TestMethod("非同步連接DB")]
  public async Task TestMethod2()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = await proxy.OpenAsync();
  }
}