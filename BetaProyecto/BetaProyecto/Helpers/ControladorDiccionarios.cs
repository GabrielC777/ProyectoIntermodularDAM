using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using System;
using System.Linq;

namespace BetaProyecto.Helpers
{
    public static class ControladorDiccionarios
    {
        // 1. CARGA INICIAL
        public static void CargarConfiguracionInicial(string tema, string idioma, string fuente)
        {
            AplicarIdioma(idioma);
            AplicarTema(tema);
            AplicarFuente(fuente);
        }

        // --- MÉTODOS PÚBLICOS ---

        public static void AplicarTema(string tema)
        {
            if (string.IsNullOrEmpty(tema)) tema = "ModoClaro";
            string uri = $"avares://BetaProyecto/Assets/Interfaces/{tema}.axaml";
            ReemplazarRecurso(uri, "Interfaces");
        }

        public static void AplicarIdioma(string idioma)
        {
            if (string.IsNullOrEmpty(idioma)) idioma = "Spanish";
            string uri = $"avares://BetaProyecto/Assets/Language/{idioma}.axaml";
            ReemplazarRecurso(uri, "Language");
        }

        public static void AplicarFuente(string fuente)
        {
            if (string.IsNullOrEmpty(fuente)) fuente = "Lexend";

            // Ajuste de nombre por si viene sin prefijo
            string nombreArchivo = fuente.StartsWith("Fuente") ? fuente : "Fuente" + fuente;
            string uri = $"avares://BetaProyecto/Assets/Styles/{nombreArchivo}.axaml";

            ReemplazarRecurso(uri, "Styles");
        }

        // --- MOTOR INTERNO (Solo necesitamos este ahora) ---

        private static void ReemplazarRecurso(string uriNueva, string carpetaIdentificadora)
        {
            var app = Application.Current;
            if (app == null) return;

            var diccionarios = app.Resources.MergedDictionaries;

            // 1. Buscar y eliminar el viejo (buscando por la carpeta en la ruta)
            var recursoViejo = diccionarios.FirstOrDefault(d =>
                d is ResourceInclude include &&
                include.Source != null &&
                include.Source.ToString().Contains(carpetaIdentificadora));

            if (recursoViejo != null)
            {
                diccionarios.Remove(recursoViejo);
            }

            // 2. Añadir el nuevo
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