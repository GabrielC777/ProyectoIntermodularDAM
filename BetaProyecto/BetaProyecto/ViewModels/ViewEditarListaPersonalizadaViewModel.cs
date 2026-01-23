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

        // --- LÓGICA DE IMÁGENES ---

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
            catch { /* Si falla la carga visual no pasa nada grave */ }
        }

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

        private void AgregarCancion(Canciones cancion)
        {
            if (!ListaCancionesSeleccionadas.Any(c => c.Id == cancion.Id))
            {
                ListaCancionesSeleccionadas.Add(cancion);
                ListaResultados.Remove(cancion);
                TxtBusqueda = "";
            }
        }

        private void EliminarCancion(Canciones cancion)
        {
            ListaCancionesSeleccionadas.Remove(cancion);
        }

        private async Task GuardarCambios()
        {
            EstaCargando = true;
            try
            {
                string urlPortadaFinal = RutaImagen;

                // 1. ¿Ha cambiado la imagen? (Si es ruta local, subimos. Si es http, mantenemos)
                if (!RutaImagen.StartsWith("http"))
                {
                    urlPortadaFinal = await _storageService.SubirImagen(RutaImagen);
                }

                // 2. Extraer IDs de canciones
                var nuevosIds = ListaCancionesSeleccionadas.Select(c => c.Id).ToList();

                // 3. Actualizar en BD
                // (Asegúrate de que este método existe en tu MongoAtlas.cs o úsalo como te pasé antes)
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