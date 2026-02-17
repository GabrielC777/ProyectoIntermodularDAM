using BetaProyecto.Services;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewCuentaViewModel : ViewModelBase
    {
        //Servicios 
        private readonly IDialogoService _dialogoService;

        //Bidings 
        private string _nombreUsuario;
        public string NombreUsuario
        {
            get => _nombreUsuario;
            set => this.RaiseAndSetIfChanged(ref _nombreUsuario, value);
        }

        private string _email;
        public string Email
        {
            get => _email;
            set => this.RaiseAndSetIfChanged(ref _email, value);
        }

        private string _pais;
        public string Pais
        {
            get => _pais;
            set => this.RaiseAndSetIfChanged(ref _pais, value);
        }

        // El DatePicker de Avalonia usa DateTimeOffset?
        private DateTimeOffset? _fechaNacimiento;
        public DateTimeOffset? FechaNacimiento
        {
            get => _fechaNacimiento;
            set => this.RaiseAndSetIfChanged(ref _fechaNacimiento, value);
        }

        // 0 = Privada, 1 = Pública
        private int _indexPrivacidad;
        public int IndexPrivacidad
        {
            get => _indexPrivacidad;
            set => this.RaiseAndSetIfChanged(ref _indexPrivacidad, value);
        }

        // Comandos Reactive 
        public ReactiveCommand<Unit, Unit> BtnGuardar { get; }
        public ReactiveCommand<Unit, Unit> BtnRefrescar { get; }

        public ViewCuentaViewModel()
        {
            // Incializamos servicios
            _dialogoService = new DialogoService();

            // Configuramos comandos
            BtnGuardar = ReactiveCommand.CreateFromTask(GuardarCambios);
            BtnRefrescar = ReactiveCommand.Create(CargarDatos); 

            // Cargamos datos 
            CargarDatos();
        }
        /// <summary>
        /// Recupera y sincroniza la información del perfil del usuario desde los datos globales para su edición en la interfaz.
        /// </summary>
        /// <remarks>
        /// Este método actúa como un mapeador entre <see cref="GlobalData.Instance"/> y las propiedades vinculadas de la vista. 
        /// Realiza conversiones de tipos necesarias, como la transformación de <see cref="DateTime"/> a <see cref="DateTimeOffset"/> 
        /// para el selector de fecha, y traduce el estado booleano de privacidad a un índice numérico compatible con 
        /// los controles de selección de la UI.
        /// </remarks>
        private void CargarDatos()
        {
            // Leemos del Singleton (GlobalData)
            NombreUsuario = GlobalData.Instance.UsernameGD;
            Email = GlobalData.Instance.EmailGD;
            Pais = GlobalData.Instance.PaisGD;

            // Conversión de Fechas
            if (GlobalData.Instance.FechaNacimientoGD != DateTime.MinValue)
            {
                FechaNacimiento = new DateTimeOffset(GlobalData.Instance.FechaNacimientoGD);
            }
            else
            {
                FechaNacimiento = DateTimeOffset.Now;
            }

            // Conversión de Privacidad (True = Privada = Index 0)
            IndexPrivacidad = GlobalData.Instance.Es_PrivadaGD ? 0 : 1;
        }
        /// <summary>
        /// Procesa y persiste de forma asíncrona las modificaciones realizadas en el perfil del usuario tanto en la base de datos como en el estado global.
        /// </summary>
        /// <remarks>
        /// Este método realiza una validación y transformación de datos antes de la persistencia:
        /// <list type="number">
        /// <item><b>Conversión:</b> Transforma el objeto <see cref="DateTimeOffset"/> de la interfaz a <see cref="DateTime"/> y el índice de privacidad a un valor booleano.</item>
        /// <item><b>Sincronización remota:</b> Invoca al cliente de MongoDB para actualizar los documentos en la nube.</item>
        /// <item><b>Actualización local:</b> Si la operación remota es exitosa, sincroniza los nuevos valores en <see cref="GlobalData.Instance"/> para mantener la consistencia en la sesión actual.</item>
        /// </list>
        /// Notifica el resultado de la operación al usuario mediante el servicio de diálogos y registra errores críticos en la consola de depuración.
        /// </remarks>
        /// <returns>Una tarea que representa la operación de guardado asíncrona.</returns>
        private async Task GuardarCambios()
        {
            try
            {
                // Convertimos los datos de la vista a los formatos de BD
                var fechaParaGuardar = FechaNacimiento?.DateTime ?? DateTime.Now;
                bool esCuentaPrivada = (IndexPrivacidad == 0);

                bool exito = await MongoClientSingleton.Instance.Cliente.ActualizarPerfilUsuario(
                    GlobalData.Instance.UserIdGD, 
                    NombreUsuario,                
                    Email,                        
                    Pais,                         
                    fechaParaGuardar,             
                    esCuentaPrivada               
                );

                // Comprobamos si se actualizo correctamente y reflajamos los cambios en el Singleton (GlobalData) 
                if (exito)
                {
                    GlobalData.Instance.UsernameGD = NombreUsuario;
                    GlobalData.Instance.EmailGD = Email;
                    GlobalData.Instance.PaisGD = Pais;
                    GlobalData.Instance.FechaNacimientoGD = fechaParaGuardar;
                    GlobalData.Instance.Es_PrivadaGD = esCuentaPrivada;

                    _dialogoService.MostrarAlerta("MsgExitoActualizarPerfil");
                    System.Diagnostics.Debug.WriteLine("¡Perfil actualizado en BD y Memoria!");
                }
                else
                {
                    _dialogoService.MostrarAlerta("MsgErrorActualizarPerfil");
                    System.Diagnostics.Debug.WriteLine("Fallo al actualizar en Mongo.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error crítico al guardar perfil: " + ex.Message);
            }
        }
    }
}