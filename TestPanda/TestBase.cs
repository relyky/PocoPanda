using Microsoft.Extensions.Configuration;

namespace TestPanda;

[TestClass]
public class TestBase
{
  protected static IConfigurationRoot Configuration { get; private set; } = default!;

  [AssemblyInitialize]
  public static void Initialize(TestContext context)
  {
    var configurationBuilder = new ConfigurationBuilder()
      .SetBasePath(AppContext.BaseDirectory)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange:true);

    Configuration = configurationBuilder.Build();
  }
}
