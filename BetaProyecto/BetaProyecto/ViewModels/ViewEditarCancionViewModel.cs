using Avalonia.Media.Imaging;
using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewEditarCancionViewModel : ViewModelBase
    {
        //Propiedad 
        private readonly Canciones _cancionOriginal;

        //Servicios
        private readonly IDialogoService _dialogoService;
        private readonly StorageService _storageService;

        //Actions
        private readonly Action _accionVolver;

        // Bidings
        private string _txtTitulo;
        public string TxtTitulo
        {
            get => _txtTitulo;
            set => this.RaiseAndSetIfChanged(ref _txtTitulo, value);
        }

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

        private ObservableCollection<string> _listaGenerosSeleccionados;
        public ObservableCollection<string> ListaGenerosSeleccionados
        {
            get => _listaGenerosSeleccionados;
            set => this.RaiseAndSetIfChanged(ref _listaGenerosSeleccionados, value);
        }

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

        private ObservableCollection<Usuarios> _listaArtistas;
        public ObservableCollection<Usuarios> ListaArtistas
        {
            get => _listaArtistas;
            set => this.RaiseAndSetIfChanged(ref _listaArtistas, value);
        }

        private string _rutaImagen;
        public string RutaImagen
        {
            get => _rutaImagen;
            set
            {
                this.RaiseAndSetIfChanged(ref _rutaImagen, value);
                // Si es archivo local, cargamos preview
                if (!value.StartsWith("http")) CargarImagenLocal(value);
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

        // Progress bar
        private bool _estaCargando;
        public bool EstaCargando
        {
            get => _estaCargando;
            set => this.RaiseAndSetIfChanged(ref _estaCargando, value);
        }

        //  Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnCancelar { get; }
        public ReactiveCommand<Unit, Unit> BtnGuardar { get; }
        public ReactiveCommand<Unit, Unit> BtnAgregarGenero { get; }
        public ReactiveCommand<string, Unit> BtnEliminarGenero { get; }
        public ReactiveCommand<Unit, Unit> BtnBuscarUsuarios { get; }
        public ReactiveCommand<Usuarios, Unit> BtnAgregarUsuario { get; }
        public ReactiveCommand<Usuarios, Unit> BtnEliminarUsuario { get; }


        // Constructor
        public ViewEditarCancionViewModel(Canciones cancion, Action accionVolver)
        {
            //Asignamos propiedades
            _cancionOriginal = cancion;
            _accionVolver = accionVolver;
            
            // Inicializamos servicios
            _dialogoService = new DialogoService();
            _storageService = new StorageService();

            // Inicializar listas
            ListaResultados = new ObservableCollection<Usuarios>();
            ListaArtistas = new ObservableCollection<Usuarios>();
            ListaGeneros = new ObservableCollection<string>();
            ListaGenerosSeleccionados = new ObservableCollection<string>();

            // Cargamos datos 
            TxtTitulo = cancion.Titulo;
            RutaImagen = cancion.ImagenPortadaUrl;
            ListaGenerosSeleccionados = new ObservableCollection<string>(cancion.Datos.Generos);

            // Cargar imagen visualmente
            _ = CargarImagenDesdeUrl(cancion.ImagenPortadaUrl);

            // Configuramos comandos reactivos
            BtnCancelar = ReactiveCommand.Create(() => _accionVolver());
            BtnAgregarGenero = ReactiveCommand.Create(AgregarGenero);
            BtnEliminarGenero = ReactiveCommand.Create<string>(EliminarGenero);
            BtnBuscarUsuarios = ReactiveCommand.Create(BuscarUsuarios);
            BtnAgregarUsuario = ReactiveCommand.Create<Usuarios>(AgregarUsuario);
            BtnEliminarUsuario = ReactiveCommand.Create<Usuarios>(EliminarUsuario);

            // Buscador reactivo
            this.WhenAnyValue(x => x.TxtBusqueda)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Where(x => !string.IsNullOrWhiteSpace(x) && x.Length > 2)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => BuscarUsuarios());

            // Validación Guardar
            var validacionGuardar = this.WhenAnyValue(
                x => x.TxtTitulo,
                x => x.RutaImagen,
                x => x.ListaGenerosSeleccionados.Count,
                (titulo, imagen, generos) =>
                    !string.IsNullOrWhiteSpace(titulo) &&
                    !string.IsNullOrWhiteSpace(imagen) &&
                    generos > 0
            );

            BtnGuardar = ReactiveCommand.CreateFromTask(GuardarCambios, validacionGuardar);

            // Cargamos datos en segundo plano
            _ = CargarGenerosDisponibles();
            _ = CargarColaboradoresOriginales();
        }
        /// <summary>
        /// Recupera de forma asíncrona el catálogo completo de géneros musicales definidos en el sistema.
        /// </summary>
        /// <remarks>
        /// Este método establece conexión con la base de datos a través de <see cref="MongoClientSingleton"/> para 
        /// obtener la lista maestra de nombres de géneros. Una vez recibidos, inicializa la propiedad 
        /// <see cref="ListaGeneros"/> con una nueva colección observable, permitiendo que los selectores de la interfaz 
        /// de usuario se pueblen dinámicamente con los valores actualizados.
        /// </remarks>
        /// <returns>Una tarea que representa la operación de carga asíncrona.</returns>
        private async Task CargarGenerosDisponibles()
        {
            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var generos = await MongoClientSingleton.Instance.Cliente.ObtenerNombresGeneros();
                ListaGeneros = new ObservableCollection<string>(generos);
            }
        }

        /// <summary>
        /// Recupera de forma asíncrona la información detallada de los colaboradores originales de la canción basándose en sus identificadores.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la conversión de la lista de IDs almacenada en los metadatos de la canción a objetos de tipo <see cref="Usuarios"/>. 
        /// Consulta la base de datos a través de <see cref="MongoClientSingleton"/> y, tras obtener los perfiles correspondientes, 
        /// inicializa la propiedad <see cref="ListaArtistas"/> con una nueva colección observable para su representación en la interfaz.
        /// </remarks>
        /// <returns>Una tarea que representa la operación de carga asíncrona.</returns>
        private async Task CargarColaboradoresOriginales()
        {
            // Necesitamos convertir la lista de IDs en objetos Usuarios
            if (_cancionOriginal.AutoresIds != null && _cancionOriginal.AutoresIds.Count > 0)
            {
                var usuarios = await MongoClientSingleton.Instance.Cliente.ObtenerUsuariosPorListaIds(_cancionOriginal.AutoresIds);

                if (usuarios != null)
                    ListaArtistas = new ObservableCollection<Usuarios>(usuarios);
            }
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
        /// El método incluye un bloque try-catch silencioso para asegurar que fallos en la red o URLs inválidas no interrumpan la ejecución de la aplicación.
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
        /// Intenta cargar y visualizar una imagen desde el almacenamiento local del sistema.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la carga de recursos gráficos locales mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación:</b> Comprueba la existencia física del archivo en la <paramref name="ruta"/> proporcionada.</item>
        /// <item><b>Decodificación:</b> Si el archivo existe, inicializa la propiedad <see cref="ImagenPortada"/> con un nuevo objeto <see cref="Bitmap"/>.</item>
        /// <item><b>Control de Estado:</b> Actualiza la propiedad booleana <see cref="TieneImagen"/> y notifica el cambio a la interfaz mediante <c>RaisePropertyChanged</c>.</item>
        /// </list>
        /// El método captura cualquier excepción durante la lectura para evitar interrupciones en la ejecución, asegurando que el estado de la UI se mantenga consistente.
        /// </remarks>
        /// <param name="ruta">La ruta absoluta del archivo de imagen en el disco local.</param>
        private void CargarImagenLocal(string ruta)
        {
            try
            {
                if (System.IO.File.Exists(ruta))
                {
                    ImagenPortada = new Bitmap(ruta);
                    TieneImagen = true;
                }
                else TieneImagen = false;
            }
            catch { TieneImagen = false; }
            this.RaisePropertyChanged(nameof(TieneImagen));
        }

        /// <summary>
        /// Añade el género seleccionado actualmente a la lista de géneros asociados, validando que no esté vacío y que no se haya añadido previamente.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la selección de etiquetas musicales mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación de entrada:</b> Verifica si <see cref="GeneroSeleccionado"/> contiene un valor válido y no nulo.</item>
        /// <item><b>Control de duplicados:</b> Comprueba si el género ya existe en <see cref="ListaGenerosSeleccionados"/> mediante una comparación insensible a mayúsculas.</item>
        /// <item><b>Actualización:</b> Si es un género nuevo, lo añade a la colección. En caso contrario, notifica al usuario a través de <see cref="_dialogoService"/>.</item>
        /// </list>
        /// Al finalizar, restablece la propiedad <see cref="GeneroSeleccionado"/> a nulo para limpiar el selector de la interfaz.
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
        /// Este método gestiona la edición de etiquetas musicales mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación:</b> Verifica si el género proporcionado existe dentro de la colección <see cref="ListaGenerosSeleccionados"/>.</item>
        /// <item><b>Remoción:</b> Si se encuentra la coincidencia, elimina el elemento de la lista.</item>
        /// <item><b>Sincronización:</b> La interfaz de usuario se actualiza automáticamente al ser una colección de tipo observable.</item>
        /// </list>
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
        /// Realiza una búsqueda asíncrona de usuarios en la base de datos basándose en el texto introducido, filtrando aquellos que ya han sido seleccionados.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la recuperación de perfiles mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación de servicio:</b> Verifica la disponibilidad del cliente de MongoDB a través de <see cref="MongoClientSingleton"/>.</item>
        /// <item><b>Consulta con exclusión:</b> Ejecuta la búsqueda utilizando <see cref="TxtBusqueda"/> y envía una lista de IDs de <see cref="ListaArtistas"/> para evitar resultados duplicados.</item>
        /// <item><b>Actualización de interfaz:</b> Si se obtienen resultados, inicializa <see cref="ListaResultados"/> con una nueva colección observable para refrescar la vista.</item>
        /// </list>
        /// </remarks>
        private async void BuscarUsuarios()
        {
            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var resultados = await MongoClientSingleton.Instance.Cliente.ObtenerUsuariosPorBusqueda(TxtBusqueda, ListaArtistas.Select(x => x.Id).ToList());
                if (resultados != null) ListaResultados = new ObservableCollection<Usuarios>(resultados);
            }
        }
        /// <summary>
        /// Añade un usuario a la lista de artistas seleccionados, evitando duplicados y limpiando los resultados de búsqueda actuales.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la selección de colaboradores mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación de existencia:</b> Verifica mediante el identificador único si el <paramref name="usuario"/> ya se encuentra en <see cref="ListaArtistas"/>.</item>
        /// <item><b>Actualización de colección:</b> Si el usuario no es un duplicado, se añade a la lista de artistas vinculados.</item>
        /// <item><b>Limpieza de interfaz:</b> Independientemente del resultado, se restablece <see cref="TxtBusqueda"/> y se vacía <see cref="ListaResultados"/> para preparar una nueva consulta.</item>
        /// </list>
        /// </remarks>
        /// <param name="usuario">El objeto de tipo <see cref="Usuarios"/> que se desea vincular a la canción o lista.</param>
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
        /// Este método gestiona la remoción de colaboradores mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación de identidad:</b> Comprueba si el <paramref name="usuario"/> a eliminar coincide con el ID del usuario actual en <see cref="GlobalData.Instance.UserIdGD"/>.</item>
        /// <item><b>Restricción de seguridad:</b> Si coinciden, se muestra una alerta de error mediante <see cref="_dialogoService"/> para impedir que el usuario se elimine a sí mismo.</item>
        /// <item><b>Actualización:</b> Si la validación es correcta y el usuario existe en la colección, se procede a removerlo de <see cref="ListaArtistas"/>.</item>
        /// </list>
        /// </remarks>
        /// <param name="usuario">El objeto de tipo <see cref="Usuarios"/> que se desea remover de la selección actual.</param>
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
        /// Procesa y persiste de forma asíncrona las modificaciones realizadas en una canción existente, gestionando la actualización de medios y metadatos.
        /// </summary>
        /// <remarks>
        /// Este método orquesta la actualización de la canción siguiendo un flujo transaccional:
        /// <list type="number">
        /// <item><b>Gestión de Imagen:</b> Detecta si la ruta de la imagen es local o remota. Si es local, procede a subir el nuevo archivo mediante <see cref="_storageService"/>.</item>
        /// <item><b>Sincronización remota:</b> Envía los nuevos títulos, IDs de colaboradores y géneros al cliente de MongoDB para actualizar el registro físico.</item>
        /// <item><b>Actualización de estado local:</b> Si la persistencia es exitosa, sincroniza los cambios en el objeto <c>_cancionOriginal</c> para asegurar la consistencia visual al regresar a la vista anterior.</item>
        /// </list>
        /// Durante el proceso, controla la propiedad <see cref="EstaCargando"/> para feedback visual y gestiona posibles excepciones mediante el servicio de diálogos.
        /// </remarks>
        /// <returns>Una tarea que representa la operación de guardado asíncrona.</returns>
        private async Task GuardarCambios()
        {
            EstaCargando = true;
            try
            {
                string urlPortadaFinal = RutaImagen;

                // Si la ruta NO es web (es decir, es un archivo local C:\Users\...), subimos la nueva.
                if (!RutaImagen.StartsWith("http"))
                {
                    urlPortadaFinal = await _storageService.SubirImagen(RutaImagen);
                }

                // Recogemos los datos nuevos 
                var nuevosAutoresIds = ListaArtistas.Select(u => u.Id).ToList();
                var nuevosGeneros = ListaGenerosSeleccionados.ToList();

                // Y los actualizamos a mongo
                bool exito = await MongoClientSingleton.Instance.Cliente.ActualizarCancion(
                    TxtTitulo,
                    urlPortadaFinal,
                    nuevosAutoresIds,
                    nuevosGeneros,
                    _cancionOriginal
                );

                if (exito)
                {
                    // Actualizamos el objeto original para que los cambios se reflejen al volver a la vista anterior
                    _cancionOriginal.Titulo = TxtTitulo;
                    _cancionOriginal.ImagenPortadaUrl = urlPortadaFinal;
                    _cancionOriginal.AutoresIds = nuevosAutoresIds;

                    if (_cancionOriginal.Datos == null) _cancionOriginal.Datos = new DatosCancion();
                    _cancionOriginal.Datos.Generos = nuevosGeneros;

                    // Actualizar nombre artista principal (display)
                    if (ListaArtistas.Count > 0)
                        _cancionOriginal.NombreArtista = ListaArtistas[0].Username;

                    _accionVolver();
                }
                else
                {
                    _dialogoService.MostrarAlerta("Msg_Error_GuardarCambios");
                }
            }
            catch (Exception ex)
            {
                _dialogoService.MostrarAlerta("Msg_Error_Inesperado");
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                EstaCargando = false;
            }
        }
    }
}