using BetaProyecto.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BetaProyecto.ViewModels
{
    public class ViewListaPersonalizadaViewModel : ViewModelBase
    {
        public ListaPersonalizada Playlist { get; }

        // Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnVolver { get; }

        //Propiedades calculadas
        public int CantidadCanciones => Playlist.CancionesCompletas?.Count ?? 0;

        public ViewListaPersonalizadaViewModel(ListaPersonalizada playlist, Action accionVolver)
        {
            Playlist = playlist;

            // Creamos el comando reactivo
            BtnVolver = ReactiveCommand.Create(accionVolver);
        }

    }
}
