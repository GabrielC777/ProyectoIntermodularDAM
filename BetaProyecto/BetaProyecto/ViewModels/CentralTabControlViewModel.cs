using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;

namespace BetaProyecto.ViewModels
{
    public class CentralTabControlViewModel : ViewModelBase
    {
        // 1. LOS EMPLEADOS (Sub-ViewModels)
        public TabItemInicioViewModel InicioVM { get; }
        public TabItemBuscadorViewModel BuscadorVM { get; }
        public TabItemPopularesViewModel PopularesVM { get; }

        // 2. ACCIONES GLOBALES (Navegación y Reproductor)
        public Action? IrAPerfil { get; set; }
        public Action? IrACuenta { get; set; }
        public Action? IrAGestionarCuenta { get; set; }
        public Action? IrAConfig { get; set; }
        public Action? IrASobreNosotros { get; set; }
        public Action? IrAAyuda { get; set; }
        public Action? IrAPublicarCancion { get; set; }
        public Action? IrACrearPlaylist { get; set; }
        public Action<Canciones>? IrADetallesCancion { get; set; }
        public Action<string>? IrAVerArtista { get; set; }
        public Action<Canciones>? IrACrearReporte {  get; set; }
        public Action<ListaPersonalizada>? IrADetallesPlaylist { get; set; }
        public Action<Canciones, List<Canciones>> SolicitudCancion { get; set; }

        // 3. COMANDOS GLOBALES
        public ReactiveCommand<Unit, Unit> BtnPerfil { get; }
        public ReactiveCommand<Unit, Unit> BtnCuenta { get; }
        public ReactiveCommand<Unit, Unit> BtnGestionarCuenta { get; }
        public ReactiveCommand<Unit, Unit> BtnConfiguracion { get; }
        public ReactiveCommand<Unit, Unit> BtnSobreNosotros { get; }
        public ReactiveCommand<Unit, Unit> BtnAyuda { get; }
        public ReactiveCommand<Unit,Unit> BtnPublicarCancion { get; }
        public ReactiveCommand<Unit, Unit> BtnCrearPlaylist { get; set; }

        // El botón de Play sigue aquí porque el reproductor es Global
        public ReactiveCommand<Canciones, Unit> BtnReproducir { get; }

        // Propiedades Globales
        private string _imagenPerfil;
        public string ImagenPerfil
        {
            get => _imagenPerfil;
            set => this.RaiseAndSetIfChanged(ref _imagenPerfil, value);
        }

        public CentralTabControlViewModel()
        {
            // A. CONTRATAMOS AL EMPLEADO
            InicioVM= new TabItemInicioViewModel();

            InicioVM.EnviarReproduccion = (cancion, lista) =>
            {
                SolicitudCancion?.Invoke(cancion, lista);
            };

            InicioVM.SolicitudVerDetalles = (cancion) =>
            {
                IrADetallesCancion?.Invoke(cancion);
            };

            InicioVM.SolicitudVerArtista = (idUsuario) =>
            {
                IrAVerArtista?.Invoke(idUsuario);
            };

            InicioVM.SolicitudCrearReporte = (cancion) =>
            {
                IrACrearReporte?.Invoke(cancion);
            };
            InicioVM.SolicitudVerDetallasPlaylist = (playlist) =>
            {
                IrADetallesPlaylist?.Invoke(playlist);
            };

            BuscadorVM = new TabItemBuscadorViewModel();
            PopularesVM = new TabItemPopularesViewModel();

            // B. DATOS GLOBALES
            ImagenPerfil = GlobalData.Instance.UrlFotoPerfilGD;

            // CONFIGURAR COMANDOS DE NAVEGACIÓN
            BtnPerfil = ReactiveCommand.Create(() => { 
                Debug.WriteLine("Pulsado Perfil"); 
                IrAPerfil?.Invoke(); 
            });
            BtnCuenta = ReactiveCommand.Create(() => { 
                Debug.WriteLine("Pulsado Cuenta"); 
                IrACuenta?.Invoke(); 
            });
            BtnGestionarCuenta = ReactiveCommand.Create(() => {
                Debug.WriteLine("Pulsado Gestionar Contenido");
                IrAGestionarCuenta?.Invoke();
            });
            BtnConfiguracion = ReactiveCommand.Create(() => { 
                Debug.WriteLine("Pulsado Configuración"); 
                IrAConfig?.Invoke(); 
            });
            BtnSobreNosotros = ReactiveCommand.Create(() => {
                Debug.WriteLine("Pulsado Sobre nosotros");
                IrASobreNosotros?.Invoke(); 
            });
            BtnAyuda = ReactiveCommand.Create(() => {
                Debug.WriteLine("Pulsado Ayuda");
                IrAAyuda?.Invoke(); 
            });
            BtnPublicarCancion = ReactiveCommand.Create(() =>
            {
                Debug.WriteLine("Pulsado Publicar Canción");
                IrAPublicarCancion?.Invoke();
            });
            BtnCrearPlaylist = ReactiveCommand.Create(() =>
            {
                Debug.WriteLine("Pulsado Crear Lista");
                IrACrearPlaylist?.Invoke();
            });


            //Reproducción individual
            BtnReproducir = ReactiveCommand.Create<Canciones>((cancion) =>
            {
                if (cancion != null)
                {
                    Debug.WriteLine($"[PLAY] Solicitando reproducir: {cancion.Titulo}");
                    SolicitudCancion?.Invoke(cancion,null);
                }
            });
        }
    }
}