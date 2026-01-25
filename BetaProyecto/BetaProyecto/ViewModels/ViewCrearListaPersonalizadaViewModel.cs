using Avalonia.Media.Imaging;
using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewCrearListaPersonalizadaViewModel : ViewModelBase
    {
        //Servicios
        private readonly IDialogoService _dialogoService;
        private readonly StorageService _storageService;
       
        
        private readonly Action _Volver;

        // --- DATOS DE LA LISTA ---
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

        // --- PORTADA ---
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

        // --- BUSCADOR DE CANCIONES ---
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

            BtnCrear = ReactiveCommand.CreateFromTask(CrearLista, validacionCrear);
        }

        // --- LÓGICA ---

        private async void BuscarCanciones()
        {
            if (MongoClientSingleton.Instance.Cliente != null)
            {
                // NOTA: Necesitarás implementar este método en MongoAtlas.cs si no existe
                // Es igual que buscar usuarios pero buscando canciones por título
                var resultados = await MongoClientSingleton.Instance.Cliente.ObtenerCancionesPorBusqueda(TxtBusqueda);

                if (resultados != null)
                {
                    // Filtramos las que ya están seleccionadas para no duplicar visualmente
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
                ListaResultados.Remove(cancion); // La quitamos de resultados para que quede limpio
                TxtBusqueda = "";
            }
        }

        private void EliminarCancion(Canciones cancion)
        {
            ListaCancionesSeleccionadas.Remove(cancion);
        }

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

        private async Task CrearLista()
        {
            EstaCargando = true;
            try
            {
                // 1. Subir Imagen
                string urlPortada = await _storageService.SubirImagen(RutaImagen);

                // 2. Crear Objeto
                var nuevaLista = new ListaPersonalizada
                {
                    Nombre = TxtNombre,
                    Descripcion = TxtDescripcion,
                    UrlPortada = urlPortada,
                    IdUsuario = GlobalData.Instance.UserIdGD,
                    IdsCanciones = ListaCancionesSeleccionadas.Select(c => c.Id).ToList()
                };

                // 3. Guardar en BD (Necesitas crear este método en MongoAtlas.cs)
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
