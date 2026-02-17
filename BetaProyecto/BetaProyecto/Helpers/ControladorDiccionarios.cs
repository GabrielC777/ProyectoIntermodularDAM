using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using System;
using System.Linq;

namespace BetaProyecto.Helpers
{
    public static class ControladorDiccionarios
    {
        // Carga inicial 
        /// <summary>
        /// Establece el entorno visual y regional de la aplicación al iniciar.
        /// </summary>
        /// <remarks>
        /// Este método centraliza la carga de preferencias del usuario, invocando secuencialmente 
        /// las funciones de localización (idioma), estilo (tema oscuro/claro) y tipografía.
        /// </remarks>
        /// <param name="tema">Nombre del recurso de estilo a aplicar (ej. "Dark" o "Light").</param>
        /// <param name="idioma">Código de cultura o nombre del archivo de traducción.</param>
        /// <param name="fuente">Nombre de la familia tipográfica global de la interfaz.</param>
        public static void CargarConfiguracionInicial(string tema, string idioma, string fuente)
        {
            AplicarIdioma(idioma);
            AplicarTema(tema);
            AplicarFuente(fuente);
        }

        // Métodos publicos 

        /// <summary>
        /// Cambia dinámicamente el aspecto visual de la aplicación cargando y aplicando un diccionario de recursos de tema específico.
        /// </summary>
        /// <remarks>
        /// Este método gestiona el motor de estilos mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación:</b> Si el parámetro <paramref name="tema"/> es nulo o vacío, se establece "ModoClaro" como valor predeterminado por seguridad.</item>
        /// <item><b>Construcción de URI:</b> Genera una ruta de recurso de Avalonia (URI) apuntando a los archivos <c>.axaml</c> de la carpeta <c>Assets/Interfaces/</c>.</item>
        /// <item><b>Inyección de Estilos:</b> Delega en <see cref="ReemplazarRecurso"/> para sustituir el diccionario actual identificado con el alias "Interfaces" por el nuevo tema cargado.</item>
        /// </list>
        /// </remarks>
        /// <param name="tema">El nombre del tema que se desea aplicar (ej. "ModoOscuro", "ModoClaro"). Este debe coincidir con el nombre del archivo .axaml en los activos.</param>
        public static void AplicarTema(string tema)
        {
            if (string.IsNullOrEmpty(tema)) tema = "ModoClaro";
            string uri = $"avares://BetaProyecto/Assets/Interfaces/{tema}.axaml";
            ReemplazarRecurso(uri, "Interfaces");
        }
        /// <summary>
        /// Cambia dinámicamente el idioma de la interfaz de usuario cargando el diccionario de recursos de traducción correspondiente.
        /// </summary>
        /// <remarks>
        /// Este método orquestra la localización de la aplicación mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Normalización:</b> Si el parámetro <paramref name="idioma"/> no está definido, se establece "Spanish" como idioma base predeterminado.</item>
        /// <item><b>Construcción de Ruta:</b> Genera una URI de recurso de Avalonia que apunta a los diccionarios de idiomas situados en la carpeta <c>Assets/Language/</c>.</item>
        /// <item><b>Actualización de Recursos:</b> Invoca a <see cref="ReemplazarRecurso"/> para sustituir el diccionario identificado con la clave "Language", desencadenando la actualización inmediata de todas las etiquetas vinculadas mediante el convertidor de localización.</item>
        /// </list>
        /// </remarks>
        /// <param name="idioma">El nombre del archivo de idioma (sin extensión) que se desea aplicar (ej. "Spanish", "English").</param>
        public static void AplicarIdioma(string idioma)
        {
            if (string.IsNullOrEmpty(idioma)) idioma = "Spanish";
            string uri = $"avares://BetaProyecto/Assets/Language/{idioma}.axaml";
            ReemplazarRecurso(uri, "Language");
        }
        /// <summary>
        /// Cambia dinámicamente la familia tipográfica global de la aplicación cargando el diccionario de estilos correspondiente.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la identidad visual a través de las fuentes mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Asignación por Defecto:</b> Si el parámetro <paramref name="fuente"/> es nulo o vacío, se utiliza "Lexend" como tipografía base del sistema.</item>
        /// <item><b>Normalización de Nombre:</b> Verifica si el nombre de la fuente incluye el prefijo "Fuente". Si no es así, lo concatena para coincidir con la nomenclatura de los archivos <c>.axaml</c> de estilos.</item>
        /// <item><b>Construcción de URI:</b> Genera la ruta de acceso al recurso dentro de la carpeta <c>Assets/Styles/</c>.</item>
        /// <item><b>Aplicación de Estilo:</b> Invoca a <see cref="ReemplazarRecurso"/> para sustituir el diccionario con el alias "Styles", actualizando la fuente en toda la interfaz de usuario en tiempo de ejecución.</item>
        /// </list>
        /// </remarks>
        /// <param name="fuente">El nombre de la fuente o del archivo de estilo (ej. "Lexend", "Roboto", "FuenteInter").</param>
        public static void AplicarFuente(string fuente)
        {
            if (string.IsNullOrEmpty(fuente)) fuente = "Lexend";

            // Ajuste de nombre por si viene sin prefijo
            string nombreArchivo = fuente.StartsWith("Fuente") ? fuente : "Fuente" + fuente;
            string uri = $"avares://BetaProyecto/Assets/Styles/{nombreArchivo}.axaml";

            ReemplazarRecurso(uri, "Styles");
        }

        // Motor de reemplazo de recursos
        /// <summary>
        /// Localiza y sustituye un diccionario de recursos específico dentro de la colección global de la aplicación.
        /// </summary>
        /// <remarks>
        /// Este método es el motor de la personalización dinámica y opera mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Acceso Global:</b> Obtiene la instancia actual de la aplicación y accede a su colección <see cref="ResourceDictionary.MergedDictionaries"/>.</item>
        /// <item><b>Identificación:</b> Escanea los diccionarios cargados buscando un objeto <see cref="ResourceInclude"/> cuya propiedad <c>Source</c> contenga la cadena definida en <paramref name="carpetaIdentificadora"/> (ej. "Language", "Interfaces", "Styles").</item>
        /// <item><b>Limpieza:</b> Si se encuentra un recurso previo del mismo tipo, se elimina de la colección para evitar conflictos de claves de recursos.</item>
        /// <item><b>Inyección:</b> Instancia un nuevo <see cref="ResourceInclude"/> con la <paramref name="uriNueva"/> y lo añade a la colección global.</item>
        /// </list>
        /// Si la URI es inválida o el archivo no existe, el error se captura en el bloque <c>catch</c> para mantener la estabilidad de la interfaz de usuario.
        /// </remarks>
        /// <param name="uriNueva">La ruta absoluta del nuevo archivo AXAML que se desea cargar.</param>
        /// <param name="carpetaIdentificadora">La palabra clave (nombre de la subcarpeta) que identifica qué tipo de recurso se está reemplazando.</param>
        private static void ReemplazarRecurso(string uriNueva, string carpetaIdentificadora)
        {
            var app = Application.Current;
            if (app == null) return;

            var diccionarios = app.Resources.MergedDictionaries;

            // Buscar y eliminar el viejo (buscando por la carpeta en la ruta)
            var recursoViejo = diccionarios.FirstOrDefault(d =>
                d is ResourceInclude include &&
                include.Source != null &&
                include.Source.ToString().Contains(carpetaIdentificadora));

            if (recursoViejo != null)
            {
                diccionarios.Remove(recursoViejo);
            }

            // Añadimos el nuevo recurso
            try
            {
                var nuevoRecurso = new ResourceInclude(new Uri("avares://BetaProyecto/"))
                {
                    Source = new Uri(uriNueva)
                };
                diccionarios.Add(nuevoRecurso);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando RECURSO {uriNueva}: {ex.Message}");
            }
        }
    }
}