using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json.Nodes;

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
            // 1. DEFINIR RUTAS SEGURAS
            // Ruta de instalación (Solo lectura en Program Files)
            string appDir = AppContext.BaseDirectory;

            // Ruta de datos de usuario (Lectura y Escritura permitida)
            string userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusicSearchApi");

            // Crear carpeta segura si no existe
            if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);

            // Definimos las rutas finales
            _ytDlpPath = Path.Combine(userDir, "yt-dlp.exe");
            _cookiesPath = Path.Combine(appDir, "cookies.txt"); // Las cookies las leemos del origen, no pasa nada

            // 2. PREPARAR YT-DLP EN CARPETA SEGURA
            InicializarYtDlp(appDir);

            // 3. ACTUALIZAR (Ahora sí funcionará porque estamos en AppData)
            ActualizarYtDlp();
        }

        private void InicializarYtDlp(string origenDir)
        {
            try
            {
                // Si ya existe en AppData, no hacemos nada (a menos que pese 0 bytes por error)
                if (System.IO.File.Exists(_ytDlpPath) && new FileInfo(_ytDlpPath).Length > 0) return;

                string origenExe = Path.Combine(origenDir, "yt-dlp.exe");

                if (System.IO.File.Exists(origenExe))
                {
                    // Copiamos de Program Files a AppData la primera vez
                    Console.WriteLine($"[API] 📦 Instalando yt-dlp en: {_ytDlpPath}");
                    System.IO.File.Copy(origenExe, _ytDlpPath, true);
                }
                else
                {
                    Console.WriteLine("[API] ❌ ERROR: No encuentro el yt-dlp.exe original para instalarlo.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] ⚠️ Error instalando yt-dlp: {ex.Message}");
            }
        }

        private void ActualizarYtDlp()
        {
            try
            {
                if (!System.IO.File.Exists(_ytDlpPath)) return;

                Console.WriteLine("[API] 🔄 Buscando actualizaciones de yt-dlp...");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = "--update",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                process.WaitForExit();

                Console.WriteLine("[API] ✅ Proceso de actualización terminado.");
            }
            catch (Exception ex)
            {
                // Importante: Capturamos el error pero NO detenemos la API
                Console.WriteLine($"[API] ⚠️ No se pudo actualizar (¿Sin internet?): {ex.Message}");
            }
        }

        [HttpGet("stream")]
        public async Task<IActionResult> GetStreamUrl([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return BadRequest("Falta la URL");

            try
            {
                if (!System.IO.File.Exists(_ytDlpPath))
                {
                    return StatusCode(500, "FATAL: yt-dlp no está instalado en la carpeta de usuario.");
                }

                string argumentos = $"--dump-json --no-playlist -f bestaudio \"{url}\"";

                if (System.IO.File.Exists(_cookiesPath))
                {
                    argumentos += $" --cookies \"{_cookiesPath}\"";
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = argumentos,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var process = new Process { StartInfo = processStartInfo };

                process.Start();
                string jsonOutput = await process.StandardOutput.ReadToEndAsync();
                string errorOutput = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(jsonOutput))
                {
                    Console.WriteLine($"[API ERROR] {errorOutput}");
                    return StatusCode(500, "Error procesando audio. Posible bloqueo de YouTube.");
                }

                var nodo = JsonNode.Parse(jsonOutput);
                string? urlStream = nodo["url"]?.ToString();
                int duracion = nodo["duration"]?.GetValue<int>() ?? 0;

                return Ok(new { url = urlStream, duracion = duracion });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API CRITICAL] {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}