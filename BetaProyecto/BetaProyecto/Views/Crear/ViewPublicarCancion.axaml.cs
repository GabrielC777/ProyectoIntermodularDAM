using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BetaProyecto.ViewModels;

namespace BetaProyecto.Views.Crear;

public partial class ViewPublicarCancion : UserControl
{
    public ViewPublicarCancion()
    {
        InitializeComponent();
    }

    //Aqui utilizamos Code Behind por que necesitamos tocar cosas de la interfaz para simplificarlo
    //y no usar un injector de dependencias complejo escrebimos aqui la logica para que sea más sencill
    //intuitivo

    // BOTÓN: SELECCIONAR IMAGEN
    private async void BtnSelectImagen_Click(object? sender, RoutedEventArgs e)
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
            if (DataContext is ViewPublicarCancionViewModel vm)
            {
                vm.RutaImagen = archivos[0].Path.LocalPath;
            }
        }
    }
    // BOTÓN: SELECCIONAR MP3
    private async void BtnSelectMp3_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Filtro manual para audios
        var audioFilter = new FilePickerFileType("Archivos de Audio")
        {
            Patterns = new[] { "*.mp3", "*.wav", "*.m4a" }
        };

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona el archivo de audio",
            AllowMultiple = false,
            FileTypeFilter = new[] { audioFilter }
        });

        if (archivos.Count >= 1)
        {
            if (DataContext is ViewPublicarCancionViewModel vm)
            {
                vm.RutaMp3 = archivos[0].Path.LocalPath;
            }
        }
    }
}