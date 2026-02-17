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
    public class ViewCrearListaPersonalizadaViewModel : ViewModelBase
    {
        //Servicios
        private readonly IDialogoService _dialogoService;
        private readonly StorageService _storageService;
       
        
        private readonly Action _Volver;

        // Binding de datos
        private string _txtNombre;
        public string TxtNombre
        {
            get => _txtNombre;
            set => this.RaiseAndSetIfChanged(ref _txtNombre, value);
        }

        private string _txtDescripcion;
        public string TxtDescripcion
        {
            get => _txtDescripcion;
            set => this.RaiseAndSetIfChanged(ref _txtDescripcion, value);
        }

        // Biding para la imagen
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

        // Binding para el buscador de canciones
        private string _txtBusqueda;
        public string TxtBusqueda
        {
            get => _txtBusqueda;
            set => this.RaiseAndSetIfChanged(ref _txtBusqueda, value);
        }

        private ObservableCollection<Canciones> _listaResultados;
        public ObservableCollection<Canciones> ListaResultados
        {
            get => _listaResultados;
            set => this.RaiseAndSetIfChanged(ref _listaResultados, value);
        }

        private ObservableCollection<Canciones> _listaCancionesSeleccionadas;
        public ObservableCollection<Canciones> ListaCancionesSeleccionadas
        {
            get => _listaCancionesSeleccionadas;
            set => this.RaiseAndSetIfChanged(ref _listaCancionesSeleccionadas, value);
        }

        // Para le progressbar
        private bool _estaCargando;
        public bool EstaCargando
        {
            get => _estaCargando;
            set => this.RaiseAndSetIfChanged(ref _estaCargando, value);
        }

        // Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnVolverAtras { get; }
        public ReactiveCommand<Unit, Unit> BtnCrear { get; }
        public ReactiveCommand<Unit, Unit> BtnBuscarCanciones { get; }
        public ReactiveCommand<Canciones, Unit> BtnAgregarCancion { get; }
        public ReactiveCommand<Canciones, Unit> BtnEliminarCancion { get; }


        public ViewCrearListaPersonalizadaViewModel(Action accionVolver)
        {
            _Volver = accionVolver;
            _dialogoService = new DialogoService();
            _storageService = new StorageService();

            ListaResultados = new ObservableCollection<Canciones>();
            ListaCancionesSeleccionadas = new ObservableCollection<Canciones>();

            // Comandos
            BtnVolverAtras = ReactiveCommand.Create(accionVolver);
            BtnBuscarCanciones = ReactiveCommand.Create(BuscarCanciones);
            BtnAgregarCancion = ReactiveCommand.Create<Canciones>(AgregarCancion);
            BtnEliminarCancion = ReactiveCommand.Create<Canciones>(EliminarCancion);

            // Buscador Reactivo
            this.WhenAnyValue(x => x.TxtBusqueda)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Where(x => !string.IsNullOrWhiteSpace(x) && x.Length > 2)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => BuscarCanciones());

            // Validación para Crear
            var validacionCrear = this.WhenAnyValue(
                x => x.TxtNombre,
                x => x.RutaImagen,
                x => x.ListaCancionesSeleccionadas.Count,
                (nombre, imagen, count) =>
                    !string.IsNullOrWhiteSpace(nombre) &&
                    !string.IsNullOrWhiteSpace(imagen) &&
                    count > 0 // Al menos una canción
            );
            //Configuración de comandos reactive
            BtnCrear = ReactiveCommand.CreateFromTask(CrearLista, validacionCrear);
        }
        /// <summary>
        /// Realiza una búsqueda asíncrona de canciones en la base de datos utilizando el texto introducido por el usuario.
        /// </summary>
        /// <remarks>
        /// Este método consulta MongoDB y filtra los resultados obtenidos para excluir aquellas canciones 
        /// que ya están presentes en la lista de selección (<see cref="ListaCancionesSeleccionadas"/>).
        /// Esto evita duplicados visuales y actualiza la colección de resultados disponibles para añadir.
        /// </remarks>
        private async void BuscarCanciones()
        {
            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var resultados = await MongoClientSingleton.Instance.Cliente.ObtenerCancionesPorBusqueda(TxtBusqueda);

                if (resultados != null)
                {
                    // Filtramos las que ya están seleccionadas para no duplicar visualmente
                    var filtradas = resultados.Where(c => !ListaCancionesSeleccionadas.Any(sel => sel.Id == c.Id));
                    ListaResultados = new ObservableCollection<Canciones>(filtradas);
                }
            }
        }

        /// <summary>
        /// Agrega la canción seleccionada a la lista temporal de canciones que formarán parte de la nueva playlist.
        /// </summary>
        /// <remarks>
        /// Este método realiza tres acciones clave:
        /// <list type="number">
        /// <item>Verifica que la canción no esté ya añadida para evitar duplicados.</item>
        /// <item>Mueve visualmente la canción: la añade a <see cref="ListaCancionesSeleccionadas"/> y la elimina de <see cref="ListaResultados"/>.</item>
        /// <item>Limpia el campo de búsqueda para facilitar una nueva consulta inmediata.</item>
        /// </list>
        /// </remarks>
        /// <param name="cancion">El objeto <see cref="Canciones"/> que el usuario ha seleccionado para añadir.</param>
        private void AgregarCancion(Canciones cancion)
        {
            if (!ListaCancionesSeleccionadas.Any(c => c.Id == cancion.Id))
            {
                ListaCancionesSeleccionadas.Add(cancion);
                ListaResultados.Remove(cancion); // La quitamos de resultados para que quede limpio
                TxtBusqueda = "";
            }
        }

        /// <summary>
        /// Elimina una canción de la lista de canciones seleccionadas para la nueva playlist.
        /// </summary>
        /// <remarks>
        /// Permite al usuario rectificar su selección quitando canciones individuales de <see cref="ListaCancionesSeleccionadas"/>
        /// antes de guardar la lista definitiva. La interfaz de usuario refleja el cambio inmediatamente.
        /// </remarks>
        /// <param name="cancion">El objeto <see cref="Canciones"/> que se desea descartar de la selección actual.</param>
        private void EliminarCancion(Canciones cancion)
        {
            ListaCancionesSeleccionadas.Remove(cancion);
        }

        /// <summary>
        /// Intenta cargar y visualizar una imagen local desde la ruta especificada.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Este método gestiona de forma segura la carga de archivos de imagen. Si el archivo no existe 
        /// o el formato no es válido (lanzando una excepción), la propiedad <see cref="ImagenPortada"/> 
        /// se establece en <c>null</c> para evitar errores visuales.
        /// </para>
        /// <para>
        /// Al finalizar, fuerza una notificación de cambio en <see cref="TieneImagen"/> para que la interfaz 
        /// actualice la visibilidad de los controles dependientes (como el botón de "Quitar imagen").
        /// </para>
        /// </remarks>
        /// <param name="ruta">La ruta absoluta del sistema de archivos donde se encuentra la imagen.</param>
        private void CargarImagenLocal(string ruta)
        {
            try
            {
                if (System.IO.File.Exists(ruta))
                    ImagenPortada = new Bitmap(ruta);
                else
                    ImagenPortada = null;
            }
            catch
            {
                ImagenPortada = null;
            }
            this.RaisePropertyChanged(nameof(TieneImagen));
        }
        /// <summary>
        /// Orquesta el proceso completo de creación de una nueva lista de reproducción personalizada de forma asíncrona.
        /// </summary>
        /// <remarks>
        /// Este método sigue un flujo de transacciones paso a paso:
        /// <list type="number">
        /// <item><b>Carga de medios:</b> Sube la imagen de portada seleccionada al servicio de almacenamiento en la nube.</item>
        /// <item><b>Construcción del modelo:</b> Crea una instancia de <see cref="ListaPersonalizada"/> con los metadatos y la selección de canciones actual.</item>
        /// <item><b>Persistencia:</b> Invoca al cliente de MongoDB para guardar la nueva lista en la base de datos.</item>
        /// </list>
        /// Gestiona los estados de carga (<see cref="EstaCargando"/>) para bloquear la UI durante el proceso y maneja excepciones globales.
        /// </remarks>
        /// <returns>Una <see cref="Task"/> que representa la operación asíncrona.</returns>
        private async Task CrearLista()
        {
            EstaCargando = true;
            try
            {
                // Subir Imagen
                string urlPortada = await _storageService.SubirImagen(RutaImagen);

                // Crear Objeto
                var nuevaLista = new ListaPersonalizada
                {
                    Nombre = TxtNombre,
                    Descripcion = TxtDescripcion,
                    UrlPortada = urlPortada,
                    IdUsuario = GlobalData.Instance.UserIdGD,
                    IdsCanciones = ListaCancionesSeleccionadas.Select(c => c.Id).ToList()
                };

                // Guardarmos en BD
                bool exito = await MongoClientSingleton.Instance.Cliente.CrearListaReproduccion(nuevaLista);

                if (exito)
                {
                    EstaCargando = false;
                    _Volver();
                }
                else
                {
                    EstaCargando = false;
                    _dialogoService.MostrarAlerta("Msg_Error_CrearPlaylist");
                }
            }
            catch (Exception ex)
            {
                EstaCargando = false;
                _dialogoService.MostrarAlerta("Msg_Error_Inesperado");
                System.Diagnostics.Debug.WriteLine("Error crear lista: " + ex.Message);
            }
        }
    }
}
