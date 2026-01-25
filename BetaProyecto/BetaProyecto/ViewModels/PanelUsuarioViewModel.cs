using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class PanelUsuarioViewModel : ViewModelBase, INavegable
    {
        //Sub-ViewModels
        public ViewPerfilViewModel ViewPerfilVM { get; }
        public ViewCuentaViewModel ViewCuentaVM { get; }
        public ViewGestionarCuentaViewModel ViewGestionarCuentaVM { get; }
        public ViewConfiguracionViewModel ViewConfiguracionVM { get; }
        public ViewGestionarReportesViewModel ViewGestionarReportesVM { get; }
        public ViewGestionarBDViewModel ViewGestionarBDVM { get; }

        //Action para conectar con el marco
        public Action VolverAtras { get; set; }
        public Action AccionLogout { get; set; }
        public Action AccionSalir { get; set; }
        public Action<ListaPersonalizada> IrAEditarPlaylist { get; set;} 
        public Action<Canciones> IrAEditarCancion { get; set; }
        public Action? AccionRefrescarDesdePadre { get; set; }

        // 0 = Perfil, 1 = Cuenta, 2 = GestionarCuenta, 3 = Configuración
        private int _indiceTab;
        public int IndiceTab
        {
            get => _indiceTab;
            set => this.RaiseAndSetIfChanged(ref _indiceTab, value);
        }

        // Solo para SuperAdmin
        private bool _puedeVerBD;
        public bool PuedeVerBD
        {
            get => _puedeVerBD;
            set => this.RaiseAndSetIfChanged(ref _puedeVerBD, value);
        }

        // Para Admin y SuperAdmin
        private bool _puedeVerReportes;
        public bool PuedeVerReportes
        {
            get => _puedeVerReportes;
            set => this.RaiseAndSetIfChanged(ref _puedeVerReportes, value);
        }
        // CONSTRUCTOR: Le pasamos el índice inicial (por defecto 0 si no decimos nada)
        public PanelUsuarioViewModel(int tabInicial = 0)
        {
            IndiceTab = tabInicial;

            ViewPerfilVM = new ViewPerfilViewModel();
            ViewCuentaVM = new ViewCuentaViewModel();
            ViewGestionarCuentaVM = new ViewGestionarCuentaViewModel();

            ViewGestionarCuentaVM.SolicitudIrAEditarPlaylist = (playlist) =>
            {
                IrAEditarPlaylist?.Invoke(playlist);
            };

            ViewGestionarCuentaVM.SolicitudIrAEditarCanciones = (canciones) =>
            {
                IrAEditarCancion?.Invoke(canciones);
            };

            ViewConfiguracionVM = new ViewConfiguracionViewModel(
                            accionVolver: () => VolverAtras?.Invoke(),
                            accionLogout: () => AccionLogout?.Invoke(),
                            accionSalir: () => AccionSalir?.Invoke(),
                            accionRefrescar: () => AccionRefrescarDesdePadre?.Invoke()
            );
            ConfigurarPermisos();

            ViewGestionarReportesVM = new ViewGestionarReportesViewModel();
            ViewGestionarBDVM = new ViewGestionarBDViewModel();
        }
        private void ConfigurarPermisos()
        {
            string rolActual = GlobalData.Instance.RolGD;

            // Gestión de Base de Datos -> SOLO SuperAdmin
            PuedeVerBD = (rolActual == Roles.SuperAdmin);

            // Gestión de Reportes -> SuperAdmin O Admin
            PuedeVerReportes = (rolActual == Roles.SuperAdmin || rolActual == Roles.Admin);
        }
    }
}
