using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text; // Necesario para Encoding

namespace BetaProyecto.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MusicController : ControllerBase
    {
        private readonly string _ytDlpPath;
        private readonly string _cookiesPath;

        public MusicController()
        {
            // 1. DEFINIR RUTAS
            string appDir = AppContext.BaseDirectory;

            // Carpeta segura en AppData (donde tenemos permisos de escritura)
            string userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusicSearchApi");
            if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);

            // Rutas finales en la carpeta segura
            _ytDlpPath = Path.Combine(userDir, "yt-dlp.exe");
            _cookiesPath = Path.Combine(userDir, "cookies.txt"); // ¡OJO! Ahora usaremos la copia limpia en AppData

            // 2. INSTALACIÓN Y LIMPIEZA
            InicializarEntorno(appDir, userDir);

            // 3. ACTUALIZAR YT-DLP
            ActualizarYtDlp();
        }

        private void InicializarEntorno(string appDir, string userDir)
        {
            try
            {
                // A. INSTALAR YT-DLP
                // Solo copiamos si no existe o pesa 0
                if (!System.IO.File.Exists(_ytDlpPath) || new FileInfo(_ytDlpPath).Length == 0)
                {
                    string origenExe = Path.Combine(appDir, "yt-dlp.exe");
                    if (System.IO.File.Exists(origenExe))
                    {
                        Console.WriteLine($"[API] Instalando yt-dlp en: {_ytDlpPath}");
                        System.IO.File.Copy(origenExe, _ytDlpPath, true);
                    }
                }

                // B. LIMPIEZA DE COOKIES (SANITIZAR BOM) 🧼
                // Leemos el cookies.txt original y lo guardamos SIN la marca invisible (\ufeff)
                string origenCookies = Path.Combine(appDir, "cookies.txt");
                if (System.IO.File.Exists(origenCookies))
                {
                    // Leemos el texto (esto se traga el BOM automáticamente)
                    string contenido = System.IO.File.ReadAllText(origenCookies);

                    // Lo guardamos forzando UTF8 SIN BOM (new UTF8Encoding(false))
                    // Esto crea un archivo "cookies.txt" perfecto para yt-dlp en la carpeta AppData
                    System.IO.File.WriteAllText(_cookiesPath, contenido, new UTF8Encoding(false));

                    Console.WriteLine($"[API] Cookies saneadas y copiadas a: {_cookiesPath}");
                }
                else
                {
                    Console.WriteLine("[API] No se encontró cookies.txt original.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error inicializando entorno: {ex.Message}");
            }
        }

        private void ActualizarYtDlp()
        {
            try
            {
                if (!System.IO.File.Exists(_ytDlpPath)) return;
                Console.WriteLine("[API] Buscando actualizaciones de yt-dlp...");

                var psi = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = "--update",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process.WaitForExit();
                Console.WriteLine("[API] Actualización completada.");
            }
            catch { /* Ignorar errores de red */ }
        }

        [HttpGet("stream")]
        public async Task<IActionResult> GetStreamUrl([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return BadRequest("Falta la URL");

            try
            {
                if (!System.IO.File.Exists(_ytDlpPath)) return StatusCode(500, "FATAL: yt-dlp no instalado.");

                // Argumentos: Usamos la ruta _cookiesPath que apunta a la versión LIMPIA en AppData
                string argumentos = $"--dump-json --no-playlist -f bestaudio \"{url}\"";

                if (System.IO.File.Exists(_cookiesPath))
                {
                    argumentos += $" --cookies \"{_cookiesPath}\"";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = argumentos,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using var process = new Process { StartInfo = psi };
                process.Start();
                string jsonOutput = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(jsonOutput))
                {
                    return StatusCode(500, "Error obteniendo audio. Revisa cookies o bloqueo regional.");
                }

                var nodo = JsonNode.Parse(jsonOutput);
                return Ok(new
                {
                    url = nodo["url"]?.ToString(),
                    duracion = nodo["duration"]?.GetValue<int>() ?? 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API CRITICAL] {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}