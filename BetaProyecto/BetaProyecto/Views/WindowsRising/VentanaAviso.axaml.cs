using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BetaProyecto.ViewModels;

namespace BetaProyecto.Views.WindowsRising;

public partial class VentanaAviso : Window
{
    // Constructor vacío (Para el preview)
    public VentanaAviso() 
    { 
        InitializeComponent(); 
    }

    // Constructor que recibe el mensaje
    public VentanaAviso(string mensaje)
    {
        InitializeComponent();

        // Creamos el VM y lo conectamos ademas le pasamos una Lambda que llama a Close() de esta ventana
        this.DataContext = new VentanaAvisoViewModel(mensaje, () => this.Close());
    }
} 