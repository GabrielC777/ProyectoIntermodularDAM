using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BetaProyecto.ViewModels;
using BetaProyecto.Views.MarcoApp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BetaProyecto
{
    public partial class App : Application
    {
        private Process? _apiProcess;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 1. INICIAR VENTANA PRIMERO (Prioridad UX)
                desktop.MainWindow = new MarcoApp
                {
                    DataContext = new MarcoAppViewModel(),
                };

                // 2. CONFIGURAR LIMPIEZA AL CERRAR
                // Solo cuando el usuario cierra la app, matamos la API.
                desktop.Exit += (sender, args) => CerrarApi();

                // 3. ARRANCAR API EN SEGUNDO PLANO
                // Iniciamos "normal", sin matar nada previo (asumimos inicio limpio).
                Task.Run(() => IniciarApiNormal());
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void IniciarApiNormal()
        {
            try
            {
                // Lógica de búsqueda de rutas (Desarrollo vs Producción)
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string apiPath = "";

                // Rutas posibles
                string rutaDev = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\BetaProyecto.API\bin\Debug\net9.0\BetaProyecto.API.exe"));
                string rutaProd = Path.Combine(baseDir, "BetaProyecto.API.exe");
                string rutaProdSubcarpeta = Path.Combine(baseDir, "API", "BetaProyecto.API.exe");

                if (File.Exists(rutaDev)) apiPath = rutaDev;
                else if (File.Exists(rutaProd)) apiPath = rutaProd;
                else if (File.Exists(rutaProdSubcarpeta)) apiPath = rutaProdSubcarpeta;

                if (!string.IsNullOrEmpty(apiPath))
                {
                    // Si ya hay un proceso nuestro vivo, no arrancamos otro
                    if (_apiProcess != null && !_apiProcess.HasExited) return;

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = apiPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        WorkingDirectory = Path.GetDirectoryName(apiPath)
                    };

                    _apiProcess = Process.Start(startInfo);
                    Debug.WriteLine($"[App] 🚀 API arrancada (PID: {_apiProcess?.Id})");
                }
                else
                {
                    Debug.WriteLine("[App] ⚠️ No encuentro la API. ¿Compilada?");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] ❌ Error arranque API: {ex.Message}");
            }
        }

        private void CerrarApi()
        {
            // ESTA ES LA CLAVE: Solo matamos cuando la app muere
            try
            {
                // Matar nuestro proceso hijo
                if (_apiProcess != null && !_apiProcess.HasExited)
                {
                    _apiProcess.Kill();
                    _apiProcess = null;
                    Debug.WriteLine("[App] 💀 API cerrada correctamente.");
                }

                // OPCIONAL: Barrido de seguridad por si acaso quedó algún zombie suelto de un crash anterior
                // (Solo lo hacemos al salir para dejar el PC limpio para la próxima vez)
                var zombies = Process.GetProcessesByName("BetaProyecto.API");
                foreach (var z in zombies)
                {
                    try { z.Kill(); } catch { }
                }
            }
            catch { }
        }
    }
}