using ReactiveUI;
using System;
using System.Reactive;

namespace BetaProyecto.ViewModels
{
    public class VentanaAvisoViewModel : ViewModelBase
    {
        // Texto a mostrar
        public string Mensaje { get; }

        // Comando para el botón
        public ReactiveCommand<Unit, Unit> BtnAceptar { get; }

        // Recibimos el mensaje y una "Action" que es la función para cerrar la ventana física
        public VentanaAvisoViewModel(string mensaje, Action cerrarVentana)
        {
            Mensaje = mensaje;

            // Al pulsar aceptar, ejecutamos la acción de cerrar
            BtnAceptar = ReactiveCommand.Create(() =>
            {
                cerrarVentana();
            });
        }
    }
}
