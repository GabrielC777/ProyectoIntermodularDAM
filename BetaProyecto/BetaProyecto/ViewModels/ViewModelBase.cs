using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using System;

namespace BetaProyecto.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {

        protected Bitmap CargarImagen(string nombreArchivo)
        {
            try
            {
                // "avares://" significa "Avalonia Resources"
                var uri = new Uri($"avares://BetaProyecto/Assets/Imagenes/{nombreArchivo}");
                return new Bitmap(AssetLoader.Open(uri));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando imagen '{nombreArchivo}': {ex.Message}");
                return null; // Devuelve null si falla 
            }
        }
    }
}
