using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;

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

        //Actions 
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
        // Constructor
        // Le pasamos el índice inicial (por defecto 0 si no decimos nada) para la pestaña 
        public PanelUsuarioViewModel(int tabInicial = 0)
        {
            IndiceTab = tabInicial;

            //Inicializamos los sub-ViewModels
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
        /// <summary>
        /// Evalúa y establece los privilegios de acceso del usuario a las funciones administrativas del panel basándose en su rol.
        /// </summary>
        /// <remarks>
        /// Este método consulta el rol actual desde <see cref="GlobalData.Instance.RolGD"/> y actualiza las propiedades 
        /// de visibilidad de la interfaz. La gestión de base de datos se restringe exclusivamente al rol <see cref="Roles.SuperAdmin"/>, 
        /// mientras que el acceso a reportes se habilita tanto para administradores como para superadministradores.
        /// </remarks>
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
