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
                // Primero inicializamos la ventana que vamos a usar como si fuera un marco
                desktop.MainWindow = new MarcoApp
                {
                    DataContext = new MarcoAppViewModel(), //Le conectamos su ViewModel 
                };

                // Configuramos el cierre de la API para que solo se ejecute cuando el usuario cierre la app
                desktop.Exit += (sender, args) => CerrarApi();

                // Arrancamos la API en segundo plano
                Task.Run(() =>
                {
                    //Buscar si ya existe el proceso (Puerto 7500 ocupado) para que no crashée al arrancar la API si ya esta ocupado
                    try
                    {
                        var zombies = Process.GetProcessesByName("BetaProyecto.API");

                        if (zombies.Length > 0) // Si hay procesos previos, los matamos para liberar el puerto
                        {
                            Debug.WriteLine($"[App] Detectada API previa ({zombies.Length} procesos). Matando para liberar puerto 7500...");
                            foreach (var proc in zombies)
                            {
                                proc.Kill();
                                proc.WaitForExit(); // Esperamos a que Windows libere el puerto
                            }
                            Debug.WriteLine("[App] Limpieza completada.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[App] Error al intentar matar zombies: {ex.Message}");
                    }

                    // Iniciamos API una vez que comprobamos si tenemos los puertos limpios
                    IniciarApiNormal();
                });
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void IniciarApiNormal()
        {
            try
            {
                // Lógica de búsqueda de rutas (Desarrollo vs Producción(App real))
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string apiPath = "";

                // Rutas posibles (En desarrollo suele estar en la carpeta del proyecto API, en producción(App real) puede estar dentro de una subcarpeta "API" o directamente en el mismo nivel que el ejecutable)
                string rutaDev = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\BetaProyecto.API\bin\Debug\net9.0\BetaProyecto.API.exe"));
                string rutaProdSubcarpeta = Path.Combine(baseDir, "API", "BetaProyecto.API.exe");

                //Buscamos si existen las rutas para ver si estamos en desarrollo o producción(App real)
                if (File.Exists(rutaDev))
                {
                    apiPath = rutaDev; 
                } 
                else if (File.Exists(rutaProdSubcarpeta))
                {
                    apiPath = rutaProdSubcarpeta;
                }

                if (!string.IsNullOrEmpty(apiPath)) // Por seguridad, solo intentamos arrancar si encontramos el .exe para evitar errores
                {
                    // Si ya hay un proceso nuestro vivo, no arrancamos otro
                    if (_apiProcess != null && !_apiProcess.HasExited) return;

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = apiPath, // La ruta al .exe de la API
                        UseShellExecute = false, // Para poder controlar los detalles de la ventana
                        CreateNoWindow = false, // Crea la ventana interna para que luego podamos ocultarla
                        WindowStyle = ProcessWindowStyle.Hidden, // Ponemos la ventana oculta. 
                        WorkingDirectory = Path.GetDirectoryName(apiPath) // Para que encuentre pueda encontrar la API los archivos appsettings.json y cookies.txt 
                    };

                    _apiProcess = Process.Start(startInfo); // Arrancamos la API
                    Debug.WriteLine($"[App]  API arrancada (PID: {_apiProcess?.Id})");
                }
                else
                {
                    Debug.WriteLine("[App]  No encuentro la API. ¿Compilada?");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App]  Error arranque API: {ex.Message}");
            }
        }

        private void CerrarApi()
        {
            try
            {
                // Matar nuestro proceso hijo (La API)
                if (_apiProcess != null && !_apiProcess.HasExited)
                {
                    _apiProcess.Kill();
                    _apiProcess = null;
                    Debug.WriteLine("[App] API cerrada correctamente.");
                }

                // Barrido de seguridad por si acaso quedó algún zombie suelto de un crash anterior
                var zombies = Process.GetProcessesByName("BetaProyecto.API");
                foreach (var z in zombies)
                {
                    try 
                    { 
                        z.Kill(); 
                    } 
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[App] Error al matar proceso zombie: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Error al cerrar API: {ex.Message}");
            }
        }
    }
}