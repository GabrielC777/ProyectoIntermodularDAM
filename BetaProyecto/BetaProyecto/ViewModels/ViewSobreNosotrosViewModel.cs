using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;

namespace BetaProyecto.ViewModels
{

    public class ViewSobreNosotrosViewModel : ViewModelBase, INavegable
    {
        public Action? VolverAtras { get; set; }
        public ReactiveCommand<Unit, Unit> btnVolverAtras { get; }
        public ReactiveCommand<Unit, Unit> BtnAbrirGitHub { get; } 
        public ViewSobreNosotrosViewModel()
        {
            // Configuramos el comando reactive
            btnVolverAtras = ReactiveCommand.Create(() =>
            {
                Debug.WriteLine("Volviendo desde el Sobre Nosotros...");
                VolverAtras?.Invoke();
            });
            BtnAbrirGitHub = ReactiveCommand.Create(() =>
            {
                try
                {
                    // Intentamos abrir la URL en el navegador predeterminado
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
