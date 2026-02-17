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
        //Bidings 
        private string _nombreUsuario;
        public string NombreUsuario { get => _nombreUsuario; set => this.RaiseAndSetIfChanged(ref _nombreUsuario, value); }

        private string _imagenPerfil;
        public string ImagenPerfil { get => _imagenPerfil; set => this.RaiseAndSetIfChanged(ref _imagenPerfil, value); }

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
            //Inicializamos listas
            Secciones = new ObservableCollection<ListaUsuarios>();
            
            //Configuramos comandos reactive 
            BtnRefrescar = ReactiveCommand.CreateFromTask(async () => await CargarDatos()); 
            
            _ = CargarDatos(); // Cargamos los datos en segundo plano
        }
        /// <summary>
        /// Recupera y organiza de forma asíncrona la información del perfil, artistas sugeridos y usuarios seguidos.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la carga de la red social del usuario mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Inicialización:</b> Carga la identidad básica (nombre y foto) desde <see cref="GlobalData.Instance"/>.</item>
        /// <item><b>Carga Paralela:</b> Ejecuta simultáneamente las peticiones a MongoDB para obtener los perfiles seguidos y el catálogo global de usuarios mediante <see cref="Task.WhenAll"/>.</item>
        /// <item><b>Categorización:</b> Estructura los resultados en secciones diferenciadas ("Descubre Artistas" y "Siguiendo") utilizando claves de traducción para los encabezados.</item>
        /// <item><b>Asignación:</b> Actualiza la propiedad <see cref="Secciones"/>, lo que dispara la actualización de los controles agrupados en la interfaz.</item>
        /// </list>
        /// Cualquier fallo durante la consulta se registra en la consola de depuración para evitar el colapso de la vista.
        /// </remarks>
        /// <returns>Una tarea que representa la operación de carga y estructuración asíncrona.</returns>
        private async Task CargarDatos()
        {
            try
            {
                // Carga datos básicos
                NombreUsuario = GlobalData.Instance.UsernameGD ?? "Usuario";
                ImagenPerfil = GlobalData.Instance.UrlFotoPerfilGD ?? "https://i.ibb.co/dbQSrpB/Perfil.png";

                // Carga listas en paralelo
                var listaIds = GlobalData.Instance.SeguidoresGD;
                var taskSeguidores = MongoClientSingleton.Instance.Cliente.ObtenerUsuariosPorListaIds(listaIds);
                var taskTodos = MongoClientSingleton.Instance.Cliente.ObtenerTodosLosUsuarios();

                await Task.WhenAll(taskSeguidores, taskTodos);

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