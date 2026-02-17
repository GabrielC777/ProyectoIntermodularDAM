using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace BetaProyecto.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageController : ControllerBase
    {
        // CLAVES PARA IMGBB (FOTOS)
        private const string ImgbbApiKey = "718db7c8748de9625904739b3e6a4265";

        // CLAVES PARA CLOUDINARY (AUDIO)
        private const string CloudName = "dyyi9sb9v";
        private const string ApiKey = "926712452194393";
        private const string ApiSecret = "VCgQV20lLbnA5DO9I9NGMdKvjII";

        private readonly Cloudinary _cloudinary;

        public StorageController()
        {
            // Creamos un objeto 'Account' empaquetando nuestras 3 claves.
            var account = new Account(CloudName, ApiKey, ApiSecret);
            // Creamos la conexión real con Cloudinary usando esa cuenta.
            _cloudinary = new Cloudinary(account);
            // Obligamos a que la conexión use HTTPS (seguridad, candado verde).
            _cloudinary.Api.Secure = true;
        }
        /// <summary>
        /// Recibe un archivo de imagen, lo procesa en memoria y lo carga en el servicio externo ImgBB.
        /// </summary>
        /// <remarks>
        /// Este endpoint actúa como un puente (proxy) entre la aplicación cliente y ImgBB:
        /// <list type="number">
        /// <item><b>Validación:</b> Asegura que el archivo no sea nulo.</item>
        /// <item><b>Conversión:</b> Transforma la imagen binaria a una cadena Base64 (texto), que es el formato requerido por la API de ImgBB.</item>
        /// <item><b>Transmisión:</b> Realiza una petición POST segura enviando la API Key y el contenido.</item>
        /// <item><b>Resolución:</b> Extrae la URL directa de la respuesta JSON para que la App pueda guardarla en su base de datos.</item>
        /// </list>
        /// </remarks>
        /// <param name="archivo">El archivo de imagen enviado desde el formulario (IFormFile).</param>
        /// <returns>Un objeto JSON con la URL pública de la imagen alojada.</returns>
        // ENDPOINT 1: SUBIR IMAGEN -> Va a ImgBB
        [HttpPost("subir-imagen")] //ruta --> api/Storage/subir-imagen
        public async Task<IActionResult> SubirImagen(IFormFile archivo)
        {
            // Verificación de seguridad: ¿El archivo existe y tiene datos?
            if (archivo == null || archivo.Length == 0) return BadRequest("No hay imagen");

            try
            {
                // 'using': Crea un cliente HTTP (un navegador web invisible) y lo destruye al terminar para ahorrar RAM.
                using (var client = new HttpClient())
                {
                    // ImgBB necesita Base64
                    // Variable para guardar la imagen convertida a texto.
                    string base64Image;

                    // Abrimos un flujo de memoria temporal en la RAM.
                    using (var ms = new MemoryStream())
                    {
                        // Copiamos el archivo que llegó de internet(nuestra aplicación) a la memoria RAM.
                        await archivo.CopyToAsync(ms);
                        //Convertimos los bytes de la imagen a una cadena de texto Base64.
                        base64Image = Convert.ToBase64String(ms.ToArray());
                    }
                    // Preparamos el "formulario" virtual para enviar.
                    var content = new FormUrlEncodedContent(new[]
                    {
                        // Campo 1: La clave API.
                        new KeyValuePair<string, string>("key", ImgbbApiKey),
                        // Campo 2: La imagen convertida a texto.
                        new KeyValuePair<string, string>("image", base64Image)
                    });

                    // Enviamos la petición POST a la URL de ImgBB con el formulario.
                    var response = await client.PostAsync("https://api.imgbb.com/1/upload", content);
                    // Leemos la respuesta del servidor (que viene en texto JSON).
                    var jsonString = await response.Content.ReadAsStringAsync();

                    // Si ImgBB dice que algo salió mal (código 400 o 500)...
                    if (!response.IsSuccessStatusCode)
                        return StatusCode(500, "Error ImgBB: " + jsonString);

                    // Analizamos el texto JSON para convertirlo en objeto.
                    var json = JObject.Parse(jsonString);

                    // Buscamos dentro del JSON: objeto 'data' -> propiedad 'url'.
                    string urlFinal = json["data"]["url"].ToString();

                    // Devolvemos un código 200 OK con la URL lista para usar.
                    return Ok(new { url = urlFinal });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error subiendo imagen: " + ex.Message);
            }
        }
        /// <summary>
        /// Procesa un archivo multimedia de audio y lo carga de forma asíncrona en el servicio de almacenamiento de Cloudinary.
        /// </summary>
        /// <remarks>
        /// El flujo de este endpoint incluye:
        /// <list type="number">
        /// <item><b>Validación:</b> Comprobación de integridad del archivo recibido.</item>
        /// <item><b>Tratamiento de Stream:</b> Apertura del archivo como flujo de datos para evitar la carga total en RAM.</item>
        /// <item><b>Categorización:</b> Uso de parámetros de video (requeridos por Cloudinary para archivos de audio) y asignación de carpeta destino.</item>
        /// <item><b>Persistencia:</b> Obtención de una URL segura (HTTPS) para su almacenamiento en la base de datos.</item>
        /// </list>
        /// </remarks>
        /// <param name="archivo">El archivo de audio (mp3, wav, etc.) enviado a través de la petición HTTP.</param>
        /// <returns>Un objeto JSON con la URL segura del recurso alojado o un mensaje de error detallado.</returns>
        // ENDPOINT 2: SUBIR AUDIO -> Va a Cloudinary
        [HttpPost("subir-audio")]
        public async Task<IActionResult> SubirAudio(IFormFile archivo)
        {
            // Verificación: ¿El archivo existe?
            if (archivo == null || archivo.Length == 0) return BadRequest("No hay audio");

            try
            {
                // Abrimos el archivo como un flujo de lectura (Stream).
                using (var stream = archivo.OpenReadStream())
                {
                    // CONFIGURACIÓN CLAVE:
                    // Usamos 'VideoUploadParams' porque Cloudinary trata el audio como video.
                    var uploadParams = new VideoUploadParams()
                    {
                        // Adjuntamos el archivo (Nombre + Flujo de datos).
                        File = new FileDescription(archivo.FileName, stream),
                        // Carpeta en Cloudinary
                        Folder = "musicsearch_audios" 
                    };

                    // Llamamos a la librería para que suba el archivo.
                    // Esto hace todo el proceso de conexión y subida por nosotros.
                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    // Verificamos si la librería reportó algún error interno.
                    if (uploadResult.Error != null)
                        return StatusCode(500, "Error Cloudinary: " + uploadResult.Error.Message);

                    // Si todo fue bien, devolvemos la 'SecureUrl' (la dirección HTTPS del audio).
                    return Ok(new { url = uploadResult.SecureUrl.ToString() });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error subiendo audio: " + ex.Message);
            }
        }
        /// <summary>
        /// Solicita la eliminación permanente de un recurso multimedia alojado en Cloudinary a partir de su URL pública.
        /// </summary>
        /// <remarks>
        /// Este endpoint implementa una lógica de filtrado y limpieza:
        /// <list type="number">
        /// <item><b>Discriminación de dominio:</b> Solo procesa eliminaciones si la URL pertenece a Cloudinary, ignorando otros proveedores (como ImgBB) para evitar errores de API.</item>
        /// <item><b>Extracción de Identificador:</b> Procesa la URL para obtener el <c>PublicId</c>, que es la clave única que Cloudinary necesita para localizar el archivo.</item>
        /// <item><b>Borrado por tipo de recurso:</b> Define específicamente el <c>ResourceType.Video</c> (usado para audio en tu configuración) para asegurar que el motor de búsqueda de Cloudinary encuentre el objeto.</item>
        /// </list>
        /// </remarks>
        /// <param name="url">La dirección URL completa del archivo que se desea eliminar.</param>
        /// <returns>Un mensaje de confirmación de éxito o un error detallado si la operación falla.</returns>
        [HttpDelete("eliminar")]
        public async Task<IActionResult> EliminarArchivo([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url)) return BadRequest("URL vacía");

            // Solo borramos cosas de Cloudinary
            if (!url.Contains("cloudinary.com"))
                return Ok(new { mensaje = "Ignorado (No es Cloudinary)" });

            try
            {
                // Sacamos el ID oculto en la URL
                string publicId = ObtenerPublicId(url);
                if (string.IsNullOrEmpty(publicId)) return BadRequest("URL no válida");

                // Intentamos borrar como si fuera un video
                var paramsVid = new DeletionParams(publicId) { ResourceType = ResourceType.Video };
                var result = await _cloudinary.DestroyAsync(paramsVid);

                if (result.Result == "ok") return Ok(new { mensaje = "Eliminado" });
                else return StatusCode(500, "Fallo al borrar: " + result.Result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error API: " + ex.Message);
            }
        }
        // Metodos Helpers
        /// <summary>
        /// Analiza una URL de Cloudinary y extrae el identificador único (Public ID) necesario para operaciones de gestión.
        /// </summary>
        /// <remarks>
        /// El método realiza un "parseo" quirúrgico de la URL siguiendo este algoritmo:
        /// <list type="number">
        /// <item><b>Localización:</b> Busca el segmento <c>/upload/</c> que separa la configuración del servidor de los datos del archivo.</item>
        /// <item><b>Limpieza de Versión:</b> Omite el componente de versión (ej. <c>v17397... </c>) que Cloudinary genera automáticamente.</item>
        /// <item><b>Extracción de Carpeta y Nombre:</b> Captura la ruta interna y el nombre del archivo.</item>
        /// <item><b>Remoción de Extensión:</b> Elimina el sufijo del formato (ej. <c>.mp3</c>) para obtener el ID limpio que requiere la API de borrado.</item>
        /// </list>
        /// </remarks>
        /// <param name="url">La dirección URL completa del recurso alojado en Cloudinary.</param>
        /// <returns>El Public ID del recurso (incluyendo carpetas) o <c>null</c> si el formato de la URL es inválido.</returns>
        private string ObtenerPublicId(string url)
        {
            try
            {
                var uri = new Uri(url);
                string path = uri.AbsolutePath;
                // Ejemplo: /dyyi9sb9v/video/upload/v1234/musicsearch_audios/cancion.mp3

                int indexUpload = path.IndexOf("/upload/");
                if (indexUpload == -1) return null;

                // Cortamos todo hasta después de /upload/
                string resto = path.Substring(indexUpload + 8);

                // Saltamos la versión (v12345/...)
                int indexSlash = resto.IndexOf('/');
                string idConExt = resto.Substring(indexSlash + 1);

                // Quitamos la extensión (.mp3 o .jpg)
                int indexPunto = idConExt.LastIndexOf('.');
                return idConExt.Substring(0, indexPunto);
            }
            catch { return null; }
        }
    }
}
