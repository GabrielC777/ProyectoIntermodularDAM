using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;


namespace BetaProyecto.Services
{
    public class AudioService
    {
        // ⚠️ IMPORTANTE: Aquí va TU puerto HTTPS (el que acabamos de probar y funciona)
        private const string API_URL = "https://localhost:7500/api/Music/stream";

        private readonly HttpClient _httpClient;
        public class InfoCancionNube
        {
            public string Url { get; set; }
            public int DuracionSegundos { get; set; }
        }

        public AudioService()
        {
            // --- TRUCO PARA DESARROLLO ---
            // Esto le dice a tu App: "No te quejes si el certificado del servidor es 'raro' o de desarrollo".
            // Sin esto, la petición HTTPS suele fallar en local.
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }
       
        public async Task<InfoCancionNube> ObtenerMp3(string urlYoutube)
        {
            try
            {
                // Construimos la petición: https://localhost:7153/api/Music/stream?url=...
                string urlPeticion = $"{API_URL}?url={urlYoutube}";

                // Llamamos a TU servidor
                var respuesta = await _httpClient.GetAsync(urlPeticion);

                if (respuesta.IsSuccessStatusCode)
                {
                    // Leemos la respuesta (que es un texto JSON)
                    string jsonString = await respuesta.Content.ReadAsStringAsync();

                    // Sacamos el valor de "url"
                    var nodoJson = JsonNode.Parse(jsonString);

                    return new InfoCancionNube
                    {
                        Url = nodoJson["url"]?.ToString(),
                        // Si 'duracion' viene nulo, ponemos 0
                        DuracionSegundos = nodoJson["duracion"] != null ? (int)nodoJson["duracion"] : 0
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error llamando a la API: " + ex.Message);
            }

            return null;
        }
        public async Task<string> ObtenerRutaAudioSegura(string url, string idCancion)
        {
            try
            {
                // 1. DEFINIR RUTAS
                // carpetaStorage: Aquí guardaremos los archivos .enc (CIFRADOS). Es persistente.
                string carpetaStorage = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetaProyecto", "EncryptedStorage");
                // carpetaTemp: Aquí guardaremos temporalmente el .mp3 (DESCIFRADO) para VLC.
                string carpetaTemp = Path.Combine(Path.GetTempPath(), "BetaProyectoMusicTemp");
                // Nos aseguramos de que las carpetas existan
                Directory.CreateDirectory(carpetaStorage);
                Directory.CreateDirectory(carpetaTemp);

                // Rutas finales de los archivos
                string rutaCifrada = Path.Combine(carpetaStorage, $"{idCancion}.enc");
                string rutaPlay = Path.Combine(carpetaTemp, $"{idCancion}.mp3");

                // 2. COMPROBAR CACHÉ (¿Ya la hemos descargado antes?)
                if (File.Exists(rutaCifrada))
                {
                    System.Diagnostics.Debug.WriteLine($"[CACHE] Canción cifrada encontrada en disco.");

                    // Si ya tenemos el temporal listo, lo usamos directo para ir rápido
                    if (File.Exists(rutaPlay)) return rutaPlay;

                    // Si no está el temporal, usamos el Encriptador para "abrir el candado"
                    // Lee el .enc -> Usa la clave AES -> Escribe el .mp3
                    await Helpers.Encriptador.DesencriptarArchivo(rutaCifrada, rutaPlay);

                    return rutaPlay;
                }

                // 3. DESCARGAR Y PROTEGER (Si no la tenemos)
                System.Diagnostics.Debug.WriteLine($"[RED] Descargando audio...");

                // Descargamos los bytes crudos de internet
                var datosAudio = await _httpClient.GetByteArrayAsync(url);

                System.Diagnostics.Debug.WriteLine($"[SEGURIDAD] Cifrando con AES...");

                // Usamos el Encriptador para "cerrar el candado" antes de guardar
                byte[] datosCifrados = Helpers.Encriptador.EncriptarBytes(datosAudio);

                // A) Guardamos la versión CIFRADA (Esta es la que se queda en el disco duro)
                await File.WriteAllBytesAsync(rutaCifrada, datosCifrados);

                // B) Guardamos la versión DESCIFRADA temporalmente (Solo para que VLC la reproduzca ahora)
                await File.WriteAllBytesAsync(rutaPlay, datosAudio);

                return rutaPlay;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Fallo en descarga segura: {ex.Message}");
                // Si algo falla, devolvemos la URL original para intentar streaming directo como plan B
                return url;
            }
        }
    }
}