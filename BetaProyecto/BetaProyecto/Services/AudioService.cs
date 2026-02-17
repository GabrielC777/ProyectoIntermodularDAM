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
        // URL de nuestra API 
        private const string API_URL = "https://localhost:7500/api/Music/stream";

        private readonly HttpClient _httpClient;
        public class InfoCancionNube
        {
            public string Url { get; set; }
            public int DuracionSegundos { get; set; }
        }

        public AudioService()
        {
            // Esto le dice a la App: "No te quejes si el certificado del servidor es 'raro' o de desarrollo".
            // Sin esto evitamos que falle la conexión HTTPS
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }
        /// <summary>
        /// Solicita de forma asíncrona la extracción y el análisis de metadatos de un recurso de YouTube a través de una API externa.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la comunicación con el microservicio de streaming mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Construcción:</b> Genera una URI de consulta codificando la <paramref name="urlYoutube"/> como parámetro.</item>
        /// <item><b>Petición:</b> Realiza una llamada GET utilizando el <c>HttpClient</c> configurado.</item>
        /// <item><b>Procesamiento:</b> Si la respuesta es exitosa, deserializa el cuerpo JSON para extraer la URL del flujo de audio y la duración total en segundos.</item>
        /// </list>
        /// En caso de fallo en la red o error en el servidor, captura la excepción y devuelve <c>null</c> para evitar cierres inesperados.
        /// </remarks>
        /// <param name="urlYoutube">La dirección URL completa del video de YouTube que se desea procesar.</param>
        /// <returns>
        /// Un objeto <see cref="InfoCancionNube"/> con la URL de streaming y la duración; 
        /// devuelve <c>null</c> si la API no responde correctamente o el recurso no es válido.
        /// </returns>
        public async Task<InfoCancionNube> ObtenerMp3(string urlYoutube)
        {
            try
            {
                // Construimos la petición: https://localhost:7500/api/Music/stream?url=...
                string urlPeticion = $"{API_URL}?url={urlYoutube}";

                // Llamamos al servidor
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
        /// <summary>
        /// Gestiona la descarga, protección mediante cifrado y recuperación de archivos de audio en el almacenamiento local.
        /// </summary>
        /// <remarks>
        /// Este método implementa un sistema de seguridad y caché distribuido en dos niveles:
        /// <list type="number">
        /// <item><b>Caché persistente (.enc):</b> Si el archivo no existe, se descarga de internet, se cifra mediante AES y se guarda en la carpeta de datos locales de la aplicación.</item>
        /// <item><b>Caché temporal (.mp3):</b> Para permitir la reproducción en el motor de audio (VLC), el archivo se desencripta "on-the-fly" hacia una ruta temporal volátil.</item>
        /// <item><b>Optimización:</b> Si el archivo ya ha sido procesado previamente, evita la descarga redundante y prioriza la recuperación desde el disco.</item>
        /// </list>
        /// En caso de error crítico durante el cifrado o acceso a disco, el método retorna la URL original como mecanismo de contingencia.
        /// </remarks>
        /// <param name="url">La dirección URL de origen del flujo de audio.</param>
        /// <param name="idCancion">Identificador único de la canción, utilizado para nombrar los archivos en el almacenamiento físico.</param>
        /// <returns>
        /// Una cadena con la ruta local al archivo MP3 desencriptado listo para su reproducción, 
        /// o la URL original si ocurre una excepción.
        /// </returns>
        public async Task<string> ObtenerRutaAudioSegura(string url, string idCancion)
        {
            try
            {
                // Definimos rutas
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

                // COMPROBAR CACHÉ (¿Ya la hemos descargado antes?)
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

                // DESCARGAR Y PROTEGER (Si no la tenemos)
                System.Diagnostics.Debug.WriteLine($"[RED] Descargando audio...");

                // Descargamos los bytes crudos de internet
                var datosAudio = await _httpClient.GetByteArrayAsync(url);

                System.Diagnostics.Debug.WriteLine($"[SEGURIDAD] Cifrando con AES...");

                // Usamos el Encriptador para "cerrar el candado" antes de guardar
                byte[] datosCifrados = Helpers.Encriptador.EncriptarBytes(datosAudio);

                // Guardamos la versión CIFRADA (Esta es la que se queda en el disco duro)
                await File.WriteAllBytesAsync(rutaCifrada, datosCifrados);

                // Guardamos la versión DESCIFRADA temporalmente (Solo para que VLC la reproduzca ahora)
                await File.WriteAllBytesAsync(rutaPlay, datosAudio);

                return rutaPlay;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Fallo en descarga segura: {ex.Message}");
                // Si algo falla, devolvemos la URL original para intentar streaming directo que lo mas seguro es que falle,
                // pero al menos no se crasea la app.
                return url;
            }
        }
    }
}