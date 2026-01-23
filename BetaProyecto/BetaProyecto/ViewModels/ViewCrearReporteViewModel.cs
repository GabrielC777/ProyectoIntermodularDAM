using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
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
        // OJO: Si quieres traducir estos tipos de problemas, lo ideal sería que fueran claves también
        // Pero para el ComboBox, necesitarías un Converter o cargarlos ya traducidos.
        // De momento lo dejamos así o lo cambiamos a claves si prefieres.
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

        public ReactiveCommand<Unit, Unit> BtnEnviarReporte { get; }
        public ReactiveCommand<Unit, Unit> BtnCancelar { get; }

        // --- CONSTRUCTOR ---
        public ViewCrearReporteViewModel(Canciones cancion, Action accionVolver)
        {
            _cancionAReportar = cancion;
            _Volver = accionVolver;

            // 1. Validación (Se queda aquí porque es configuración visual rápida)
            var validacionCampos = this.WhenAnyValue(
                x => x.TipoSeleccionado,
                x => x.DescripcionTexto,
                (tipo, desc) => !string.IsNullOrEmpty(tipo) && !string.IsNullOrWhiteSpace(desc)
            );

            // 2. Comandos (Apuntan a métodos fuera para no ensuciar)
            BtnEnviarReporte = ReactiveCommand.CreateFromTask(EnviarReporteAsync, canExecute: validacionCampos);

            BtnCancelar = ReactiveCommand.Create(accionVolver);
        }

        // --- MÉTODOS DE LÓGICA ---

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
                        UsuarioReportanteId = GlobalData.Instance.userIdGD
                    }
                };

                await MongoClientSingleton.Instance.Cliente.EnviarReporte(reporte);

                // "✅ Reporte enviado correctamente."
                MensajeEstado = "Msg_Exito_Reporte";

                await Task.Delay(1500);
                _Volver();
            }
            catch (Exception ex)
            {
                // "❌ Error al enviar."
                MensajeEstado = "Msg_Error_Reporte";
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}