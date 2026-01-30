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
        // ==========================================
        // 1. CLAVES PARA IMGBB (FOTOS)
        // ==========================================
        private const string ImgbbApiKey = "718db7c8748de9625904739b3e6a4265";

        // ==========================================
        // 2. CLAVES PARA CLOUDINARY (AUDIO)
        // ==========================================
        // Copialas de tu Dashboard de Cloudinary (botón View API Keys)
        private const string CloudName = "dyyi9sb9v";
        private const string ApiKey = "926712452194393";
        private const string ApiSecret = "VCgQV20lLbnA5DO9I9NGMdKvjII";

        private readonly Cloudinary _cloudinary;

        public StorageController()
        {
            // Creamos un objeto 'Account' empaquetando tus 3 claves.
            var account = new Account(CloudName, ApiKey, ApiSecret);
            // Creamos la conexión real con Cloudinary usando esa cuenta.
            _cloudinary = new Cloudinary(account);
            // Obligamos a que la conexión use HTTPS (seguridad, candado verde).
            _cloudinary.Api.Secure = true;
        }
        // ---------------------------------------------------------
        // ENDPOINT 1: SUBIR IMAGEN -> Va a ImgBB
        // ---------------------------------------------------------
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
        // ---------------------------------------------------------
        // ENDPOINT 2: SUBIR AUDIO -> Va a Cloudinary
        // ---------------------------------------------------------
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

                // 1. Intentamos borrar como si fuera un video
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
