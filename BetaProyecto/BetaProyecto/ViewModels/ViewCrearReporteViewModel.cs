using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewCrearReporteViewModel : ViewModelBase
    {
        //Variables
        private Canciones _cancionAReportar;
        public Canciones CancionAReportar => _cancionAReportar;

        //Actions 
        private readonly Action _Volver;


        //Bidings
        public List<string> TiposDeProblema { get; } = new List<string>
        {
            "Copyright / Derechos de autor",
            "Contenido ofensivo o inapropiado",
            "Audio defectuoso o silencio",
            "Spam / Información falsa",
            "Otro"
        };

        private string _tipoSeleccionado;
        public string TipoSeleccionado
        {
            get => _tipoSeleccionado;
            set => this.RaiseAndSetIfChanged(ref _tipoSeleccionado, value);
        }

        private string _descripcionTexto;
        public string DescripcionTexto
        {
            get => _descripcionTexto;
            set => this.RaiseAndSetIfChanged(ref _descripcionTexto, value);
        }

        private string _mensajeEstado;
        public string MensajeEstado
        {
            get => _mensajeEstado;
            set => this.RaiseAndSetIfChanged(ref _mensajeEstado, value);
        }

        //Comandos reactive
        public ReactiveCommand<Unit, Unit> BtnEnviarReporte { get; }
        public ReactiveCommand<Unit, Unit> BtnCancelar { get; }

        // Constructor
        public ViewCrearReporteViewModel(Canciones cancion, Action accionVolver)
        {
            _cancionAReportar = cancion;
            _Volver = accionVolver;

            // Validación
            var validacionCampos = this.WhenAnyValue(
                x => x.TipoSeleccionado,
                x => x.DescripcionTexto,
                (tipo, desc) => !string.IsNullOrEmpty(tipo) && !string.IsNullOrWhiteSpace(desc)
            );

            // Comandos reactive
            BtnEnviarReporte = ReactiveCommand.CreateFromTask(EnviarReporteAsync, canExecute: validacionCampos);
            BtnCancelar = ReactiveCommand.Create(accionVolver);
        }
        
        /// <summary>
        /// Envía un informe de forma asíncrona utilizando los detalles del informe actual y actualiza el mensaje de estado en función del
        /// resultado.
        /// </summary>
        /// <remarks>Si el informe se envía con éxito, se actualiza el mensaje de estado para indicar que ha tenido éxito.
        /// y el método navega de regreso después de un breve retraso. Si se produce un error, el mensaje de estado se actualiza a
        /// indica el fallo. </remarks>
        /// <returns>Devuelve una tarea que representa la operación asíncrona. </returns>
        private async Task EnviarReporteAsync()
        {
            try
            {
                var reporte = new Reportes
                {
                    TipoProblema = TipoSeleccionado,
                    Descripcion = DescripcionTexto,
                    Estado = "Pendiente",
                    FechaCreacion = DateTime.UtcNow,
                    Referencias = new ReferenciasReporte
                    {
                        CancionReportadaId = _cancionAReportar.Id,
                        UsuarioReportanteId = GlobalData.Instance.UserIdGD
                    }
                };

                await MongoClientSingleton.Instance.Cliente.EnviarReporte(reporte);

                MensajeEstado = "Msg_Exito_Reporte";

                await Task.Delay(1500);
                _Volver();
            }
            catch (Exception ex)
            {
                MensajeEstado = "Msg_Error_Reporte";
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}