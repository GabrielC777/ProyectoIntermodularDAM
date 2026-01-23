using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Threading;

namespace BetaProyecto.ViewModels
{

    public class ViewConfiguracionViewModel : ViewModelBase
    {
        //Variables de rastreo de diccionarios
        private ResourceInclude? _diccionarioIdiomaActual;
        private ResourceInclude? _diccionarioFuenteActual;
        private ResourceInclude? _diccionarioTemaActual;

        //Actions 
        private readonly Action _accionRefrescarTema;

        //Bindings
        private int _indiceFuente;
        public int IndiceFuente
        {
            get => _indiceFuente;
            set
            { 
                this.RaiseAndSetIfChanged(ref _indiceFuente, value);
                // Cuando cambia la propiedad , cambiamos el diccionario
                CambiarFuente(value);
            }
        }

        // 0 = Español, 1 = Inglés
        private int _indiceIdioma;
        public int IndiceIdioma
        {
            get => _indiceIdioma;
            set
            {
                this.RaiseAndSetIfChanged(ref _indiceIdioma, value);
                CambiarIdioma(value);
            }
        }

        // IndiceTema: Claro, Oscuro
        private bool _indiceTema = true;
        public bool IndiceTema
        {
            get => _indiceTema;
            set
            {
                this.RaiseAndSetIfChanged(ref _indiceTema, value);
                CambiarTema(value);
                _accionRefrescarTema?.Invoke();

            }
        }
        public bool IndiceTemaOscuro
        {
            get => !_indiceTema; // Devuelve lo contrario (Si es Claro(true) -> devuelve False)
            set
            {
                // Si el usuario marca "Oscuro" (true), ponemos el tema en false (Oscuro)
                if (value)
                {
                    IndiceTema = false;
                }
            }
        }

        //Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnVolverAtras { get; }
        public ReactiveCommand<Unit, Unit> BtnCerrarSesion { get; }
        public ReactiveCommand<Unit, Unit> BtnSalirApp { get; }

        public ViewConfiguracionViewModel(Action accionVolver, Action accionLogout, Action accionSalir, Action accionRefrescar)
        {
            _accionRefrescarTema = accionRefrescar;

            // Inicializamos referencias es decir escaneamos lo que hay en el App.xaml cargado
            InicializarReferenciasDeDiccionarios();

            // Configuramos comandos reactive
            BtnVolverAtras = ReactiveCommand.Create(() =>
            {
                accionVolver();
            });

            BtnCerrarSesion = ReactiveCommand.Create(() =>
            {
                //Limpiamos los datos de memoriaa
                GlobalData.Instance.ClearUserData();
                accionLogout?.Invoke();
            });
            BtnSalirApp = ReactiveCommand.Create(() =>
            {
                accionSalir?.Invoke();
            });
        }
        private void InicializarReferenciasDeDiccionarios()
        {
            //Cargamos todos los diccionarios en una variable local
            var diccionarios = Application.Current.Resources.MergedDictionaries;

            // Buscamos una única vez usando texto para saber qué cargó el App.axaml por defecto

            // Idioma (Spanish.axaml)
            _diccionarioIdiomaActual = diccionarios.FirstOrDefault(d =>
                d is ResourceInclude ri &&
                ri.Source != null &&
                ri.Source.ToString().Contains("Language")) as ResourceInclude;

            // Fuente (Lexend.axaml)
            _diccionarioFuenteActual = diccionarios.FirstOrDefault(d =>
                d is ResourceInclude ri &&
                ri.Source != null &&
                ri.Source.ToString().Contains("Styles")) as ResourceInclude;

            // Tema (ModoClaro.axaml)
            _diccionarioTemaActual = diccionarios.FirstOrDefault(d =>
                d is ResourceInclude ri &&
                ri.Source != null &&
                ri.Source.ToString().Contains("Interfaces")) as ResourceInclude;

            // SINCRONIZACIÓN AL NACER
            if (_diccionarioTemaActual != null)
            {
                bool esClaro = _diccionarioTemaActual.Source.ToString().Contains("ModoClaro");
                _indiceTema = esClaro;

                // Avisamos a AMBAS propiedades
                this.RaisePropertyChanged(nameof(IndiceTema));
                this.RaisePropertyChanged(nameof(IndiceTemaOscuro));
            }
        }
        private void CambiarIdioma(int indice)
        {
            // 1. Definir el código de cultura (es-ES para español, en-US para inglés)
            string codigoCultura = indice == 0 ? "es-ES" : "en-US";
            // Elegir ruta
            string ruta = indice == 0
                ? "avares://BetaProyecto/Assets/Language/Spanish.axaml"
                : "avares://BetaProyecto/Assets/Language/English.axaml";

            var nuevaCultura = new CultureInfo(codigoCultura);
            Thread.CurrentThread.CurrentCulture = nuevaCultura;
            Thread.CurrentThread.CurrentUICulture = nuevaCultura;

            // Ejecutar el cambio
            _diccionarioIdiomaActual = AplicarCambioDiccionario(ruta, _diccionarioIdiomaActual);
        }
        private void CambiarFuente(int indice)
        {
            string ruta = indice switch
            {
                0 => "avares://BetaProyecto/Assets/Styles/FuenteLexend.axaml",
                1 => "avares://BetaProyecto/Assets/Styles/FuenteCarlito.axaml",
                2 => "avares://BetaProyecto/Assets/Styles/FuenteArial.axaml",
                3 => "avares://BetaProyecto/Assets/Styles/FuenteGloriaHallelujah.axaml",
                4 => "avares://BetaProyecto/Assets/Styles/FuenteOpenSans.axaml",
                5 => "avares://BetaProyecto/Assets/Styles/FuenteRoboto.axaml",
                _ => "avares://BetaProyecto/Assets/Styles/FuenteLexend.axaml"
            };

            _diccionarioFuenteActual = AplicarCambioDiccionario(ruta, _diccionarioFuenteActual);
        }
        private void CambiarTema(bool esClaro)
        {

            string ruta = esClaro
                ? "avares://BetaProyecto/Assets/Interfaces/ModoClaro.axaml"
                : "avares://BetaProyecto/Assets/Interfaces/ModoOscuro.axaml";

            _diccionarioTemaActual = AplicarCambioDiccionario(ruta, _diccionarioTemaActual);
        }



        // Este método quita el viejo, añade el nuevo y devulve una referencia para poder cambiarlo cuando queremos
        private ResourceInclude? AplicarCambioDiccionario(string rutaNueva, ResourceInclude? diccionarioAntiguo)
        {
            try
            {
                var uri = new Uri(rutaNueva);
                var diccionariosApp = Application.Current.Resources.MergedDictionaries;

                // 1. Quitar el viejo
                if (diccionarioAntiguo != null && diccionariosApp.Contains(diccionarioAntiguo))
                {
                    diccionariosApp.Remove(diccionarioAntiguo);
                }

                // 2. Crear el nuevo
                var nuevoDiccionario = new ResourceInclude(uri) { Source = uri };

                // 3. Añadir el nuevo a la App
                diccionariosApp.Add(nuevoDiccionario);

                // 4. Devolvemos la nueva para guardarlo en la variable
                return nuevoDiccionario;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cambiando recurso ({rutaNueva}): {ex.Message}");
                return diccionarioAntiguo; // Si falla, nos quedamos con el que teníamos
            }
        }
    }
}
