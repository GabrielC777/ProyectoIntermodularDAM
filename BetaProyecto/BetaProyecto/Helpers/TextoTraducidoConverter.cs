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

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return BindingNotification.UnsetValue;
        }
    }
}
