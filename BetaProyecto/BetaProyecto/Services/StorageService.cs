using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BetaProyecto.Services
{
    public class StorageService
    {
        // Url de nuestra API
        private const string ApiUrl = "https://localhost:7500/api/Storage";


        /// <summary>
        /// Carga un archivo de imagen desde el sistema de archivos local hacia el servidor de almacenamiento en la nube.
        /// </summary>
        /// <remarks>
        /// Este método actúa como un envoltorio especializado que invoca la lógica genérica de comunicación con la API 
        /// utilizando el endpoint específico para el procesamiento de imágenes. Es ideal para la gestión de 
        /// avatares de usuario, portadas de álbumes y miniaturas de playlists.
        /// </remarks>
        /// <param name="rutaArchivoEnTuPc">La ruta absoluta del archivo de imagen en el almacenamiento local.</param>
        /// <returns>
        /// Una tarea que representa la operación asíncrona. El valor devuelto contiene la URL pública 
        /// generada por el servicio de almacenamiento (Cloudinary) si la carga fue exitosa.
        /// </returns>
        public async Task<string> SubirImagen(string rutaArchivoEnTuPc)
        {
            return await EnviarA_Api(rutaArchivoEnTuPc, "subir-imagen");
        }

        /// <summary>
        /// Carga un archivo de audio desde el almacenamiento local hacia el servidor de distribución en la nube.
        /// </summary>
        /// <remarks>
        /// Este método encapsula la lógica de transferencia de archivos multimedia utilizando el endpoint dedicado 
        /// para audio. El proceso sigue el siguiente flujo:
        /// <list type="number">
        /// <item><b>Empaquetado:</b> Invoca al método core <c>EnviarA_Api</c> para convertir el archivo físico en un flujo de datos (Stream).</item>
        /// <item><b>Transmisión:</b> Envía el recurso mediante una petición POST multipart/form-data al microservicio de almacenamiento.</item>
        /// <item><b>Respuesta:</b> Retorna la URL segura (HTTPS) proporcionada por Cloudinary, necesaria para la persistencia en la base de datos.</item>
        /// </list>
        /// </remarks>
        /// <param name="rutaArchivoEnTuPc">La ruta absoluta del archivo de audio (ej. .mp3, .wav) en el disco local.</param>
        /// <returns>
        /// Una tarea que contiene la URL pública del archivo alojado si la operación tiene éxito; 
        /// de lo contrario, devuelve una cadena vacía o nula.
        /// </returns>
        public async Task<string> SubirCancion(string rutaArchivoEnTuPc)
        {
            return await EnviarA_Api(rutaArchivoEnTuPc, "subir-audio");
        }

        // --- MÉTODO PARA ELIMINAR ---
        /// <summary>
        /// Solicita de forma asíncrona la eliminación de un recurso almacenado en la nube a través de la API de almacenamiento.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la baja de archivos (imágenes o audio) siguiendo este flujo:
        /// <list type="number">
        /// <item><b>Validación:</b> Verifica que la URL proporcionada no sea nula o vacía.</item>
        /// <item><b>Seguridad:</b> Configura un <see cref="HttpClientHandler"/> para omitir la validación de certificados SSL, permitiendo peticiones a entornos <c>localhost</c> con certificados autofirmados.</item>
        /// <item><b>Petición:</b> Ejecuta un verbo HTTP <c>DELETE</c> enviando la URL del archivo como parámetro de consulta.</item>
        /// <item><b>Confirmación:</b> Evalúa la respuesta de la API para confirmar si el archivo fue removido con éxito del proveedor externo (Cloudinary).</item>
        /// </list>
        /// </remarks>
        /// <param name="urlCompleta">La dirección URL absoluta del recurso que se desea eliminar del almacenamiento remoto.</param>
        /// <returns>
        /// Una tarea que devuelve <c>true</c> si el servidor confirma la eliminación (Success Status Code); 
        /// de lo contrario, <c>false</c> si el recurso no existe o hubo un fallo en la comunicación.
        /// </returns>
        public async Task<bool> EliminarArchivo(string urlCompleta)
        {
            // Validación básica: Si no hay URL, no hay nada que borrar.
            if (string.IsNullOrEmpty(urlCompleta)) return false;

            try
            {
                // Configuración para aceptar certificados locales
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                using (var client = new HttpClient(handler))
                {
                    // LA LLAMADA (DELETE)
                    // Construimos la URL así: https://localhost:7500/api/Storage/eliminar?url=https://
                    var response = await client.DeleteAsync($"{ApiUrl}/eliminar?url={urlCompleta}");

                    // RESULTADO
                    // Devuelve true si la API respondió 200 OK (Borrado exitoso)
                    // Devuelve false si falló (400, 500, etc.)
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                // Si hay error de conexión, solo lo registramos y devolvemos false
                System.Diagnostics.Debug.WriteLine("[StorageService] Error al eliminar: " + ex.Message);
                return false;
            }
        }

        // Motor interno para enviar archivos a la API, reutilizado por ambos métodos públicos (SubirImagen y SubirCancion).
        /// <summary>
        /// Realiza la carga física de un archivo hacia la API de almacenamiento mediante una petición POST de tipo multipart/form-data.
        /// </summary>
        /// <remarks>
        /// Este método núcleo (core) orquestra la transferencia de archivos multimedia siguiendo este flujo:
        /// <list type="number">
        /// <item><b>Verificación local:</b> Valida la existencia del archivo en el sistema de archivos del cliente antes de iniciar la conexión.</item>
        /// <item><b>Configuración de Seguridad:</b> Implementa un bypass de validación SSL para permitir el desarrollo en entornos locales con certificados no firmados.</item>
        /// <item><b>Empaquetado (Multipart):</b> Abre el archivo como un flujo de datos (<see cref="StreamContent"/>) y lo encapsula en un contenedor compatible con formularios web.</item>
        /// <item><b>Transmisión y Respuesta:</b> Ejecuta la petición asíncrona hacia el <paramref name="endpoint"/> especificado y procesa la respuesta JSON para extraer la URL persistente generada por el servidor.</item>
        /// </list>
        /// </remarks>
        /// <param name="ruta">La ruta absoluta del archivo en el disco duro local.</param>
        /// <param name="endpoint">El segmento final de la URL de la API (ej. "subir-imagen" o "subir-audio").</param>
        /// <returns>La URL pública del archivo cargado en el servidor de almacenamiento.</returns>
        /// <exception cref="FileNotFoundException">Se lanza si la ruta especificada no apunta a un archivo válido.</exception>
        /// <exception cref="Exception">Encapsula errores de conectividad, rechazos de la API (códigos 4xx o 5xx) o fallos en el análisis del JSON.</exception>
        private async Task<string> EnviarA_Api(string ruta, string endpoint)
        {
            // Comprobamos si existe el archivo
            if (!File.Exists(ruta)) throw new FileNotFoundException("¡No encuentro el archivo en tu PC!");

            try
            {
                // Como estamos en desarrollo, el certificado de seguridad de 'localhost' no es oficial.
                // Esta línea le dice al código: "Confía en el servidor".
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                // Creamos el cliente 
                using (var client = new HttpClient(handler))
                {
                    // Preparamos el paquete 
                    using (var content = new MultipartFormDataContent())
                    {
                        // Abrimos el archivo de tu disco duro para leerlo.
                        var fileStream = File.OpenRead(ruta);

                        // Convertimos el archivo en un flujo de datos (StreamContent) para enviarlo.
                        var streamContent = new StreamContent(fileStream);

                        // Le ponemos una etiqueta para decirle que un archivo de datos 
                        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");

                        // Metemos el archivo en el sobre.
                        // "archivo" -> Es el nombre EXACTO que espera nuestra API
                        content.Add(streamContent, "archivo", Path.GetFileName(ruta));

                        // El envío (POST)
                        // Este sale hacia: https://localhost:7500/api/Storage/subir-imagen (o audio)
                        var response = await client.PostAsync($"{ApiUrl}/{endpoint}", content);

                        //Comprueba si fue bien. Si falla, se lanza HttpRequestException.
                        response.EnsureSuccessStatusCode();

                        // Si llegamos aquí, es que todo fue bien (Código 200)
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var json = JObject.Parse(jsonString);
                        return json["url"].ToString();
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Aquí atrapamos errores específicos de la web (404, 500...)
                throw new Exception($"La API rechazó el archivo: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                // Si explota la conexión (servidor apagado, sin internet, etc.)
                throw new Exception($"Error conectando con la API: {ex.Message}");
            }
        }
    }
}
