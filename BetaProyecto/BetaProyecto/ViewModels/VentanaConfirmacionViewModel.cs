using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class VentanaConfirmacionViewModel : ViewModelBase
    {
        // Propiedades de texto
        public string TituloCabecera { get; }
        public string MensajeCuerpo { get; }
        public string TextoBotonSi { get; }
        public string TextoBotonNo { get; }

        // Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnSi { get; }
        public ReactiveCommand<Unit, Unit> BtnNo { get; }

        // La acción recibe un bool: true (Si) o false (No)
        public VentanaConfirmacionViewModel(string titulo, string mensaje, string textoSi, string textoNo, Action<bool> cerrarConResultado)
        {
            TituloCabecera = titulo;
            MensajeCuerpo = mensaje;
            TextoBotonSi = textoSi;
            TextoBotonNo = textoNo;

            BtnSi = ReactiveCommand.Create(() => cerrarConResultado(true));
            BtnNo = ReactiveCommand.Create(() => cerrarConResultado(false));
        }
    }
}
