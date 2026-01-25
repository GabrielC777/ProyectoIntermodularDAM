using Avalonia.Controls;
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

        //Para cuendo volvamos no veamos el radio button vacio si estamos en modo oscuro
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

            // 1. CARGAR ESTADO ACTUAL (Desde GlobalData)
            CargarEstadoInicial();

            // 2. CONFIGURAR COMANDOS
            BtnVolverAtras = ReactiveCommand.Create(() => accionVolver?.Invoke());

            BtnCerrarSesion = ReactiveCommand.Create(() =>
            {
                GlobalData.Instance.ClearUserData();
                accionLogout?.Invoke();
            });

            BtnSalirApp = ReactiveCommand.Create(() => accionSalir?.Invoke());
        }

        // Para comprobar que diccionarios hay activos configuracion
        private void CargarEstadoInicial()
        {
            // Usamos las variables REALES de tu GlobalData
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

        // --- MÉTODOS DE CAMBIO Y GUARDADO ---

        private void AplicarCambioTema(bool esClaro)
        {
            string nuevoTema = esClaro ? "ModoClaro" : "ModoOscuro";

            // 1. Visual (Instantáneo)
            ControladorDiccionarios.AplicarTema(nuevoTema);

            // 2. Comprobamos si realmente cambió respecto a GlobalData
            if (GlobalData.Instance.DiccionarioTemaGD != nuevoTema)
            {
                // 3. Guardamos en Mongo PRIMERO (para que detecte el cambio)
                GuardarConfiguracionEnMongo(nuevoTema, null, null);

                // 4. Actualizamos GlobalData AL FINAL
                GlobalData.Instance.DiccionarioTemaGD = nuevoTema;
            }
        }

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

        // Método auxiliar para construir el objeto y enviarlo
        private void GuardarConfiguracionEnMongo(string? temaNuevo, string? idiomaNuevo, string? fuenteNuevo)
        {
            if (string.IsNullOrEmpty(GlobalData.Instance.UserIdGD)) return;

            // Construimos el objeto Configuración mezclando lo NUEVO con lo VIEJO (de GlobalData)
            // Si pasas 'null' en un parámetro, significa que ese no cambió, así que cogemos el de GlobalData.
            var config = new ConfiguracionUser
            {
                DiccionarioTema = temaNuevo ?? GlobalData.Instance.DiccionarioTemaGD,
                DiccionarioIdioma = idiomaNuevo ?? GlobalData.Instance.DiccionarioIdiomaGD,
                DiccionarioFuente = fuenteNuevo ?? GlobalData.Instance.DiccionarioFuenteGD
            };

            // Llamada asíncrona "Fire and Forget" al método inteligente de MongoAtlas
            _ = MongoClientSingleton.Instance.Cliente.ActualizarConfiguracionUsuario(GlobalData.Instance.UserIdGD, config);
        }
    }
}