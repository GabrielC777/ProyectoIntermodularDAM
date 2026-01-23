using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewAyudaViewModel : ViewModelBase, INavegable
    {
        public Action? VolverAtras { get; set; }
        public ReactiveCommand<Unit, Unit> btnVolverAtras { get; }
        public ViewAyudaViewModel() {
            
            // Configuramos el comando directamente aquí
            btnVolverAtras = ReactiveCommand.Create(() =>
            {
                Debug.WriteLine("Volviendo desde el Ayuda...");
                VolverAtras?.Invoke();
            });
        }
    }
}
