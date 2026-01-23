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
        private readonly IDialogoService _dialogoService;

        // --- LISTAS ---
        public ObservableCollection<Reportes> ListaPendientes { get; } 
        public ObservableCollection<Reportes> ListaInvestigando { get; } 
        public ObservableCollection<Reportes> ListaFinalizados { get; } 

        // --- EL JEFE (Lo que ve el formulario) ---
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

        // --- LOS SUBORDINADOS (Selecciones individuales para que no se peleen) ---

        // 1. Selección Pendientes
        private Reportes _selPendiente;
        public Reportes SelectedPendiente
        {
            get => _selPendiente;
            set
            {
                this.RaiseAndSetIfChanged(ref _selPendiente, value);
                if (value != null)
                {
                    ReporteSeleccionado = value;   // Avisamos al Jefe
                    SelectedInvestigando = null;   // Limpiamos las otras listas
                    SelectedFinalizado = null;
                }
            }
        }

        // 2. Selección Investigando
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

        // 3. Selección Finalizados
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

        // --- RESTO IGUAL QUE ANTES ---
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

        public ObservableCollection<string> OpcionesEstado { get; } = new()
        { "Pendiente", "Investigando", "Finalizado" };

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

        private async Task GuardarCambios()
        {
            if (ReporteSeleccionado == null) return;

            bool exito = await MongoClientSingleton.Instance.Cliente.ActualizarEstadoReporte(
                EstadoEdit, ResolucionEdit, ReporteSeleccionado
            );

            if (exito)
            {
                _dialogoService.MostrarAlerta("Reporte actualizado.");
                await CargarDatos();
            }
            else
            {
                _dialogoService.MostrarAlerta("Sin cambios o error.");
            }
        }
    }
}