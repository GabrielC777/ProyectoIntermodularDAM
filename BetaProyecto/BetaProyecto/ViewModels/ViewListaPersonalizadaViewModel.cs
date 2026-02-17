using BetaProyecto.Models;
using ReactiveUI;
using System;
using System.Reactive;

namespace BetaProyecto.ViewModels
{
    public class ViewListaPersonalizadaViewModel : ViewModelBase
    {
        public ListaPersonalizada Playlist { get; }

        // Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnVolver { get; }

        //Propiedad calculada
        public int CantidadCanciones => Playlist.CancionesCompletas?.Count ?? 0;

        public ViewListaPersonalizadaViewModel(ListaPersonalizada playlist, Action accionVolver)
        {
            Playlist = playlist;

            // Configuramos comando reactive
            BtnVolver = ReactiveCommand.Create(accionVolver);
        }

    }
}
