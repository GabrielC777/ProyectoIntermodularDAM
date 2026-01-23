using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BetaProyecto.ViewModels;
using System;
using System.Linq;

namespace BetaProyecto.Views.Crear
{
    public partial class ViewCrearUsuario : UserControl
    {
        public ViewCrearUsuario()
        {
            InitializeComponent();
        }

        private async void BtnSeleccionarImagen_Click(object sender, RoutedEventArgs e)
        {
            // 1. Obtenemos la ventana principal para poder lanzar el diálogo encima
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            // 2. Abrimos el selector de archivos
            var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Selecciona tu foto de perfil",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll } // Solo deja elegir imágenes
            });

            // 3. Si el usuario eligió algo...
            if (archivos.Count >= 1)
            {
                // Obtenemos la ruta local del archivo
                string rutaArchivo = archivos[0].Path.LocalPath;

                if (this.DataContext is ViewCrearUsuarioViewModel vm)
                {
                    vm.CargarImagenPrevia(rutaArchivo);
                }
            }
        }
    }
}