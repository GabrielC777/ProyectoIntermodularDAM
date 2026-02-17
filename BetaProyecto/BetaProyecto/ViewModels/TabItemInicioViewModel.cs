using Avalonia.Controls;
using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class TabItemInicioViewModel : ViewModelBase
    {
        //Servicio de diálogo
        private readonly IDialogoService _dialogoService;

        //Actions
        public Action<Canciones, List<Canciones>>? EnviarReproduccion { get; set; }
        public Action<Canciones>? SolicitudVerDetalles { get; set; }
        public Action<string>? SolicitudVerArtista { get; set; }
        public Action<Canciones>? SolicitudCrearReporte { get; set; }
        public Action<ListaPersonalizada>? SolicitudVerDetallasPlaylist { get; set; }

        //Comandos Reactive
        public ReactiveCommand<object, Unit> BtnReproducirDesdeTarjeta { get; }
        public ReactiveCommand<ListaPersonalizada, Unit> BtnReproducirPlaylist { get; }
        public ReactiveCommand<Unit, Unit> BtnRefrescar { get; }

        // Menú de los 3 puntos Canciones
        public ReactiveCommand<Canciones, Unit> BtnIrADetalleCancion { get; }
        public ReactiveCommand<object, Unit> BtnIrAArtista { get; }
        public ReactiveCommand<Canciones, Unit> BtnIrAReportar { get; }
        
        //Menú de los 3 puntos Playlists
        public ReactiveCommand<ListaPersonalizada, Unit> BtnIrADetallesPlaylist { get; }

        // Biding que contiene las listas (Novedades, Rock, etc.)
        private ObservableCollection<TarjetasCanciones> _tarjetas;
        public ObservableCollection<TarjetasCanciones> Tarjetas
        {
            get => _tarjetas;
            set => this.RaiseAndSetIfChanged(ref _tarjetas, value);
        }

        private ObservableCollection<TarjetasListas> _playlists;
        public ObservableCollection<TarjetasListas> Playlists
        {
            get => _playlists;
            set => this.RaiseAndSetIfChanged(ref _playlists, value);

        }
        private string _txtFv;
        public string TxtFav
        {
            get => _txtFv;
            set => this.RaiseAndSetIfChanged(ref _txtFv, value);
        }

        public TabItemInicioViewModel()
        {
            // Inicializamos el servicios 
            _dialogoService = new DialogoService();

            // Configuramos los comandos reactive
            BtnReproducirDesdeTarjeta = ReactiveCommand.Create<object>(ReproducirDesdeBoton);

            BtnReproducirPlaylist = ReactiveCommand.Create<ListaPersonalizada>(ReproducirPlaylist);

            BtnIrADetalleCancion = ReactiveCommand.Create<Canciones>(cancion =>
            {
                System.Diagnostics.Debug.WriteLine($"Solicitando detalles de: {cancion.Titulo}");
                SolicitudVerDetalles?.Invoke(cancion);
            });

            BtnIrAArtista = ReactiveCommand.Create<object>(IrAArtistaDesdeBoton);

            BtnIrAReportar = ReactiveCommand.Create<Canciones>(cancion =>
            {
                System.Diagnostics.Debug.WriteLine($"Creando reporte de: {cancion.Titulo} con el id {cancion.Id}");
                SolicitudCrearReporte?.Invoke(cancion);
            });
            BtnIrADetallesPlaylist = ReactiveCommand.Create<ListaPersonalizada>(playlist =>
            {
                System.Diagnostics.Debug.WriteLine($"Solicitando detalles de lista: {playlist.Nombre}");
                SolicitudVerDetallasPlaylist?.Invoke(playlist);

            });
            BtnRefrescar = ReactiveCommand.CreateFromTask(async () =>
            {
                await CargarDatosCanciones();
            });
            // Ejecutamos la tarea en segundo plano para no bloquear la interfaz
            _ = CargarDatosCanciones();
        }
        /// <summary>
        /// Dirige la navegación a la vista de un artista cuando se activa mediante un botón asociado con el nombre del artista.    
        /// </summary>
        /// <remarks>Este método se utiliza típicamente como un manejador de comandos para elementos de la interfaz de usuario que representan
        /// artistas dentro de un contexto de canción. Recupera el artista y la información de la canción relevante desde el botón
        /// jerarquía y plantea una solicitud para mostrar los detalles del artista. El parámetro debe estructurarse como
        /// descrito para que la navegación tenga éxito. </remarks>
        /// <param name="parametro">El parametro de comando, que se espera sea un botón cuyo DataContext contiene el nombre del artista y cuya etiqueta
        /// hace referencia al botón padre que contiene la información de la canción. </param>
        private void IrAArtistaDesdeBoton(object parametro)
        {
            // Recuperamos el objeto de donde viene el comando (El botón con el nombre del artista)
            // y lo casteamos a Button para sacarle la inforamación que necesitamos
            if (parametro is Button botonPequeño)
            {
                // Recuperamos el Nombre del Artista (DataContext del botón (Que seria un Objeto Canciones))
                var nombreArtista = botonPequeño.DataContext as string;

                // Recuperamos el botón "jefe" (el abre el menú contextual donde esta el nombre del artistas)
                // Que guardamos su referencia en el Tag del botón pequeño
                if (botonPequeño.Tag is Button botonJefe)
                {
                    //Ahora le decimos que se oculte par que no se quede abierto al pasar la vista artista
                    botonJefe.Flyout?.Hide();

                    // Y recuperamos la canción (DataContext del botón jefe)
                    var cancion = botonJefe.DataContext as Canciones;

                    // Aseguramos que no esten vacíos
                    if (!string.IsNullOrEmpty(nombreArtista) && cancion != null)
                    {
                        // Buscamos el índice del artista en la lista del artista que queremos mostrar
                        int indice = cancion.ListaArtistasIndividuales.IndexOf(nombreArtista);
                        // Con ese índice, buscamos el ID del artista en la lista de IDs (que es paralela a la de nombres)
                        if (indice >= 0 && cancion.AutoresIds != null && indice < cancion.AutoresIds.Count)
                        {
                            string idUsuario = cancion.AutoresIds[indice];//Sacamos el id del artista a mostrar
                            SolicitudVerArtista?.Invoke(idUsuario);// Enviamos la solicitud para mostrar la vista del artista con su ID
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Maneja el comando de reproducción disparado desde un botón, iniciando la reproducción de la canción seleccionada y su
        /// colección asociada.
        /// </summary>
        /// <remarks>Este método se utiliza típicamente como un manejador de comandos para botones de reproducción en el usuario
        /// interfaz. El DataContext del botón debe hacer referencia a la canción que se va a reproducir, y su etiqueta debe hacer referencia al
        /// colección a la que pertenece la canción. Si falta alguno de los valores o es inválido, el método lo hace
        /// nada. </remarks>
        /// <param name="parametro">El parámetro de comando, que se espera sea un botón cuyo DataContext es una canción a reproducir y cuya etiqueta contiene
        /// la colección de canciones. No debe ser nula y debe ser un botón con valores válidos de DataContext y Tag. </param>
        private void ReproducirDesdeBoton(object parametro)
        {
            // Recuperamos el objeto de donde viene el comando (El botón de Play)
            // y lo casteamos a Button para sacar la información que necesitamos
            if (parametro is Button boton)
            {
                // Recuperamos la Canción a reproducir (DataContext del botón (Que sería el Objeto Canciones))
                var cancion = boton.DataContext as Canciones;

                // Recuperamos la lista completa a la que pertenece esa canción (la lista de origen)
                // Que guardamos en el Tag del botón para saber el contexto (si viene de Populares, Buscador, etc.)
                // Viene como 'IEnumerable', así que la casteamos para poder trabajar con ella
                var coleccionOrigen = boton.Tag as IEnumerable<Canciones>;

                // Aseguramos que hemos recuperado bien tanto la canción como su lista de origen
                if (cancion != null && coleccionOrigen != null)
                {
                    // Convertimos la colección a una Lista concreta para asegurarnos de pasar una copia exacta al reproductor
                    var listaExacta = coleccionOrigen.ToList();

                    // Enviamos la solicitud de reproducción al padre (MarcoApp) pasando:
                    // 1. La canción concreta que se ha pulsado (para que empiece por ahí)
                    // 2. La lista completa de contexto (para que sepa qué poner cuando acabe esta)
                    EnviarReproduccion?.Invoke(cancion, listaExacta);
                }
            }
        }
        /// <summary>
        /// Inicia la reproducción de la lista de reproducción especificada, reproduciendo la primera canción y poniendo en fila las canciones restantes para
        /// reproducción.
        /// </summary>
        /// <remarks>Si la lista de reproducción es nula o no contiene canciones, se muestra una alerta para informar el
        /// usuario que la lista de reproducción está vacía. </remarks>
        /// <param name="playlist">La lista de reproducción a reproducir. No debe ser nula y debe contener al menos una canción. </param>
        private void ReproducirPlaylist(ListaPersonalizada playlist)
        {
            if (playlist != null && playlist.CancionesCompletas.Count > 0)
            {
                // La primera canción que sonará
                var primeraCancion = playlist.CancionesCompletas[0];

                // La lista completa (cola de reproducción)
                var cola = playlist.CancionesCompletas;

                // Enviamos al MarcoApp
                EnviarReproduccion?.Invoke(primeraCancion, cola);
            }
            else
            {
                // "Esta lista está vacía."
                _dialogoService.MostrarAlerta("Msg_Error_PlaylistVacia");
            }
        }
        /// <summary>
        /// Carga y organiza de forma asíncrona los datos de canciones y listas de reproducción desde la base de datos, actualizando los correspondientes
        /// colecciones para la vinculación de datos.
        /// </summary>
        /// <remarks>Si la conexión a la base de datos no está disponible, se muestra una alerta y no hay datos
        /// cargado. Las canciones y listas de reproducción se agrupan en secciones como favoritas, nuevos lanzamientos, rock, personalizadas
        /// listas y listas de comunidades, asignadas a sus respectivas colecciones para la vinculación de la interfaz. </remarks>
        /// <returns>Devuelve una tarea que representa la operación de carga asíncrona. </returns>
        private async Task CargarDatosCanciones()
        {
            if (MongoClientSingleton.Instance.Cliente == null)
            {
                // "Error de conexión a la base de datos"
                _dialogoService.MostrarAlerta("Msg_Error_Conexion");
            }
            else
            {
                var cliente = MongoClientSingleton.Instance.Cliente;
                var miIdUsuario = GlobalData.Instance.UserIdGD;

                // Lógica de carga
                //-Listade canciones
                var songsNovedades = cliente.ObtenerCacionesNovedades();
                var songsFavoritos = cliente.ObtenerCancionesFavoritos();
                var songsRock = cliente.ObtenerCancionesPorGenero("Rock");
                var songsGeneral = cliente.ObtenerCanciones();
                //-Playlist
                var taskPlaylists = cliente.ObtenerListasReproduccion();

                // Esperamos todo a la vez (Paralelismo)
                await Task.WhenAll(songsNovedades, songsFavoritos, songsRock, songsGeneral, taskPlaylists);

                // Creamos la lista de secciones
                var listaTarjetas = new ObservableCollection<TarjetasCanciones>();

                // --- PROCESAR CANCIONES (TarjetasCanciones) ---
                // OJO: Estas claves "Sec_..." vienen de un Diccionario si posieramos el texto directamente,
                // se podria direcamente esa palabra pero no cambiaria entre idiomas

                if (songsFavoritos.Result.Count > 0)
                    listaTarjetas.Add(new TarjetasCanciones("Sec_Favoritos", new ObservableCollection<Canciones>(songsFavoritos.Result))); // "Favoritos "

                listaTarjetas.Add(new TarjetasCanciones("Sec_Novedades", new ObservableCollection<Canciones>(songsNovedades.Result))); // "Novedades "

                if (songsRock.Result.Count > 0)
                    listaTarjetas.Add(new TarjetasCanciones("Sec_Rock", new ObservableCollection<Canciones>(songsRock.Result))); // "Rock "

                listaTarjetas.Add(new TarjetasCanciones("Sec_ParaTi", new ObservableCollection<Canciones>(songsGeneral.Result))); // "Para ti "


                Tarjetas = listaTarjetas;// Asignamos la lista al Binding

                // --- PROCESAR LISTAS (TarjetasListas) ---
                // Aquí separamos las listas en secciones
                var todasListas = taskPlaylists.Result;
                var listaPlaylist = new ObservableCollection<TarjetasListas>();

                // Mis Listas (Filtramos por mi ID)
                var misListas = todasListas.Where(l => l.IdUsuario == miIdUsuario).ToList();
                if (misListas.Count > 0)
                {
                    listaPlaylist.Add(new TarjetasListas("Sec_MisListas", new ObservableCollection<ListaPersonalizada>(misListas))); // "Mis Listas 👤"
                }

                // Comunidad (El resto)
                var otrasListas = todasListas.Where(l => l.IdUsuario != miIdUsuario).ToList();
                if (otrasListas.Count > 0)
                {
                    listaPlaylist.Add(new TarjetasListas("Sec_Comunidad", new ObservableCollection<ListaPersonalizada>(otrasListas))); // "De la Comunidad 🌎"
                }

                Playlists = listaPlaylist;// Asignamos la lista al Binding

            }
        }
    }
}