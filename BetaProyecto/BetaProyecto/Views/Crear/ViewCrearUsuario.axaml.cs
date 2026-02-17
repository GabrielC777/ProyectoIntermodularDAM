using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BetaProyecto.Services;
using BetaProyecto.ViewModels;

namespace BetaProyecto.Views.Crear
{
    public partial class ViewCrearUsuario : UserControl
    {
        public readonly IDialogoService _dialogoService;

        public ViewCrearUsuario()
        {
            //Inicializamos servicios
            _dialogoService = new DialogoService();

            InitializeComponent();
        }

        private async void BtnSeleccionarImagen_Click(object sender, RoutedEventArgs e)
        {
            // Obtenemos la ventana principal
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            string tituloSelector = "Selecciona tu foto"; // Valor por defecto por si falla

            // Buscamos en los recursos de la App
            if (Application.Current!.Resources.TryGetResource("Reg_TituloSelector", null, out var recurso))
            {
                tituloSelector = recurso as string; // Si lo encuentra, lo usamos
            }
            // Abrimos el selector de archivos
            var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = tituloSelector,
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
            });

            // Si el usuario eligió algo...
            if (archivos.Count >= 1)
            {
                var archivo = archivos[0];

                // Obtenemos las propiedades (tamaño)
                var propiedades = await archivo.GetBasicPropertiesAsync();

                // Definimos el límite: 32 MB en Bytes
                // 32 * 1024 = KB * 1024 = Bytes
                const long limiteBytes = 32 * 1024 * 1024;

                // Comprobamos (.Size puede ser null, usamos 0 por seguridad)
                if ((propiedades.Size ?? 0) > limiteBytes)
                {
                    _dialogoService.MostrarAlerta("Reg_Msg_ImagenDemasiadoGrande");
                    return;
                }

                // Si pasamos el filtro, cargamos la imagen normal
                string rutaArchivo = archivo.Path.LocalPath;

                if (this.DataContext is ViewCrearUsuarioViewModel vm)
                {
                    vm.CargarImagenPrevia(rutaArchivo);
                }
            }
        }
    }
}