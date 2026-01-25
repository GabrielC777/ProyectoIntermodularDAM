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

        private void CargarDatos()
        {
            // Leemos del Singleton (GlobalData)
            NombreUsuario = GlobalData.Instance.UsernameGD;
            Email = GlobalData.Instance.EmailGD;
            Pais = GlobalData.Instance.PaisGD; // Ahora ya existe esta propiedad

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

                // 2. COMPROBAMOS SI SE ACTUALIZÓ DE VERDAD
                if (exito)
                {
                    GlobalData.Instance.UsernameGD = NombreUsuario;
                    GlobalData.Instance.EmailGD = Email;
                    GlobalData.Instance.PaisGD = Pais;
                    GlobalData.Instance.FechaNacimientoGD = fechaParaGuardar;
                    GlobalData.Instance.Es_PrivadaGD = esCuentaPrivada;

                    _dialogoService.MostrarAlerta("Perfil actualizado correctamente.");
                    System.Diagnostics.Debug.WriteLine("¡Perfil actualizado en BD y Memoria!");
                }
                else
                {
                    _dialogoService.MostrarAlerta("Error al guardar en la base de datos.");
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