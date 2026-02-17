using BetaProyecto.Helpers;
using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Reactive;

namespace BetaProyecto.ViewModels
{
    public class ViewConfiguracionViewModel : ViewModelBase
    {
        // Actions
        private readonly Action _accionRefrescarTema;

        // Bindings

        // FUENTE: 0=Lexend, 1=Carlito, 2=Arial, 3=Gloria, 4=OpenSans, 5=Roboto
        private int _indiceFuente;
        public int IndiceFuente
        {
            get => _indiceFuente;
            set
            {
                this.RaiseAndSetIfChanged(ref _indiceFuente, value);
                AplicarCambioFuente(value);
            }
        }

        // IDIOMA: 0 = Español, 1 = Inglés
        private int _indiceIdioma;
        public int IndiceIdioma
        {
            get => _indiceIdioma;
            set
            {
                this.RaiseAndSetIfChanged(ref _indiceIdioma, value);
                AplicarCambioIdioma(value);
            }
        }

        // TEMA: True = Claro, False = Oscuro
        private bool _indiceTema = true;
        public bool IndiceTema
        {
            get => _indiceTema;
            set
            {
                this.RaiseAndSetIfChanged(ref _indiceTema, value);
                AplicarCambioTema(value);
                _accionRefrescarTema?.Invoke();
            }
        }

        //Para cuando volvamos no veamos el radio button vacio si estamos en modo oscuro
        public bool IndiceTemaOscuro
        {
            get => !_indiceTema;
            set
            {
                IndiceTema = !value;
            }
        }

        // Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnVolverAtras { get; }
        public ReactiveCommand<Unit, Unit> BtnCerrarSesion { get; }
        public ReactiveCommand<Unit, Unit> BtnSalirApp { get; }

        // Constructor
        public ViewConfiguracionViewModel(Action accionVolver, Action accionLogout, Action accionSalir, Action accionRefrescar)
        {
            _accionRefrescarTema = accionRefrescar;

            // Cargar el estado inicial basado en el GlobalData
            CargarEstadoInicial();

            // Configuramos comandos reactive 
            BtnVolverAtras = ReactiveCommand.Create(() => accionVolver?.Invoke());

            BtnCerrarSesion = ReactiveCommand.Create(() =>
            {
                GlobalData.Instance.ClearUserData();
                accionLogout?.Invoke();
            });

            BtnSalirApp = ReactiveCommand.Create(() => accionSalir?.Invoke());
        }

        /// <summary>
        /// Sincroniza la interfaz de configuración con las preferencias del usuario almacenadas en los datos globales.
        /// </summary>
        /// <remarks>
        /// Este método recupera los valores actuales de tema, idioma y tipografía desde <see cref="GlobalData.Instance"/>. 
        /// Posteriormente, traduce estas cadenas de texto a los índices correspondientes que utilizan los selectores de la vista 
        /// y fuerza la notificación de cambio de propiedades mediante <c>RaisePropertyChanged</c> para que la UI 
        /// refleje el estado real de la configuración.
        /// </remarks>
        private void CargarEstadoInicial()
        {
            // Usamos las variables de GlobalData para enseñar el estado actual de la configuración al usuario
            var tema = GlobalData.Instance.DiccionarioTemaGD ?? "ModoClaro";
            var idioma = GlobalData.Instance.DiccionarioIdiomaGD ?? "Spanish";
            var fuente = GlobalData.Instance.DiccionarioFuenteGD ?? "Lexend";

            // Sincronizar Tema
            _indiceTema = (tema == "ModoClaro");
            this.RaisePropertyChanged(nameof(IndiceTema));
            this.RaisePropertyChanged(nameof(IndiceTemaOscuro));

            // Sincronizar Idioma
            _indiceIdioma = (idioma == "English") ? 1 : 0;
            this.RaisePropertyChanged(nameof(IndiceIdioma));

            // Sincronizar Fuente
            _indiceFuente = fuente switch
            {
                "Lexend" => 0,
                "Carlito" => 1,
                "Arial" => 2,
                "GloriaHallelujah" => 3,
                "OpenSans" => 4,
                "Roboto" => 5,
                _ => 0
            };
            this.RaisePropertyChanged(nameof(IndiceFuente));
        }
        /// <summary>
        /// Ejecuta el cambio de apariencia visual de la aplicación entre modo claro y modo oscuro.
        /// </summary>
        /// <remarks>
        /// Este método gestiona el cambio de tema en tres niveles:
        /// <list type="number">
        /// <item><b>Visual:</b> Aplica el diccionario de recursos de forma instantánea mediante <see cref="ControladorDiccionarios"/>.</item>
        /// <item><b>Persistencia:</b> Si el tema es distinto al actual, sincroniza la preferencia en la base de datos MongoDB.</item>
        /// <item><b>Estado Global:</b> Actualiza la propiedad en <see cref="GlobalData.Instance"/> para mantener la consistencia en toda la sesión.</item>
        /// </list>
        /// </remarks>
        /// <param name="esClaro">Indica si se debe aplicar el "ModoClaro" (<c>true</c>) o el "ModoOscuro" (<c>false</c>).</param>
        private void AplicarCambioTema(bool esClaro)
        {
            string nuevoTema = esClaro ? "ModoClaro" : "ModoOscuro";

            //Aplicamos el tema en la app
            ControladorDiccionarios.AplicarTema(nuevoTema);

            // Comprobamos si realmente cambió respecto a GlobalData
            if (GlobalData.Instance.DiccionarioTemaGD != nuevoTema)
            {
                // Guardamos en Mongo
                GuardarConfiguracionEnMongo(nuevoTema, null, null);

                // Actualizamos GlobalData
                GlobalData.Instance.DiccionarioTemaGD = nuevoTema;
            }
        }
        /// <summary>
        /// Ejecuta el cambio de idioma de la interfaz de usuario basándose en el índice seleccionado.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la internacionalización en tres niveles:
        /// <list type="number">
        /// <item><b>Visual:</b> Cambia el diccionario de strings de forma dinámica mediante <see cref="ControladorDiccionarios"/>.</item>
        /// <item><b>Persistencia:</b> Si el idioma es diferente al actual, sincroniza la nueva preferencia en la base de datos MongoDB.</item>
        /// <item><b>Estado Global:</b> Actualiza la referencia en <see cref="GlobalData.Instance"/> para asegurar la persistencia durante la sesión activa.</item>
        /// </list>
        /// </remarks>
        /// <param name="indice">El índice del selector: <c>0</c> para "Spanish" y <c>1</c> para "English".</param>
        private void AplicarCambioIdioma(int indice)
        {
            string nuevoIdioma = indice == 1 ? "English" : "Spanish";

            ControladorDiccionarios.AplicarIdioma(nuevoIdioma);

            if (GlobalData.Instance.DiccionarioIdiomaGD != nuevoIdioma)
            {
                GuardarConfiguracionEnMongo(null, nuevoIdioma, null);
                GlobalData.Instance.DiccionarioIdiomaGD = nuevoIdioma;
            }
        }
        /// <summary>
        /// Ejecuta el cambio de la fuente tipográfica de la aplicación basándose en el índice seleccionado.
        /// </summary>
        /// <remarks>
        /// Este método gestiona la personalización visual en tres niveles:
        /// <list type="number">
        /// <item><b>Visual:</b> Cambia el diccionario de estilos de fuente de forma dinámica mediante <see cref="ControladorDiccionarios"/>.</item>
        /// <item><b>Persistencia:</b> Si la fuente es diferente a la actual, sincroniza la nueva preferencia en la base de datos MongoDB.</item>
        /// <item><b>Estado Global:</b> Actualiza la referencia en <see cref="GlobalData.Instance"/> para asegurar que la tipografía se mantenga durante la sesión activa.</item>
        /// </list>
        /// </remarks>
        /// <param name="indice">El índice del selector que determina la familia tipográfica (0: Lexend, 1: Carlito, 2: Arial, etc.).</param>
        private void AplicarCambioFuente(int indice)
        {
            string nuevaFuente = indice switch
            {
                0 => "Lexend",
                1 => "Carlito",
                2 => "Arial",
                3 => "GloriaHallelujah",
                4 => "OpenSans",
                5 => "Roboto",
                _ => "Lexend"
            };

            ControladorDiccionarios.AplicarFuente(nuevaFuente);

            if (GlobalData.Instance.DiccionarioFuenteGD != nuevaFuente)
            {
                GuardarConfiguracionEnMongo(null, null, nuevaFuente);
                GlobalData.Instance.DiccionarioFuenteGD = nuevaFuente;
            }
        }

        /// <summary>
        /// Sincroniza de forma asíncrona las preferencias de personalización del usuario en la base de datos MongoDB.
        /// </summary>
        /// <remarks>
        /// Este método implementa una lógica de actualización parcial. Construye un objeto de configuración 
        /// combinando los nuevos valores proporcionados con los valores actuales almacenados en <see cref="GlobalData.Instance"/>. 
        /// Si un parámetro se recibe como <c>null</c>, se preserva el valor existente. La actualización se lanza 
        /// mediante una tarea en segundo plano para no bloquear la interfaz.
        /// </remarks>
        /// <param name="temaNuevo">El nombre del nuevo tema visual o <c>null</c> si no ha cambiado.</param>
        /// <param name="idiomaNuevo">El nombre del nuevo idioma de la interfaz o <c>null</c> si no ha cambiado.</param>
        /// <param name="fuenteNuevo">El nombre de la nueva familia tipográfica o <c>null</c> si no ha cambiado.</param>
        private void GuardarConfiguracionEnMongo(string? temaNuevo, string? idiomaNuevo, string? fuenteNuevo)
        {
            if (string.IsNullOrEmpty(GlobalData.Instance.UserIdGD)) return;

            // Construimos el objeto Configuración mezclando lo NUEVO con lo VIEJO para actualizarlo
            var config = new ConfiguracionUser
            {
                DiccionarioTema = temaNuevo ?? GlobalData.Instance.DiccionarioTemaGD,
                DiccionarioIdioma = idiomaNuevo ?? GlobalData.Instance.DiccionarioIdiomaGD,
                DiccionarioFuente = fuenteNuevo ?? GlobalData.Instance.DiccionarioFuenteGD
            };

            // Ejecutamos en segundo plano para actualizar la configuraciñon del usuario. 
            _ = MongoClientSingleton.Instance.Cliente.ActualizarConfiguracionUsuario(GlobalData.Instance.UserIdGD, config);
        }
    }
}