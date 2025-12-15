using Avalonia;
using Avalonia.Browser;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace SquareRootTrainer;

[SupportedOSPlatform("browser")]
public partial class BrowserProgram
{
    public static Task Main(string[] args)
    {
        return BuildAvaloniaApp()
            .WithInterFont()
            .LogToTrace()
            .StartBrowserAppAsync("out");
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
