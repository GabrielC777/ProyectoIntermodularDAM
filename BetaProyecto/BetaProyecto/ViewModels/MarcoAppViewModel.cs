using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using BetaProyecto.Views.Editar;
using BetaProyecto.Views.Visores;
using LibVLCSharp.Shared;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
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

        //Propiedades para Binding del controlador de música información canción
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
        //Propiedades para Binding del controlador de música iconos botones
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

        // --- CONSTRUCTOR (Al arrancar) ---
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

            ///Inicializamos iconos del reproductor con recursos del diccionario(url del propio proyecto que van a /Assets/Imagenes/)
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
        public void IrAPanelUsuario(int pestania)
        {
            // REUTILIZACIÓN DE PANEL (Punto 3)
            if (_panelUsuarioVM == null)
            {
                _panelUsuarioVM = new PanelUsuarioViewModel(pestania);
            }
            else
            {
                // Si ya existe, solo actualizamos la pestaña que queremos ver
                _panelUsuarioVM.IndiceTab = pestania;
            }


            //Conexiones finales del los Actions
            _panelUsuarioVM.IrAEditarCancion = (cancion) => IrAEditarCancion(cancion);
            _panelUsuarioVM.IrAEditarPlaylist = (playlist) => IrAEditarPlaylist(playlist);
            _panelUsuarioVM.VolverAtras = () =>
            {
                VistaActual = _centralTabVM; // Volvemos a la caché central
                BarraVisible = true;
            };

            _panelUsuarioVM.AccionLogout = CerrarSesion;
            _panelUsuarioVM.AccionSalir = CerrarAplicacion;
            _panelUsuarioVM.AccionRefrescarDesdePadre = RefrescarIconos;


            // Al entrar en Configuración/Perfil ocultamos la barra
            BarraVisible = false;
            VistaActual = _panelUsuarioVM;
        }
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
        private void IrAPublicarCancion()
        {
            //Como vamos a generar una instancia nueva por cada publicacion no usamos el metodo ActivarVolverAtras
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
        private void IrACrearPlaylist()
        {
            //Como vamos a generar una instancia nueva por cada publicacion no usamos el metodo ActivarVolverAtras
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
        private void IrADetallesCancion(Canciones cancion)
        {
            var viewCancionesVM = new ViewCancionesViewModel(
                cancion: cancion,
                accionVolver: () =>
                {
                    PopupActual = null;
                },
                // AÑADIR ESTA LÍNEA: Pasamos la función de reproducir
                accionReproducir: (cancion) => ReproducirCancion(cancion, null),
                accionLike: async (cancion) => await AlterarFavorito(cancion)
            );
            //Cambiamos la vista 
            PopupActual = viewCancionesVM;
        }
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
        public void ActivarVolverAtras(INavegable vm)
        {
            vm.VolverAtras = () =>
            {
                IrAlCentralTabControl();
                BarraVisible = true;
            };
        }
        #endregion


        // Metodos para cerrar sesión del usuario
        private void CerrarSesion()
        {
            // 1. Parar música
            if (_mediaPlayer.IsPlaying) _mediaPlayer.Stop();
            
            _timer.Stop();
            IconoPlayPause= "Img_Play";
            ValorSliderCancion = 0;
            TiempoActualCancion = "--:--";
            TiempoTotalCancion = "--:--";
            NombreCancionActual = "";
            NombreArtistaActual = "";
            ImagenCancionActual = "https://i.ibb.co/v6CJTMX2/Icono-Musica.jpg";

            // 2. Limpiar Datos Globales
            GlobalData.Instance.ClearUserData();
            BarraVisible = false;

            // 3. LIMPIAR CACHÉ DE USUARIO (Seguridad)
            // Destruimos las vistas que contienen datos del usuario anterior
            _centralTabVM = null;
            _panelUsuarioVM = null;
            _sobreNosotrosVM = null;
            _ayudaVM = null;         
            // (Ayuda y SobreNosotros no hace falta borrarlos si son estáticos)

            // 4. RESETEAR LOGIN
            // Limpiamos los campos del LoginVM existente para que no salgan rellenos
            _loginVM.TxtUsuario = "";
            _loginVM.TxtPass = "";

            // Volvemos a la vista Login original (que tiene el _dialogoService bien puesto)
            VistaActual = _loginVM;
        }
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

            // 2. LIMPIAR RECURSOS
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
            }
            LimpiarArchivoTemporal();
            if (_libVLC != null) _libVLC.Dispose();

            _timer.Stop();

            // 3. CERRAR APP AVALONIA
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
            else
            {
                Environment.Exit(0);
            }
        }
        // Método que se activa cuando se cambia el tema de la aplicación
        // Para refrescar los iconos
        private void RefrescarIconos()
        {
            this.RaisePropertyChanged(nameof(IconoPlayPause));
            this.RaisePropertyChanged(nameof(IconoNext));
            this.RaisePropertyChanged(nameof(IconoBack));
            this.RaisePropertyChanged(nameof(IconoAleatorio));
            this.RaisePropertyChanged(nameof(IconoLike));
            this.RaisePropertyChanged(nameof(ValorSliderCancion));
        }
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

                    // ¡Lo borramos! 🗑️
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


        #region Métodos para reproducción de música
        public void ReproducirCancion(Canciones cancion, List<Canciones>? listaOrigen = null)
        {
            // 1. GESTIÓN DE LA LISTA
            if (listaOrigen != null && listaOrigen.Count > 0)
            {
                _colaReproduccion = listaOrigen;
                _indiceCancionActual = _colaReproduccion.IndexOf(cancion);
            }
            else
            {
                // Lista artificial de 1 canción
                _colaReproduccion = new List<Canciones> { cancion };
                _indiceCancionActual = 0;
            }


            // ACTUALIZAR LOS ICONOS
            ActualizarIconoAleatorio(cancion);
            ActualizarIconoNextBack();

            // 3. REPRODUCIR
            CargarYReproducir(cancion);
        }
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

                // Pedir la URL a la API 
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
                // CASO 2: Es un archivo directo de tu Nube (Cloud)
                else
                {
                    System.Diagnostics.Debug.WriteLine("Modo Archivo: Solicitando acceso seguro...");

                    // Llamamos al servicio. Él se encarga de:
                    // 1. Descargar si no existe.
                    // 2. Cifrar en AES para guardar en disco (Storage).
                    // 3. Descifrar a un temporal para que VLC lo lea ahora.

                    urlStream = await _audioService.ObtenerRutaAudioSegura(cancion.UrlCancion, cancion.Id);
                    // GUARDA LA RUTA para borrarla luego
                    if (urlStream.Contains("BetaProyectoMusicTemp"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[RUTAS] Carpeta temporal: {urlStream}");
                        _rutaTemporalActual = urlStream;
                    }

                }

                if (!string.IsNullOrEmpty(urlStream))
                {
                    // 3. Reproducir en VLC
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
        private void AlternarAleatorio()
        {
            if (_colaReproduccion == null || _colaReproduccion.Count <= 1)
            {
                return;
            } 
            //Intercambiamos el estado del botón (Interruptor)
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
        private async Task AlterarFavorito(Canciones cancion)
        {
            if (cancion == null)
            {
                return;
            }
            var listaFavoritos = GlobalData.Instance.FavoritosGD;
            var idUsuario = GlobalData.Instance.UserIdGD;
            var idCancion = cancion.Id;
            
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
            else
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
        private void NextCancion()
        {
            if (_colaReproduccion != null && _colaReproduccion.Count > 0)
            {
                if (_btnaleatorioActivo) // --- MODO ALEATORIO ---
                {
                    // A. ¿Estamos navegando hacia adelante en el historial? (Habíamos dado Back antes)
                    if (_indiceHistorialModoAleatorio < _historialAleatorio.Count - 1)
                    {
                        _indiceHistorialModoAleatorio++;
                        var cancionHistorial = _historialAleatorio[_indiceHistorialModoAleatorio];
                        ActualizarIconoNextBack();
                        CargarYReproducir(cancionHistorial);
                    }
                    else
                    {
                        // B. Generar NUEVA canción (Que NO haya sonado ya)

                        // 1. Buscamos qué canciones de la lista original NO están en el historial todavía
                        // Usamos LINQ: "Dame las canciones de la cola EXCEPTO las del historial"
                        var cancionesPendientes = _colaReproduccion.Except(_historialAleatorio).ToList();

                        // 2. Comprobamos si quedan canciones por sonar
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
                        else
                        {
                            // ¡Se han reproducido todas las canciones!
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

                        // 1. Cambiamos los botones (Ahora Back se activará si estaba apagado)
                        ActualizarIconoNextBack();

                        // 2. Reproducimos
                        CargarYReproducir(_colaReproduccion[_indiceCancionActual]);
                    }
                }
            }
        }

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

                        // 1. Cambiamos los botones (Ahora Next se activará si estaba apagado)
                        ActualizarIconoNextBack();

                        // 2. Reproducimos
                        CargarYReproducir(_colaReproduccion[_indiceCancionActual]);
                    }
                }
            }
        }
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
        private void Timer_Tick(object? sender, EventArgs e)
        {   
            // Solo actualizamos si VLC está reproduciendo Y si ya sabe cuánto dura la canción
            if (_mediaPlayer.IsPlaying && _mediaPlayer.Length > 0)
            {
                // ACTUALIZAR SLIDER

                // _mediaPlayer.Position va de 0.0 a 1.0 (es un porcentaje)
                // Lo multiplicamos por 100 para que coincida con tu Slider (Maximum=100)
                // Usamos Math.Min y Max para evitar errores raros de desbordamiento
                double nuevoValor = _mediaPlayer.Position * 100;
                ValorSliderCancion = Math.Clamp(nuevoValor, 0, 100);

                // ACTUALIZAR TIEMPOS
                
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
                // Si la canción ha terminado SOLA (llegó al final)
                if (_mediaPlayer.State == VLCState.Ended)
                {
                    // 1. Paramos el timer un momento para que no se repita el evento
                    _timer.Stop();

                    // 2. ¡Llamamos a SIGUIENTE automáticamente!
                    // Como NextCancion ya tiene la lógica de Aleatorio/Normal, funcionará perfecto.
                    NextCancion();
                }
                // Si la canción se detuvo manualmente (Stop) o error
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

    }
}