using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.Helpers
{
    // Este convertidor recibe una CLAVE (string) y devuelve el TEXTO traducido del diccionario
    public class TextoTraducidoConverter : IValueConverter
    {
        /// <summary>
        /// Traduce dinámicamente una clave de recurso en su valor correspondiente definido en los diccionarios de la aplicación.
        /// </summary>
        /// <remarks>
        /// Este convertidor facilita la internacionalización (i18n) en la capa de vista mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación:</b> Verifica si el valor de entrada es una cadena de texto válida (la clave del recurso).</item>
        /// <item><b>Búsqueda:</b> Consulta el diccionario de recursos activo de <see cref="Application.Current"/> intentando localizar la clave.</item>
        /// <item><b>Resolución:</b> Si encuentra el recurso (ej. una traducción o una ruta de imagen), lo devuelve; de lo contrario, retorna la clave original como valor de respaldo (fallback).</item>
        /// </list>
        /// Es ideal para enlazar propiedades de texto en XAML que deben reaccionar a cambios de idioma en tiempo de ejecución.
        /// </remarks>
        /// <param name="value">La clave del recurso (string) que se desea localizar.</param>
        /// <param name="targetType">El tipo de la propiedad de destino.</param>
        /// <param name="parameter">Parámetro opcional (no utilizado).</param>
        /// <param name="culture">Información de cultura (no utilizada, se prioriza el diccionario activo).</param>
        /// <returns>El objeto localizado encontrado en los recursos o el texto original si no existe coincidencia.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Si el valor es nulo o no es string, no hacemos nada
            if (value is not string claveRecurso)
                return value;

            // Intentamos buscar la clave en los recursos de la App
            if (Application.Current != null &&
                Application.Current.TryGetResource(claveRecurso, null, out var recursoEncontrado))
            {
                return recursoEncontrado;
            }

            // Si no se encuentra (o si es un texto normal), devolvemos el texto original
            return claveRecurso;
        }
        //ConvertBack no lo necesitamos porque solo es para mostrar texto, no para editarlo, así que devolvemos UnsetValue para indicar que no se puede convertir hacia atrás.
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return BindingNotification.UnsetValue;
        }
    }
}
