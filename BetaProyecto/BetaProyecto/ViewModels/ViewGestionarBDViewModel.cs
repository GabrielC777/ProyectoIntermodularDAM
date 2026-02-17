using Avalonia.Controls;
using BetaProyecto.Helpers;
using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using MongoDB.Bson;
using MongoDB.Driver;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewGestionarBDViewModel : ViewModelBase
    {
        //Servicios
        private readonly IDialogoService _dialogoService;
        private readonly StorageService _storageService;
        private readonly AudioService _audioService;

        //Comandos reactive
        public ReactiveCommand<Unit, Unit> BtnRecargar { get; }
        public ReactiveCommand<Unit, Unit> BtnGuardarCambios { get; }

        //Biding 
        // Para saber en qué pestaña estamos (0=Usuarios, 1=Canciones, etc.)
        private int _indiceTab;
        public int IndiceTab
        {
            get => _indiceTab;
            set => this.RaiseAndSetIfChanged(ref _indiceTab, value);
        }

        // ==========================================
        // LOGICA PESTAÑA USUARIOS
        // ==========================================

        // --- DataGrid / Colecciones ---
        public ObservableCollection<Usuarios> ListaUsuarios { get; }
        public ObservableCollection<string> RolesDisponibles { get; } // Auxiliar

        // --- Crear ---
        private Usuarios _nuevoUsuario;
        public Usuarios NuevoUsuario
        {
            get => _nuevoUsuario;
            set => this.RaiseAndSetIfChanged(ref _nuevoUsuario, value);
        }
        //Comandos reactive
        public ReactiveCommand<Unit, Unit> BtnCrearUsuario { get; }
        public ReactiveCommand<Unit, Unit> BtnEliminarUsuario { get; }

        // --- Editar / Eliminar ---
        private Usuarios _selectedUsuario;
        public Usuarios SelectedUsuario
        {
            get => _selectedUsuario;
            set => this.RaiseAndSetIfChanged(ref _selectedUsuario, value);
        }


        // ==========================================
        // LOGICA PESTAÑA CANCIONES
        // ==========================================

        // --- DataGrid / Colecciones ---
        public ObservableCollection<Canciones> ListaCanciones { get; }
        public ObservableCollection<string> ListaGenerosCombox { get; } // Auxiliar

        // --- Crear ---
        private Canciones _nuevaCancion;
        public Canciones NuevaCancion
        {
            get => _nuevaCancion;
            set => this.RaiseAndSetIfChanged(ref _nuevaCancion, value);
        }

        // Buscador Artistas (Crear)
        private string _txtBusquedaCrear;
        public string TxtBusquedaCrear
        {
            get => _txtBusquedaCrear;
            set => this.RaiseAndSetIfChanged(ref _txtBusquedaCrear, value);
        }

        private bool _hayResultadosCrear;
        public bool HayResultadosCrear
        {
            get => _hayResultadosCrear;
            set => this.RaiseAndSetIfChanged(ref _hayResultadosCrear, value);
        }
        public ObservableCollection<Usuarios> ListaResultadosCrear { get; }
        public ObservableCollection<Usuarios> ListaArtistasCrear { get; } // Seleccionados

        // Gestión Géneros (Crear)
        private string _generoSeleccionadoCrear;
        public string GeneroSeleccionadoCrear
        {
            get => _generoSeleccionadoCrear;
            set => this.RaiseAndSetIfChanged(ref _generoSeleccionadoCrear, value);
        }
        public ObservableCollection<string> ListaGenerosSeleccionadosCrear { get; }

        // Audio (Crear)
        private bool _esArchivoLocal;
        public bool EsArchivoLocal
        {
            get => _esArchivoLocal;
            set
            {
                this.RaiseAndSetIfChanged(ref _esArchivoLocal, value);
                this.RaisePropertyChanged(nameof(EsYoutube));
            }
        }
        public bool EsYoutube => !EsArchivoLocal;

        // Comandos reactivos para buscador de usuarios (Crear)
        public ReactiveCommand<Unit, Unit> BtnBuscarUsuariosCrear { get; }
        public ReactiveCommand<Usuarios, Unit> BtnAgregarUsuarioCrear { get; }
        public ReactiveCommand<Usuarios, Unit> BtnEliminarUsuarioCrear { get; }

        // Comandos reactivos para gestión de géneros (Crear)
        public ReactiveCommand<Unit, Unit> BtnAgregarGeneroCrear { get; }
        public ReactiveCommand<string, Unit> BtnEliminarGeneroCrear { get; }

        // Comando reactive para crear una canción
        public ReactiveCommand<Unit, Unit> BtnCrearCancion { get; }

        // Comando reactive para eliminar una canción
        public ReactiveCommand<Unit, Unit> BtnEliminarCancion { get; }

        // --- Editar / Eliminar ---
        private Canciones _selectedCancion;
        public Canciones SelectedCancion
        {
            get => _selectedCancion;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedCancion, value);
                CargarDatosEditarCancion(); // Carga artistas, géneros y audio
            }
        }

        // Buscador Artistas (Editar)
        private string _txtBusquedaEditar;
        public string TxtBusquedaEditar
        {
            get => _txtBusquedaEditar;
            set => this.RaiseAndSetIfChanged(ref _txtBusquedaEditar, value);
        }

        private bool _hayResultadosEditar;
        public bool HayResultadosEditar
        {
            get => _hayResultadosEditar;
            set => this.RaiseAndSetIfChanged(ref _hayResultadosEditar, value);
        }
        public ObservableCollection<Usuarios> ListaResultadosEditar { get; }
        public ObservableCollection<Usuarios> ListaArtistasEditar { get; } // Seleccionados

        // Gestión Géneros (Editar)
        private string _generoSeleccionadoEditar;
        public string GeneroSeleccionadoEditar
        {
            get => _generoSeleccionadoEditar;
            set => this.RaiseAndSetIfChanged(ref _generoSeleccionadoEditar, value);
        }
        public ObservableCollection<string> ListaGenerosSeleccionadosEditar { get; }

        // Audio (Editar)
        private string _txtRutaArchivoEditar;
        public string TxtRutaArchivoEditar
        {
            get => _txtRutaArchivoEditar;
            set => this.RaiseAndSetIfChanged(ref _txtRutaArchivoEditar, value);
        }

        private string _txtUrlYoutubeEditar;
        public string TxtUrlYoutubeEditar
        {
            get => _txtUrlYoutubeEditar;
            set => this.RaiseAndSetIfChanged(ref _txtUrlYoutubeEditar, value);
        }

        private bool _esArchivoLocalEditar;
        public bool EsArchivoLocalEditar
        {
            get => _esArchivoLocalEditar;
            set
            {
                this.RaiseAndSetIfChanged(ref _esArchivoLocalEditar, value);
                this.RaisePropertyChanged(nameof(EsYoutubeEditar));
            }
        }
        public bool EsYoutubeEditar => !EsArchivoLocalEditar;

        // Comandos reactivos para buscador de usuarios (Editar)
        public ReactiveCommand<Unit, Unit> BtnBuscarUsuariosEditar { get; }
        public ReactiveCommand<Usuarios, Unit> BtnAgregarUsuarioEditar { get; }
        public ReactiveCommand<Usuarios, Unit> BtnEliminarUsuarioEditar { get; }

        // Comandos reactivos para gestión de géneros (Editar)
        public ReactiveCommand<Unit, Unit> BtnAgregarGeneroEditar { get; }
        public ReactiveCommand<string, Unit> BtnEliminarGeneroEditar { get; }

        // ==========================================
        // LOGICA PESTAÑA GÉNEROS
        // ==========================================

        // --- DataGrid / Colecciones ---
        public ObservableCollection<Generos> ListaGeneros { get; }

        // --- Crear ---
        private string _nuevoGeneroTxt;
        public string NuevoGeneroTxt
        {
            get => _nuevoGeneroTxt;
            set => this.RaiseAndSetIfChanged(ref _nuevoGeneroTxt, value);
        }
        //Comandos reactive para crear género
        public ReactiveCommand<Unit, Unit> BtnCrearGenero { get; }

        //Comandos reactive para eliminar género
        public ReactiveCommand<Unit, Unit> BtnEliminarGenero { get; }

        // --- Editar / Eliminar ---
        private Generos _selectedGenero;
        public Generos SelectedGenero
        {
            get => _selectedGenero;
            set => this.RaiseAndSetIfChanged(ref _selectedGenero, value);
        }

        // ==========================================
        // LOGICA PESTAÑA PLAYLISTS
        // ==========================================

        // --- DataGrid / Colecciones ---
        public ObservableCollection<ListaPersonalizada> ListaPlaylists { get; }

        // --- Crear ---
        private ListaPersonalizada _nuevaPlaylist;
        public ListaPersonalizada NuevaPlaylist
        {
            get => _nuevaPlaylist;
            set => this.RaiseAndSetIfChanged(ref _nuevaPlaylist, value);
        }

        // Buscador Canciones para Playlist (Crear)
        private string _txtBusquedaCancionCrear;
        public string TxtBusquedaCancionCrear
        {
            get => _txtBusquedaCancionCrear;
            set => this.RaiseAndSetIfChanged(ref _txtBusquedaCancionCrear, value);

        }

        private bool _hayResultadosCancionCrear;
        public bool HayResultadosCancionCrear
        {
            get => _hayResultadosCancionCrear;
            set => this.RaiseAndSetIfChanged(ref _hayResultadosCancionCrear, value);
        }

        public ObservableCollection<Canciones> ListaResultadosCancionesCrear { get; }
        public ObservableCollection<Canciones> ListaCancionesPlaylistCrear { get; } // La lista visual de objetos

        // Comandos reactivos para buscador de canciones (Crear)
        public ReactiveCommand<Unit, Unit> BtnBuscarCancionesCrear { get; }
        public ReactiveCommand<Canciones, Unit> BtnAgregarCancionPlaylistCrear { get; }
        public ReactiveCommand<Canciones, Unit> BtnEliminarCancionPlaylistCrear { get; }

        // Comando reactive para crear una playlist
        public ReactiveCommand<Unit, Unit> BtnCrearPlaylist { get; }

        // Comando reactive para eliminar una playlist
        public ReactiveCommand<Unit, Unit> BtnEliminarPlaylist { get; }

        // --- Editar / Eliminar ---
        private ListaPersonalizada _selectedPlaylist;
        public ListaPersonalizada SelectedPlaylist
        {
            get => _selectedPlaylist;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPlaylist, value);
                CargarDatosEditarPlaylist(); // Convierte IDs a Objetos Cancion
            }
        }

        // Buscador Canciones para Playlist (Editar)
        private string _txtBusquedaCancionEditar;
        public string TxtBusquedaCancionEditar
        {
            get => _txtBusquedaCancionEditar;
            set => this.RaiseAndSetIfChanged(ref _txtBusquedaCancionEditar, value);
        }

        private bool _hayResultadosCancionEditar;
        public bool HayResultadosCancionEditar
        {
            get => _hayResultadosCancionEditar;
            set => this.RaiseAndSetIfChanged(ref _hayResultadosCancionEditar, value);

        }
        public ObservableCollection<Canciones> ListaResultadosCancionesEditar { get; }
        public ObservableCollection<Canciones> ListaCancionesPlaylistEditar { get; } // La lista visual de objetos

        // Comandos reactivos para buscador de canciones (Editar)
        public ReactiveCommand<Unit, Unit> BtnBuscarCancionesEditar { get; }
        public ReactiveCommand<Canciones, Unit> BtnAgregarCancionPlaylistEditar { get; }
        public ReactiveCommand<Canciones, Unit> BtnEliminarCancionPlaylistEditar { get; }


        // ==========================================
        // LOGICA PESTAÑA REPORTES
        // ==========================================

        // --- DataGrid / Colecciones ---
        public ObservableCollection<Reportes> ListaReportes { get; }
        public List<string> EstadosReporte { get; } = new List<string>
        {
            "Pendiente",
            "Investigando",
            "Finalizado"
        };
        public ObservableCollection<Reportes> ListaTipoProblema { get; }
        public List<string> TipoProblema { get; } = new List<string>
        {
            "Copyright / Derechos de autor",
            "Contenido ofensivo o inapropiado",
            "Audio defectuoso o silencio",
            "Spam / Información falsa",
            "Otro"
        };

        // --- Crear ---
        private Reportes _nuevoReporte;
        public Reportes NuevoReporte
        {
            get => _nuevoReporte;
            set => this.RaiseAndSetIfChanged(ref _nuevoReporte, value);
        }
        //Comandos reactive para crear un reporte
        public ReactiveCommand<Unit, Unit> BtnCrearReporte { get; }

        //Comandos reactive para eliminar un reporte
        public ReactiveCommand<Unit, Unit> BtnEliminarReporte { get; }

        // --- Editar ---
        private Reportes _selectedReporte;
        public Reportes SelectedReporte
        {
            get => _selectedReporte;
            set => this.RaiseAndSetIfChanged(ref _selectedReporte, value);
        }
        // Progress bar 
        private bool _estaCargando;
        public bool EstaCargando
        {
            get => _estaCargando;
            set
            {
                this.RaiseAndSetIfChanged(ref _estaCargando, value);
                this.RaisePropertyChanged(nameof(NoEstaCargando));
            }
        }
        public bool NoEstaCargando => !EstaCargando;

        private string _mensajeCarga;
        public string MensajeCarga
        {
            get => _mensajeCarga;
            set => this.RaiseAndSetIfChanged(ref _mensajeCarga, value);
        }

        // ============
        // CONSTRUCTOR 
        // ============
        public ViewGestionarBDViewModel()
        {
            // Inicializamos servicios
            _dialogoService = new DialogoService();
            _storageService = new StorageService();
            _audioService = new AudioService();

            // Inicialización Colecciones Tablas
            ListaUsuarios = new ObservableCollection<Usuarios>();
            ListaCanciones = new ObservableCollection<Canciones>();
            ListaPlaylists = new ObservableCollection<ListaPersonalizada>();
            ListaReportes = new ObservableCollection<Reportes>();
            ListaGeneros = new ObservableCollection<Generos>();

            // Inicialización Auxiliares
            RolesDisponibles = new ObservableCollection<string> { "SuperAdmin", "Admin", "Usuario" };
            ListaGenerosCombox = new ObservableCollection<string>();

            // Inicialización Listas y Variables CANCIONES (Crear)
            ListaResultadosCrear = new ObservableCollection<Usuarios>();
            ListaArtistasCrear = new ObservableCollection<Usuarios>();
            ListaGenerosSeleccionadosCrear = new ObservableCollection<string>();
            _esArchivoLocal = true;

            // Inicialización Listas CANCIONES (Editar)
            ListaResultadosEditar = new ObservableCollection<Usuarios>();
            ListaArtistasEditar = new ObservableCollection<Usuarios>();
            ListaGenerosSeleccionadosEditar = new ObservableCollection<string>();
            _esArchivoLocalEditar = true;

            // Inicialización Listas PLAYLISTS (Crear y Editar)
            ListaResultadosCancionesCrear = new ObservableCollection<Canciones>();
            ListaCancionesPlaylistCrear = new ObservableCollection<Canciones>();
            ListaResultadosCancionesEditar = new ObservableCollection<Canciones>();
            ListaCancionesPlaylistEditar = new ObservableCollection<Canciones>();


            //Validación campos reactivos

            // Validaciones Eliminar
            var hayUsuario = this.WhenAnyValue(x => x.SelectedUsuario)
                                 .Select(u => u != null);

            var hayCancion = this.WhenAnyValue(x => x.SelectedCancion)
                                 .Select(c => c != null);

            var hayPlaylist = this.WhenAnyValue(x => x.SelectedPlaylist)
                                  .Select(p => p != null);

            var hayGenero = this.WhenAnyValue(x => x.SelectedGenero)
                                .Select(g => g != null);

            var hayReporte = this.WhenAnyValue(x => x.SelectedReporte)
                                 .Select(r => r != null);

            // Comandos reactive generales 
            BtnRecargar = ReactiveCommand.CreateFromTask(CargarTodo);
            BtnGuardarCambios = ReactiveCommand.CreateFromTask(() =>
                EjecutarConCarga(GuardarSeleccionado, "Msg_Carga_Guardando"));

            // Comandos crear
            BtnCrearCancion = ReactiveCommand.CreateFromTask(() =>
                EjecutarConCarga(CrearCancionTask, "Msg_Carga_CreandoCancion"));

            BtnCrearUsuario = ReactiveCommand.CreateFromTask(() =>
                EjecutarConCarga(CrearUsuarioTask, "Msg_Carga_CreandoUsuario"));

            BtnCrearPlaylist = ReactiveCommand.CreateFromTask(() =>
                EjecutarConCarga(CrearPlaylistTask, "Msg_Carga_CreandoPlaylist"));

            BtnCrearReporte = ReactiveCommand.CreateFromTask(() =>
                EjecutarConCarga(CrearReporteTask, "Msg_Carga_CreandoReporte"));

            BtnCrearGenero = ReactiveCommand.CreateFromTask(() =>
                EjecutarConCarga(AgregarGeneroBD, "Msg_Carga_CreandoGenero"));

            // Comandos eliminar
            BtnEliminarUsuario = ReactiveCommand.CreateFromTask(EliminarUsuarioTask, hayUsuario);
            BtnEliminarCancion = ReactiveCommand.CreateFromTask(EliminarCancionTask, hayCancion);
            BtnEliminarPlaylist = ReactiveCommand.CreateFromTask(EliminarPlaylistTask, hayPlaylist);
            BtnEliminarGenero = ReactiveCommand.CreateFromTask(EliminarGeneroTask, hayGenero);
            BtnEliminarReporte = ReactiveCommand.CreateFromTask(EliminarReporteTask, hayReporte);

            // Comandos Artistas/Generos (Crear)
            BtnBuscarUsuariosCrear = ReactiveCommand.Create(() => BuscarUsuarios(TxtBusquedaCrear, ListaResultadosCrear, ListaArtistasCrear, val => HayResultadosCrear = val));
            BtnAgregarUsuarioCrear = ReactiveCommand.Create<Usuarios>(u => AgregarUsuario(u, ListaArtistasCrear, () => { TxtBusquedaCrear = ""; HayResultadosCrear = false; }));
            BtnEliminarUsuarioCrear = ReactiveCommand.Create<Usuarios>(u => EliminarUsuario(u, ListaArtistasCrear));
            BtnAgregarGeneroCrear = ReactiveCommand.Create(() => AgregarGenero(GeneroSeleccionadoCrear, ListaGenerosSeleccionadosCrear, () => GeneroSeleccionadoCrear = null));
            BtnEliminarGeneroCrear = ReactiveCommand.Create<string>(g => EliminarGenero(g, ListaGenerosSeleccionadosCrear));

            // Comandos Artistas/Generos (Editar)
            BtnBuscarUsuariosEditar = ReactiveCommand.Create(() => BuscarUsuarios(TxtBusquedaEditar, ListaResultadosEditar, ListaArtistasEditar, val => HayResultadosEditar = val));
            BtnAgregarUsuarioEditar = ReactiveCommand.Create<Usuarios>(u => AgregarUsuario(u, ListaArtistasEditar, () => { TxtBusquedaEditar = ""; HayResultadosEditar = false; }));
            BtnEliminarUsuarioEditar = ReactiveCommand.Create<Usuarios>(u => EliminarUsuario(u, ListaArtistasEditar));
            BtnAgregarGeneroEditar = ReactiveCommand.Create(() => AgregarGenero(GeneroSeleccionadoEditar, ListaGenerosSeleccionadosEditar, () => GeneroSeleccionadoEditar = null));
            BtnEliminarGeneroEditar = ReactiveCommand.Create<string>(g => EliminarGenero(g, ListaGenerosSeleccionadosEditar));

            // Comandos Canciones en Playlist (Crear)
            BtnBuscarCancionesCrear = ReactiveCommand.Create(() => BuscarCanciones(TxtBusquedaCancionCrear, ListaResultadosCancionesCrear, ListaCancionesPlaylistCrear, val => HayResultadosCancionCrear = val));
            BtnAgregarCancionPlaylistCrear = ReactiveCommand.Create<Canciones>(c => AgregarCancionAPlaylist(c, ListaCancionesPlaylistCrear, () => { TxtBusquedaCancionCrear = ""; HayResultadosCancionCrear = false; }));
            BtnEliminarCancionPlaylistCrear = ReactiveCommand.Create<Canciones>(c => ListaCancionesPlaylistCrear.Remove(c));

            // Comandos Canciones en Playlist (Editar)
            BtnBuscarCancionesEditar = ReactiveCommand.Create(() => BuscarCanciones(TxtBusquedaCancionEditar, ListaResultadosCancionesEditar, ListaCancionesPlaylistEditar, val => HayResultadosCancionEditar = val));
            BtnAgregarCancionPlaylistEditar = ReactiveCommand.Create<Canciones>(c => AgregarCancionAPlaylist(c, ListaCancionesPlaylistEditar, () => { TxtBusquedaCancionEditar = ""; HayResultadosCancionEditar = false; }));
            BtnEliminarCancionPlaylistEditar = ReactiveCommand.Create<Canciones>(c => ListaCancionesPlaylistEditar.Remove(c));


            // Lógica reactiva (BUSCADORES)
            // Artistas
            this.WhenAnyValue(x => x.TxtBusquedaCrear).Throttle(TimeSpan.FromMilliseconds(500)).Where(x => !string.IsNullOrWhiteSpace(x)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => BtnBuscarUsuariosCrear.Execute().Subscribe());
            this.WhenAnyValue(x => x.TxtBusquedaEditar).Throttle(TimeSpan.FromMilliseconds(500)).Where(x => !string.IsNullOrWhiteSpace(x)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => BtnBuscarUsuariosEditar.Execute().Subscribe());
            // Canciones en Playlist
            this.WhenAnyValue(x => x.TxtBusquedaCancionCrear).Throttle(TimeSpan.FromMilliseconds(500)).Where(x => !string.IsNullOrWhiteSpace(x)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => BtnBuscarCancionesCrear.Execute().Subscribe());
            this.WhenAnyValue(x => x.TxtBusquedaCancionEditar).Throttle(TimeSpan.FromMilliseconds(500)).Where(x => !string.IsNullOrWhiteSpace(x)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => BtnBuscarCancionesEditar.Execute().Subscribe());

            // Carga Inicial
            ResetearBorradores();
            _ = CargarTodo();
        }


        // ==========================================================
        // MÉTODOS DE APOYO Y CARGA DE DATOS EDITAR
        // ==========================================================
        /// <summary>
        /// Reinicializa todos los objetos de borrador y limpia las listas temporales utilizadas en los formularios de creación.
        /// </summary>
        private void ResetearBorradores()
        {
            NuevoUsuario = new Usuarios { Perfil = new PerfilUsuario(), Estadisticas = new EstadisticasUsuario(), Listas = new ListasUsuario() };
            NuevaCancion = new Canciones ();
            NuevaPlaylist = new ListaPersonalizada();
            NuevoReporte = new Reportes { Referencias = new ReferenciasReporte() };

            // Reset Listas Crear Canción
            ListaArtistasCrear.Clear();
            ListaGenerosSeleccionadosCrear.Clear();
            ListaResultadosCrear.Clear();
            TxtBusquedaCrear = "";
            HayResultadosCrear = false;

            // Reset Listas Crear Playlist
            ListaCancionesPlaylistCrear.Clear();
            ListaResultadosCancionesCrear.Clear();
            TxtBusquedaCancionCrear = "";
            HayResultadosCancionCrear = false;

            NuevoGeneroTxt = "";
            GeneroSeleccionadoCrear = "";
        }

        // Cargar datos en listas temporales EDITAR CANCION + AUDIO
        /// <summary>
        /// Prepara el formulario de edición de canciones cargando los metadatos y detectando el tipo de origen del audio (Local vs YouTube).
        /// </summary>
        private void CargarDatosEditarCancion()
        {
            ListaArtistasEditar.Clear();
            ListaGenerosSeleccionadosEditar.Clear();
            ListaResultadosEditar.Clear();
            TxtBusquedaEditar = "";
            HayResultadosEditar = false;

            if (SelectedCancion != null)
            {
                // Listas
                if (SelectedCancion.Datos != null && SelectedCancion.Datos.Generos != null)
                    foreach (var g in SelectedCancion.Datos.Generos) ListaGenerosSeleccionadosEditar.Add(g);

                if (SelectedCancion.AutoresIds != null)
                {
                    foreach (var id in SelectedCancion.AutoresIds)
                    {
                        var user = ListaUsuarios.FirstOrDefault(u => u.Id == id);
                        if (user != null) ListaArtistasEditar.Add(user);
                    }
                }

                // Audio (Detectar Youtube vs Local)
                if (!string.IsNullOrEmpty(SelectedCancion.UrlCancion))
                {
                    bool esLink = SelectedCancion.UrlCancion.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                                  SelectedCancion.UrlCancion.StartsWith("www", StringComparison.OrdinalIgnoreCase);

                    if (esLink)
                    {
                        EsArchivoLocalEditar = false; TxtUrlYoutubeEditar = SelectedCancion.UrlCancion; TxtRutaArchivoEditar = string.Empty;
                    }
                    else
                    {
                        EsArchivoLocalEditar = true; TxtRutaArchivoEditar = SelectedCancion.UrlCancion; TxtUrlYoutubeEditar = string.Empty;
                    }
                }
                else
                {
                    EsArchivoLocalEditar = true; TxtRutaArchivoEditar = ""; TxtUrlYoutubeEditar = "";
                }
            }
        }

        // Cargar datos en listas temporales EDITAR PLAYLIST (IDs -> Objetos)
        /// <summary>
        /// Mapea los identificadores de canciones de una playlist seleccionada a objetos completos para su edición en la lista de pistas.
        /// </summary>
        private void CargarDatosEditarPlaylist()
        {
            ListaCancionesPlaylistEditar.Clear();
            ListaResultadosCancionesEditar.Clear();
            TxtBusquedaCancionEditar = "";
            HayResultadosCancionEditar = false;

            if (SelectedPlaylist != null && SelectedPlaylist.IdsCanciones != null)
            {
                foreach (var id in SelectedPlaylist.IdsCanciones)
                {
                    var cancionReal = ListaCanciones.FirstOrDefault(c => c.Id == id);
                    if (cancionReal != null) ListaCancionesPlaylistEditar.Add(cancionReal);
                }
            }
        }


        // ==========================================================
        // MÉTODOS LÓGICOS DE AGREGAR/BUSCAR
        // ==========================================================
        /// <summary>
        /// Filtra la lista global de usuarios basándose en un criterio de búsqueda y excluye a los que ya han sido seleccionados.
        /// </summary>
        /// <param name="texto">Cadena de búsqueda.</param>
        /// <param name="resultados">Colección donde se volcarán los resultados.</param>
        /// <param name="yaSeleccionados">Colección de usuarios a excluir.</param>
        /// <param name="setHayResultados">Acción para actualizar el estado de visibilidad de resultados.</param>
        private void BuscarUsuarios(string texto, ObservableCollection<Usuarios> resultados, ObservableCollection<Usuarios> yaSeleccionados, Action<bool> setHayResultados)
        {
            if (string.IsNullOrWhiteSpace(texto)) { resultados.Clear(); setHayResultados(false); return; }
            var res = ListaUsuarios.Where(u => u.Username != null && u.Username.Contains(texto, StringComparison.OrdinalIgnoreCase) && !yaSeleccionados.Contains(u)).ToList();
            resultados.Clear();
            foreach (var r in res) resultados.Add(r);
            setHayResultados(resultados.Count > 0);
        }
        /// <summary>
        /// Añade un usuario a la lista de destino y ejecuta una acción de limpieza en la interfaz.
        /// </summary>
        private void AgregarUsuario(Usuarios usuario, ObservableCollection<Usuarios> listaDestino, Action limpiarUI)
        {
            if (usuario != null && !listaDestino.Contains(usuario)) { listaDestino.Add(usuario); limpiarUI(); }
        }
        /// <summary>
        /// Remueve un usuario de la colección especificada.
        /// </summary>
        private void EliminarUsuario(Usuarios usuario, ObservableCollection<Usuarios> listaObjetivo)
        {
            if (usuario != null && listaObjetivo.Contains(usuario)) listaObjetivo.Remove(usuario);
        }

        /// <summary>
        /// Valida y añade un nuevo género a la lista de selección, evitando duplicados.
        /// </summary>
        private void AgregarGenero(string genero, ObservableCollection<string> listaDestino, Action limpiarCombo)
        {
            if (!string.IsNullOrWhiteSpace(genero))
            {
                if (!listaDestino.Any(g => g.Equals(genero, StringComparison.OrdinalIgnoreCase))) { listaDestino.Add(genero); limpiarCombo(); }
                else _dialogoService.MostrarAlerta("Ese género ya está añadido.");
            }
        }
        /// <summary>
        /// Remueve un género de la colección especificada.
        /// </summary>
        private void EliminarGenero(string genero, ObservableCollection<string> listaObjetivo)
        {
            if (listaObjetivo.Contains(genero)) listaObjetivo.Remove(genero);
        }

        // Lógica Playlist (Buscar Canciones)
        /// <summary>
        /// Remueve un género de la colección especificada.
        /// </summary>
        private void BuscarCanciones(string texto, ObservableCollection<Canciones> resultados, ObservableCollection<Canciones> yaSeleccionadas, Action<bool> setHayResultados)
        {
            if (string.IsNullOrWhiteSpace(texto)) { resultados.Clear(); setHayResultados(false); return; }
            var res = ListaCanciones.Where(c => c.Titulo != null && c.Titulo.Contains(texto, StringComparison.OrdinalIgnoreCase) && !yaSeleccionadas.Contains(c)).ToList();
            resultados.Clear();
            foreach (var r in res) resultados.Add(r);
            setHayResultados(resultados.Count > 0);
        }
        /// <summary>
        /// Añade una canción a la colección de destino de la playlist y limpia el estado de búsqueda.
        /// </summary>
        private void AgregarCancionAPlaylist(Canciones c, ObservableCollection<Canciones> destino, Action limpiar)
        {
            if (c != null && !destino.Contains(c)) {
                destino.Add(c); limpiar(); 
            }
        }

        // ==========================================================
        // OPERACIONES BASE DE DATOS (CARGAR, CREAR, EDITAR, ELIMINAR)
        // ==========================================================

        // --- CARGAR TODO ---
        /// <summary>
        /// Realiza una carga masiva inicial de usuarios, canciones, playlists, reportes y géneros desde MongoDB.
        /// </summary>
        private async Task CargarTodo()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;

            // Cargar datos
            var users = await cliente.ObtenerTodosLosUsuarios();
            ListaUsuarios.Clear();
            foreach (var u in users) ListaUsuarios.Add(u);

            var songs = await cliente.ObtenerCanciones();
            ListaCanciones.Clear();
            foreach (var s in songs) ListaCanciones.Add(s);

            var plays = await cliente.ObtenerListasReproduccion();
            ListaPlaylists.Clear();
            foreach (var p in plays) ListaPlaylists.Add(p);

            var reps = await cliente.ObtenerReportes();
            ListaReportes.Clear();
            foreach (var r in reps) ListaReportes.Add(r);

            var gens = await cliente.ObtenerGenerosCompletos();
            ListaGeneros.Clear();
            ListaGenerosCombox.Clear();
            foreach (var g in gens) { ListaGeneros.Add(g); ListaGenerosCombox.Add(g.Nombre); }
        }

        // --- CREAR ---
        /// <summary>
        /// Orquesta el proceso de creación de un nuevo usuario, incluyendo validación de duplicados, carga de imagen a la nube y encriptación de credenciales.
        /// </summary>
        private async Task CrearUsuarioTask()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;
            if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

            // Buscamos si OTRO usuario ya tiene ese email en el datagrid 
            bool emailDuplicado = ListaUsuarios.Any(u => u.Email.Equals(NuevoUsuario.Email, StringComparison.OrdinalIgnoreCase));
            if (emailDuplicado)
            {
                _dialogoService.MostrarAlerta("Msg_Error_EmailDuplicado");
                return;
            }

            // Validación de campos vacios
            if (string.IsNullOrWhiteSpace(NuevoUsuario.Username) ||
                string.IsNullOrWhiteSpace(NuevoUsuario.Email) ||
                string.IsNullOrWhiteSpace(NuevoUsuario.Password) ||
                string.IsNullOrWhiteSpace(NuevoUsuario.Rol) ||
                string.IsNullOrWhiteSpace(NuevoUsuario.Perfil.Pais) ||
                string.IsNullOrWhiteSpace(NuevoUsuario.Perfil.ImagenUrl) ||
                NuevoUsuario.Perfil.FechaNacimiento == default)
            {
                _dialogoService.MostrarAlerta("Msg_Error_FaltanDatosUser");
                return;
            }

            // Subir imagen (Si es un archivo local)
            if (File.Exists(NuevoUsuario.Perfil.ImagenUrl))
            {
                try
                {
                    string urlNube = await _storageService.SubirImagen(NuevoUsuario.Perfil.ImagenUrl);
                    NuevoUsuario.Perfil.ImagenUrl = urlNube;
                }
                catch (Exception ex)
                {
                    _dialogoService.MostrarAlerta("Msg_Error_SubirImagen" + ex.Message);
                    return;
                }
            }

            // Encripatar contraseña 
            NuevoUsuario.Password = Encriptador.HashPassword(NuevoUsuario.Password);

            // Preparamos de datos
            NuevoUsuario.Id = ObjectId.GenerateNewId().ToString();
            NuevoUsuario.FechaRegistro = DateTime.Now;
            if (NuevoUsuario.Listas == null) NuevoUsuario.Listas = new ListasUsuario();
            if (NuevoUsuario.Estadisticas == null) NuevoUsuario.Estadisticas = new EstadisticasUsuario();

            // Guardamos en la base de datos
            bool exito = await cliente.CrearUsuario(NuevoUsuario);

            if (exito)
            {
                ListaUsuarios.Add(NuevoUsuario);
                ResetearBorradores();
                _dialogoService.MostrarAlerta("Msg_Exito_UsuarioCreado");
            }
            else
            {
                _dialogoService.MostrarAlerta("Msg_Error_CrearUsuario");
            }
        }
        /// <summary>
        /// Gestiona la publicación de una nueva canción, procesando la subida de archivos multimedia y el cálculo de duraciones mediante APIs externas o análisis local.
        /// </summary>
        private async Task CrearCancionTask()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;

            if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }
            // Validamos
            if (string.IsNullOrWhiteSpace(NuevaCancion.Titulo) ||
                string.IsNullOrWhiteSpace(NuevaCancion.UrlCancion) ||
                string.IsNullOrWhiteSpace(NuevaCancion.ImagenPortadaUrl))
            {
                _dialogoService.MostrarAlerta("Msg_Error_FaltanDatosCancion");
                return;
            }
            if (ListaArtistasCrear.Count == 0 || ListaGenerosSeleccionadosCrear.Count == 0)
            {
                _dialogoService.MostrarAlerta("Msg_Error_FaltanArtistasGeneros");
                return;
            }

            try
            {
                // Subimos portada
                if (File.Exists(NuevaCancion.ImagenPortadaUrl))
                {
                    NuevaCancion.ImagenPortadaUrl = await _storageService.SubirImagen(NuevaCancion.ImagenPortadaUrl);
                }

                // Subimos audio y obtenemos la duración
                if (EsYoutube)
                {
                    // Si es YouTube, usamos la API para sacar la duración
                    var info = await _audioService.ObtenerMp3(NuevaCancion.UrlCancion);
                    if (info != null && info.DuracionSegundos > 0)
                    {
                        NuevaCancion.Datos.DuracionSegundos = info.DuracionSegundos;
                    }
                }
                else
                {
                    // Si es archivo local
                    if (File.Exists(NuevaCancion.UrlCancion))
                    {
                        // Calculamos duración antes de subir
                        NuevaCancion.Datos.DuracionSegundos = ObtenerDuracionLocal(NuevaCancion.UrlCancion);
                        // Subimos el archivo y guardamos la URL de la nube
                        NuevaCancion.UrlCancion = await _storageService.SubirCancion(NuevaCancion.UrlCancion);
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogoService.MostrarAlerta("Msg_Error_GestionArchivos" + ex.Message);
                return;
            }

            // Preparamos datos
            if (NuevaCancion.Datos == null) NuevaCancion.Datos = new DatosCancion();
            NuevaCancion.Datos.Generos = ListaGenerosSeleccionadosCrear.ToList();
            NuevaCancion.AutoresIds = ListaArtistasCrear.Select(u => u.Id).ToList();

            NuevaCancion.Datos.FechaLanzamiento = DateTime.Now;
            // Generar nombre artista para la vista rápida
            NuevaCancion.NombreArtista = string.Join(", ", ListaArtistasCrear.Select(u => u.Username));

            if (EsYoutube)
            {
                // Avisamos al usuario en el mensaje de carga (ya que esto tarda un poco)
                MensajeCarga = "Msg_Carga_AnalizandoDuracion";
                var info = await _audioService.ObtenerMp3(NuevaCancion.UrlCancion);
                if (info != null && info.DuracionSegundos > 0)
                {
                    NuevaCancion.Datos.DuracionSegundos = info.DuracionSegundos;
                }
            }
            // Guardamos en la base de datos
            bool exito = await cliente.PublicarCancion(NuevaCancion);

            if (exito)
            {
                if (NuevaCancion.AutoresIds != null)
                {
                    foreach (var idAutor in NuevaCancion.AutoresIds)
                    {
                        await cliente.IncrementarContadorCancionesUsuario(idAutor, 1);
                    }
                }
                ListaCanciones.Add(NuevaCancion);
                ResetearBorradores();
                _dialogoService.MostrarAlerta("Msg_Exito_CancionPublicada");
            }
            else
            {
                _dialogoService.MostrarAlerta("Msg_Error_SubirCancion");
            }
        }
        /// <summary>
        /// Inserta un nuevo género musical en la base de datos tras validar su inexistencia previa.
        /// </summary>
        private async Task AgregarGeneroBD()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;
            if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

            // Validamos
            if (string.IsNullOrWhiteSpace(NuevoGeneroTxt))
            {
                _dialogoService.MostrarAlerta("Msg_Error_NombreGeneroVacio");
                return;
            }

            // Validamos que no exista ya ese género
            bool generoExiste = ListaGeneros.Any(g => g.Nombre.Equals(NuevoGeneroTxt, StringComparison.OrdinalIgnoreCase));
            if (generoExiste)
            {
                _dialogoService.MostrarAlerta("Msg_Error_GeneroExiste");
                return;
            }

            // Guardamos
            bool exito = await cliente.CrearGenero(NuevoGeneroTxt);

            if (exito)
            {
                NuevoGeneroTxt = "";
                await CargarTodo();
                _dialogoService.MostrarAlerta("Msg_Exito_GeneroAnadido");
            }
            else
            {
                _dialogoService.MostrarAlerta("Msg_Error_CrearGenero");
            }
        }
        /// <summary>
        /// Persiste una nueva playlist en MongoDB, gestionando la subida de la imagen de portada y la vinculación de IDs de canciones.
        /// </summary>
        private async Task CrearPlaylistTask()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;

            if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

            // Validamos
            if (string.IsNullOrWhiteSpace(NuevaPlaylist.Nombre) ||
                string.IsNullOrWhiteSpace(NuevaPlaylist.IdUsuario) |
                string.IsNullOrWhiteSpace(NuevaPlaylist.Descripcion) ||
                string.IsNullOrWhiteSpace(NuevaPlaylist.UrlPortada))
            {
                _dialogoService.MostrarAlerta("Msg_Error_FaltanDatosPlaylist");
                return;
            }
            // Subimos portada
            if (File.Exists(NuevaPlaylist.UrlPortada))
            {
                try
                {
                    NuevaPlaylist.UrlPortada = await _storageService.SubirImagen(NuevaPlaylist.UrlPortada);
                }
                catch (Exception ex)
                {
                    _dialogoService.MostrarAlerta("Msg_Error_SubirImagen" + ex.Message);
                    return;
                }
            }

            // Preparamos datos
            NuevaPlaylist.IdsCanciones = ListaCancionesPlaylistCrear.Select(c => c.Id).ToList();

            // Guardamos en la base de datos
            bool exito = await cliente.CrearListaReproduccion(NuevaPlaylist);

            if (exito)
            {
                ListaPlaylists.Add(NuevaPlaylist);
                ResetearBorradores();
                _dialogoService.MostrarAlerta("Msg_Exito_PlaylistCreada");
            }
            else
            {
                _dialogoService.MostrarAlerta("Msg_Error_CrearPlaylist");
            }
        }
        /// <summary>
        /// Registra un nuevo reporte de error o infracción en el sistema.
        /// </summary>
        private async Task CrearReporteTask()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;

            if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

            // Validamos
            if (string.IsNullOrWhiteSpace(NuevoReporte.TipoProblema) ||
                string.IsNullOrWhiteSpace(NuevoReporte.Descripcion) ||
                string.IsNullOrWhiteSpace(NuevoReporte.Estado))
            {
                _dialogoService.MostrarAlerta("Msg_Error_FaltanDatosReporte");
                return;
            }
            if (string.IsNullOrWhiteSpace(NuevoReporte.Referencias.CancionReportadaId) ||
                string.IsNullOrWhiteSpace(NuevoReporte.Referencias.UsuarioReportanteId))
            {
                _dialogoService.MostrarAlerta("Msg_Error_FaltanRefReporte");
                return;
            }

            // Preparamos datos
            NuevoReporte.FechaCreacion = DateTime.Now;

            // Guardamos
            bool exito = await cliente.EnviarReporte(NuevoReporte);

            if (exito)
            {
                ResetearBorradores();
                _ = CargarTodo();
                _dialogoService.MostrarAlerta("Msg_Exito_ReporteRegistrado");
            }
            else
            {
                _dialogoService.MostrarAlerta("Msg_Exito_ReporteRegistrado");
            }
        }

        // --- GUARDAR EDICIÓN (UPDATE) ---
        /// <summary>
        /// Método central de edición que detecta la pestaña activa del panel (Usuario, Canción, Género, Playlist, Reporte) y sincroniza los cambios con MongoDB.
        /// </summary>
        private async Task GuardarSeleccionado()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;
            bool exito = false;
            switch (IndiceTab)
            {
                // -----------
                // 0. USUARIOS
                // -----------
                case 0:
                    if (SelectedUsuario != null)
                    {
                        // 1. Validaciones
                        if (string.IsNullOrWhiteSpace(SelectedUsuario.Username) ||
                            string.IsNullOrWhiteSpace(SelectedUsuario.Email) ||
                            string.IsNullOrWhiteSpace(SelectedUsuario.Perfil.Pais))
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_FaltanDatosObligatorios");
                            return;
                        }

                        Usuarios usuarioOriginal = null;
                        try
                        {
                            var lista = await cliente.ObtenerUsuariosPorListaIds(new List<string> { SelectedUsuario.Id });
                            usuarioOriginal = lista.FirstOrDefault();

                            if (usuarioOriginal != null && usuarioOriginal.Perfil == null)
                                usuarioOriginal.Perfil = new PerfilUsuario { ImagenUrl = "" };
                        }
                        catch (Exception x )
                        { 
                            System.Diagnostics.Debug.WriteLine("[ERROR] Fallo al recuperar original: " + x.Message);
                        }

                        if (SelectedUsuario.Password != usuarioOriginal.Password)
                        {
                            SelectedUsuario.Password = Encriptador.HashPassword(SelectedUsuario.Password);
                        }

                        // 3. GESTIÓN DE FOTO DE PERFIL
                        try
                        {
                            // Comparamos la URL actual del TextBox con la de la BD
                            if (SelectedUsuario.Perfil.ImagenUrl != usuarioOriginal.Perfil.ImagenUrl)
                            {
                                // Si son diferentes y es un archivo local -> SUBIR
                                if (File.Exists(SelectedUsuario.Perfil.ImagenUrl))
                                {
                                    MensajeCarga = "Msg_Carga_SubiendoAvatar";
                                    SelectedUsuario.Perfil.ImagenUrl = await _storageService.SubirImagen(SelectedUsuario.Perfil.ImagenUrl);
                                }
                            }
                            else
                            {
                                // Si es igual, restauramos la original para asegurar integridad
                                SelectedUsuario.Perfil.ImagenUrl = usuarioOriginal.Perfil.ImagenUrl;
                            }
                        }
                        catch (Exception ex)
                        {
                            _dialogoService.MostrarAlerta("[ERROR] Error subiendo imagen: " + ex.Message);
                            return;
                        }

                        if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

                        // 4. GUARDAR CAMBIOS
                        exito = await cliente.ActualizarUsuario(
                            SelectedUsuario.Id,
                            SelectedUsuario.Username,
                            SelectedUsuario.Email,
                            SelectedUsuario.Password,
                            SelectedUsuario.Rol,
                            SelectedUsuario.Perfil.Pais,
                            SelectedUsuario.Perfil.ImagenUrl,
                            SelectedUsuario.Perfil.FechaNacimiento,
                            SelectedUsuario.Perfil.EsPrivada);

                        if (exito)
                        {
                            // 6. ACTUALIZAR SESIÓN ACTUAL (Si me he editado a mí mismo)
                            if (SelectedUsuario.Id == GlobalData.Instance.UserIdGD)
                            {
                                GlobalData.Instance.SetUserData(SelectedUsuario);
                                System.Diagnostics.Debug.WriteLine("[INFO] Datos de sesión actualizados tras edición.");
                            }
                            await CargarTodo();
                            _dialogoService.MostrarAlerta("Msg_Exito_UsuarioActualizado.");
                        }
                        else
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_ActualizarUsuario");
                        }
                    }
                    break;

                // ---------------------------
                // 1. CANCIONES
                // ---------------------------
                case 1:
                    if (SelectedCancion != null)
                    {
                        // 1. Validaciones
                        if (string.IsNullOrWhiteSpace(SelectedCancion.Titulo) ||
                            string.IsNullOrWhiteSpace(SelectedCancion.UrlCancion) ||
                            string.IsNullOrWhiteSpace(SelectedCancion.ImagenPortadaUrl))
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_FaltanDatosObligatorios");
                            return;
                        }

                        // =========================================================
                        // 2. RECUPERAR VERSIÓN ORIGINAL (PARA COMPARAR)
                        // =========================================================
                        Canciones cancionOriginal = null;
                        List<string> idsViejos = new List<string>();
                        
                        try
                        {
                            // Traemos la canción tal cual está en la base de datos ahora mismo
                            var listaTemp = await cliente.ObtenerCancionesPorListaIds(new List<string> { SelectedCancion.Id });
                            cancionOriginal = listaTemp.FirstOrDefault();
                            
                            if (cancionOriginal != null)
                            {
                                // Guardamos ids viejos para el contador luego
                                idsViejos = cancionOriginal.AutoresIds != null ? cancionOriginal.AutoresIds.ToList() : new List<string>();

                                // Si en la BD los campos son null, los convertimos a listas vacías
                                if (cancionOriginal.AutoresIds == null) cancionOriginal.AutoresIds = new List<string>();
                                if (cancionOriginal.Datos == null) cancionOriginal.Datos = new DatosCancion();
                                if (cancionOriginal.Datos.Generos == null) cancionOriginal.Datos.Generos = new List<string>();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("[ERROR] Fallo al recuperar original: " + ex.Message);
                        }

                        // Si falló la recuperación, creamos uno vacío seguro para no bloquear
                        if (cancionOriginal == null) 
                        {
                            cancionOriginal = new Canciones 
                            { 
                                Id = SelectedCancion.Id, 
                                AutoresIds = new List<string>(), // Lista vacía, no null
                                Datos = new DatosCancion { Generos = new List<string>() } // Lista vacía, no null
                            };
                        }

                        // 3. ACTUALIZAR OBJETO LOCAL 
                        if (SelectedCancion.Datos == null) SelectedCancion.Datos = new DatosCancion();
                        SelectedCancion.Datos.Generos = ListaGenerosSeleccionadosEditar.ToList();
                        SelectedCancion.AutoresIds = ListaArtistasEditar.Select(u => u.Id).ToList();

                        // 4. GESTIÓN DE ARCHIVOS
                        try
                        {
                            // Solo procesamos si la URL es DIFERENTE a la que había antes
                            if (SelectedCancion.ImagenPortadaUrl != cancionOriginal.ImagenPortadaUrl)
                            {
                                // Si ha cambiado y ADEMÁS es un archivo físico en el PC -> Subimos
                                if (File.Exists(SelectedCancion.ImagenPortadaUrl))
                                {
                                    MensajeCarga = "Msg_Carga_SubiendoPortada";
                                    SelectedCancion.ImagenPortadaUrl = await _storageService.SubirImagen(SelectedCancion.ImagenPortadaUrl);
                                }
                            }
                            else
                            {
                                // Si no ha cambiado, aseguramos que se queda la original (por seguridad)
                                System.Diagnostics.Debug.WriteLine("[INFO] La portada no ha cambiado.");
                                SelectedCancion.ImagenPortadaUrl = cancionOriginal.ImagenPortadaUrl;
                            }
                            // GESTIÓN DE AUDIO 
                            // 1. Determinamos cuál es la URL nueva propuesta según el TextBox activo
                            string nuevaUrlAudio = EsArchivoLocalEditar ? TxtRutaArchivoEditar : TxtUrlYoutubeEditar;

                            // 2. Solo procesamos si la URL HA CAMBIADO respecto a la base de datos
                            if (nuevaUrlAudio != cancionOriginal.UrlCancion)
                            {
                                System.Diagnostics.Debug.WriteLine("[DEBUG] El audio ha cambiado. Procesando...");

                                if (EsArchivoLocalEditar)
                                {
                                    // CASO LOCAL: Solo subimos si es un archivo físico en el disco
                                    if (File.Exists(TxtRutaArchivoEditar))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Subiendo nuevo archivo local: {TxtRutaArchivoEditar}");

                                        // Calculamos duración con TagLib
                                        SelectedCancion.Datos.DuracionSegundos = ObtenerDuracionLocal(TxtRutaArchivoEditar);

                                        // Subimos a Cloudinary y obtenemos la nueva URL
                                        SelectedCancion.UrlCancion = await _storageService.SubirCancion(TxtRutaArchivoEditar);
                                    }
                                    else
                                    {
                                        // Si no existe el archivo (ej: url rota), mantenemos el texto tal cual por si acaso
                                        SelectedCancion.UrlCancion = TxtRutaArchivoEditar;
                                        _dialogoService.MostrarAlerta("El archivo local especificado no existe. Comprueba");
                                    }
                                }
                                else
                                {
                                    // CASO YOUTUBE: Asignamos la nueva URL y consultamos la API
                                    SelectedCancion.UrlCancion = TxtUrlYoutubeEditar;

                                    if (!string.IsNullOrEmpty(SelectedCancion.UrlCancion) &&
                                       (SelectedCancion.UrlCancion.Contains("youtube") || SelectedCancion.UrlCancion.Contains("youtu.be")))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Analizando nueva URL de YouTube...");
                                        var info = await _audioService.ObtenerMp3(SelectedCancion.UrlCancion);
                                        if (info != null && info.DuracionSegundos > 0)
                                        {
                                            SelectedCancion.Datos.DuracionSegundos = info.DuracionSegundos;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // NO HA CAMBIADO: Asignamos la URL vieja y nos ahorramos el trabajo
                                System.Diagnostics.Debug.WriteLine("[DEBUG] El audio NO ha cambiado. Se mantiene.");
                                SelectedCancion.UrlCancion = cancionOriginal.UrlCancion;
                            }
                        }
                        catch (Exception ex)
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_GestionArchivos" + ex.Message);
                            return; 
                        }

                        if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

                        // 5. LLAMADA A ACTUALIZAR
                        // Ahora 'cancionOriginal' es segura (no tiene nulls), así que MongoAtlas no fallará.
                        exito = await cliente.ActualizarCancion(
                            SelectedCancion.Titulo,
                            SelectedCancion.ImagenPortadaUrl,
                            SelectedCancion.AutoresIds,
                            SelectedCancion.Datos.Generos,
                            cancionOriginal 
                        );

                        // 6. ACTUALIZAR CONTADORES DE USUARIOS
                        if (exito)
                        {
                            try
                            {
                                var idsNuevos = SelectedCancion.AutoresIds;

                                // A. Sumar a los nuevos
                                var nuevosAutores = idsNuevos.Except(idsViejos).ToList();
                                foreach (var id in nuevosAutores)
                                    await cliente.IncrementarContadorCancionesUsuario(id, 1);

                                // B. Restar a los viejos
                                var autoresEliminados = idsViejos.Except(idsNuevos).ToList();
                                foreach (var id in autoresEliminados)
                                    await cliente.IncrementarContadorCancionesUsuario(id, -1);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Warning contadores: " + ex.Message);
                            }

                            await CargarTodo();
                            _dialogoService.MostrarAlerta("Msg_Exito_CancionActualizada");
                        }
                        else
                        {
                            // Si devuelve false, es que no detectó cambios reales (o hubo error interno controlado)
                            _dialogoService.MostrarAlerta("Msg_Error_NoCambios");
                        }
                    }
                    break;

                // ----------
                // 2. GÉNEROS
                // ----------
                case 2:
                    if (SelectedGenero != null)
                    {
                        if (string.IsNullOrWhiteSpace(SelectedGenero.Nombre))
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_NombreGeneroVacio");
                            return;
                        }

                        // Comprobamos si hay OTRO género con ese nombre (excluyendo al propio) en el propio datagrid
                        bool existeGenero = ListaGeneros.Any(g => g.Nombre.Equals(SelectedGenero.Nombre, StringComparison.OrdinalIgnoreCase)
                                                                  && g.Id != SelectedGenero.Id);
                        if (existeGenero)
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_GeneroExiste");
                            return;
                        }

                        if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

                        exito = await cliente.ActualizarGenero(SelectedGenero.Id, SelectedGenero.Nombre);

                        if (exito)
                        {
                            await CargarTodo();
                            _dialogoService.MostrarAlerta("Msg_Exito_GeneroActualizado");
                        }
                        else
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_ActualizarGenero");
                        }
                    }
                    break;

                // ------------
                // 3. PLAYLISTS
                // ------------
                case 3:
                    if (SelectedPlaylist != null)
                    {
                        // 1. Validaciones
                        if (string.IsNullOrWhiteSpace(SelectedPlaylist.Nombre) ||
                            string.IsNullOrWhiteSpace(SelectedPlaylist.Descripcion) ||
                            string.IsNullOrWhiteSpace(SelectedPlaylist.UrlPortada))
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_FaltanDatosPlaylist");
                            return;
                        }
                        if (ListaCancionesPlaylistEditar.Count == 0)
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_PlaylistVacia");
                            return;
                        }

                        ListaPersonalizada playlistOriginal = null;
                        try
                        {
                            // Accedemos directo a la colección para buscar por ID
                            // (Asegúrate de tener 'using MongoDB.Driver;' arriba si te marca error en 'Builders')
                            var col = cliente.Database.GetCollection<ListaPersonalizada>("listapersonalizada");
                            var filtro = Builders<ListaPersonalizada>.Filter.Eq(x => x.Id, SelectedPlaylist.Id);

                            playlistOriginal = await col.Find(filtro).FirstOrDefaultAsync();
                        }
                        catch {
                        }

                        // Fallback de seguridad si no se encontró
                        if (playlistOriginal == null)
                        {
                            playlistOriginal = new ListaPersonalizada
                            {
                                Id = SelectedPlaylist.Id,
                                UrlPortada = "", // Para que la comparación no falle
                                IdsCanciones = new List<string>()
                            };
                        }

                        // 3. ACTUALIZAR OBJETO LOCAL
                        SelectedPlaylist.IdsCanciones = ListaCancionesPlaylistEditar.Select(c => c.Id).ToList();

                        // 4. GESTIÓN DE PORTADA (SI CAMBIÓ)
                        try
                        {
                            // Comparamos la URL del TextBox con la de la BD
                            if (SelectedPlaylist.UrlPortada != playlistOriginal.UrlPortada)
                            {
                                // Si es diferente y es un archivo local -> SUBIR
                                if (File.Exists(SelectedPlaylist.UrlPortada))
                                {
                                    MensajeCarga = "Msg_Carga_SubiendoPortadaPly";
                                    SelectedPlaylist.UrlPortada = await _storageService.SubirImagen(SelectedPlaylist.UrlPortada);
                                }
                            }
                            else
                            {
                                // Si es igual, restauramos la original para asegurar
                                SelectedPlaylist.UrlPortada = playlistOriginal.UrlPortada;
                            }
                        }
                        catch (Exception ex)
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_SubirImagen" + ex.Message);
                            return;
                        }

                        if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

                        // 5. GUARDAR EN BD
                        exito = await cliente.ActualizarPlaylist(
                            SelectedPlaylist.Nombre,
                            SelectedPlaylist.Descripcion,
                            SelectedPlaylist.IdsCanciones,
                            SelectedPlaylist.UrlPortada,
                            playlistOriginal
                        );

                        if (exito)
                        {
                            await CargarTodo();
                            _dialogoService.MostrarAlerta("Msg_Exito_PlaylistActualizada");
                        }
                        else
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_NoCambios");
                        }
                    }
                    break;

                // ------------
                // 4. REPORTES
                // ------------
                case 4:
                    if (SelectedReporte != null)
                    {
                        if (string.IsNullOrWhiteSpace(SelectedReporte.Estado))
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_FaltaEstadoReporte");
                            return;
                        }

                        if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

                        exito = await cliente.ActualizarEstadoReporte(SelectedReporte.Estado, SelectedReporte.Resolucion, SelectedReporte);

                        if (exito)
                        {
                            await CargarTodo();
                            _dialogoService.MostrarAlerta("Msg_Exito_ReporteActualizado");
                        }
                        else
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_ActualizarReporte");
                        }
                    }
                    break;
            }
        }

        // --- ELIMINAR (DELETE) ---
        /// <summary>
        /// Elimina permanentemente un usuario de la base de datos tras confirmar la acción y validar que no sea el usuario en sesión.
        /// </summary>
        private async Task EliminarUsuarioTask()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;
            if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }
            // TRADUCCIÓN DEL DIÁLOGO DE CONFIRMACIÓN
            if (!await _dialogoService.Preguntar(
                "Msg_Confirmar_Titulo",
                "Msg_Confirmar_BorrarUsuario",
                "Msg_Confirmar_BtnSi",
                "Msg_Confirmar_BtnNo")) return;

            // --- BLOQUEO DE SEGURIDAD ---
            if (SelectedUsuario.Id == GlobalData.Instance.UserIdGD)
            {
                _dialogoService.MostrarAlerta("Msg_Error_BorrarPropioUser"); 
                return;
            }

            await EjecutarConCarga(async () =>
            {
                bool exito = await cliente.EliminarUsuario(SelectedUsuario.Id);
                if (exito)
                {
                    await CargarTodo();
                    _dialogoService.MostrarAlerta("Msg_Exito_UsuarioEliminado"); 
                }
                else _dialogoService.MostrarAlerta("Msg_Error_EliminarUsuario"); 
            }, "Msg_Carga_EliminandoUser");
        }
        /// <summary>
        /// Ejecuta el proceso de borrado de una canción, eliminando el archivo físico de Cloudinary si aplica y actualizando los contadores de sus autores.
        /// </summary>
        private async Task EliminarCancionTask()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;
            if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

            if (!await _dialogoService.Preguntar(
                "Msg_Confirmar_Titulo",
                "Msg_Confirmar_BorrarCancion",
                "Msg_Confirmar_BtnSi",
                "Msg_Confirmar_BtnNo")) return;

            // 2. EJECUTAR (CON CARGA)
            await EjecutarConCarga(async () =>
            {
                // Borrar de Cloudinary si hace falta
                bool esYoutube = !string.IsNullOrEmpty(SelectedCancion.UrlCancion) &&
                                 (SelectedCancion.UrlCancion.Contains("youtube.com") || SelectedCancion.UrlCancion.Contains("youtu.be"));

                if (!esYoutube && !string.IsNullOrEmpty(SelectedCancion.UrlCancion))
                {
                    if (SelectedCancion.UrlCancion.Contains("cloudinary"))
                    {
                        bool seEliminoNube = await _storageService.EliminarArchivo(SelectedCancion.UrlCancion);
                        if (!seEliminoNube)
                        {
                            _dialogoService.MostrarAlerta("Msg_Error_EliminarAudioNube"); 
                            return;
                        }
                    }
                }

                // Borrar de BD
                bool exito = await cliente.EliminarCancionPorId(SelectedCancion.Id);

                if (exito)
                {
                    // Restar contadores
                    if (SelectedCancion.AutoresIds != null)
                    {
                        foreach (var idAutor in SelectedCancion.AutoresIds)
                            await cliente.IncrementarContadorCancionesUsuario(idAutor, -1);
                    }
                    await CargarTodo();
                    _dialogoService.MostrarAlerta("Msg_Exito_CancionEliminada"); 
                }
                else _dialogoService.MostrarAlerta("Msg_Error_EliminarCancion"); 

            }, "Msg_Carga_EliminandoCancion"); 
        }
        /// <summary>
        /// Remueve una playlist de la base de datos tras confirmación del usuario.
        /// </summary>
        private async Task EliminarPlaylistTask()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;
            if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

            if (!await _dialogoService.Preguntar(
                "Msg_Confirmar_Titulo",
                "Msg_Confirmar_BorrarPlaylist",
                "Msg_Confirmar_BtnSi",
                "Msg_Confirmar_BtnNo")) return;

            await EjecutarConCarga(async () =>
            {
                bool exito = await cliente.EliminarPlaylistPorId(SelectedPlaylist.Id);
                if (exito)
                {
                    await CargarTodo();
                    _dialogoService.MostrarAlerta("Msg_Exito_PlaylistEliminada"); 
                }
                else _dialogoService.MostrarAlerta("Msg_Error_EliminarPlaylist"); 
            }, "Msg_Carga_BorrandoPlaylist"); 
        }
        /// <summary>
        /// Elimina un género musical del catálogo global de la aplicación.
        /// </summary>
        private async Task EliminarGeneroTask()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;
            if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

            if (!await _dialogoService.Preguntar(
                "Msg_Confirmar_Titulo",
                "Msg_Confirmar_BorrarGenero",
                "Msg_Confirmar_BtnSi",
                "Msg_Confirmar_BtnNo")) return;

            await EjecutarConCarga(async () =>
            {
                bool exito = await cliente.EliminarGenero(SelectedGenero);
                if (exito)
                {
                    await CargarTodo();
                    _dialogoService.MostrarAlerta("Msg_Exito_GeneroEliminado"); 
                }
                else _dialogoService.MostrarAlerta("Msg_Error_EliminarGenero"); 
            }, "Msg_Carga_EliminandoGenero");
        }
        /// <summary>
        /// Remueve el registro de un reporte del sistema.
        /// </summary>
        private async Task EliminarReporteTask()
        {
            var cliente = MongoClientSingleton.Instance.Cliente;
            if (cliente == null) { _dialogoService.MostrarAlerta("Msg_Error_Conexion"); return; }

            if (!await _dialogoService.Preguntar(
                "Msg_Confirmar_Titulo",
                "Msg_Confirmar_BorrarReporte",
                "Msg_Confirmar_BtnSi",
                "Msg_Confirmar_BtnNo")) return;

            await EjecutarConCarga(async () =>
            {
                bool exito = await cliente.EliminarReporte(SelectedReporte.Id);
                if (exito)
                {
                    await CargarTodo();
                    _dialogoService.MostrarAlerta("Msg_Exito_ReporteEliminado"); 
                }
                else _dialogoService.MostrarAlerta("Msg_Error_EliminarReporte");
            }, "Msg_Carga_EliminandoReporte"); 
        }
        /// <summary>
        /// Wrapper para ejecutar tareas asíncronas controlando los estados de carga y visualización de mensajes para el usuario.
        /// </summary>
        /// <param name="tarea">Función asíncrona a ejecutar.</param>
        /// <param name="mensaje">Mensaje de carga a mostrar en la interfaz.</param>
        private async Task EjecutarConCarga(Func<Task> tarea, string mensaje = "Procesando...")
        {
            if (EstaCargando) return; // Evitar doble clic

            try
            {
                EstaCargando = true;
                MensajeCarga = mensaje;
                await tarea();
            }
            catch (Exception ex)
            {
                _dialogoService.MostrarAlerta($"Error: {ex.Message}");
            }
            finally
            {
                EstaCargando = false;
            }
        }
        /// <summary>
        /// Analiza un archivo multimedia local para extraer su duración exacta en segundos utilizando la librería TagLib.
        /// </summary>
        /// <param name="rutaArchivo">Ruta física del archivo en el disco.</param>
        /// <returns>Segundos de duración (0 en caso de fallo).</returns>
        private int ObtenerDuracionLocal(string rutaArchivo)
        {
            try
            {
                if (System.IO.File.Exists(rutaArchivo))
                {
                    var archivo = TagLib.File.Create(rutaArchivo);
                    return (int)archivo.Properties.Duration.TotalSeconds;
                }
            }
            catch (Exception x)
            {
                System.Diagnostics.Debug.WriteLine("Error al obtener duración lo  cal: " + x.Message);
            }
            return 0;
        }
    }
}