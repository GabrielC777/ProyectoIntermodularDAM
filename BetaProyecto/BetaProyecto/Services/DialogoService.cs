using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BetaProyecto.Views.WindowsRising;
using System.Threading.Tasks; // Asegúrate de que aquí está tu VentanaAviso

namespace BetaProyecto.Services
{
    // Esta clase "firma" el contrato (: IDialogService)
    public class DialogoService : IDialogoService
    {
        public void MostrarAlerta(string mensaje)
        {
            // La lógica visual de Avalonia se queda encapsulada aquí
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var aviso = new VentanaAviso(mensaje);
                aviso.ShowDialog(desktop.MainWindow);
            }
        }
        public async Task<bool> Preguntar(string titulo, string mensaje, string textoSi, string textoNo)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Creamos la ventana pasando todos los textos personalizables
                var ventana = new VentanaConfirmacion(titulo, mensaje, textoSi, textoNo);

                // Esperamos el resultado (ShowDialogAsync devuelve lo que pasamos en this.Close(resultado))
                var resultado = await ventana.ShowDialog<bool>(desktop.MainWindow);

                return resultado;
            }
            return false;
        }
    }
}