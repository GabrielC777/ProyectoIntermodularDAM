using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;

namespace BetaProyecto.ViewModels
{
    public class ViewAyudaViewModel : ViewModelBase, INavegable
    {
        //Actions 
        public Action? VolverAtras { get; set; }
        //Comandos reactive 
        public ReactiveCommand<Unit, Unit> btnVolverAtras { get; }
        public ViewAyudaViewModel() {
            
            // Configuramos el comandos reactive 
            btnVolverAtras = ReactiveCommand.Create(() =>
            {
                Debug.WriteLine("Volviendo desde el Ayuda...");
                VolverAtras?.Invoke();
            });
        }
    }
}
