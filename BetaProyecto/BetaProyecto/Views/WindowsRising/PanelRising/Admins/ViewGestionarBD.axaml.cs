using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BetaProyecto.Models; // Necesario para asegurar tipos si hace falta
using BetaProyecto.ViewModels;
using ReactiveUI;

namespace BetaProyecto.Views.WindowsRising.PanelRising.Admins;

public partial class ViewGestionarBD : UserControl
{
    public ViewGestionarBD()
    {
        InitializeComponent();
    }

    // ================================================================
    // 1. USUARIOS (CREAR Y EDITAR)
    // ================================================================

    private async void BtnImgUserCrear_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona Foto Perfil",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (archivos.Count >= 1)
        {
          TxtImagenUserCrear.Text = archivos[0].Path.LocalPath;
        }
    }

    private async void BtnImgUserEditar_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona Foto Perfil",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (archivos.Count >= 1)
        {
            TxtImagenUserEditar.Text = archivos[0].Path.LocalPath;
        }
    }

    // ================================================================
    // 2. CANCIONES (CREAR Y EDITAR)
    // ================================================================

    // --- PORTADA CREAR ---
    private async void BtnImgSongCrear_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona la portada",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (archivos.Count >= 1)
        {
            TxtImageSongCrear.Text = archivos[0].Path.LocalPath;
        }
    }

    // --- AUDIO CREAR ---
    private async void BtnAudioSongCrear_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var audioFilter = new FilePickerFileType("Archivos de Audio")
        {
            Patterns = new[] { "*.mp3", "*.wav", "*.m4a", "*.flac" }
        };

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona el archivo de audio",
            AllowMultiple = false,
            FileTypeFilter = new[] { audioFilter }
        });

        if (archivos.Count >= 1)
        {
            TxtAudioSongCrear.Text = archivos[0].Path.LocalPath;
        }
    }
    // --- AUDIO EDITAR ---
    private async void BtnAudioSongEditar_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var audioFilter = new FilePickerFileType("Archivos de Audio")
        {
            Patterns = new[] { "*.mp3", "*.wav", "*.m4a", "*.flac" }
        };

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Cambiar archivo de audio",
            AllowMultiple = false,
            FileTypeFilter = new[] { audioFilter }
        });

        if (archivos.Count >= 1)
        {
            if (DataContext is ViewGestionarBDViewModel vm)
            {
                vm.TxtRutaArchivoEditar = archivos[0].Path.LocalPath;
            }
        }
    }

    // --- PORTADA EDITAR ---
    private async void BtnImgSongEditar_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Cambiar Portada",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (archivos.Count >= 1)
        {
            TxtImageSongEditar.Text = archivos[0].Path.LocalPath;
        }
    }

    // ================================================================
    // 3. PLAYLISTS (CREAR Y EDITAR)
    // ================================================================

    private async void BtnImgPlaylistCrear_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecciona la portada",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (archivos.Count >= 1)
        {
            TxtImagePlaylistCrear.Text = archivos[0].Path.LocalPath;
        }
    }

    private async void BtnImgPlaylistEditar_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Cambiar Portada",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (archivos.Count >= 1)
        {
            TxtImagePlaylistEditar.Text = archivos[0].Path.LocalPath;
        }
    }
}