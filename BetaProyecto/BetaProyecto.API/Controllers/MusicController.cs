using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text;

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
            // Definimos rutas
            string appDir = AppContext.BaseDirectory;

            // Carpeta segura en AppData (donde tenemos permisos de escritura)
            string userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusicSearchApi");
            if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);

            // Rutas finales en la carpeta segura
            _ytDlpPath = Path.Combine(userDir, "yt-dlp.exe");
            _cookiesPath = Path.Combine(userDir, "cookies.txt");

            // Inicializamos el entorno instalando yt-dlp y preparando cookies
            InicializarEntorno(appDir, userDir);

            // Comprobamos actualizaciones de yt-dlp al iniciar la API 
            ActualizarYtDlp();
        }
        /// <summary>
        /// Configura el entorno de ejecución local, asegurando la presencia de las dependencias binarias y sanitizando archivos de configuración.
        /// </summary>
        /// <remarks>
        /// Este método de preparación realiza las siguientes operaciones críticas:
        /// <list type="number">
        /// <item><b>Despliegue de Binarios:</b> Verifica la existencia de <c>yt-dlp.exe</c> en la ruta de ejecución. Si no está presente o el archivo está dañado (0 bytes), realiza una copia desde el directorio de instalación.</item>
        /// <item><b>Sanitización de Cookies:</b> Procesa el archivo <c>cookies.txt</c> para eliminar la marca de orden de bytes (BOM). Esto es indispensable ya que <c>yt-dlp</c> requiere una codificación UTF-8 pura para validar sesiones de usuario.</item>
        /// <item><b>Normalización de Rutas:</b> Centraliza los archivos operativos en carpetas de datos de usuario para evitar problemas de permisos de escritura.</item>
        /// </list>
        /// Cualquier error durante el acceso a archivos o escritura de disco se captura y se registra en la consola para facilitar el diagnóstico.
        /// </remarks>
        /// <param name="appDir">Directorio raíz donde se encuentran los archivos originales de la aplicación.</param>
        /// <param name="userDir">Directorio de datos de usuario (AppData) donde se desplegará el entorno de trabajo.</param>
        private void InicializarEntorno(string appDir, string userDir)
        {
            try
            {
                // Instalar YT-DLP
                // Solo copiamos si no existe o pesa 0 bytes (por seguridad)
                if (!System.IO.File.Exists(_ytDlpPath) || new FileInfo(_ytDlpPath).Length == 0)
                {
                    string origenExe = Path.Combine(appDir, "yt-dlp.exe");
                    if (System.IO.File.Exists(origenExe))
                    {
                        Console.WriteLine($"[API] Instalando yt-dlp en: {_ytDlpPath}");
                        System.IO.File.Copy(origenExe, _ytDlpPath, true);
                    }
                }

                // Limpieza de cookies (SANITIZAR BOM)
                // Leemos el cookies.txt original y lo guardamos SIN la marca invisible(BOM) (\ufeff)
                // Esto es crucial porque yt-dlp no reconoce las cookies si el archivo tiene BOM, lo que causa errores de autenticación.
                // El bom es una marca que algunos editores de texto agregan al inicio de los archivos para indicar que están codificados en UTF-8, pero yt-dlp no lo maneja bien.
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
        /// <summary>
        /// Ejecuta el comando de auto-actualización del binario <c>yt-dlp</c> de forma silenciosa.
        /// </summary>
        /// <remarks>
        /// Dado que las plataformas de video cambian sus algoritmos frecuentemente, este método asegura que la herramienta 
        /// de extracción esté en su versión más reciente mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación:</b> Comprueba la existencia del ejecutable antes de intentar la actualización.</item>
        /// <item><b>Ejecución en segundo plano:</b> Inicia un proceso externo con el argumento <c>--update</c> configurado para no mostrar ventanas (<c>CreateNoWindow</c>).</item>
        /// <item><b>Redirección:</b> Captura las salidas del proceso para evitar bloqueos y espera su finalización síncrona.</item>
        /// <item><b>Resiliencia:</b> El bloque <c>catch</c> ignora silenciosamente fallos (como falta de internet o bloqueos de firewall) para permitir que la aplicación principal siga funcionando incluso si la actualización falla.</item>
        /// </list>
        /// </remarks>
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


        /// <summary>
        /// Procesa una solicitud HTTP GET para extraer la URL de streaming directo y los metadatos de un video de YouTube.
        /// </summary>
        /// <remarks>
        /// El flujo de ejecución de este endpoint es el siguiente:
        /// <list type="number">
        /// <item><b>Validación:</b> Comprueba que la URL recibida no sea nula y que el binario <c>yt-dlp</c> esté disponible en el servidor.</item>
        /// <item><b>Preparación de Argumentos:</b> Configura <c>yt-dlp</c> con los parámetros <c>--dump-json</c> (para obtener datos en lugar de descargar) y <c>--cookies</c> (usando la versión sanitizada para evitar bloqueos).</item>
        /// <item><b>Ejecución de Proceso:</b> Inicia un proceso externo de forma asíncrona, capturando la salida estándar codificada en UTF-8 para evitar errores con caracteres especiales.</item>
        /// <item><b>Análisis de Datos:</b> Parsea la salida JSON generada por la herramienta para extraer el enlace directo (<c>url</c>) y la duración exacta en segundos (<c>duration</c>).</item>
        /// </list>
        /// </remarks>
        /// <param name="url">La dirección URL del video de YouTube proporcionada como parámetro de consulta (query string).</param>
        /// <returns>
        /// Un objeto JSON que contiene la URL de streaming directo y la duración; 
        /// o un código de error (400 o 500) con el detalle del fallo.
        /// </returns>
        [HttpGet("stream")]
        public async Task<IActionResult> GetStreamUrl([FromQuery] string url)
        {
            // Validamos que la URL no llegue vacía o con espacios
            if (string.IsNullOrWhiteSpace(url)) return BadRequest("Falta la URL");

            try
            {
                // Verificamos físicamente si el ejecutable yt-dlp está en la carpeta del servidor
                if (!System.IO.File.Exists(_ytDlpPath)) return StatusCode(500, "FATAL: yt-dlp no instalado.");

                // Preparamos los comandos para el ejecutable:
                // --dump-json: No descarga, solo devuelve datos técnicos.
                // --no-playlist: Ignora listas, solo procesa el video del enlace.
                // -f bestaudio: Busca el enlace con la mejor calidad de sonido disponible.
                string argumentos = $"--dump-json --no-playlist -f bestaudio \"{url}\"";

                // Si tenemos el archivo de cookies (ya saneado), lo añadimos para evitar bloqueos de YouTube
                if (System.IO.File.Exists(_cookiesPath))
                {
                    argumentos += $" --cookies \"{_cookiesPath}\"";
                }

                // Configuramos cómo se va a lanzar el proceso externo (yt-dlp.exe)
                var psi = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,           // Ruta del .exe
                    Arguments = argumentos,          // Los comandos de arriba
                    RedirectStandardOutput = true,   // Permite que la API lea lo que el programa escribe
                    RedirectStandardError = true,    // Permite leer errores si los hay
                    UseShellExecute = false,         // Obligatorio para redirigir flujos de datos
                    CreateNoWindow = true,           // No abre la ventana negra de consola
                    StandardOutputEncoding = Encoding.UTF8 // Asegura que tildes y caracteres raros se lean bien
                };
                //Iniciamos el proceso
                using var process = new Process { StartInfo = psi };
                process.Start();
                
                //Leemos de forma asíncrona toda la respuesta JSON que genera yt-dlp
                string jsonOutput = await process.StandardOutput.ReadToEndAsync();
                
                //Esperamos a que el programa termine de cerrarse
                await process.WaitForExitAsync();

                //Si el programa falló (ExitCode != 0) o no devolvió nada, avisamos del error
                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(jsonOutput))
                {
                    return StatusCode(500, "Error obteniendo audio. Revisa cookies o bloqueo regional.");
                }

                // Convertimos el texto JSON en un objeto manipulable en C#
                var nodo = JsonNode.Parse(jsonOutput);

                // Devolvemos un objeto limpio con solo los dos datos que le importan al cliente:
                // La URL directa del flujo de audio y la duración en segundos.
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