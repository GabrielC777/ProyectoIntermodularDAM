using Avalonia.Controls;
using BetaProyecto.ViewModels;

namespace BetaProyecto.Views.WindowsRising;

public partial class VentanaConfirmacion : Window
{   
    //Para poder usar el preview de Avalonia
    public VentanaConfirmacion() 
    { 
        InitializeComponent(); 
    }

    public VentanaConfirmacion(string titulo, string mensaje, string txtSi, string txtNo)
    {
        InitializeComponent();

        // Pasamos la acción "Close(resultado)" al ViewModel
        this.DataContext = new VentanaConfirmacionViewModel(
            titulo,
            mensaje,
            txtSi,
            txtNo,
            (resultado) => this.Close(resultado) // Esto devuelve el bool al ShowDialogAsync
        );
    }
}