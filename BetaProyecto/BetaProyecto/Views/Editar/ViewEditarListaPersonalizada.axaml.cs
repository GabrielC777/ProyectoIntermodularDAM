using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BetaProyecto.ViewModels;

namespace BetaProyecto.Views.Editar;

public partial class ViewEditarListaPersonalizada : UserControl
{
    public ViewEditarListaPersonalizada()
    {
        InitializeComponent();
    }
    private async void BtnSelectImagen_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Accedemos a la ventana principal para lanzar el diálogo
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Configuramos el filtro (solo imágenes)
        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona la portada",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (archivos.Count >= 1)
        {
            // Pasamos la ruta al ViewModel
            if (DataContext is ViewCrearListaPersonalizadaViewModel vm)
            {
                vm.RutaImagen = archivos[0].Path.LocalPath;
            }
        }
    }
}