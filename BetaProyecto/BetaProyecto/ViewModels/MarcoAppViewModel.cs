using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using LibVLCSharp.Shared;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class MarcoAppViewModel : ViewModelBase
    {
        //Sercivio de Audio
        private readonly AudioService _audioService;
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;

        // Variable para recordar cuál es el archivo temporal actual que está sonando
        private string _rutaTemporalActual = "";

        //Reloj
        private readonly DispatcherTimer _timer;

        //Servicio de dialogos para ventanas de aviso
        private readonly IDialogoService _dialogoService;

        //Cache de los viewmodel para no crearlos cada vez cambiamos de vista
        private LoginViewModel _loginVM;
        private CentralTabControlViewModel _centralTabVM;
        private PanelUsuarioViewModel _panelUsuarioVM;
        private ViewSobreNosotrosViewModel _sobreNosotrosVM;
        private ViewAyudaViewModel _ayudaVM;

        //Canción actual 
        private Canciones _cancionActual;
        private List<Canciones> _colaReproduccion; 
        private int _indiceCancionActual;

        //Propiedades para boton aleatorio
        private bool _btnaleatorioActivo = false;
        private Random _random = new Random();
        private List<Canciones> _historialAleatorio = new List<Canciones>();
        private int _indiceHistorialModoAleatorio = -1;

        //Propiedades para Binding del controlador de música, la informacióm de la canción nombre, artista, imagen)
        private string _nombreCancion;
        public string NombreCancionActual
        {
            get => _nombreCancion;
            set => this.RaiseAndSetIfChanged(ref _nombreCancion, value);
        }

        private string _nombreArtista = "";
        public string NombreArtistaActual
        {
            get => _nombreArtista;
            set => this.RaiseAndSetIfChanged(ref _nombreArtista, value);
        }

        private string _imagenCancion = "https://i.ibb.co/v6CJTMX2/Icono-Musica.jpg";
        public string ImagenCancionActual
        {
            get => _imagenCancion;
            set => this.RaiseAndSetIfChanged(ref _imagenCancion, value);
        }
        //Propiedades para Binding del controlador de música iconos botones (play/pause, next, back, aleatorio, favorito)
        private string _iconoPlayPause;
        public string IconoPlayPause
        {
            get => _iconoPlayPause;
            set => this.RaiseAndSetIfChanged(ref _iconoPlayPause, value);
        }

        private string _iconoNext;
        public string IconoNext
        {
            get => _iconoNext;
            set => this.RaiseAndSetIfChanged(ref _iconoNext, value);
        }

        private string _iconoBack;
        public string IconoBack
        {
            get => _iconoBack;
            set => this.RaiseAndSetIfChanged(ref _iconoBack, value);
        }

        private string _iconAleatorio;
        public string IconoAleatorio
        {
            get => _iconAleatorio;
            set => this.RaiseAndSetIfChanged(ref _iconAleatorio, value);
        }

        private string _iconLike;
        public string IconoLike
        {
            get => _iconLike;
            set => this.RaiseAndSetIfChanged(ref _iconLike, value);
        }

        //Propiedad para Binding del tiempo de la canción
        private string _tiempoActualCancion = "--:--";
        public string TiempoActualCancion
        {
            get => _tiempoActualCancion;
            set => this.RaiseAndSetIfChanged(ref _tiempoActualCancion, value);
        }

        private string _tiempoTotalCancion = "--:--";
        public string TiempoTotalCancion
        {
            get => _tiempoTotalCancion;
            set => this.RaiseAndSetIfChanged(ref _tiempoTotalCancion, value);
        }
        //Propiedad para Binding del slider vincualado a la canción

        private double _valorSliderCancion = 0;
        public double ValorSliderCancion
        {
            get => _valorSliderCancion;
            set => this.RaiseAndSetIfChanged(ref _valorSliderCancion, value);
        }

        //Propiedad para Binding del slider de volumen

        private double _valorSliderVolumen = 100;
        public double ValorSliderVolumen
        {
            get => _valorSliderVolumen;
            set
            {
                this.RaiseAndSetIfChanged(ref _valorSliderVolumen, value);
                //Actualiza volumen del MediaPlayer cada vez que cambia de valor
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Volume = (int)value;
                }
            }
        }

        //Navegación
        private ViewModelBase _vistaActual;
        public ViewModelBase VistaActual
        {
            get => _vistaActual;
            set => this.RaiseAndSetIfChanged(ref _vistaActual, value);
        }
        // Propidades para hacer POPUPS
        //Este contiene el ViewModel
        private ViewModelBase _popupActual;
        public ViewModelBase PopupActual
        {
            get => _popupActual;
            set
            {
                this.RaiseAndSetIfChanged(ref _popupActual, value);
                // Calculamos si el popup es visible automáticamente
                PopupVisible = value != null;
            }
        }
        //Este controla la visibilidad del popup
        private bool _popupVisible;
        public bool PopupVisible
        {
            get => _popupVisible;
            set => this.RaiseAndSetIfChanged(ref _popupVisible, value);
        }


        // Propiedad para mostrar u ocultar el controlador de musica
        private bool _barraVisible;
        public bool BarraVisible
        {
            get => _barraVisible;
            set => this.RaiseAndSetIfChanged(ref _barraVisible, value);
        }

        //Comandos ReactiveUI para los botones del controlador de música
        public ReactiveCommand<Unit, Unit> BtnPlayPauseCommand { get; }
        public ReactiveCommand<Unit, Unit> BtnFavCommand { get; }
        public ReactiveCommand<Unit, Unit> BtnNextCommand { get; }
        public ReactiveCommand<Unit, Unit> BtnBackCommand { get; }
        public ReactiveCommand<Unit, Unit> BtnAleatorioCommand { get; }

        //Constructor 
        public MarcoAppViewModel()
        {
            //Inicializamos Servicios
            _dialogoService = new DialogoService();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            _audioService = new AudioService();

            //Configuramos el timer (Se actualiza al segundo)
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            //Inicializamos iconos del reproductor con recursos del diccionario url del propio proyecto que van a /Assets/Imagenes/)
            IconoPlayPause = "Img_Play";
            IconoNext = "Img_Next_Disabled";
            IconoBack = "Img_Back_Disabled";
            IconoAleatorio = "Img_Aleatorio_Disabled";
            IconoLike = "Img_Like_OFF";

            //Configuramos vista inicial (Login)

            // Creamos el login
            _loginVM = new LoginViewModel(_dialogoService);

            //Actions

            //Action AlCompletarLogin: Cuando se pulse el botón de completar login, se ejecutará este código si el usuario es correcto
            _loginVM.AlCompletarLogin = () =>
            {
                IrAlCentralTabControl();
                BarraVisible = true;
            };
            //Action IrARegistarUser: Cuando se pulse el botón de registrar usuario
            _loginVM.IrARegistarUser = () =>
            {
                IrACrearUsuario();
            };

            // Inicializamos los comandos 

            //- Comando btnPlayPause 
            BtnPlayPauseCommand = ReactiveCommand.Create(AccionarPlayPause);
            //- Comando Favorito
            BtnFavCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_cancionActual != null)
                    await AlterarFavorito(_cancionActual);
            });
            //- Comando Next
            BtnNextCommand = ReactiveCommand.Create(NextCancion);
            //- Comando Back
            BtnBackCommand = ReactiveCommand.Create(BackCancion);
            //- Comando Aleatorio
            BtnAleatorioCommand = ReactiveCommand.Create(AlternarAleatorio);

            // Asignamos la vista inicial
            VistaActual = _loginVM;
            // Al principio estamos en Login, así que ocultamos la barra
            BarraVisible = false;
        }

        #region Método para cargar las vistas
        /// <summary>
        /// Muestra la vista de creación del usuario como una ventana emergente, lo que permite al usuario crear una nueva cuenta.
        /// </summary>
        /// <remarks>
        /// Este método reemplaza la ventana emergente actual con la vista de creación del usuario. Para cerrar el
        /// popup y volver al estado anterior, usar la acción de retroalimentación proporcionada dentro de la creación del usuario
        /// </remarks>
        public void IrACrearUsuario()
        {
            var viewCrearUsuarioVM = new ViewCrearUsuarioViewModel(
                accionVolver: () =>
                {
                    PopupActual = null;
                }
            );
            PopupActual = viewCrearUsuarioVM;
        }
        /// <summary>
        /// Navega a la vista de control de pestaña central, inicializándola si es necesario y configurándola como la vista actual.
        /// </summary>
        /// <remarks>Si la vista de control de pestaña central no se ha creado, este método la inicializa y
        /// configura sus acciones de navegación. Las llamadas posteriores reutilizarán la instancia de vista existente. Este método también
        /// actualiza los iconos de la interfaz como parte del proceso de navegación.</remarks>
        public void IrAlCentralTabControl()
        {
            if (_centralTabVM == null)
            {
                _centralTabVM = new CentralTabControlViewModel();

                // Configuramos la navegación interna del TabControl
                _centralTabVM.IrAPerfil = () => IrAPanelUsuario(0);
                _centralTabVM.IrACuenta = () => IrAPanelUsuario(1);
                _centralTabVM.IrAGestionarCuenta = () => IrAPanelUsuario(2);
                _centralTabVM.IrAConfig = () => IrAPanelUsuario(3);
                _centralTabVM.IrASobreNosotros = () => IrASobreNosotros();
                _centralTabVM.IrAAyuda = () => IrAAyuda();
                _centralTabVM.IrAPublicarCancion = () => IrAPublicarCancion();
                _centralTabVM.IrACrearPlaylist = () => IrACrearPlaylist();
                _centralTabVM.IrADetallesCancion = (cancion) => IrADetallesCancion(cancion);
                _centralTabVM.IrAVerArtista = (idUsuario) => IrAVerArtista(idUsuario);
                _centralTabVM.IrACrearReporte = (cancion)=> IrACrearReporte(cancion);
                _centralTabVM.IrADetallesPlaylist = (playlist) => IrADetallesPlaylist(playlist);
                // Configurar reproducción
                _centralTabVM.SolicitudCancion = (cancion, lista) =>
                {
                    ReproducirCancion(cancion, lista);
                };
            }
            RefrescarIconos();
            VistaActual = _centralTabVM;
        }
        /// <summary>
        /// Navega al panel de usuario y muestra la pestaña especificada.
        /// </summary>
        /// <remarks> Si el panel de usuario no se ha creado, este método lo inicializa y configura
        /// acciones relacionadas. Si el panel ya existe, actualiza la pestaña mostrada. La navegación oculta los principales
        /// barra mientras el panel de usuario está activo.</remarks>
        /// <param name="pestania">El índice de la pestaña que se mostrará en el panel de usuario. Debe ser un índice de pestañas válido y compatible con el panel.</param>
        public void IrAPanelUsuario(int pestania)
        {
            // Comprobamos si el panel de usuario ya existe o no
            if (_panelUsuarioVM == null)
            {
                _panelUsuarioVM = new PanelUsuarioViewModel(pestania);
            }
            else
            {
                // Si ya existe, solo actualizamos la pestaña que queremos ver
                _panelUsuarioVM.IndiceTab = pestania;
            }

            //Conexiones de los Actions desde el PanelUsuario (Editar canción, editar playlist, volver atrás, logout, salir, refrescar)
            _panelUsuarioVM.IrAEditarCancion = (cancion) => IrAEditarCancion(cancion);
            _panelUsuarioVM.IrAEditarPlaylist = (playlist) => IrAEditarPlaylist(playlist);
            _panelUsuarioVM.VolverAtras = () =>
            {
                VistaActual = _centralTabVM; // Volvemos a la vista anterior (TabControl)
                BarraVisible = true;
            };

            _panelUsuarioVM.AccionLogout = CerrarSesion;
            _panelUsuarioVM.AccionSalir = CerrarAplicacion;
            _panelUsuarioVM.AccionRefrescarDesdePadre = RefrescarIconos;


            // Al entrar ocultamos la barra
            BarraVisible = false;
            // Y mostramos el panel de usuario
            VistaActual = _panelUsuarioVM;
        }
        /// <summary>
        /// Navega a la vista 'Sobre Nosotros', creándola y configurándola si no existe.
        /// </summary>
        /// <remarks>Si no se ha creado la vista 'Sobre Nosotros', este método la inicializa y
        /// lo establece como la vista actual. Las llamadas posteriores reutilizarán la instancia de vista existente. La barra de música está oculta
        /// mientras esta vista está activa. </remarks>
        public void IrASobreNosotros()
        {
            // Si no existe, la creamos y configuramos
            if (_sobreNosotrosVM == null)
            {
                _sobreNosotrosVM = new ViewSobreNosotrosViewModel();
                ActivarVolverAtras(_sobreNosotrosVM); // Le pasamos la función de "Volver"
            }

            // Si ya existe, simplemente la mostramos
            VistaActual = _sobreNosotrosVM;
            BarraVisible = false; // Ocultamos la barra de música
        }
        /// <summary>
        /// Muestra la vista de ayuda en la aplicación, creándola y configurándola si no existe.
        /// </summary>
        /// <remarks>Cuando se invoca, este método cambia la vista actual a la vista de ayuda y oculta el
        /// barra de aplicaciones. Si la vista de ayuda no se ha creado previamente, se inicializa y configura antes
        /// se está mostrando. </remarks>
        public void IrAAyuda()
        {
            // Si no existe, la creamos y configuramos
            if (_ayudaVM == null)
            {
                _ayudaVM = new ViewAyudaViewModel();
                ActivarVolverAtras(_ayudaVM); // Le pasamos la función de "Volver"
            }

            // Si ya existe, simplemente la mostramos
            VistaActual = _ayudaVM;
            BarraVisible = false;
        }
        /// <summary>
        /// Navega a la vista para publicar una nueva canción y actualiza el estado actual de la vista en consecuencia.
        /// </summary>
        /// <remarks>Este método reemplaza la vista actual con la vista de publicación de canciones y oculta los
        /// barra de navegación. Cuando el usuario regresa desde la vista de publicación, se restaura la vista original del control de pestañas.
        /// y la barra de navegación vuelve a ser visible. </remarks>
        private void IrAPublicarCancion()
        {
            // Creamos una nueva instancia del ViewModel de publicar canción cada vez para asegurarnos de que se reinicia el formulario
            var publicarCancionVM = new ViewPublicarCancionViewModel(
                accionVolver: () =>
                {
                    // AL VOLVER: Restauramos el TabControl como vista actual
                    VistaActual = _centralTabVM;
                    BarraVisible = true; 
                }
            );
            // Cambiamos la vista visible
            BarraVisible = false;
            VistaActual = publicarCancionVM;

        }
        /// <summary>
        /// Navega a la vista para crear una nueva lista de reproducción personalizada y restablece el formulario de creación de la lista de reproducción.
        /// </summary>
        /// <remarks>Este método reemplaza la vista actual con la vista de creación de listas de reproducción y oculta los
        /// barra de navegación. Cuando el usuario regresa desde la vista de creación de listas de reproducción, la vista original y la barra de navegación
        /// se restauran. </remarks>
        private void IrACrearPlaylist()
        {
            // Creamos una nueva instancia del ViewModel de publicar canción cada vez para asegurarnos de que se reinicia el formulario
            var crearplaylistVM = new ViewCrearListaPersonalizadaViewModel(
                accionVolver: () =>
                {
                    // AL VOLVER: Restauramos el TabControl como vista actual
                    VistaActual = _centralTabVM;
                    BarraVisible = true;
                }
            );
            // Cambiamos la vista
            BarraVisible = false;
            VistaActual = crearplaylistVM;

        }
        /// <summary>
        /// Muestra la vista de detalles para la canción especificada y establece acciones relacionadas como reproducir, marcar como favorita y devolver.
        /// </summary>
        /// <param name="cancion">La canción para la que se muestran los detalles. No puede ser nula. </param>
        private void IrADetallesCancion(Canciones cancion)
        {
            var viewCancionesVM = new ViewCancionesViewModel(
                cancion: cancion,
                accionVolver: () =>
                {
                    PopupActual = null;
                },
                //Le pasamos las funciones de reproducción y favorito mediantes Actions para que se puedan usar en esa ventana
                accionReproducir: (cancion) => ReproducirCancion(cancion, null),
                accionLike: async (cancion) => await AlterarFavorito(cancion)
            );
            //Cambiamos la vista 
            PopupActual = viewCancionesVM;
        }
        /// <summary>
        /// Muestra los detalles del usuario especificado en una vista emergente.
        /// </summary>
        /// <param name="idUsuario">El identificador único del usuario cuyos detalles se deben mostrar. No puede ser nulo o vacío. </param>
        private void IrAVerArtista(string idUsuario)
        {
            var viewUsuarioVM = new ViewUsuariosViewModel(
                idUsuario: idUsuario,
                accionVolver: () =>
                {
                    PopupActual = null;
                }
            );
            //Cambiamos la vista 
            PopupActual = viewUsuarioVM;
        }
        /// <summary>
        /// Muestra la vista de creación del informe para la canción especificada.
        /// </summary>
        /// <param name="cancion">La canción para la que se mostrará la vista de creación del informe. No puede ser nula. </param>
        private void IrACrearReporte(Canciones cancion)
        {
            var viewCrearReporteVM = new ViewCrearReporteViewModel(
                cancion: cancion,
                accionVolver: () =>
                {
                    PopupActual = null;
                }
            );
            //Cambiamos la vista 
            PopupActual = viewCrearReporteVM;
        }
        private void IrADetallesPlaylist(ListaPersonalizada playlist)
        {
            var viewListaPersonalizadaVM = new ViewListaPersonalizadaViewModel(
                playlist: playlist,
                accionVolver: () =>
                {
                    PopupActual = null;
                }
            );
            //Cambiamos la vista 
            PopupActual = viewListaPersonalizadaVM;
        }
        /// <summary>
        /// Muestra la vista de edición de canciones para la canción especificada.
        /// </summary>
        /// <param name="cancion">La canción a editar. No puede ser nula. </param>
        private void IrAEditarCancion(Canciones cancion)
        {
            var viewEditarCancionVM = new ViewEditarCancionViewModel(
                cancion: cancion,
                accionVolver: () =>
                {
                    PopupActual = null;
                }
            );
            //Cambiamos la vista 
            PopupActual = viewEditarCancionVM;
        }
        /// <summary>
        /// Muestra la vista de edición de la lista de reproducción personalizada especificada, permitiendo al usuario modificar sus detalles.
        /// </summary>
        /// <param name="playlist">La lista de reproducción personalizada que se va a editar. No puede ser nula. </param>
        private void IrAEditarPlaylist(ListaPersonalizada playlist)
        {
            var viewEditarListaPersonalizadaVM = new ViewEditarListaPersonalizadaViewModel(
                playlist: playlist,
                accionVolver: () =>
                {
                    PopupActual = null;
                }
            );
            //Cambiamos la vista 
            PopupActual = viewEditarListaPersonalizadaVM;
        }


        // (Usamos una interfaz para ahorramos sobrecargar el metodo solo tendremos que añadir la interfaz)
        /// <summary>
        /// Asigna una llamada de retorno al modelo de vista especificado que permite la navegación de regreso a la vista principal.
        /// </summary>
        /// <remarks>Utilice este método para proporcionar un comportamiento de retorno estándar para los modelos de vista que admiten
        /// navegación. Después de invocar el callback asignado, la barra de navegación principal se vuelve visible. </remarks>
        /// <param name="vm">El modelo de vista que implementa la interfaz INavegable. El método establece su acción VolverAtras para navegar
        /// de vuelta al control central de pestañas. </param>
        public void ActivarVolverAtras(INavegable vm)
        {
            vm.VolverAtras = () =>
            {
                IrAlCentralTabControl();
                BarraVisible = true;
            };
        }
        #endregion

        #region Métodos para reproducción de música
        /// <summary>
        /// Inicia la reproducción de la canción especificada, opcionalmente usando una lista de reproducción proporcionada como cola de reproducción.
        /// </summary>
        /// <remarks>Si se proporciona una lista de reproducción, la reproducción comenzará con la canción especificada dentro de esa lista.
        /// lista. De lo contrario, la reproducción se limita a la canción individual proporcionada. La cola de reproducción y el índice de canciones actual
        /// se actualizan en consecuencia. </remarks>
        /// <param name="cancion">La canción a reproducir. No puede ser nula. </param>
        /// <param name="listaOrigen">Una lista opcional de canciones para usar como cola de reproducción. Si es nula o vacía, solo la canción especificada será
        /// jugado. </param>
        public void ReproducirCancion(Canciones cancion, List<Canciones>? listaOrigen = null)
        {
            //Gestion de la cola de reproducción
            if (listaOrigen != null && listaOrigen.Count > 0)//Si se proporciona una lista de reproducción válida, la usamos como cola
            {
                _colaReproduccion = listaOrigen;
                _indiceCancionActual = _colaReproduccion.IndexOf(cancion);
            }
            else// Si no se proporciona una lista, creamos una cola artificial con solo la canción actual
            {
                _colaReproduccion = new List<Canciones> {cancion};
                _indiceCancionActual = 0;
            }


            // Actualizamos los iconos
            ActualizarIconoAleatorio(cancion);
            ActualizarIconoNextBack();

            // Finalmente, cargamos y reproducimos la canción
            CargarYReproducir(cancion);
        }
        /// <summary>
        /// Carga la canción especificada y comienza la reproducción, actualizando la interfaz del reproductor y el estado relacionado en consecuencia.
        /// </summary>
        /// <remarks>Este método actualiza la información actual de la canción, los controles de reproducción y el usuario
        /// elementos de interfaz para reflejar la canción cargada. Determina la fuente de audio apropiada en función del
        /// URL de la canción, que admite enlaces a archivos en la nube tanto de YouTube como directos. Si la canción está marcada como favorita, el
        /// el icono like se actualiza. La reproducción se inicia de forma asíncrona y las métricas de reproducción de canciones se incrementan en el
        /// fondo. Si no se puede cargar el audio, se muestra una alerta al usuario. Este método es asincrónico.
        /// pero devuelve nulo; las excepciones se capturan y registran internamente. </remarks>
        /// <param name="cancion">La canción a cargar y reproducir. No debe ser nula y debe contener metadatos válidos y una URL reproducible.</param>
        public async void CargarYReproducir(Canciones cancion)
        {
            
            LimpiarArchivoTemporal();

            System.Diagnostics.Debug.WriteLine("3. [RECIBIDO] MarcoApp ha recibido la canción: " + cancion.Titulo);
            _cancionActual = cancion;
            try
            {
                //Mostrar la barra y actualizar textos (Binding)
                NombreCancionActual = cancion.Titulo;
                NombreArtistaActual = cancion.NombreArtista;
                ImagenCancionActual = cancion.ImagenPortadaUrl;
                //Ajustamos inferdaz de reproductor de música
                IconoPlayPause = "Img_Pause";
                ValorSliderCancion = 0;
                TiempoActualCancion = "--:--";
                TiempoTotalCancion= "--:--";

                var listaFavoritos = GlobalData.Instance.FavoritosGD;

                // Comprobamos si la lista existe y si contiene el ID de la canción
                if (listaFavoritos != null && listaFavoritos.Contains(cancion.Id))
                {
                    IconoLike = "Img_Like_ON"; 
                }
                else
                {
                    IconoLike = "Img_Like_OFF";
                }

                System.Diagnostics.Debug.WriteLine($"Buscando audio para: {cancion.Titulo}...");

                string urlStream = "";
                // CASO 1: Es un video de YouTube
                if (cancion.UrlCancion.Contains("youtube.com") || cancion.UrlCancion.Contains("youtu.be"))
                {
                    System.Diagnostics.Debug.WriteLine("Detectado enlace de YouTube. Solicitando stream a la API...");
                    
                    // Llamamos al nuevo método (que devuelve InfoCancionNube)
                    var infoAudio = await _audioService.ObtenerMp3(cancion.UrlCancion);

                    if (infoAudio != null && !string.IsNullOrEmpty(infoAudio.Url))
                    {
                        urlStream = infoAudio.Url; // Usamos la URL "traducida" temporal
                    }
                }
                // CASO 2: Es un archivo directo de nuestra nube [CLOUDNARY]
                else
                {
                    System.Diagnostics.Debug.WriteLine("Modo Archivo: Solicitando acceso seguro...");

                    // Llamamos al servicio. Él se encarga de:
                    // 1. Descargar si no existe.
                    // 2. Cifrar en AES para guardar en disco (Storage).
                    // 3. Descifrar a un temporal para que VLC lo lea ahora.

                    urlStream = await _audioService.ObtenerRutaAudioSegura(cancion.UrlCancion, cancion.Id);

                    // Guarda la ruta para luego borrarla
                    if (urlStream.Contains("BetaProyectoMusicTemp"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[RUTAS] Carpeta temporal: {urlStream}");
                        _rutaTemporalActual = urlStream;
                    }

                }

                if (!string.IsNullOrEmpty(urlStream))
                {
                    // Ahora que tenemos la URL/Ruta del .mp3 , la pasamos a VLC para reproducirla
                    var media = new Media(_libVLC, new Uri(urlStream));
                    _mediaPlayer.Play(media);
                    _timer.Start();
                    System.Diagnostics.Debug.WriteLine("Reproduciendo...");
                    // Lanzamos la tarea en segundo plano para no bloquear la música
                    _ = MongoClientSingleton.Instance.Cliente.IncrementarMetricaCancion(cancion.Id, "metricas.total_reproducciones", 1);
                    System.Diagnostics.Debug.WriteLine("Visualización añadida");

                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Error: La API no devolvió nada.");
                    _dialogoService.MostrarAlerta("No se pudo cargar el audio de esta canción.");
                    IconoPlayPause = "Img_Play";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al reproducir: " + ex.Message);
            }
        }
        /// <summary>
        /// Cambia la reproducción del reproductor multimedia entre los estados de reproducción y pausa.
        /// </summary>
        /// <remarks>Si el reproductor multimedia está reproduciéndose, este método detiene la reproducción y actualiza el
        /// reproducir/pausa el icono en consecuencia. Si el reproductor multimedia se detiene, este método inicia la reproducción y actualiza el icono,
        /// y inicia el temporizador asociado. Este método no genera excepciones y asume que el reproductor multimedia y
        /// los temporizadores están correctamente inicializados. </remarks>
        private void AccionarPlayPause()
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                IconoPlayPause = "Img_Play"; // Volvemos a mostrar Play
            }
            else
            {
                _mediaPlayer.Play();
                IconoPlayPause = "Img_Pause"; // Mostramos Pause
                _timer.Start();
            }
        }
        /// <summary>
        /// Cambia el modo de reproducción aleatoria para la lista de reproducción actual. Cuando está activado, el orden de reproducción se aleatoriza;
        /// cuando está desactivado, la reproducción se reanuda en el orden original de la canción actual.
        /// </summary>
        /// <remarks>Este método no tiene efecto si la lista de reproducción es nula o contiene uno o menos elementos.
        /// Cuando se activa el modo aleatorio, la canción actual se añade al historial de reproducción aleatoria para permitir su retorno
        /// a ella. Cuando se desactiva, la reproducción continúa desde la posición actual de la canción en la lista de reproducción original
        /// orden. </remarks>
        private void AlternarAleatorio()
        {
            // Comprobamos que la cola de reproducción es válida y tiene suficientes canciones para justificar el modo aleatorio
            if (_colaReproduccion == null || _colaReproduccion.Count <= 1)
            {
                return;
            } 
            //Intercambiamos el estado del botón
            _btnaleatorioActivo = !_btnaleatorioActivo;

            if (_btnaleatorioActivo)
            {
                // --- ACTIVAR ---
                System.Diagnostics.Debug.WriteLine("[ALEATORIO] ON");
                IconoAleatorio = "Img_Aleatorio_ON";

                // Iniciamos el historial con la canción actual para poder volver a ella
                _historialAleatorio.Clear();
                if (_cancionActual != null)
                {
                    _historialAleatorio.Add(_cancionActual);
                    _indiceHistorialModoAleatorio = 0;
                }
            }
            else
            {
                // --- DESACTIVAR ---
                System.Diagnostics.Debug.WriteLine("[ALEATORIO] OFF");
                IconoAleatorio = "Img_Aleatorio_OFF";

                // Al salir del modo aleatorio, buscamos la canción actual en la lista original
                // para seguir el orden normal desde ahí.
                if (_colaReproduccion != null && _cancionActual != null)
                {
                    _indiceCancionActual = _colaReproduccion.IndexOf(_cancionActual);
                }
            }
            ActualizarIconoNextBack();
        }
        /// <summary>
        /// Añade o elimina la canción especificada de la lista de favoritos del usuario, actualizando el estado favorito
        /// en consecuencia.
        /// </summary>
        /// <remarks>Si la canción ya está en la lista de favoritos, se elimina; de lo contrario, se añade.
        /// El método también actualiza el icono de like y ajusta la métrica de like count de la canción. No se realiza ninguna acción si el
        /// la canción proporcionada es nula. </remarks>
        /// <param name="cancion">La canción a añadir o quitar de la lista de favoritos. Si es nula, no se realiza la operación. </param>
        /// <returns>Devuelve una tarea que representa la operación asíncrona. </returns>
        private async Task AlterarFavorito(Canciones cancion)
        {
            // Si la canción es nula, no hacemos nada por seguridad
            if (cancion == null)
            {
                return;
            }
            // Obtenemos la lista de favoritos del usuario y el ID del usuario para realizar las operaciones necesarias
            var listaFavoritos = GlobalData.Instance.FavoritosGD;
            var idUsuario = GlobalData.Instance.UserIdGD;
            var idCancion = cancion.Id;
            // Comprobamos si la canción ya está en favoritos
            if (listaFavoritos.Contains(idCancion))
            {
                // Quitar de favoritos
                listaFavoritos.Remove(idCancion);
                if (_cancionActual != null && _cancionActual.Id == idCancion)
                    IconoLike = "Img_Like_OFF";

                await MongoClientSingleton.Instance.Cliente.EliminarDeFavorito(idUsuario, idCancion);
                System.Diagnostics.Debug.WriteLine($"Canción {idCancion} eliminada de favoritos del usuario {idUsuario}.");

                // 2. Restamos 1 al contador de la canción
                await MongoClientSingleton.Instance.Cliente.IncrementarMetricaCancion(idCancion, "metricas.total_megustas", -1);
                System.Diagnostics.Debug.WriteLine("Like quitado (-1)");
            }
            else// La canción no estaba en favoritos, la añadimos
            {
                // Añadir a favoritos
                listaFavoritos.Add(idCancion);
                if (_cancionActual != null && _cancionActual.Id == idCancion)
                    IconoLike = "Img_Like_ON";

                await MongoClientSingleton.Instance.Cliente.AgregarAFavorito(idUsuario, idCancion);
                System.Diagnostics.Debug.WriteLine($"Canción {idCancion} añadida a favoritos del usuario {idUsuario}.");

                // 2. Sumamos 1 al contador de la canción
                await MongoClientSingleton.Instance.Cliente.IncrementarMetricaCancion(idCancion, "metricas.total_megustas", 1);
                System.Diagnostics.Debug.WriteLine("Like añadido (+1)");
            }
        }
        /// <summary>
        /// Avanza a la reproducción de la siguiente canción en la lista de reproducción, manejando tanto el modo secuencial como el de mezcla.
        /// </summary>
        /// <remarks>En el modo de mezcla, este método selecciona aleatoriamente la siguiente canción entre las que aún no están disponibles.
        /// jugado, manteniendo un historial para evitar repeticiones hasta que se hayan reproducido todas las canciones. En el modo secuencial,
        /// avanza a la siguiente canción en orden si está disponible. Si todas las canciones se han reproducido en modo de mezcla, el historial
        /// se restablece excepto para la canción actual, y la reproducción continúa. Este método no tiene efecto si la lista de reproducción es
        /// vacío. </remarks>
        private void NextCancion()
        {
            // Comprobamos que la cola de reproducción es válida y tiene canciones
            if (_colaReproduccion != null && _colaReproduccion.Count > 0)
            {
                if (_btnaleatorioActivo) // --- MODO ALEATORIO ---
                {
                    // Comprobamos si el usuario a usado el Back antes de avanzar, si es así, avanzamos por el historial
                    if (_indiceHistorialModoAleatorio < _historialAleatorio.Count - 1)
                    {
                        _indiceHistorialModoAleatorio++;
                        var cancionHistorial = _historialAleatorio[_indiceHistorialModoAleatorio];
                        ActualizarIconoNextBack();
                        CargarYReproducir(cancionHistorial);
                    }
                    else // Si el usuario no ha usado el Back o ya está al final del historial, generamos elegimos una canción aleatoria
                    {
                        // Buscamos qué canciones de la lista original NO están en el historial todavía
                        // Usamos LINQ: "Dame las canciones de la cola EXCEPTO las del historial"
                        var cancionesPendientes = _colaReproduccion.Except(_historialAleatorio).ToList();

                        // Comprobamos si quedan canciones por sonar
                        if (cancionesPendientes.Count > 0)
                        {
                            // Elegimos una al azar de las que FALTAN
                            int indiceRandom = _random.Next(0, cancionesPendientes.Count);
                            var nuevaCancion = cancionesPendientes[indiceRandom];

                            // Guardamos en historial y avanzamos
                            _historialAleatorio.Add(nuevaCancion);
                            _indiceHistorialModoAleatorio++;
                            ActualizarIconoNextBack();
                            CargarYReproducir(nuevaCancion);
                        }
                        else // Si no quedan canciones por sonar, significa que el usuario ya ha escuchado toda la lista en modo aleatorio
                        {
                            // Reiniciamos el historial (salvo la canción actual)
                            var cancionActualTemp = _cancionActual;
                            _historialAleatorio.Clear();
                            _historialAleatorio.Add(cancionActualTemp);
                            _indiceHistorialModoAleatorio = 0;

                            // Volvemos a llamar a esta función para que ahora sí encuentre pendientes
                            NextCancion();
                        }
                    }
                }
                else // --- MODO NORMAL ---
                {
                    // Solo avanzamos si no es la última
                    if (_indiceCancionActual < _colaReproduccion.Count - 1)
                    {
                        _indiceCancionActual++;
                        ActualizarIconoNextBack();
                        CargarYReproducir(_colaReproduccion[_indiceCancionActual]);
                    }
                }
            }
        }
        /// <summary>
        /// Mueve la reproducción a la pista anterior en la lista de reproducción o al historial de reproducción, dependiendo de la reproducción actual
        /// modo.
        /// </summary>
        /// <remarks>En el modo de mezcla, este método navega hacia atrás a través del historial de reproducción si
        /// posible. En el modo normal, se mueve a la pista anterior en la lista de reproducción a menos que ya esté en la primera pista.
        /// No se toma ninguna medida si no hay pistas en la lista de reproducción o si ya está al principio del historial o
        /// lista de reproducción. </remarks>
        private void BackCancion()
        {
            if (_colaReproduccion != null && _colaReproduccion.Count > 0)
            {
                // --- MODO ALEATORIO ---
                if (_btnaleatorioActivo)
                {
                    // Solo retrocedemos si el puntero no está en el principio del historial
                    if (_indiceHistorialModoAleatorio > 0)
                    {
                        _indiceHistorialModoAleatorio--; 
                        var cancionAnterior = _historialAleatorio[_indiceHistorialModoAleatorio];
                        ActualizarIconoNextBack();
                        CargarYReproducir(cancionAnterior);
                    }
                }
                else // --- MODO NORMAL ---
                {
                    // Solo retrocedemos si no es la primera
                    if (_indiceCancionActual > 0)
                    {
                        _indiceCancionActual--;
                        ActualizarIconoNextBack();
                        CargarYReproducir(_colaReproduccion[_indiceCancionActual]);
                    }
                }
            }
        }
        /// <summary>
        /// Actualiza los iconos de los botones de navegación Siguiente y Atrás en función del modo y la posición de reproducción actuales
        /// en la lista de reproducción.
        /// </summary>
        /// <remarks>Este método habilita o deshabilita los iconos de los botones Siguiente y Atrás, dependiendo de si
        /// la lista de reproducción está en modo de mezcla y la posición actual de la canción. En el modo de mezcla, el botón Siguiente permanece
        /// activado, mientras que el botón Atrás solo está activado si hay un historial de reproducción. En modo normal, los botones están
        /// activado o desactivado según si la canción actual es la primera o la última en la lista de reproducción. </remarks>
        private void ActualizarIconoNextBack()
        {
            // CASO 1: No hay lista o la lista está vacía o solo tiene 1 canción
            if (_colaReproduccion == null || _colaReproduccion.Count <= 1)
            {
                IconoBack = "Img_Back_Disabled";
                IconoNext = "Img_Next_Disabled";
                return; // Salimos, no hay nada más que calcular
            }

            if(_btnaleatorioActivo)// MODO ALEATORIO
            {
                // En Aleatorio, NEXT siempre está activo (es infinito)
                IconoNext = "Img_Next";

                // BACK depende del historial
                if (_indiceHistorialModoAleatorio > 0)
                    IconoBack = "Img_Back";
                else
                    IconoBack = "Img_Back_Disabled";
            }
            else // MODO NORMAL
            {
                // CASO 2: Botón ATRÁS (Back)
                // Si el índice es 0 (primera canción), lo desactivamos. Si no, lo activamos.
                if (_indiceCancionActual == 0)
                {
                    IconoBack = "Img_Back_Disabled";
                }
                else
                {
                    IconoBack = "Img_Back";
                }
                // CASO 3: Botón SIGUIENTE (Next)
                // Si el índice es el último (Total - 1), lo desactivamos. Si no, lo activamos.
                if (_indiceCancionActual == _colaReproduccion.Count - 1)
                {
                    IconoNext = "Img_Next_Disabled";
                }
                else
                {
                    IconoNext = "Img_Next";
                }
            }
        }
        /// <summary>
        /// Actualiza el icono de reproducción aleatoria según la cola de reproducción actual y el estado del modo aleatorio.
        /// </summary>
        /// <remarks>Si la cola de reproducción contiene una o ninguna canción, el icono aleatorio está desactivado. Cuando el
        /// el modo aleatorio está activo y la cola cambia, el historial de reproducción aleatoria se restablece para comenzar desde el
        /// canción especificada. </remarks>
        /// <param name="cancionInicio">La canción que se usará como punto de partida en el historial de reproducción aleatoria cuando el modo aleatorio esté activo. </param>
        private void ActualizarIconoAleatorio(Canciones cancionInicio)
        {
            // CASO A: Lista insuficiente (1 o 0 canciones) -> SE DESHABILITA
            if (_colaReproduccion == null || _colaReproduccion.Count <= 1)
            {
                IconoAleatorio = "Img_Aleatorio_Disabled";
            }
            // CASO B: Lista válida (> 1 canción) -> SE HABILITA
            else
            {
                // Respetamos si el usuario ya lo tenía activado
                if (_btnaleatorioActivo)
                {
                    IconoAleatorio = "Img_Aleatorio_ON";

                    // IMPORTANTE: Como ha cambiado la lista, reiniciamos el historial
                    // para empezar de cero con la nueva playlist.
                    _historialAleatorio.Clear();
                    _historialAleatorio.Add(cancionInicio);
                    _indiceHistorialModoAleatorio = 0;

                    System.Diagnostics.Debug.WriteLine("[ALEATORIO] Lista cambiada. Historial reiniciado.");
                }
                else
                {
                    IconoAleatorio = "Img_Aleatorio_OFF";
                }
            }
        }
        /// <summary>
        /// Maneja las marcas de eventos del temporizador para actualizar el progreso de reproducción, la hora actual y la duración total del medio
        /// reproductor, o para avanzar a la siguiente pista cuando termina la reproducción.
        /// </summary>
        /// <remarks>Este método está destinado a ser utilizado como un manejador de eventos para un temporizador periódico.
        /// asociado con la reproducción de medios. Actualiza los elementos de la interfaz de usuario, como el deslizador de progreso y las pantallas de tiempo.
        /// respuesta al estado actual del reproductor multimedia. Si la reproducción ha terminado, avanza automáticamente a la
        /// siguiente pista o restablece la interfaz de usuario según sea apropiado. </remarks>
        /// <param name="sender">La fuente del evento, normalmente el temporizador que activó la marca. </param>
        /// <param name="e">Un objeto que contiene los datos del evento. </param>
        private void Timer_Tick(object? sender, EventArgs e)
        {   
            // Solo actualizamos si VLC está reproduciendo y si ya sabe cuánto dura la canción
            if (_mediaPlayer.IsPlaying && _mediaPlayer.Length > 0)
            {
                // Actualizar slider de progreso de la canción

                // _mediaPlayer.Position va de 0.0 a 1.0 (es un porcentaje)
                // Lo multiplicamos por 100 para que coincida con nuestro Slider (Maximum=100)
                // Usamos Math.Min y Max para evitar errores raros de desbordamiento
                double nuevoValor = _mediaPlayer.Position * 100;
                ValorSliderCancion = Math.Clamp(nuevoValor, 0, 100);

                // Actualizar tiempos de la canción

                // Tiempo actual
                var tiempoActual = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
                TiempoActualCancion = tiempoActual.ToString(@"mm\:ss");
                
                // Tiempo total (Duración)
                // Solo lo calculamos si VLC ya ha descargado los metadatos de la duración
                var tiempoTotal = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
                TiempoTotalCancion = tiempoTotal.ToString(@"mm\:ss");
            }
            else
            {
                // Si la canción ha terminado (llegó al final)
                if (_mediaPlayer.State == VLCState.Ended)
                {
                    // Paramos el timer un momento para que no se repita el evento
                    _timer.Stop();

                    // Llamamos a la siguente canción automáticamente
                    NextCancion();
                }
                // Si la canción se detuvo o error
                else if (_mediaPlayer.State == VLCState.Stopped || _mediaPlayer.State == VLCState.Error)
                {
                    _timer.Stop();
                    IconoPlayPause = "Img_Play";
                    ValorSliderCancion = 0;
                    TiempoActualCancion = "00:00";
                }
            }
        }
        #endregion

        #region Métodos helpers

        /// <summary>
        /// Actualiza los iconos y las propiedades relacionadas para reflejar el tema de la aplicación actual.
        /// </summary>
        /// <remarks>Llame a este método después de que cambie el tema de la aplicación para asegurarse de que todos los iconos y
        /// las propiedades del deslizador se actualizan y notifican cualquier vinculación de datos de los cambios. </remarks>
        private void RefrescarIconos()
        {
            //Forzamos la actualización de los iconos para que se recarguen con el nuevo tema (Binding)
            this.RaisePropertyChanged(nameof(IconoPlayPause));
            this.RaisePropertyChanged(nameof(IconoNext));
            this.RaisePropertyChanged(nameof(IconoBack));
            this.RaisePropertyChanged(nameof(IconoAleatorio));
            this.RaisePropertyChanged(nameof(IconoLike));
            this.RaisePropertyChanged(nameof(ValorSliderCancion));
        }

        /// <summary>
        /// Cierra la sesión actual del usuario y restablece el estado de la aplicación a la vista de inicio de sesión.
        /// </summary>
        /// <remarks>Este método detiene cualquier reproducción de medios activa, borra datos específicos del usuario y restablece la interfaz de usuario.
        /// elementos y elimina la información de usuario almacenada en caché para mayor seguridad. Después de la ejecución, la aplicación regresa al
        /// pantalla de inicio de sesión, asegurándose de que ningún dato de sesiones anteriores permanezca accesible. Este método debe ser llamado cuando un
        /// el usuario se desconecta o cuando una sesión debe finalizar de forma segura. </remarks>
        private void CerrarSesion()
        {
            // Parar música si esta sonando
            if (_mediaPlayer.IsPlaying) _mediaPlayer.Stop();

            //Dejamos el reproductor 
            _timer.Stop();
            IconoPlayPause = "Img_Play";
            ValorSliderCancion = 0;
            TiempoActualCancion = "--:--";
            TiempoTotalCancion = "--:--";
            NombreCancionActual = "";
            NombreArtistaActual = "";
            ImagenCancionActual = "https://i.ibb.co/v6CJTMX2/Icono-Musica.jpg";

            // Limpiamos los datos globales
            GlobalData.Instance.ClearUserData();
            BarraVisible = false;

            // Limpiamos todas las vistas que puedan contener datos del usuario  
            _centralTabVM = null;
            _panelUsuarioVM = null;

            // Limpiamos los campos del LoginVM existente para que no salgan rellenos
            _loginVM.TxtUsuario = "";
            _loginVM.TxtPass = "";

            // Volvemos a la vista Login original
            VistaActual = _loginVM;
        }
        /// <summary>
        /// Realiza un cierre limpio de la aplicación, finalizando los procesos relacionados, liberando recursos, y
        /// cerrando la ventana de la aplicación.
        /// </summary>
        /// <remarks>Este método termina por la fuerza cualquier instancia en ejecución de 'BetaProyecto.API'
        /// proceso, descarte de recursos multimedia, detiene temporizadores internos y cierra la aplicación. Si el
        /// la aplicación se ejecuta con una vida útil de escritorio clásica, utiliza el mecanismo de apagado apropiado;
        /// de lo contrario, abandona el proceso. Utilice este método para asegurarse de que todos los recursos se liberan y la aplicación
        /// salidas limpias. </remarks>
        private void CerrarAplicacion()
        {
            try
            {
                // Buscamos cualquier proceso que se llame como tu API
                var procesosApi = Process.GetProcessesByName("BetaProyecto.API");
                foreach (var proc in procesosApi)
                {
                    System.Diagnostics.Debug.WriteLine($"Cerrando API: {proc.ProcessName}");
                    proc.Kill(); // Forzamos el cierre
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al cerrar la API: " + ex.Message);
            }

            // Limpiamos recursos multimedia
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
            }
            LimpiarArchivoTemporal();
            if (_libVLC != null) _libVLC.Dispose();

            _timer.Stop();

            // Cerramos la aplicación
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
            else
            {
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Elimina el archivo temporal actual si existe y libera cualquier recurso multimedia asociado.
        /// </summary>
        /// <remarks>Si el archivo temporal está en uso o no se puede eliminar, el método suprime
        /// excepciones y registra un mensaje de depuración. Este método está destinado a ser llamado cuando los archivos multimedia temporales no están
        /// se necesita más tiempo para liberar espacio en disco y liberar atajos de archivos. </remarks>
        private void LimpiarArchivoTemporal()
        {
            try
            {
                // Si tenemos una ruta guardada y el archivo existe...
                if (!string.IsNullOrEmpty(_rutaTemporalActual) && System.IO.File.Exists(_rutaTemporalActual))
                {
                    if (_mediaPlayer.Media != null)
                    {
                        _mediaPlayer.Media.Dispose(); // Destruye el enlace al archivo
                        _mediaPlayer.Media = null;    // Limpia la propiedad del reproductor
                    }

                    // Borramos el archivo temporal
                    System.IO.File.Delete(_rutaTemporalActual);
                    System.Diagnostics.Debug.WriteLine($"[SEGURIDAD] Archivo temporal eliminado: {_rutaTemporalActual}");
                    _rutaTemporalActual = "";
                }
            }
            catch (Exception ex)
            {
                // Si falla (por ejemplo, si VLC todavía lo tiene bloqueado), no pasa nada, 
                // Windows limpia los temporales eventualmente.
                System.Diagnostics.Debug.WriteLine($"[AVISO] No se pudo borrar el temporal: {ex.Message}");
            }
        }
        #endregion

    }
}