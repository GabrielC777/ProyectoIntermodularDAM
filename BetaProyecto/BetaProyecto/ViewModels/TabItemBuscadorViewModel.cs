using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
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
            ListaBusqueda = new ObservableCollection<Canciones>();

            var validacionBuscar = this.WhenAnyValue(
                x => x.TxtBusqueda,
                (textoABuscar) => !string.IsNullOrWhiteSpace(textoABuscar)
            );
            BtnBuscar = ReactiveCommand.CreateFromTask(BuscarEnBD, validacionBuscar);
        }
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