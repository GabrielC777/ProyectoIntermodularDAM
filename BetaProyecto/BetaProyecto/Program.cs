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
            // 1. INICIALIZAR EL NÚCLEO DE VLC
            try
            {
                Core.Initialize();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inicializando VLC: {ex.Message}");
            }

            // 2. ARRANCAR LA API EN SEGUNDO PLANO (NUEVO)
            // Esto buscará la carpeta "API" junto al ejecutable y lanzará la API si existe.
            try
            {
                var rutaBase = AppContext.BaseDirectory;
                var rutaApi = Path.Combine(rutaBase, "API", "BetaProyecto.API.exe");

                if (File.Exists(rutaApi))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = rutaApi,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(rutaApi)
                    });
                }
                else
                {
                    // Esto es normal mientras desarrollas en Visual Studio (porque la API no está ahí),
                    // así que no te preocupes si no la encuentra mientras pruebas.
                    Debug.WriteLine("No se encontró la API local para iniciar automáticamente.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al intentar iniciar la API: {ex.Message}");
            }

            // 3. ARRANCAR LA APP DE AVALONIA
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