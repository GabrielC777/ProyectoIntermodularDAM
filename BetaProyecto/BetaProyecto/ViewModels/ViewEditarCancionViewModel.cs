using Avalonia.Media.Imaging;
using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.Generic;
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

        // Para progress bar
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

            // 1. Cargar datos originales
            TxtTitulo = cancion.Titulo;
            RutaImagen = cancion.ImagenPortadaUrl;
            ListaGenerosSeleccionados = new ObservableCollection<string>(cancion.Datos.Generos);

            // Cargar imagen visualmente
            _ = CargarImagenDesdeUrl(cancion.ImagenPortadaUrl);

            // 2. Comandos
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

            // 3. Cargas asíncronas iniciales
            _ = CargarGenerosDisponibles();
            _ = CargarColaboradoresOriginales(); // Importante: Recuperar los objetos Usuario de los IDs
        }

        // --- LÓGICA DE CARGA ---

        private async Task CargarGenerosDisponibles()
        {
            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var generos = await MongoClientSingleton.Instance.Cliente.ObtenerNombresGeneros();
                ListaGeneros = new ObservableCollection<string>(generos);
            }
        }

        private async Task CargarColaboradoresOriginales()
        {
            // Necesitamos convertir la lista de IDs (_cancionOriginal.AutoresIds) en objetos Usuarios
            if (_cancionOriginal.AutoresIds != null && _cancionOriginal.AutoresIds.Count > 0)
            {
                // Asumiendo que tienes un método en MongoAtlas para obtener usuarios por lista de IDs
                var usuarios = await MongoClientSingleton.Instance.Cliente.ObtenerUsuariosPorListaIds(_cancionOriginal.AutoresIds);

                if (usuarios != null)
                    ListaArtistas = new ObservableCollection<Usuarios>(usuarios);
            }
        }

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
            catch { }
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
                else TieneImagen = false;
            }
            catch { TieneImagen = false; }
            this.RaisePropertyChanged(nameof(TieneImagen));
        }

        // --- LÓGICA DE EDICIÓN ---

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

        private async void BuscarUsuarios()
        {
            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var resultados = await MongoClientSingleton.Instance.Cliente.ObtenerUsuariosPorBusqueda(TxtBusqueda, ListaArtistas.Select(x => x.Id).ToList());
                if (resultados != null) ListaResultados = new ObservableCollection<Usuarios>(resultados);
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


        // --- GUARDAR ---

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

                // 2. RECOPILAR DATOS NUEVOS
                var nuevosAutoresIds = ListaArtistas.Select(u => u.Id).ToList();
                var nuevosGeneros = ListaGenerosSeleccionados.ToList();

                // 3. LLAMADA A MONGO
                bool exito = await MongoClientSingleton.Instance.Cliente.ActualizarCancion(
                    TxtTitulo,
                    urlPortadaFinal,
                    nuevosAutoresIds,
                    nuevosGeneros,
                    _cancionOriginal
                );

                if (exito)
                {
                    // 4. ACTUALIZAR OBJETO EN MEMORIA (Para que se refresque la UI inmediatamente)
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