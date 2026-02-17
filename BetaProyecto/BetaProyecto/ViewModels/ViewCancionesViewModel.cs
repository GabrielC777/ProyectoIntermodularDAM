using Avalonia.Threading;
using BetaProyecto.Models;
using BetaProyecto.Singleton;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public class ViewCancionesViewModel : ViewModelBase
    {
        //Usamos reactive en cancion para que se actualice automaticamente con los hilos
        public Canciones _cancion;
        public Canciones Cancion
        {
            get => _cancion;
            set => this.RaiseAndSetIfChanged(ref _cancion, value);
        }

        // Bindings
        private string _iconoLike;
        public string IconoLike
        {
            get => _iconoLike;
            set => this.RaiseAndSetIfChanged(ref _iconoLike, value);
        }

        private string _txtMensajeTimer = "VisorCan_Timer_Consultando";
        public string TxtMensajeTimer
        {
            get => _txtMensajeTimer;
            set => this.RaiseAndSetIfChanged(ref _txtMensajeTimer, value);
        }

        private string _txtVariableTimer = "";
        public string TxtVariableTimer
        {
            get => _txtVariableTimer;
            set => this.RaiseAndSetIfChanged(ref _txtVariableTimer, value);
        }

        // Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnVolver { get; }
        public ReactiveCommand<Unit, Unit> BtnReproducir { get; }
        public ReactiveCommand<Unit, Unit> BtnLike { get; }

        //Control de hilos
        private CancellationTokenSource _cancelToken;

        // Propiedades formateas
        public string DuracionFormateada
        {
            get
            {
                var tiempo = TimeSpan.FromSeconds(_cancion.Datos.DuracionSegundos);
                return tiempo.ToString(@"m\:ss") + " min";
            }
        }
        public string FechaLanzamientoFormateada =>
            _cancion.Datos.FechaLanzamiento.ToString("dd MMMM, yyyy");

        public ViewCancionesViewModel(Canciones cancion, Action accionVolver, Action<Canciones>? accionReproducir, Action<Canciones> accionLike)
        {
            _cancion = cancion;

            ActualizarIconoLike();

            // Inicializamos el hilo
            _cancelToken = new CancellationTokenSource();
            IniciarHiloActualizacion(_cancelToken.Token);

            // Configuramos Comandos
            BtnVolver = ReactiveCommand.Create(() =>
            {
                _cancelToken.Cancel();
                accionVolver();
            });

            BtnReproducir = ReactiveCommand.Create(() =>
            {
                // Al pulsar Play, avisamos al padre (MarcoApp) para que suene
                accionReproducir?.Invoke(_cancion);
            });

            BtnLike = ReactiveCommand.CreateFromTask(async () =>
            {
                accionLike(_cancion);
                ActualizarIconoLike();
            });
        }
        /// <summary>
        /// Inicia un bucle de actualización en segundo plano que actualiza periódicamente la información actual de la canción hasta su cancelación
        /// es solicitado.
        /// </summary>
        /// <remarks>Este método ejecuta la lógica de actualización en un hilo en segundo plano y actualiza los elementos de la interfaz de usuario.
        /// usando el despachador. El ciclo de actualización continúa hasta que se indica el token de cancelación proporcionado. Intención
        /// para uso interno para mantener la interfaz de usuario sincronizada con los datos más recientes de las canciones. </remarks>
        /// <param name="token">Un token de cancelación que se puede usar para solicitar la finalización del ciclo de actualización. </param>
        private void IniciarHiloActualizacion(CancellationToken token)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        // --- CUENTA ATRÁS VISUAL ---
                        // En lugar de esperar 3000ms de golpe, hacemos 3 pasos de 1000ms
                        for (int i = 5; i > 0; i--)
                        {
                            // Actualizamos el texto en la UI
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                // Aquí separamos: Clave + Valor
                                TxtMensajeTimer = "VisorCan_Timer_Actualizando";
                                TxtVariableTimer = $" {i} s";
                            });

                            // Esperamos 1 segundo
                            await Task.Delay(1000, token);
                        }

                        // Avisamos que estamos consultando
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            TxtMensajeTimer = "VisorCan_Timer_Consultando";
                            TxtVariableTimer = ""; // Limpiamos el número
                        });

                        // --- CONSULTA A LA BASE DE DATOS ---
                        var listaUnaCancion = new List<string> { _cancion.Id };
                        var resultado = await MongoClientSingleton.Instance.Cliente.ObtenerCancionesPorListaIds(listaUnaCancion);

                        var cancionActualizada = resultado.FirstOrDefault();

                        if (cancionActualizada != null)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                // Actualizamos métricas
                                Cancion = cancionActualizada;
                                // Mensaje de éxito breve
                                TxtMensajeTimer = "VisorCan_Timer_Exito";
                                TxtVariableTimer = "";
                            });
                        }
                    }
                }
                catch (TaskCanceledException) { /* Hilo detenido */ }
            });
        }
        /// <summary>
        /// Actualiza el icono de like para indicar si la canción actual está marcada como favorita.
        /// </summary>
        /// <remarks>Este método establece el icono similar en función de la presencia del identificador de la canción actual
        /// en la lista global de favoritos. Debe llamarse siempre que haya cambiado el estado favorito de la canción
        /// para asegurarse de que el icono siga sincronizado con los favoritos del usuario. </remarks>
        private void ActualizarIconoLike()
        {
            var listaFavoritos = GlobalData.Instance.FavoritosGD;

            if (listaFavoritos.Contains(_cancion.Id))// Si el ID de la canción actual está en favoritos 
            {
                IconoLike = "Img_Like_ON";
            }
            else
            {
                // Si NO está -> Icono Normal
                IconoLike = "Img_Like_OFF";
            }
        }
    }
}