using Avalonia;
using System;
using System.IO;

namespace GTDCompanion;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // Salva a stacktrace da exceção fatal
            try
            {
                File.WriteAllText("fatal.log", ex.ToString());
            }
            catch
            {
                // Silencioso caso não consiga salvar (por permissão, etc)
            }
            throw; // (Opcional: remova esta linha se não quiser crashar de vez)
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
