using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewGestionarReportesViewModel : ViewModelBase
    {
        //Servicios
        private readonly IDialogoService _dialogoService;

        // Lista
        public ObservableCollection<Reportes> ListaPendientes { get; } 
        public ObservableCollection<Reportes> ListaInvestigando { get; } 
        public ObservableCollection<Reportes> ListaFinalizados { get; }
        public ObservableCollection<string> OpcionesEstado { get; } = new()
        { "Pendiente", "Investigando", "Finalizado" };

        //Bindings
        private Reportes _reporteSeleccionado;
        public Reportes ReporteSeleccionado
        {
            get => _reporteSeleccionado;
            set
            {
                this.RaiseAndSetIfChanged(ref _reporteSeleccionado, value);
                if (value != null)
                {
                    EstadoEdit = value.Estado;
                    ResolucionEdit = value.Resolucion;
                }
            }
        }
        //Hacemos selecciones individuales para cada lista para evitar conflictos al seleccionar en una y que se marque en las otras
        // Selección Pendientes
        private Reportes _selPendiente;
        public Reportes SelectedPendiente
        {
            get => _selPendiente;
            set
            {
                this.RaiseAndSetIfChanged(ref _selPendiente, value);
                if (value != null)
                {
                    ReporteSeleccionado = value;
                    SelectedInvestigando = null;   
                    SelectedFinalizado = null;
                }
            }
        }

        // Selección Investigando
        private Reportes _selInvestigando;
        public Reportes SelectedInvestigando
        {
            get => _selInvestigando;
            set
            {
                this.RaiseAndSetIfChanged(ref _selInvestigando, value);
                if (value != null)
                {
                    ReporteSeleccionado = value;
                    SelectedPendiente = null;
                    SelectedFinalizado = null;
                }
            }
        }

        // Selección Finalizados
        private Reportes _selFinalizado;
        public Reportes SelectedFinalizado
        {
            get => _selFinalizado;
            set
            {
                this.RaiseAndSetIfChanged(ref _selFinalizado, value);
                if (value != null)
                {
                    ReporteSeleccionado = value;
                    SelectedPendiente = null;
                    SelectedInvestigando = null;
                }
            }
        }

        private string _estadoEdit;
        public string EstadoEdit
        {
            get => _estadoEdit;
            set => this.RaiseAndSetIfChanged(ref _estadoEdit, value);
        }

        private string _resolucionEdit;
        public string ResolucionEdit
        {
            get => _resolucionEdit;
            set => this.RaiseAndSetIfChanged(ref _resolucionEdit, value);
        }

        //Comandos reactive
        public ReactiveCommand<Unit, Unit> BtnRefrescar { get; }
        public ReactiveCommand<Unit, Unit> BtnGuardar { get; }

        public ViewGestionarReportesViewModel()
        {
            //Inicializamos servicios 
            _dialogoService = new DialogoService();
            // Inicializamos listas
            ListaPendientes = new ObservableCollection<Reportes>();
            ListaInvestigando = new ObservableCollection<Reportes>();
            ListaFinalizados = new ObservableCollection<Reportes>();
            
            // Configuramos comandos
            BtnRefrescar = ReactiveCommand.CreateFromTask(CargarDatos);
            BtnGuardar = ReactiveCommand.CreateFromTask(GuardarCambios);
            _ = CargarDatos();
        }
        /// <summary>
        /// Recupera todos los reportes de la base de datos y los clasifica en colecciones independientes según su estado actual.
        /// </summary>
        /// <remarks>
        /// Este método realiza una limpieza integral de las listas y selecciones actuales para evitar duplicidad visual. 
        /// Posteriormente, consulta MongoDB y distribuye cada reporte en las categorías de "Pendiente", "Investigando" 
        /// o "Finalizado" basándose en el valor de su propiedad <c>Estado</c>, facilitando la organización por columnas en la interfaz.
        /// </remarks>
        /// <returns>Una tarea que representa la operación de carga y clasificación asíncrona.</returns>
        private async Task CargarDatos()
        {
            // Limpiamos todo
            ListaPendientes.Clear();
            ListaInvestigando.Clear();
            ListaFinalizados.Clear();

            // Limpiamos selecciones
            SelectedPendiente = null;
            SelectedInvestigando = null;
            SelectedFinalizado = null;
            ReporteSeleccionado = null;

            var todos = await MongoClientSingleton.Instance.Cliente.ObtenerReportes();

            foreach (var r in todos)
            {
                switch (r.Estado?.Trim())
                {
                    case "Pendiente": ListaPendientes.Add(r); break;
                    case "Investigando": ListaInvestigando.Add(r); break;
                    case "Finalizado": ListaFinalizados.Add(r); break;
                    default: ListaPendientes.Add(r); break;
                }
            }
        }
        /// <summary>
        /// Persiste de forma asíncrona las modificaciones realizadas en el estado y la resolución del reporte seleccionado.
        /// </summary>
        /// <remarks>
        /// Este método sincroniza los cambios con la base de datos MongoDB mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Validación:</b> Verifica que exista una instancia válida en <see cref="ReporteSeleccionado"/>.</item>
        /// <item><b>Sincronización:</b> Envía los nuevos valores de <c>EstadoEdit</c> y <c>ResolucionEdit</c> al servidor.</item>
        /// <item><b>Refresco:</b> Si la operación es exitosa, notifica al usuario y reejecuta <see cref="CargarDatos"/> para reorganizar los reportes en sus respectivas columnas visuales.</item>
        /// </list>
        /// </remarks>
        /// <returns>Una tarea que representa la operación de actualización asíncrona.</returns>
        private async Task GuardarCambios()
        {
            if (ReporteSeleccionado == null) return;

            bool exito = await MongoClientSingleton.Instance.Cliente.ActualizarEstadoReporte(
                EstadoEdit, ResolucionEdit, ReporteSeleccionado
            );

            if (exito)
            {
                _dialogoService.MostrarAlerta("Reportes_MsgActualizado");
                await CargarDatos();
            }
            else
            {
                _dialogoService.MostrarAlerta("Reportes_MsgSinCambios");
            }
        }
    }
}