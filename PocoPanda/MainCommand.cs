using Cocona;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PocoPanda;

class MainCommand(IConfiguration _config)
{
  [Command(Description = "SQL Server POCO tool。")]
  public Task CommandProcedure()
  {
    Console.WriteLine($"#BEGIN {nameof(MainCommand)}");

    Console.WriteLine($"OutputFolder: {_config["OutputFolder"]}");
    return Task.CompletedTask;
  }
}
