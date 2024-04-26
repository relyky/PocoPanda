using Cocona;
using Microsoft.Extensions.Configuration;

namespace PocoPanda;

/// <summary>
/// 一個 Command class 建說只提供一個 commnd 指令。
/// </summary>
class GreetingCommand(IConfiguration _config)
{
  [Command("Greeting", Description = "打招呼。用 Class-based style 實作。")]
  public Task Greeting(
    [Option('n', Description = "指定名稱")] string Name,
    [Option('h', Description = "說嘿大大")] bool Hey)
  {
    Console.WriteLine($"{(Hey ? "嘿" : "哈囉")} {Name ?? "來賓"}。");
    Console.WriteLine($"OutputFolder: {_config["OutputFolder"]}");
    return Task.CompletedTask;
  }

  /// <summary>
  /// 這不是 Command。要加 Ignore，不然 public method 預設都是 Command。
  /// </summary>
  [Ignore]
  public void DoSomething() { }

  /// <summary>
  /// 這不是 Command。不是 public method 就不會默認為 Command。
  /// </summary>
  void NotCommand() { }
}