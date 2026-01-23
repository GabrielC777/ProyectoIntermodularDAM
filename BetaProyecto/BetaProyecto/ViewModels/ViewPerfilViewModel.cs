using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewPerfilViewModel : ViewModelBase
    {
        // ... (Propiedades NombreUsuario e ImagenPerfil se quedan igual) ...
        private string _nombreUsuario;
        public string NombreUsuario { get => _nombreUsuario; set => this.RaiseAndSetIfChanged(ref _nombreUsuario, value); }

        private string _imagenPerfil;
        public string ImagenPerfil { get => _imagenPerfil; set => this.RaiseAndSetIfChanged(ref _imagenPerfil, value); }

        // --- CAMBIO IMPORTANTE: Usamos una lista de Secciones ---
        private ObservableCollection<ListaUsuarios> _secciones;
        public ObservableCollection<ListaUsuarios> Secciones
        {
            get => _secciones;
            set => this.RaiseAndSetIfChanged(ref _secciones, value);
        }
        //Comandos Reactive 
        public ReactiveCommand<Unit, Unit> BtnRefrescar { get; }

        public ViewPerfilViewModel()
        {
            Secciones = new ObservableCollection<ListaUsuarios>();
            BtnRefrescar = ReactiveCommand.CreateFromTask(async () => await CargarDatos()); 
            _ = CargarDatos();
        }

        private async Task CargarDatos()
        {
            try
            {
                // Carga datos básicos
                NombreUsuario = GlobalData.Instance.usernameGD ?? "Usuario";
                ImagenPerfil = GlobalData.Instance.urlFotoPerfilGD ?? "https://i.ibb.co/dbQSrpB/Perfil.png";

                // Carga listas en paralelo
                var listaIds = GlobalData.Instance.seguidoresGD;
                var taskSeguidores = MongoClientSingleton.Instance.Cliente.ObtenerUsuariosPorListaIds(listaIds);
                var taskTodos = MongoClientSingleton.Instance.Cliente.ObtenerTodosLosUsuarios();

                await Task.WhenAll(taskSeguidores, taskTodos);

                // --- CONSTRUIMOS LAS SECCIONES ---
                var listaSecciones = new ObservableCollection<ListaUsuarios>();

                // Sección 1: Descubre Artistas
                listaSecciones.Add(new ListaUsuarios("Perfil_SecDescubre", new ObservableCollection<Usuarios>(taskTodos.Result)));

                // Sección 2: Siguiendo
                listaSecciones.Add(new ListaUsuarios("Perfil_SecSiguiendo",new ObservableCollection<Usuarios>(taskSeguidores.Result)));

                // Asignamos a la propiedad pública
                Secciones = listaSecciones;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error cargando perfil: " + ex.Message);
            }
        }
    }
}