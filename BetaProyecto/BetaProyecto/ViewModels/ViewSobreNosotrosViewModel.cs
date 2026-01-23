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

    public class ViewSobreNosotrosViewModel : ViewModelBase, INavegable
    {
        public Action? VolverAtras { get; set; }
        public ReactiveCommand<Unit, Unit> btnVolverAtras { get; }
        public ReactiveCommand<Unit, Unit> BtnAbrirGitHub { get; } 
        public ViewSobreNosotrosViewModel()
        {
            // Configuramos el comando directamente aquí
            btnVolverAtras = ReactiveCommand.Create(() =>
            {
                Debug.WriteLine("Volviendo desde el Sobre Nosotros...");
                VolverAtras?.Invoke();
            });
            BtnAbrirGitHub = ReactiveCommand.Create(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/GabrielC777/ProyectoIntermodularDAM.git",
                        UseShellExecute = true
                    });
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al abrir GitHub: " + ex.Message);
                }
            });

        }
    }
}
