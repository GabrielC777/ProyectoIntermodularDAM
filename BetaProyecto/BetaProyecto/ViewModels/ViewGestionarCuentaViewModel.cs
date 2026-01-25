using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewGestionarCuentaViewModel : ViewModelBase
    {
        // Servicios
        private readonly IDialogoService _dialogoService;
        private readonly StorageService _storageService;

        //Actions
        public Action<ListaPersonalizada>? SolicitudIrAEditarPlaylist { get; set; }
        public Action<Canciones>? SolicitudIrAEditarCanciones { get; set; }

        // Bidings
        private ObservableCollection<Canciones> _misCanciones;
        public ObservableCollection<Canciones> MisCanciones
        {
            get => _misCanciones;
            set => this.RaiseAndSetIfChanged(ref _misCanciones, value);
        }

        private ObservableCollection<ListaPersonalizada> _misPlaylists;
        public ObservableCollection<ListaPersonalizada> MisPlaylists
        {
            get => _misPlaylists;
            set => this.RaiseAndSetIfChanged(ref _misPlaylists, value);
        }

        // Comandos Reactive
        public ReactiveCommand<Canciones, Unit> BtnEditarCancion { get; }
        public ReactiveCommand<Canciones, Unit> BtnEliminarCancion { get; }

        public ReactiveCommand<ListaPersonalizada, Unit> BtnEditarPlaylist { get; }
        public ReactiveCommand<ListaPersonalizada, Unit> BtnEliminarPlaylist { get; }

        public ReactiveCommand<Unit, Unit> BtnRefrescar { get; }

        // --- CONSTRUCTOR ---
        public ViewGestionarCuentaViewModel()
        {
            //Inicializamos servicios
            _dialogoService = new DialogoService();
            _storageService = new StorageService();

            // Inicializar listas vacías para evitar errores
            MisCanciones = new ObservableCollection<Canciones>();
            MisPlaylists = new ObservableCollection<ListaPersonalizada>();

            // Configurar comandos (Aquí conectarás tu lógica de BD más adelante)
            BtnEditarCancion = ReactiveCommand.Create<Canciones>(cancion =>
            {
                System.Diagnostics.Debug.WriteLine($"Editar Canción: {cancion.Titulo}");
                SolicitudIrAEditarCanciones?.Invoke(cancion);
            });

            BtnEliminarCancion = ReactiveCommand.Create<Canciones>(async cancion =>
            {
                System.Diagnostics.Debug.WriteLine($"Eliminar Canción: {cancion.Titulo}");
                await EliminarCancion(cancion);
            });

            BtnEditarPlaylist = ReactiveCommand.Create<ListaPersonalizada>(playlist =>
            {
                System.Diagnostics.Debug.WriteLine($"Editar Playlist: {playlist.Nombre}");
                SolicitudIrAEditarPlaylist?.Invoke(playlist);
            });

            BtnEliminarPlaylist = ReactiveCommand.Create<ListaPersonalizada>(async playlist =>
            {
                System.Diagnostics.Debug.WriteLine($"Eliminar Playlist: {playlist.Nombre}");
                await EliminarPlaylist(playlist);
            });

            BtnRefrescar = ReactiveCommand.CreateFromTask(CargarContenidoUsuario);

            // Cargar datos al iniciar
            _ = CargarContenidoUsuario();
        }
        private async Task EliminarCancion(Canciones cancion)
        {
            // Preguntar antes de borrar para evitar accidentes
            var confirm = await _dialogoService.Preguntar("MsgConfirmEliminar", "MsgPreguntaEliminarCancion", "BtnEliminar", "BtnCancelar");
            if (!confirm) 
            {
                return;
            }
            try
            {
                // -----------------------------------------------------------
                // PASO 1: LIMPIEZA DE ARCHIVOS (Nube) ☁️
                // -----------------------------------------------------------

                // Borrar Audio
                if (!string.IsNullOrEmpty(cancion.UrlCancion) && cancion.UrlCancion.Contains("cloudinary"))
                {
                    await _storageService.EliminarArchivo(cancion.UrlCancion);
                }
                // -----------------------------------------------------------
                // PASO 2: BORRADO DE DATOS (Mongo) 🗄️
                // -----------------------------------------------------------
                bool exito = await MongoClientSingleton.Instance.Cliente.EliminarCancionPorId(cancion.Id);

                if (exito)
                {
                    // PASO 3: RESTAR 1 AL CONTADOR DE CANCIONES DEL PERFIL
                    // Importante para que las estadísticas del usuario sean reales
                    await MongoClientSingleton.Instance.Cliente.IncrementarContadorCancionesUsuario(GlobalData.Instance.UserIdGD, -1);

                    // PASO 4: ACTUALIZAR PANTALLA
                    MisCanciones.Remove(cancion);
                    _dialogoService.MostrarAlerta("MsgExitoBorrado");
                }
                else
                {
                    _dialogoService.MostrarAlerta("MsgErrorBorradoDB");
                }
                
            }
            catch (Exception ex)
            {
                _dialogoService.MostrarAlerta("MsgErrorBorradoDB");
                System.Diagnostics.Debug.WriteLine($"Error base: {ex}");

            }
        }

        private async Task EliminarPlaylist(ListaPersonalizada playlist)
        {
            var confirm = await _dialogoService.Preguntar("MsgConfirmEliminar", "MsgPreguntaEliminarPlaylist", "BtnEliminar", "BtnCancelar");
            if (!confirm)
            {
                return;
            }

            bool exito = await MongoClientSingleton.Instance.Cliente.EliminarPlaylistPorId(playlist.Id);

            if (exito)
            {
                MisPlaylists.Remove(playlist);
                _dialogoService.MostrarAlerta("MsgExitoBorrado");
            }
            else
            {
                _dialogoService.MostrarAlerta("MsgErrorBorradoDB");
            }
        }
        private async Task CargarContenidoUsuario()
        {
            if(MongoClientSingleton.Instance.Cliente != null)
            {
                string miId = GlobalData.Instance.UserIdGD;

                var listaCanciones = MongoClientSingleton.Instance.Cliente.ObtenerCancionesPorAutor(miId); 
                var listaPlaylist = MongoClientSingleton.Instance.Cliente.ObtenerPlaylistsPorCreador(miId);


                await Task.WhenAll(listaCanciones,listaPlaylist);

                MisCanciones = new ObservableCollection<Canciones>(listaCanciones.Result);
                MisPlaylists = new ObservableCollection<ListaPersonalizada>(listaPlaylist.Result);
            }
        }
    }
}