using BetaProyecto.Services;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Reactive;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        //Servicios
        private readonly IDialogoService _dialogoService;

        // Action para cambiar el contenido del marco
        public Action? AlCompletarLogin { get; set; }
        public Action? IrARegistarUser { get; set; }

        // Bidings  
        private string _userText = "";
        public string TxtUsuario
        {
            get => _userText;
            set => this.RaiseAndSetIfChanged(ref _userText, value);
        }

        private string _passText = "";
        public string TxtPass
        {
            get => _passText;
            set => this.RaiseAndSetIfChanged(ref _passText, value);
        }


        // Comandos Reactive 
        public ReactiveCommand<Unit, Unit> Login { get; }
        public ReactiveCommand<Unit, Unit> BtnRegistarUser { get; }

        // Este constructor vacío es solo para el diseñador de Avalonia funcione
        public LoginViewModel() : this(null!)
        {
        }

        public LoginViewModel(IDialogoService dialogoService)
        {
            _dialogoService = dialogoService;

            var validacionLogin = this.WhenAnyValue(
                x => x.TxtUsuario,
                x => x.TxtPass,
                (usuario, pass) => !string.IsNullOrWhiteSpace(usuario) && !string.IsNullOrWhiteSpace(pass)
            );

            // Inicializamos el comando apuntando a la función asíncrona para evitar que la interfaz se congele
            Login = ReactiveCommand.CreateFromTask(IntentarLogin, validacionLogin);
            BtnRegistarUser = ReactiveCommand.Create(() =>
            {
                IrARegistarUser?.Invoke();
            });
        }

        private async Task IntentarLogin()
        {
            

            // Conectamos a la base de datos
            bool conectado = await MongoClientSingleton.Instance.Cliente.Conectar();

            if (conectado)
            {
                // B) Si hay conexión procedemos a verificar si existe el usuario y si es correcta la contraseña
                var usuario = await MongoClientSingleton.Instance.Cliente.LoginUsuario(TxtUsuario, TxtPass);

                // Comprobamos si ha encontrado el usuario
                if (usuario != null)
                {
                    // Guardamos en el Singleton
                    GlobalData.Instance.SetUserData(usuario);    

                    // Avisamos a la vista para que cambie de pantalla
                    AlCompletarLogin?.Invoke();

                    _dialogoService.MostrarAlerta("Se a conectado correctamente con el usuario " + GlobalData.Instance.usernameGD.ToString());
                }
                else
                {
                    // Conectó bien, pero usuario/pass están mal
                    _dialogoService.MostrarAlerta("Usuario o contraseña incorrectos. Inténtelo de nuevo.");
                }
            }
            else
            {
                // No hay internet o la BD está caída
                _dialogoService.MostrarAlerta("No se ha podido conectar con el servidor. Compruebe su conexión a Internet e inténtelo de nuevo más tarde.");
            }
        }
    }
}
