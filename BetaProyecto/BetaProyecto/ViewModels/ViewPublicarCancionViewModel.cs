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

        // Actions
        private readonly Action _Volver;

        // Datos de canciones
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

        // Gestión de colaboradores
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

        // Imagen y audio
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

        // Estado
        private bool _estaCargando;
        public bool EstaCargando
        {
            get => _estaCargando;
            set => this.RaiseAndSetIfChanged(ref _estaCargando, value);
        }

        // Comandos reactive
        public ReactiveCommand<Unit, Unit> BtnVolverAtras { get; }
        public ReactiveCommand<Unit, Unit> BtnPublicar { get; }
        public ReactiveCommand<Unit, Unit> BtnBuscarUsuarios { get; }
        public ReactiveCommand<Usuarios, Unit> BtnAgregarUsuario { get; }
        public ReactiveCommand<Usuarios, Unit> BtnEliminarUsuario { get; }
        public ReactiveCommand<Unit, Unit> BtnAgregarGenero { get; }
        public ReactiveCommand<string, Unit> BtnEliminarGenero { get; }

        //Constructor
        public ViewPublicarCancionViewModel(Action accionVolver)
        {
            //Heredamos actions
            _Volver = accionVolver;

            //Inicializamos servicios
            _dialogoService = new DialogoService();
            _storageService = new StorageService();
            _audioService = new AudioService();
            
            //Inicializamos listas
            ListaResultados = new ObservableCollection<Usuarios>();
            ListaArtistas = new ObservableCollection<Usuarios>();
            ListaGeneros = new ObservableCollection<string>();
            ListaGenerosSeleccionados = new ObservableCollection<string>();

            // Nos añadimos como colaborador automáticamente
            var miUsuario = GlobalData.Instance.GetUsuarioObject();
            if (miUsuario != null) ListaArtistas.Add(miUsuario);

            // Configuración del Buscador Reactivo
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

            // Configuramos comandos reactive
            BtnAgregarGenero = ReactiveCommand.Create(AgregarGenero);
            BtnEliminarGenero = ReactiveCommand.Create<string>(EliminarGenero);
            BtnBuscarUsuarios = ReactiveCommand.Create(BuscarUsuarios);
            BtnAgregarUsuario = ReactiveCommand.Create<Usuarios>(AgregarUsuario);
            BtnEliminarUsuario = ReactiveCommand.Create<Usuarios>(EliminarUsuario);
            BtnVolverAtras = ReactiveCommand.Create(accionVolver);
            BtnPublicar = ReactiveCommand.CreateFromTask(PublicarCancion, validacionPublicar);

            // Carga inicial
            _ = CargarGeneros();
        }
        /// <summary>
        /// Añade el género seleccionado actualmente a la lista de géneros asociados, validando que no esté vacío y que no se haya añadido previamente.
        /// </summary>
        /// <remarks>
        /// El método verifica si <see cref="GeneroSeleccionado"/> contiene un valor válido. Si el género ya existe en 
        /// <see cref="ListaGenerosSeleccionados"/> (comparación insensible a mayúsculas), se muestra una alerta de error 
        /// a través de <see cref="_dialogoService"/>. En cualquier caso, tras el intento de adición, se restablece 
        /// la propiedad <see cref="GeneroSeleccionado"/> a nulo para limpiar la selección de la interfaz.
        /// </remarks>
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
        /// <summary>
        /// Elimina un género específico de la lista de géneros seleccionados para la canción.
        /// </summary>
        /// <remarks>
        /// Este método verifica si el género proporcionado existe dentro de <see cref="ListaGenerosSeleccionados"/>. 
        /// Si se encuentra, lo elimina, lo que actualiza automáticamente cualquier control de la interfaz 
        /// vinculado a esta colección.
        /// </remarks>
        /// <param name="genero">El nombre del género que se desea remover de la selección actual.</param>
        private void EliminarGenero(string genero)
        {
            if (ListaGenerosSeleccionados.Contains(genero))
            {
                ListaGenerosSeleccionados.Remove(genero);
            }
        }
        /// <summary>
        /// Añade un usuario a la lista de artistas seleccionados, evitando duplicados y limpiando los resultados de búsqueda actuales.
        /// </summary>
        /// <remarks>
        /// El método verifica mediante el ID si el <paramref name="usuario"/> ya se encuentra en <see cref="ListaArtistas"/>. 
        /// Tras la validación, independientemente de si se añadió o no, se restablece <see cref="TxtBusqueda"/> 
        /// y se vacía <see cref="ListaResultados"/> para limpiar la interfaz de búsqueda.
        /// </remarks>
        /// <param name="usuario">El objeto de tipo <see cref="Usuarios"/> que se desea vincular o añadir.</param>
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
        /// <summary>
        /// Elimina un usuario de la lista de artistas seleccionados, validando que no sea el usuario que ha iniciado sesión.
        /// </summary>
        /// <remarks>
        /// El método comprueba si el <paramref name="usuario"/> a eliminar coincide con el ID del usuario actual en 
        /// <see cref="GlobalData.Instance.UserIdGD"/>. Si coinciden, se muestra una alerta de error mediante 
        /// <see cref="_dialogoService"/> para impedir que un usuario se elimine a sí mismo de una lista. 
        /// Si la validación es correcta, procede a removerlo de <see cref="ListaArtistas"/>.
        /// </remarks>
        /// <param name="usuario">El objeto de tipo <see cref="Usuarios"/> que se desea remover de la selección.</param>
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
        /// <summary>
        /// Realiza una búsqueda asíncrona de usuarios en la base de datos basada en el texto introducido, filtrando aquellos que ya han sido seleccionados.
        /// </summary>
        /// <remarks>
        /// Este método utiliza <see cref="MongoClientSingleton"/> para consultar usuarios cuyo nombre coincida con <see cref="TxtBusqueda"/>. 
        /// Para evitar duplicados, se envían los IDs de la <see cref="ListaArtistas"/> actual como lista de exclusión. 
        /// Si se encuentran resultados, se actualiza <see cref="ListaResultados"/>; de lo contrario, se limpia. 
        /// En caso de fallo en la conexión, se muestra una alerta mediante <see cref="_dialogoService"/>.
        /// </remarks>
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
        /// <summary>
        /// Carga una imagen desde una ruta local y la asigna a la propiedad ImagenPortada.
        /// </summary>
        /// <remarks>
        /// Intenta crear un objeto <see cref="Bitmap"/> a partir de la ruta proporcionada. Si el archivo no existe o ocurre un error 
        /// durante la lectura, se asigna <c>null</c> a <see cref="ImagenPortada"/> para evitar fallos visuales. 
        /// Finalmente, notifica el cambio de la propiedad <see cref="TieneImagen"/> para actualizar la UI.
        /// </remarks>
        /// <param name="ruta">La ruta del sistema de archivos donde se encuentra la imagen.</param>
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
        /// <summary>
        /// Carga la lista de géneros disponibles desde la base de datos y los asigna a la propiedad ListaGeneros.
        /// </summary>
        /// <remarks>
        /// Este método recupera todos los nombres de géneros registrados en MongoDB mediante el cliente singleton. 
        /// Si la conexión es exitosa, inicializa <see cref="ListaGeneros"/>; de lo contrario, registra el error en 
        /// el flujo de depuración del sistema.
        /// </remarks>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
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
        /// <summary>
        /// Obtiene la duración de un archivo MP3 en segundos utilizando la biblioteca TagLib#.
        /// </summary>
        /// <remarks>
        /// Accede a las propiedades del archivo en disco para extraer su duración total. Si el archivo no existe 
        /// o se produce una excepción al intentar leer los metadatos de audio, el error se captura y el método 
        /// devuelve 0 segundos para no interrumpir el flujo.
        /// </remarks>
        /// <param name="rutaArchivo">La ruta completa del archivo de audio local.</param>
        /// <returns>La duración total en segundos.</returns>
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
        /// <summary>
        /// Realiza el proceso completo de publicación de una canción, integrando subida de archivos, obtención de datos y persistencia.
        /// </summary>
        /// <remarks>
        /// Este método orquesta un flujo complejo dividido en cuatro fases principales:
        /// <list type="number">
        /// <item><b>Subida de imagen:</b> Sube la portada seleccionada a la nube.</item>
        /// <item><b>Gestión de audio:</b> Sube el archivo MP3 o procesa el enlace de YouTube para obtener la duración y URL final.</item>
        /// <item><b>Creación de modelo:</b> Construye el objeto <see cref="Canciones"/> con autores y géneros seleccionados.</item>
        /// <item><b>Persistencia:</b> Guarda la canción en la BD y actualiza el contador de canciones del usuario.</item>
        /// </list>
        /// Durante la ejecución, se controla la propiedad <see cref="EstaCargando"/> para feedback visual en la UI.
        /// </remarks>
        /// <returns>Una tarea que representa la operación de publicación asíncrona.</returns>
        private async Task PublicarCancion()
        {
            EstaCargando = true;

            try
            {
                // Subimos Imagen
                string urlImagenNube = await _storageService.SubirImagen(RutaImagen);

                string urlAudioFinal = "";
                int duracionFinal = 0;

                // Subimos Audio / Obtenemos Info
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

                // Creamos Objeto
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

                // Guardamos en BD
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