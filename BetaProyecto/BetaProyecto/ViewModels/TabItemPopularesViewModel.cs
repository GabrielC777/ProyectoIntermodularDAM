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
        // LISTA DE GÉNEROS (Para llenar el ComboBox)
        private ObservableCollection<string> _listaGeneros;
        public ObservableCollection<string> ListaGeneros
        {
            get => _listaGeneros;
            set => this.RaiseAndSetIfChanged(ref _listaGeneros, value);
        }

        //GÉNERO SELECCIONADO (El disparador)
        private string _generoSeleccionado;
        public string GeneroSeleccionado
        {
            get => _generoSeleccionado;
            set
            {
                this.RaiseAndSetIfChanged(ref _generoSeleccionado, value);
                // ¡MAGIA! Al cambiar el valor, buscamos automáticamente ✨
                if (!string.IsNullOrEmpty(value))
                    _ = CargarCancionesPorGenero(value);
            }
        }

        //TEXTO INFORMATIVO (La clave del recurso, ej: "Pop_Res_Mostrando")
        private string _txtInfo;
        public string TxtInfo
        {
            get => _txtInfo;
            set => this.RaiseAndSetIfChanged(ref _txtInfo, value);
        }

        // NUEVO: Parte dinámica del texto (Ej: " Rock") para usar con Run
        private string _txtGeneroMostrado;
        public string TxtGeneroMostrado
        {
            get => _txtGeneroMostrado;
            set => this.RaiseAndSetIfChanged(ref _txtGeneroMostrado, value);
        }

        //LISTA DE CANCIONES (Resultado)
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

            _ = CargarGeneros();
        }

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