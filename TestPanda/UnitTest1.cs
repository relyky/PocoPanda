using Microsoft.Extensions.Configuration;
using Vista.DbPanda;

namespace TestPanda;

[TestClass]
public class UnitTest1 : TestBase
{
  [TestMethod("�P�B�s��DB")]
  public void TestMethod1()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();
  }

  [TestMethod("�D�P�B�s��DB")]
  public async Task TestMethod2()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = await proxy.OpenAsync();
  }
}