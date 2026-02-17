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
using System.Net.Http;

namespace BetaProyecto.ViewModels
{
    public class ViewEditarListaPersonalizadaViewModel : ViewModelBase
    {
        //Variables
        private readonly ListaPersonalizada _playlistOriginal;
        
        //Sercivios
        private readonly IDialogoService _dialogoService;
        private readonly StorageService _storageService;

        //Actions
        private readonly Action? _accionVolver;

        // Bidings 
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

        private string _rutaImagen;
        public string RutaImagen
        {
            get => _rutaImagen;
            set
            {
                this.RaiseAndSetIfChanged(ref _rutaImagen, value);
                // Si cambiamos la ruta a un archivo local, cargamos la preview
                if (!value.StartsWith("http"))
                    CargarImagenLocal(value);
            }
        }

        private bool _tieneImagen;
        public bool TieneImagen
        {
            get => _tieneImagen;
            set => this.RaiseAndSetIfChanged(ref _tieneImagen, value);
        }

        private Bitmap? _imagenPortada;
        public Bitmap? ImagenPortada
        {
            get => _imagenPortada;
            set => this.RaiseAndSetIfChanged(ref _imagenPortada, value);
        }

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

        // Para el progess bar
        private bool _estaCargando;
        public bool EstaCargando
        {
            get => _estaCargando;
            set => this.RaiseAndSetIfChanged(ref _estaCargando, value);
        }

        // Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnAtras { get; }
        public ReactiveCommand<Unit, Unit> BtnGuardar { get; }
        public ReactiveCommand<Unit, Unit> BtnBuscarCanciones { get; }
        public ReactiveCommand<Canciones, Unit> BtnAgregarCancion { get; }
        public ReactiveCommand<Canciones, Unit> BtnEliminarCancion { get; }

        // Constructor
        public ViewEditarListaPersonalizadaViewModel(ListaPersonalizada playlist, Action accionVolver)
        {
            _playlistOriginal = playlist;
            _accionVolver = accionVolver;

            //Inicializamos servicios 
            _dialogoService = new DialogoService();
            _storageService = new StorageService();

            ListaResultados = new ObservableCollection<Canciones>();

            // Cargar datos originales
            TxtNombre = playlist.Nombre;
            TxtDescripcion = playlist.Descripcion;
            RutaImagen = playlist.UrlPortada; // Inicialmente es la URL de la nube

            // Cargar canciones
            ListaCancionesSeleccionadas = new ObservableCollection<Canciones>(playlist.CancionesCompletas);

            // Cargar la imagen visualmente
            _ = CargarImagenDesdeUrl(playlist.UrlPortada);

            //Configuramos comandos reative
            BtnAtras = ReactiveCommand.Create(() => _accionVolver());
            BtnBuscarCanciones = ReactiveCommand.Create(BuscarCanciones);
            BtnAgregarCancion = ReactiveCommand.Create<Canciones>(AgregarCancion);
            BtnEliminarCancion = ReactiveCommand.Create<Canciones>(EliminarCancion);

            // Buscador Reactivo
            this.WhenAnyValue(x => x.TxtBusqueda)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Where(x => !string.IsNullOrWhiteSpace(x) && x.Length > 2)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => BuscarCanciones());

            // Validación para Guardar
            var validacionGuardar = this.WhenAnyValue(
                x => x.TxtNombre,
                x => x.RutaImagen,
                x => x.ListaCancionesSeleccionadas.Count,
                (nombre, imagen, count) =>
                    !string.IsNullOrWhiteSpace(nombre) &&
                    !string.IsNullOrWhiteSpace(imagen) &&
                    count > 0
            );

            BtnGuardar = ReactiveCommand.CreateFromTask(GuardarCambios, validacionGuardar);
        }

        /// <summary>
        /// Descarga de forma asíncrona una imagen desde una dirección URL y la asigna al mapa de bits de la portada.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la recuperación de recursos remotos mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Petición HTTP:</b> Utiliza un <see cref="HttpClient"/> para obtener el flujo de bytes de la imagen desde la red.</item>
        /// <item><b>Procesamiento de Memoria:</b> Transfiere los bytes a un <see cref="System.IO.MemoryStream"/> para su decodificación.</item>
        /// <item><b>Asignación Visual:</b> Inicializa la propiedad <see cref="ImagenPortada"/> con el nuevo <see cref="Bitmap"/> y actualiza el estado de <see cref="TieneImagen"/>.</item>
        /// </list>
        /// En caso de error en la red o formato inválido, se captura la excepción y se notifica al usuario mediante <see cref="_dialogoService"/>.
        /// </remarks>
        /// <param name="url">La dirección URL completa de la imagen que se desea cargar.</param>
        /// <returns>Una tarea que representa la operación de carga asíncrona.</returns>
        private async Task CargarImagenDesdeUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                using (var client = new HttpClient())
                {
                    var bytes = await client.GetByteArrayAsync(url);
                    using (var stream = new System.IO.MemoryStream(bytes))
                    {
                        ImagenPortada = new Bitmap(stream);
                        TieneImagen = true;
                    }
                }
            }
            catch { 
                _dialogoService.MostrarAlerta("Msg_Error_CargarImagen");
            }
        }
        /// <summary>
        /// Intenta cargar y asignar una imagen desde el almacenamiento local del sistema de archivos.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la carga de recursos gráficos locales mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación de ruta:</b> Verifica la existencia física del archivo mediante <see cref="System.IO.File.Exists"/>.</item>
        /// <item><b>Instanciación:</b> Si el archivo es válido, crea un nuevo objeto <see cref="Bitmap"/> y lo asigna a <see cref="ImagenPortada"/>.</item>
        /// <item><b>Control de estado:</b> Actualiza la propiedad booleana <see cref="TieneImagen"/> para reflejar el éxito o fallo de la operación en la interfaz.</item>
        /// </list>
        /// El bloque <c>try-catch</c> asegura que errores de formato o permisos de lectura no interrumpan la ejecución del programa.
        /// </remarks>
        /// <param name="ruta">La ruta absoluta en el disco local donde se encuentra el archivo de imagen.</param>
        private void CargarImagenLocal(string ruta)
        {
            try
            {
                if (System.IO.File.Exists(ruta))
                {
                    ImagenPortada = new Bitmap(ruta);
                    TieneImagen = true;
                }
                else
                {
                    TieneImagen = false;
                }
            }
            catch
            {
                TieneImagen = false;
            }
        }

        /// <summary>
        /// Realiza una búsqueda asíncrona de canciones en la base de datos y filtra aquellas que ya han sido seleccionadas para la lista actual.
        /// </summary>
        /// <remarks>
        /// Este método gestiona el filtrado dinámico de contenido mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Consulta remota:</b> Solicita al cliente de MongoDB las canciones que coincidan con el término almacenado en <see cref="TxtBusqueda"/>.</item>
        /// <item><b>Filtrado local:</b> Aplica una operación LINQ para excluir de los resultados cualquier canción cuyo identificador ya se encuentre en <see cref="ListaCancionesSeleccionadas"/>.</item>
        /// <item><b>Actualización de UI:</b> Inicializa la propiedad <see cref="ListaResultados"/> con una nueva colección observable, permitiendo que la interfaz muestre únicamente las opciones elegibles.</item>
        /// </list>
        /// </remarks>
        private async void BuscarCanciones()
        {
            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var resultados = await MongoClientSingleton.Instance.Cliente.ObtenerCancionesPorBusqueda(TxtBusqueda);
                if (resultados != null)
                {
                    var filtradas = resultados.Where(c => !ListaCancionesSeleccionadas.Any(sel => sel.Id == c.Id));
                    ListaResultados = new ObservableCollection<Canciones>(filtradas);
                }
            }
        }
        /// <summary>
        /// Incorpora una canción específica a la lista de selección actual y limpia el estado de búsqueda.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la selección de pistas musicales mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación de unicidad:</b> Verifica que la canción no haya sido agregada previamente comparando su identificador único.</item>
        /// <item><b>Transferencia de estado:</b> Añade la canción a <see cref="ListaCancionesSeleccionadas"/> y la remueve simultáneamente de la lista de resultados de búsqueda para evitar duplicidad visual.</item>
        /// <item><b>Reinicio de filtros:</b> Restablece la cadena de búsqueda <see cref="TxtBusqueda"/> para facilitar una nueva consulta.</item>
        /// </list>
        /// </remarks>
        /// <param name="cancion">El objeto de tipo <see cref="Canciones"/> que se desea añadir a la lista o playlist.</param>
        private void AgregarCancion(Canciones cancion)
        {
            if (!ListaCancionesSeleccionadas.Any(c => c.Id == cancion.Id))
            {
                ListaCancionesSeleccionadas.Add(cancion);
                ListaResultados.Remove(cancion);
                TxtBusqueda = "";
            }
        }
        /// <summary>
        /// Remueve una canción específica de la colección de pistas seleccionadas para la lista de reproducción.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la edición de la lista mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Identificación:</b> Localiza la instancia del objeto <see cref="Canciones"/> dentro de la colección <see cref="ListaCancionesSeleccionadas"/>.</item>
        /// <item><b>Remoción:</b> Elimina el elemento de la lista, lo cual desencadena automáticamente la actualización de la interfaz de usuario al ser una colección observable.</item>
        /// </list>
        /// </remarks>
        /// <param name="cancion">El objeto de tipo <see cref="Canciones"/> que se desea retirar de la selección actual.</param>
        private void EliminarCancion(Canciones cancion)
        {
            ListaCancionesSeleccionadas.Remove(cancion);
        }

        /// <summary>
        /// Procesa y persiste de forma asíncrona las modificaciones realizadas en una lista de reproducción existente, incluyendo la gestión de medios y la estructura de pistas.
        /// </summary>
        /// <remarks>
        /// Este método orquesta la actualización de la playlist mediante el siguiente flujo de trabajo:
        /// <list type="number">
        /// <item><b>Sincronización de Imagen:</b> Evalúa si la ruta de la portada es local o remota. En caso de ser local, sube el archivo a la nube mediante <see cref="_storageService"/> para obtener una URL persistente.</item>
        /// <item><b>Preparación de Metadatos:</b> Extrae y proyecta los identificadores únicos de la colección <see cref="ListaCancionesSeleccionadas"/>.</item>
        /// <item><b>Persistencia en BD:</b> Invoca al cliente de MongoDB para actualizar el nombre, descripción, lista de IDs y URL de portada en el documento correspondiente.</item>
        /// <item><b>Finalización:</b> Tras el éxito, libera el estado de carga y ejecuta la acción de retorno a la vista anterior.</item>
        /// </list>
        /// En caso de error, se notifica al usuario mediante el servicio de diálogos y se registra la excepción para depuración.
        /// </remarks>
        /// <returns>Una tarea que representa la operación de guardado asíncrona.</returns>
        private async Task GuardarCambios()
        {
            EstaCargando = true;
            try
            {
                string urlPortadaFinal = RutaImagen;

                //Si la imagen es local, la subimos y obtenemos la URL de la nube
                if (!RutaImagen.StartsWith("http"))
                {
                    urlPortadaFinal = await _storageService.SubirImagen(RutaImagen);
                }

                // Extraeremos IDs de canciones
                var nuevosIds = ListaCancionesSeleccionadas.Select(c => c.Id).ToList();

                // Actualizamos en la BD
                await MongoClientSingleton.Instance.Cliente.ActualizarPlaylist(
                    TxtNombre,
                    TxtDescripcion,
                    nuevosIds,
                    urlPortadaFinal,
                    _playlistOriginal
                    );

                EstaCargando = false;
                _accionVolver();
            }
            catch (Exception ex)
            {
                EstaCargando = false;
                _dialogoService.MostrarAlerta("Msg_Error_ActualizarPlaylist");
                System.Diagnostics.Debug.WriteLine($"Error al actualizar playlist: {ex.Message}");
            }
        }
    }
}