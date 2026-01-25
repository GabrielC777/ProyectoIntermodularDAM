using Avalonia.Media.Imaging;
using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewPublicarCancionViewModel : ViewModelBase
    {
        // Servicios
        private readonly IDialogoService _dialogoService;
        private readonly StorageService _storageService;
        private readonly AudioService _audioService;

        // Action 
        private readonly Action _Volver;

        // --- 1. DATOS DE LA CANCIÓN ---
        private string _txtTitulo;
        public string TxtTitulo
        {
            get => _txtTitulo;
            set => this.RaiseAndSetIfChanged(ref _txtTitulo, value);
        }

        // Listas de Géneros
        private ObservableCollection<string> _listaGeneros;
        public ObservableCollection<string> ListaGeneros
        {
            get => _listaGeneros;
            set => this.RaiseAndSetIfChanged(ref _listaGeneros, value);
        }

        private string _generoSeleccionado;
        public string GeneroSeleccionado
        {
            get => _generoSeleccionado;
            set => this.RaiseAndSetIfChanged(ref _generoSeleccionado, value);
        }

        // Lista de etiquetas seleccionadas
        private ObservableCollection<string> _listaGenerosSeleccionados;
        public ObservableCollection<string> ListaGenerosSeleccionados
        {
            get => _listaGenerosSeleccionados;
            set => this.RaiseAndSetIfChanged(ref _listaGenerosSeleccionados, value);
        }

        // --- 2. GESTIÓN DE COLABORADORES ---
        private string _txtBusqueda;
        public string TxtBusqueda
        {
            get => _txtBusqueda;
            set => this.RaiseAndSetIfChanged(ref _txtBusqueda, value);
        }

        private ObservableCollection<Usuarios> _listaResultados;
        public ObservableCollection<Usuarios> ListaResultados
        {
            get => _listaResultados;
            set => this.RaiseAndSetIfChanged(ref _listaResultados, value);
        }

        private ObservableCollection<Usuarios> _listaArtitas;
        public ObservableCollection<Usuarios> ListaArtistas
        {
            get => _listaArtitas;
            set => this.RaiseAndSetIfChanged(ref _listaArtitas, value);
        }

        // --- 3. IMAGEN Y AUDIO ---
        private string _rutaImagen;
        public string RutaImagen
        {
            get => _rutaImagen;
            set
            {
                this.RaiseAndSetIfChanged(ref _rutaImagen, value);
                CargarImagenLocal(value);
            }
        }
        public bool TieneImagen => !string.IsNullOrEmpty(RutaImagen);

        private Bitmap? _imagenPortada;
        public Bitmap? ImagenPortada
        {
            get => _imagenPortada;
            set => this.RaiseAndSetIfChanged(ref _imagenPortada, value);
        }

        // Binding Declarativo
        private bool _esYoutube = true;
        public bool EsYoutube
        {
            get => _esYoutube;
            set
            {
                this.RaiseAndSetIfChanged(ref _esYoutube, value);
                this.RaisePropertyChanged(nameof(EsArchivo));
            }
        }
        public bool EsArchivo => !EsYoutube;

        private string _linkYoutube;
        public string LinkYoutube
        {
            get => _linkYoutube;
            set => this.RaiseAndSetIfChanged(ref _linkYoutube, value);
        }

        private string _rutaMp3;
        public string RutaMp3
        {
            get => _rutaMp3;
            set
            {
                this.RaiseAndSetIfChanged(ref _rutaMp3, value);
                if (!string.IsNullOrEmpty(value) && EsArchivo)
                {
                    _duracionCalculada = ObtenerDuracionMp3(value);
                }
            }
        }
        private int _duracionCalculada = 0;

        // --- 4. ESTADO ---
        private bool _estaCargando;
        public bool EstaCargando
        {
            get => _estaCargando;
            set => this.RaiseAndSetIfChanged(ref _estaCargando, value);
        }

        // --- COMANDOS ---
        public ReactiveCommand<Unit, Unit> BtnVolverAtras { get; }
        public ReactiveCommand<Unit, Unit> BtnPublicar { get; }
        public ReactiveCommand<Unit, Unit> BtnBuscarUsuarios { get; }
        public ReactiveCommand<Usuarios, Unit> BtnAgregarUsuario { get; }
        public ReactiveCommand<Usuarios, Unit> BtnEliminarUsuario { get; }
        public ReactiveCommand<Unit, Unit> BtnAgregarGenero { get; }
        public ReactiveCommand<string, Unit> BtnEliminarGenero { get; }

        // --- CONSTRUCTOR ---
        public ViewPublicarCancionViewModel(Action accionVolver)
        {
            _Volver = accionVolver;

            _dialogoService = new DialogoService();
            _storageService = new StorageService();
            _audioService = new AudioService();

            ListaResultados = new ObservableCollection<Usuarios>();
            ListaArtistas = new ObservableCollection<Usuarios>();
            ListaGeneros = new ObservableCollection<string>();
            ListaGenerosSeleccionados = new ObservableCollection<string>();

            // Nos añadimos como colaborador automáticamente
            var miUsuario = GlobalData.Instance.GetUsuarioObject();
            if (miUsuario != null) ListaArtistas.Add(miUsuario);

            // LÓGICA DE COMANDOS (Refactorizada en métodos abajo)
            BtnAgregarGenero = ReactiveCommand.Create(AgregarGenero);
            BtnEliminarGenero = ReactiveCommand.Create<string>(EliminarGenero);
            BtnBuscarUsuarios = ReactiveCommand.Create(BuscarUsuarios);
            BtnAgregarUsuario = ReactiveCommand.Create<Usuarios>(AgregarUsuario);
            BtnEliminarUsuario = ReactiveCommand.Create<Usuarios>(EliminarUsuario);
            BtnVolverAtras = ReactiveCommand.Create(accionVolver);

            // 1. Configuración del Buscador Reactivo
            this.WhenAnyValue(x => x.TxtBusqueda)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Where(x => !string.IsNullOrWhiteSpace(x) && x.Length > 2)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => BuscarUsuarios());

            // Validación para Publicar
            var validacionPublicar = this.WhenAnyValue(
                x => x.TxtTitulo,
                x => x.LinkYoutube,
                x => x.RutaMp3,
                x => x.EsYoutube,
                x => x.RutaImagen,
                x => x.ListaGenerosSeleccionados.Count,
                (titulo, link, mp3, esYt, imagen, cantidadGeneros) =>
                    !string.IsNullOrWhiteSpace(titulo) &&
                    !string.IsNullOrWhiteSpace(imagen) &&
                    cantidadGeneros > 0 &&
                    (esYt ? !string.IsNullOrWhiteSpace(link) : !string.IsNullOrWhiteSpace(mp3))
            );

            BtnPublicar = ReactiveCommand.CreateFromTask(PublicarCancion, validacionPublicar);

            // Carga inicial
            _ = CargarGeneros();
        }

        private void AgregarGenero()
        {
            if (string.IsNullOrWhiteSpace(GeneroSeleccionado))
            {
                return;
            }

            bool yaEstaEnLista = ListaGenerosSeleccionados.Any(g => g.Equals(GeneroSeleccionado, StringComparison.OrdinalIgnoreCase));

            if (!yaEstaEnLista)
            {
                ListaGenerosSeleccionados.Add(GeneroSeleccionado);
                GeneroSeleccionado = null;
            }
            else
            {
                _dialogoService.MostrarAlerta("Msg_Error_GeneroYaAnadido");
                GeneroSeleccionado = null;
            }
        }

        private void EliminarGenero(string genero)
        {
            if (ListaGenerosSeleccionados.Contains(genero))
            {
                ListaGenerosSeleccionados.Remove(genero);
            }
        }

        private void AgregarUsuario(Usuarios usuario)
        {
            bool yaExiste = ListaArtistas.Any(u => u.Id == usuario.Id);
            if (!yaExiste)
            {
                ListaArtistas.Add(usuario);
            }
            TxtBusqueda = string.Empty;
            ListaResultados.Clear();
        }

        private void EliminarUsuario(Usuarios usuario)
        {
            if (usuario.Id == GlobalData.Instance.UserIdGD)
            {
                _dialogoService.MostrarAlerta("Msg_Error_BorrarPropioUser");
                return;
            }
            if (ListaArtistas.Contains(usuario))
            {
                ListaArtistas.Remove(usuario);
            }
        }

        private async void BuscarUsuarios()
        {
            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var listaResultadosBusqueda = await MongoClientSingleton.Instance.Cliente.ObtenerUsuariosPorBusqueda(TxtBusqueda, ListaArtistas.Select(x => x.Id).ToList());
                if (listaResultadosBusqueda != null && listaResultadosBusqueda.Count > 0)
                {
                    ListaResultados = new ObservableCollection<Usuarios>(listaResultadosBusqueda);
                }
                else
                {
                    ListaResultados.Clear();
                }
            }
            else
            {
                _dialogoService.MostrarAlerta("Msg_Error_Conexion");
            }
        }

        private void CargarImagenLocal(string ruta)
        {
            try
            {
                if (System.IO.File.Exists(ruta))
                {
                    ImagenPortada = new Bitmap(ruta);
                }
                else
                {
                    ImagenPortada = null;
                }
            }
            catch
            {
                ImagenPortada = null;
            }
            this.RaisePropertyChanged(nameof(TieneImagen));
        }

        private async Task CargarGeneros()
        {
            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var listadeGeneros = await MongoClientSingleton.Instance.Cliente.ObtenerNombresGeneros();
                ListaGeneros = new ObservableCollection<string>(listadeGeneros);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Error en la conexión de la base de datos");
            }
        }

        private int ObtenerDuracionMp3(string rutaArchivo)
        {
            try
            {
                if (System.IO.File.Exists(rutaArchivo))
                {
                    var archivo = TagLib.File.Create(rutaArchivo);
                    return (int)archivo.Properties.Duration.TotalSeconds;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error leyendo duración: " + ex.Message);
            }
            return 0;
        }

        private async Task PublicarCancion()
        {
            EstaCargando = true;

            try
            {
                // 1. Subir Imagen
                string urlImagenNube = await _storageService.SubirImagen(RutaImagen);

                string urlAudioFinal = "";
                int duracionFinal = 0;

                // 2. Subir Audio / Obtener Info
                if (EsYoutube)
                {
                    var infoYoutube = await _audioService.ObtenerMp3(LinkYoutube);

                    if (infoYoutube != null)
                    {
                        urlAudioFinal = LinkYoutube; // Guardamos el enlace original
                        duracionFinal = infoYoutube.DuracionSegundos;
                    }
                    else
                    {
                        throw new InvalidOperationException("Msg_Error_InfoYoutube");
                    }
                }
                else
                {
                    urlAudioFinal = await _storageService.SubirCancion(RutaMp3);
                    duracionFinal = _duracionCalculada;
                }

                // 3. Crear Objeto
                var nuevaCancion = new Canciones
                {
                    Titulo = TxtTitulo,
                    AutoresIds = ListaArtistas.Select(u => u.Id).ToList(),
                    ImagenPortadaUrl = urlImagenNube,
                    UrlCancion = urlAudioFinal,
                    Datos = new DatosCancion
                    {
                        FechaLanzamiento = DateTime.Now,
                        DuracionSegundos = duracionFinal,
                        Generos = ListaGenerosSeleccionados.ToList() // Guardamos la lista completa
                    }
                };

                // 4. Guardar en BD
                bool exito = await MongoClientSingleton.Instance.Cliente.PublicarCancion(nuevaCancion);

                if (exito)
                {
                    await MongoClientSingleton.Instance.Cliente.IncrementarContadorCancionesUsuario(GlobalData.Instance.UserIdGD, 1);
                    EstaCargando = false;

                    _dialogoService.MostrarAlerta("Msg_Exito_CancionPublicada");
                    _Volver();
                }
                else
                {
                    EstaCargando = false;
                    _dialogoService.MostrarAlerta("Msg_Error_SubirCancion");
                }
            }
            catch (InvalidOperationException ex)
            {
                EstaCargando = false;
                _dialogoService.MostrarAlerta("Msg_Error_OperacionInvalida");
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                EstaCargando = false;
                _dialogoService.MostrarAlerta("Msg_Error_Inesperado");
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}