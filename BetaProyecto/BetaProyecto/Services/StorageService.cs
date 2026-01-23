using Avalonia.Animation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.Services
{
    public class StorageService
    {
        // ---------------------------------------------------------------------------
        // ⚠️ IMPORTANTE: Mira tu pantalla negra de la API. 
        // Si pone https://localhost:7500, déjalo así. Si pone otro puerto, cámbialo.
        // ---------------------------------------------------------------------------
        private const string ApiUrl = "https://localhost:7500/api/Storage";

        // --- MÉTODOS PÚBLICOS (Los que usará tu pantalla) ---

        public async Task<string> SubirImagen(string rutaArchivoEnTuPc)
        {
            return await EnviarA_Api(rutaArchivoEnTuPc, "subir-imagen");
        }

        public async Task<string> SubirCancion(string rutaArchivoEnTuPc)
        {
            return await EnviarA_Api(rutaArchivoEnTuPc, "subir-audio");
        }

        // --- MÉTODO PARA ELIMINAR (Llama a tu API) ---
        public async Task<bool> EliminarArchivo(string urlCompleta)
        {
            // 1. Validación básica: Si no hay URL, no hay nada que borrar.
            if (string.IsNullOrEmpty(urlCompleta)) return false;

            try
            {
                // 2. Configuración para aceptar certificados locales (igual que al subir)
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                using (var client = new HttpClient(handler))
                {
                    // 3. LA LLAMADA (DELETE)
                    // Construimos la URL así: https://localhost:7500/api/Storage/eliminar?url=https://...
                    // Usamos DeleteAsync porque es la acción correcta para borrar.
                    var response = await client.DeleteAsync($"{ApiUrl}/eliminar?url={urlCompleta}");

                    // 4. RESULTADO
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

        // --- LA MAQUINARIA INTERNA (Privado) ---
        private async Task<string> EnviarA_Api(string ruta, string endpoint)
        {
            // 1. COMPROBACIÓN: ¿El archivo existe de verdad en el PC?
            if (!File.Exists(ruta)) throw new FileNotFoundException("¡No encuentro el archivo en tu PC!");

            try
            {
                // 2. EL TRUCO DEL CERTIFICADO (Importante para localhost)
                // Como estamos en desarrollo, el certificado de seguridad de 'localhost' no es oficial.
                // Esta línea le dice al código: "Confía en el servidor".
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                // 3. CREAMOS EL CLIENTE (El cartero)
                using (var client = new HttpClient(handler))
                {
                    // 4. PREPARAMOS EL PAQUETE (MultipartFormData)
                    // Es como un sobre acolchado especial para enviar archivos por internet.
                    using (var content = new MultipartFormDataContent())
                    {
                        // Abrimos el archivo de tu disco duro para leerlo.
                        var fileStream = File.OpenRead(ruta);

                        // Convertimos el archivo en un flujo de datos (StreamContent) para enviarlo.
                        var streamContent = new StreamContent(fileStream);

                        // Le ponemos una etiqueta: "Oye, esto es data de un formulario".
                        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");

                        // ⚠️ CLAVE: Metemos el archivo en el sobre.
                        // "archivo" -> Es el nombre EXACTO que espera tu API (public async Task... (IFormFile archivo))
                        // Si cambias "archivo" por otra cosa, la API no lo verá.
                        content.Add(streamContent, "archivo", Path.GetFileName(ruta));

                        // 5. EL ENVÍO (POST)
                        // El cartero sale hacia: https://localhost:7500/api/Storage/subir-imagen (o audio)
                        var response = await client.PostAsync($"{ApiUrl}/{endpoint}", content);

                        //Esta línea comprueba si fue bien. Si falla, se lanza HttpRequestException.
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
