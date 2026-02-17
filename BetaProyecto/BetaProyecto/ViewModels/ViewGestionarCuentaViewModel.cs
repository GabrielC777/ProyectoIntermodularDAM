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

        // Constructor
        public ViewGestionarCuentaViewModel()
        {
            //Inicializamos servicios
            _dialogoService = new DialogoService();
            _storageService = new StorageService();

            // Inicializar listas 
            MisCanciones = new ObservableCollection<Canciones>();
            MisPlaylists = new ObservableCollection<ListaPersonalizada>();

            // Configurar comandos 
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

            // Cargar datos al iniciar en segundo plano
            _ = CargarContenidoUsuario();
        }
        /// <summary>
        /// Gestiona el proceso integral de eliminación de una canción, incluyendo la limpieza de recursos en la nube y la actualización de la base de datos.
        /// </summary>
        /// <remarks>
        /// Este método ejecuta un flujo de borrado seguro mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Confirmación:</b> Solicita permiso al usuario mediante <see cref="_dialogoService"/> para evitar eliminaciones accidentales.</item>
        /// <item><b>Limpieza de Almacenamiento:</b> Identifica si el archivo reside en Cloudinary y lo elimina físicamente mediante <see cref="_storageService"/>.</item>
        /// <item><b>Persistencia y Métricas:</b> Remueve el registro en MongoDB y decrementa el contador de canciones publicadas del usuario.</item>
        /// <item><b>Actualización de UI:</b> Remueve la instancia de la colección local <see cref="MisCanciones"/> para reflejar el cambio instantáneamente.</item>
        /// </list>
        /// </remarks>
        /// <param name="cancion">El objeto <see cref="Canciones"/> que se desea eliminar definitivamente del sistema.</param>
        /// <returns>Una tarea que representa la operación de eliminación asíncrona.</returns>
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
                // Borrar Audio en la nube (Cloudinary)
                if (!string.IsNullOrEmpty(cancion.UrlCancion) && cancion.UrlCancion.Contains("cloudinary"))
                {
                    await _storageService.EliminarArchivo(cancion.UrlCancion);
                }
                //Borrar datos en MongoDB
                bool exito = await MongoClientSingleton.Instance.Cliente.EliminarCancionPorId(cancion.Id);

                if (exito)
                {
                    //Restar 1 al contador de canciones del usuario en MongoDB
                    await MongoClientSingleton.Instance.Cliente.IncrementarContadorCancionesUsuario(GlobalData.Instance.UserIdGD, -1);

                    //Actualizar pantalla
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
        /// <summary>
        /// Gestiona el proceso de eliminación de una lista de reproducción personalizada de la base de datos y de la interfaz de usuario.
        /// </summary>
        /// <remarks>
        /// Este método ejecuta un flujo de borrado seguro estructurado en los siguientes pasos:
        /// <list type="number">
        /// <item><b>Confirmación:</b> Solicita una validación explícita al usuario a través de <see cref="_dialogoService"/> para prevenir eliminaciones accidentales.</item>
        /// <item><b>Persistencia:</b> Invoca al cliente de MongoDB para eliminar el registro físico de la lista mediante su identificador único.</item>
        /// <item><b>Actualización de UI:</b> Si la operación en la base de datos es exitosa, remueve la instancia de la colección <see cref="MisPlaylists"/> para refrescar la vista inmediatamente.</item>
        /// </list>
        /// Notifica al usuario el resultado de la operación mediante mensajes de alerta traducidos.
        /// </remarks>
        /// <param name="playlist">El objeto <see cref="ListaPersonalizada"/> que se desea eliminar definitivamente del sistema.</param>
        /// <returns>Una tarea que representa la operación de eliminación asíncrona.</returns>
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
        /// <summary>
        /// Recupera y carga de forma asíncrona el catálogo de canciones y listas de reproducción creadas por el usuario actual.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la carga de contenido personal en dos fases:
        /// <list type="number">
        /// <item><b>Consulta paralela:</b> Lanza simultáneamente las peticiones a MongoDB para obtener las canciones por autor y las playlists por creador utilizando el ID de <see cref="GlobalData.Instance.UserIdGD"/>.</item>
        /// <item><b>Sincronización:</b> Utiliza <see cref="Task.WhenAll"/> para optimizar el tiempo de respuesta y, una vez recibidos los datos, inicializa las colecciones <see cref="MisCanciones"/> y <see cref="MisPlaylists"/>.</item>
        /// </list>
        /// Esto asegura que la interfaz de usuario se actualice con todo el contenido propio del usuario de una sola vez.
        /// </remarks>
        /// <returns>Una tarea que representa la operación de carga asíncrona.</returns>
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