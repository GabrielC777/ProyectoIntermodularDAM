using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using DynamicData;
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
        // EL MENÚ DE 3 PUNTOS
        public ReactiveCommand<Canciones, Unit> BtnIrADetalleCancion { get; }
        public ReactiveCommand<object, Unit> BtnIrAArtista { get; }
        public ReactiveCommand<Canciones, Unit> BtnIrAReportar { get; }
        public ReactiveCommand<ListaPersonalizada, Unit> BtnIrADetallesPlaylist { get; }
        public ReactiveCommand<Unit, Unit> BtnRefrescar { get; }

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
            _dialogoService = new DialogoService();

            // Inicializamos el comandos reactive
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
            // Al nacer, este ViewModel se pone a trabajar solo
            _ = CargarDatosCanciones();
        }
        private void IrAArtistaDesdeBoton(object parametro)
        {
            // 1. Recibimos el botón pequeño (el del nombre del artista)
            if (parametro is Button botonPequeño)
            {
                // Recuperamos el Nombre del Artista (DataContext del botón pequeño)
                var nombreArtista = botonPequeño.DataContext as string;

                // 2. Recuperamos al JEFE (El botón de los 3 puntos) desde el Tag
                if (botonPequeño.Tag is Button botonJefe)
                {
                    // A) ¡JEFE, CIERRE EL MENÚ! (Infalible)
                    botonJefe.Flyout?.Hide();

                    // B) ¡JEFE, DEME LA CANCIÓN!
                    // La canción vive en el DataContext del botón jefe (la fila de la lista)
                    var cancion = botonJefe.DataContext as Canciones;

                    // 3. Lógica de navegación (Igual que antes)
                    if (!string.IsNullOrEmpty(nombreArtista) && cancion != null)
                    {
                        int indice = cancion.ListaArtistasIndividuales.IndexOf(nombreArtista);
                        if (indice >= 0 && cancion.AutoresIds != null && indice < cancion.AutoresIds.Count)
                        {
                            string idUsuario = cancion.AutoresIds[indice];
                            SolicitudVerArtista?.Invoke(idUsuario);
                        }
                    }
                }
            }
        }

        private void ReproducirDesdeBoton(object parametro)
        {
            // 1. Buscamos el botón
            if (parametro is Button boton)
            {
                // 2. Sacamos la CANCIÓN (está en el DataContext del botón)
                var cancion = boton.DataContext as Canciones;

                // 3. Sacamos la LISTA REAL (la metimos en el Tag)
                // Viene como 'IEnumerable', así que la convertimos a List<Canciones>
                var coleccionOrigen = boton.Tag as IEnumerable<Canciones>;

                if (cancion != null && coleccionOrigen != null)
                {
                    var listaExacta = coleccionOrigen.ToList();

                    // 4. Enviamos al padre: Canción + Su Lista Exacta
                    EnviarReproduccion?.Invoke(cancion, listaExacta);
                }
            }
        }
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
                var miIdUsuario = GlobalData.Instance.userIdGD;

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
                // OJO: Estas claves "Sec_..." deben ir al Diccionario o usarse con un traductor manual si el objeto no lo hace
                // Asumimos que la vista hará el Binding con Converter o que el traductor es global.
                // Si la vista NO usa converter en el Título, necesitarás un Helper para traducir aquí.

                listaTarjetas.Add(new TarjetasCanciones("Sec_Favoritos", new ObservableCollection<Canciones>(songsFavoritos.Result))); // "Favoritos "
                listaTarjetas.Add(new TarjetasCanciones("Sec_Novedades", new ObservableCollection<Canciones>(songsNovedades.Result))); // "Novedades "

                if (songsRock.Result.Count > 0)
                    listaTarjetas.Add(new TarjetasCanciones("Sec_Rock", new ObservableCollection<Canciones>(songsRock.Result))); // "Rock "

                listaTarjetas.Add(new TarjetasCanciones("Sec_ParaTi", new ObservableCollection<Canciones>(songsGeneral.Result))); // "Para ti "


                Tarjetas = listaTarjetas;

                // --- PROCESAR LISTAS (TarjetasListas) ---
                // Aquí separamos las listas en secciones
                var todasListas = taskPlaylists.Result;
                var listaPlaylist = new ObservableCollection<TarjetasListas>();

                // A. Mis Listas (Filtramos por mi ID)
                var misListas = todasListas.Where(l => l.IdUsuario == miIdUsuario).ToList();
                if (misListas.Count > 0)
                {
                    listaPlaylist.Add(new TarjetasListas("Sec_MisListas", new ObservableCollection<ListaPersonalizada>(misListas))); // "Mis Listas 👤"
                }

                // B. Comunidad (El resto)
                var otrasListas = todasListas.Where(l => l.IdUsuario != miIdUsuario).ToList();
                if (otrasListas.Count > 0)
                {
                    listaPlaylist.Add(new TarjetasListas("Sec_Comunidad", new ObservableCollection<ListaPersonalizada>(otrasListas))); // "De la Comunidad 🌎"
                }

                // Si no hay filtro, añadimos todas en una general
                if (listaPlaylist.Count == 0 && todasListas.Count > 0)
                {
                    listaPlaylist.Add(new TarjetasListas("Sec_TodasLasListas", new ObservableCollection<ListaPersonalizada>(todasListas))); // "Listas de Reproducción"
                }

                Playlists = listaPlaylist;

            }
        }
    }
}