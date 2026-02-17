using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class TabItemPopularesViewModel : ViewModelBase
    {

        //Binding
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
            set
            {
                this.RaiseAndSetIfChanged(ref _generoSeleccionado, value);
                // Al cambiar el valor, buscamos automáticamente 
                if (!string.IsNullOrEmpty(value))
                    _ = CargarCancionesPorGenero(value);
            }
        }

        private string _txtInfo;
        public string TxtInfo
        {
            get => _txtInfo;
            set => this.RaiseAndSetIfChanged(ref _txtInfo, value);
        }

        private string _txtGeneroMostrado;
        public string TxtGeneroMostrado
        {
            get => _txtGeneroMostrado;
            set => this.RaiseAndSetIfChanged(ref _txtGeneroMostrado, value);
        }

        //Lista de canciones
        private ObservableCollection<Canciones> _listaPopulares;
        public ObservableCollection<Canciones> ListaPopulares
        {
            get => _listaPopulares;
            set => this.RaiseAndSetIfChanged(ref _listaPopulares, value);
        }

        public TabItemPopularesViewModel()
        {
            // Inicializamos listas
            ListaGeneros = new ObservableCollection<string>();
            ListaPopulares = new ObservableCollection<Canciones>();

            // Dejamos esto vacío al inicio o con una clave de "Seleccione..."
            TxtInfo = "";
            TxtGeneroMostrado = "";

            // Ejecutamos tarea en segndo plano para cargar géneros
            _ = CargarGeneros();
        }

        /// <summary>
        /// Carga asincrónicamente la lista de canciones populares para el género especificado y actualiza la pantalla relacionada
        /// propiedades.
        /// </summary>
        /// <remarks>Si no se encuentran canciones para el género especificado, las propiedades de visualización se actualizan a
        /// indica que no se encontraron resultados. Si la conexión a la base de datos no está disponible, se establece un mensaje de error
        /// en su lugar. </remarks>
        /// <param name="genero">El nombre del género para el que se recuperarán las canciones populares. No puede ser nulo ni vacío. </param>
        /// <returns>Devuelve una tarea que representa la operación de carga asíncrona. </returns>
        private async Task CargarCancionesPorGenero(string genero)
        {
            // "Buscando..."
            TxtInfo = "Bus_Msg_Buscando";
            TxtGeneroMostrado = "";
            ListaPopulares.Clear();

            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var resultados = await MongoClientSingleton.Instance.Cliente.ObtenerMixPorGenero(genero);

                if (resultados != null && resultados.Count > 0)
                {
                    ListaPopulares = new ObservableCollection<Canciones>(resultados);

                    // "Mostrando resultados de:" + " Rock"
                    TxtInfo = "Pop_Res_Mostrando";
                    TxtGeneroMostrado = $" {genero}";
                }
                else
                {
                    // "No se encontraron coincidencias."
                    TxtInfo = "Bus_Res_SinResultados";
                    TxtGeneroMostrado = "";
                }
            }
            else
            {
                TxtInfo = "Msg_Error_Conexion";
            }
        }

        /// <summary>
        /// Carga de forma asíncrona la lista de nombres de género desde la base de datos y actualiza la colección utilizada por la vista.
        /// </summary>
        /// <remarks>Si la conexión a la base de datos no está disponible, el método escribe un mensaje de error en el
        /// salida de depuración y no se actualiza la lista de géneros. </remarks>
        /// <returns>Devuelve una tarea que representa la operación de carga asíncrona. </returns>
        private async Task CargarGeneros()
        {
            if (MongoClientSingleton.Instance.Cliente != null)
            {
                var listadeGeneros = await MongoClientSingleton.Instance.Cliente.ObtenerNombresGeneros();
                ListaGeneros = new ObservableCollection<string>(listadeGeneros);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Error en la conexion de la base de datos");
            }
        }
    }
}