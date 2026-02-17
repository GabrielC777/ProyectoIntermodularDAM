using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class TabItemBuscadorViewModel : ViewModelBase
    {
        //Bidings 
        private string _txtBusqueda;
        public string TxtBusqueda
        {
            get => _txtBusqueda;
            set => this.RaiseAndSetIfChanged(ref _txtBusqueda, value);
        }
        private string _txtInfoResultado;
        public string TxtInfoResultado
        {
            get => _txtInfoResultado;
            set => this.RaiseAndSetIfChanged(ref _txtInfoResultado, value);
        }

        // Propiedad extra para el número (para usar con Run en XAML)
        private string _txtContador;
        public string TxtContador
        {
            get => _txtContador;
            set => this.RaiseAndSetIfChanged(ref _txtContador, value);
        }

        private ObservableCollection<Canciones> _listaBusqueda;
        public ObservableCollection<Canciones> ListaBusqueda
        {
            get => _listaBusqueda;
            set => this.RaiseAndSetIfChanged(ref _listaBusqueda, value);
        }

        //Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnBuscar { get; }

        public TabItemBuscadorViewModel()
        {
            //Inicialización de lista
            ListaBusqueda = new ObservableCollection<Canciones>();

            // Validación para habilitar el botón de búsqueda solo cuando haya texto
            var validacionBuscar = this.WhenAnyValue(
                x => x.TxtBusqueda,
                (textoABuscar) => !string.IsNullOrWhiteSpace(textoABuscar)
            );
            //Configuramos comandos reactive
            BtnBuscar = ReactiveCommand.CreateFromTask(BuscarEnBD, validacionBuscar);
        }
        
        /// <summary>
        /// Realiza una búsqueda asíncrona de canciones en la base de datos utilizando el texto de búsqueda actual y actualiza el
        /// resultados de búsqueda y propiedades de estado relacionadas.
        /// </summary>
        /// <remarks>Si la conexión a la base de datos no está disponible, el método actualiza las propiedades de estado
        /// para indicar un error de conexión. Los resultados de búsqueda y las propiedades de estado se actualizan en función de si hay alguno
        /// las canciones coincidentes se encuentran. </remarks>
        /// <returns>Devuelve una tarea que representa la operación de búsqueda asíncrona. </returns>
        private async Task BuscarEnBD()
        {
            TxtInfoResultado = "Bus_Msg_Buscando";
            TxtContador = "";

            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var listaResultadosBusqueda = await MongoClientSingleton.Instance.Cliente.ObtenerCancionesPorBusqueda(TxtBusqueda);

                if (listaResultadosBusqueda != null && listaResultadosBusqueda.Count > 0)
                {
                    ListaBusqueda = new ObservableCollection<Canciones>(listaResultadosBusqueda);
                    TxtInfoResultado = "Bus_Res_Encontrados";
                    TxtContador = $" {listaResultadosBusqueda.Count}";
                }
                else
                {
                    ListaBusqueda.Clear();
                    TxtInfoResultado = "Bus_Res_SinResultados";
                    TxtContador = "";
                }
            }
            else
            {
                TxtInfoResultado = "Msg_Error_Conexion";
                TxtContador = "";
            }
        }
    }
}