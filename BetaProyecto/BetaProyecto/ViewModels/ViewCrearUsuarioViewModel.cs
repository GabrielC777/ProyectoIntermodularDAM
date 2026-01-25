using Avalonia.Media.Imaging;
using BetaProyecto.Helpers;
using BetaProyecto.Models;
using BetaProyecto.Services;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewCrearUsuarioViewModel : ViewModelBase
    {
        // Servicios
        private readonly IDialogoService _dialogoService;
        private readonly StorageService _storageService;
        private readonly Action _accionVolver; // Para volver al login

        // Bidings
        private Usuarios _nuevoUsuario;
        public Usuarios NuevoUsuario
        {
            get => _nuevoUsuario;
            set => this.RaiseAndSetIfChanged(ref _nuevoUsuario, value);
        }

        // Propiedad extra para validar password
        private string _confirmarPass;
        public string ConfirmarPass
        {
            get => _confirmarPass;
            set => this.RaiseAndSetIfChanged(ref _confirmarPass, value);
        }

        // Progress bar
        private bool _estaCargando;
        public bool EstaCargando
        {
            get => _estaCargando;
            set => this.RaiseAndSetIfChanged(ref _estaCargando, value);
        }

        private Bitmap? _fotoPerfilBitmap;
        public Bitmap? FotoPerfilBitmap
        {
            get => _fotoPerfilBitmap;
            set => this.RaiseAndSetIfChanged(ref _fotoPerfilBitmap, value);
        }

        // Comandos reactive
        public ReactiveCommand<Unit, Unit> BtnRegistrarse { get; }
        public ReactiveCommand<Unit, Unit> BtnVolver { get; }

        public ViewCrearUsuarioViewModel(Action accionVolver)
        {
            // Guardamos la acción de volver
            _accionVolver = accionVolver;

            // Inicializamos servicios
            _dialogoService = new DialogoService();
            _storageService = new StorageService();

            // Inicializamos el objeto vacío pero con sus estructuras listas
            NuevoUsuario = new Usuarios
            {
                Perfil = new PerfilUsuario(),
                Estadisticas = new EstadisticasUsuario(),
                Listas = new ListasUsuario()
            };

            // Configuramos comandos
            BtnRegistrarse = ReactiveCommand.CreateFromTask(RegistrarseTask);
            BtnVolver = ReactiveCommand.Create(() => _accionVolver?.Invoke());
        }

        private async Task RegistrarseTask()
        {
            if (EstaCargando) return;
            EstaCargando = true;

            try
            {
                // 1. VALIDACIONES BÁSICAS
                if (string.IsNullOrWhiteSpace(NuevoUsuario.Username) ||
                    string.IsNullOrWhiteSpace(NuevoUsuario.Email) ||
                    string.IsNullOrWhiteSpace(NuevoUsuario.Password) ||
                    string.IsNullOrWhiteSpace(ConfirmarPass) ||
                    string.IsNullOrWhiteSpace(NuevoUsuario.Perfil.Pais) ||
                    string.IsNullOrWhiteSpace(NuevoUsuario.Perfil.ImagenUrl)
                    )
                {
                    _dialogoService.MostrarAlerta("Reg_Error_FaltanCampos");
                    EstaCargando = false;
                    return;
                }

                // 2. VALIDAR CONTRASEÑAS COINCIDENTES
                if (NuevoUsuario.Password != ConfirmarPass)
                {
                    _dialogoService.MostrarAlerta("Reg_Error_PassNoCoinciden");
                    EstaCargando = false;
                    return;
                }

                // 3. CONEXIÓN A MONGO
                var cliente = MongoClientSingleton.Instance.Cliente;
                if (!await cliente.Conectar())
                {
                    _dialogoService.MostrarAlerta("Msg_Error_Conexion");
                    EstaCargando = false;
                    return;
                }

                // 4. PREPARAR DATOS
                NuevoUsuario.Rol = Roles.Usuario;
                NuevoUsuario.FechaRegistro = DateTime.Now;
                NuevoUsuario.Password = Encriptador.HashPassword(NuevoUsuario.Password);
                // 5. GESTIÓN DE FOTO DE PERFIL

                try
                {
                    // Subimos a Imgbb
                    NuevoUsuario.Perfil.ImagenUrl = await _storageService.SubirImagen(NuevoUsuario.Perfil.ImagenUrl);
                }
                catch
                {
                    _dialogoService.MostrarAlerta("Reg_Error_SubirImagen");
                    NuevoUsuario.Perfil.ImagenUrl = "";
                }

                // Si no puso imagen o falló, ponemos una por defecto
                if (string.IsNullOrEmpty(NuevoUsuario.Perfil.ImagenUrl))
                {
                    NuevoUsuario.Perfil.ImagenUrl = "https://i.ibb.co/hRJ440cz/image.png";
                }

                // 6. GUARDAR EN BD
                bool exito = await cliente.CrearUsuario(NuevoUsuario);

                if (exito)
                {
                    _dialogoService.MostrarAlerta("Reg_Exito_CuentaCreada");
                    _accionVolver?.Invoke(); // Volvemos al Login automáticamente
                }
                else
                {
                    _dialogoService.MostrarAlerta("Reg_Error_UsuarioExiste");
                }
            }
            catch (Exception ex)
            {
                _dialogoService.MostrarAlerta("Msg_Error_Inesperado");
            }
            finally
            {
                EstaCargando = false;
            }
        }
        public void CargarImagenPrevia(string ruta)
        {
            try
            {
                if (System.IO.File.Exists(ruta))
                {
                    // Cargamos el Bitmap desde el archivo
                    FotoPerfilBitmap = new Bitmap(ruta);

                    // Y guardamos la ruta en el modelo para subirla luego
                    NuevoUsuario.Perfil.ImagenUrl = ruta;
                }
            }
            catch (Exception)
            {
                // Si falla (no es imagen válida), ponemos null o una imagen por defecto
                FotoPerfilBitmap = null;
            }
        }
    }
}