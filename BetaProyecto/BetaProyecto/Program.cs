using Avalonia;
using Avalonia.ReactiveUI;
using LibVLCSharp.Shared;
using System;
using System.Diagnostics; 
using System.IO;        

namespace BetaProyecto
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // Primero , inicializamos LibVLCSharp
            try
            {
                Core.Initialize();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inicializando VLC: {ex.Message}");
            }


            // Arrancamos la aplicación Avalonia
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
    }
}