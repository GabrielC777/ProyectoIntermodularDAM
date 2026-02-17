using Avalonia.Threading;
using BetaProyecto.Models;
using BetaProyecto.Services;
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
    public class ViewUsuariosViewModel : ViewModelBase
    {
        //Servicios
        private readonly IDialogoService _dialogoService;


        //Dato principal 
        private string _idUsuarioCargado; // Guardamos el ID 

        //El usuario completo que como lo actualizamos cada X segundos, lo hacemos reactive
        private Usuarios _usuario;
        public Usuarios Usuario
        {
            get => _usuario;
            set => this.RaiseAndSetIfChanged(ref _usuario, value);
        }

        //Bidings
        private List<Canciones> _cancionesSubidas;
        public List<Canciones> CancionesSubidas
        {
            get => _cancionesSubidas;
            set => this.RaiseAndSetIfChanged(ref _cancionesSubidas, value);
        }

        private List<ListaPersonalizada> _playlistsCreadas;
        public List<ListaPersonalizada> PlaylistsCreadas
        {
            get => _playlistsCreadas;
            set => this.RaiseAndSetIfChanged(ref _playlistsCreadas, value);
        }

        // --- TEMPORIZADOR DE ACTUALIZACIÓN ---

        private string _txtMensajeTimer = "VisorUser_Timer_Iniciando";
        public string TxtMensajeTimer
        {
            get => _txtMensajeTimer;
            set => this.RaiseAndSetIfChanged(ref _txtMensajeTimer, value);
        }

        // 2. Variable numérica (Ej: " 3s")
        private string _txtVariableTimer = "";
        public string TxtVariableTimer
        {
            get => _txtVariableTimer;
            set => this.RaiseAndSetIfChanged(ref _txtVariableTimer, value);
        }

        private bool _esSeguido;
        public bool EsSeguido
        {
            get => _esSeguido;
            set
            {
                this.RaiseAndSetIfChanged(ref _esSeguido, value);
                // Actualizamos el texto (CLAVE) y color automáticamente
                TextoBotonSeguir = value ? "VisorUser_Btn_DejarSeguir" : "VisorUser_Btn_Seguir";
                ColorBotonSeguir = value ? "#D32F2F" : "#4939DC"; // Rojo si sigues, Azul si no
            }
        }

        private string _textoBotonSeguir = "VisorUser_Btn_Seguir";
        public string TextoBotonSeguir
        {
            get => _textoBotonSeguir;
            set => this.RaiseAndSetIfChanged(ref _textoBotonSeguir, value);
        }

        private string _colorBotonSeguir = "#4939DC";
        public string ColorBotonSeguir
        {
            get => _colorBotonSeguir;
            set => this.RaiseAndSetIfChanged(ref _colorBotonSeguir, value);
        }

        // Propiedades formateas
        public string FechaNacimientoFormateada =>
                    _usuario?.Perfil?.FechaNacimiento.ToString("dd MMMM yyyy") ?? "";
        public int CantidadCanciones =>
            _usuario?.Estadisticas?.NumCancionesSubidas ?? 0;

        //Comandos Reactive
        public ReactiveCommand<Unit, Unit> BtnVolver { get; }
        public ReactiveCommand<Unit, Unit> BtnSeguir { get; }

        //Control de hilos
        private CancellationTokenSource _cancelToken;

        //Constructor
        public ViewUsuariosViewModel(string idUsuario, Action accionVolver)
        {
            //Inicializamos servicio
            _dialogoService = new DialogoService();

            _idUsuarioCargado = idUsuario;

            // Inicializamos listas vacías para evitar errores null en el XAML al arrancar
            _cancionesSubidas = new List<Canciones>();
            _playlistsCreadas = new List<ListaPersonalizada>();

            // Configurar comandos reactive
            BtnVolver = ReactiveCommand.Create(() =>
            {
                _cancelToken.Cancel(); // Matamos el hilo al salir
                accionVolver();
            });
            BtnSeguir = ReactiveCommand.CreateFromTask(AlterarSeguimiento);

            // Arrancar el Hilo de PSP
            _cancelToken = new CancellationTokenSource();
            IniciarHiloActualizacion(_cancelToken.Token);
            ActualizarBtnSeguir();
        }
        /// <summary>
        /// Cambia el estado de seguimiento del usuario cargado actualmente para el usuario activo. Si el usuario activo ya está
        /// siguiendo al usuario cargado, este método dejará de seguir; de lo contrario, iniciará un seguimiento.
        /// </summary>
        /// <remarks>Si el usuario activo intenta seguirse a sí mismo, se muestra una alerta y no hay acción
        /// se toma. El método actualiza tanto el estado de seguimiento como la lista local de seguidores al éxito. </remarks>
        /// <returns>Devuelve una tarea que representa la operación asíncrona. </returns>
        private async Task AlterarSeguimiento()
        {
            string miId = GlobalData.Instance.UserIdGD;

            if (_idUsuarioCargado == miId)
            {
                // "No puedes seguirte a ti mismo"
                _dialogoService.MostrarAlerta("Msg_Error_SeguirseMismo");
                return;
            }

            bool exito;

            if (EsSeguido)
            {
                exito = await MongoClientSingleton.Instance.Cliente.DejarDeSeguirUsuario(miId, _idUsuarioCargado);
                if (exito)
                {
                    EsSeguido = false;
                    // Actualizamos la lista local 
                    Usuario.Listas.Seguidores?.Remove(_idUsuarioCargado);
                }
            }
            else
            {
                exito = await MongoClientSingleton.Instance.Cliente.SeguirUsuario(miId, _idUsuarioCargado);
                if (exito)
                {
                    EsSeguido = true;
                    // Actualizamos la lista local
                    Usuario.Listas.Seguidores?.Add(_idUsuarioCargado);
                }
            }
        }
        /// <summary>
        /// Actualiza el indicador de estado de seguimiento según si el usuario cargado está presente en los seguidores globales
        /// lista.
        /// </summary>
        /// <remarks>Este método establece el valor de la propiedad EsSeguido para reflejar si el actualmente
        /// el usuario cargado está siendo seguido. Debe llamarse cada vez que la lista de seguidores o el usuario cargado cambie a
        /// asegúrese de que el estado de seguimiento siga siendo preciso. </remarks>
        private void ActualizarBtnSeguir()
        {
            List<string> lista = GlobalData.Instance.SeguidoresGD;

            if (lista != null && lista.Contains(_idUsuarioCargado))
            {
                EsSeguido = true;
            }
            else
            {
                EsSeguido = false;
            }
        }
        /// <summary>
        /// Inicia un ciclo de actualización en segundo plano que actualiza periódicamente el usuario y las listas relacionadas hasta que se cancele
        /// solicitado.
        /// </summary>
        /// <remarks>El bucle de actualización se ejecuta de forma asíncrona y actualiza los elementos de la interfaz de usuario para reflejar el estado actual.
        /// estado de actualización. El método no bloquea el hilo de llamada. Para detener el proceso de actualización, indica
        /// cancelación a través del token proporcionado. </remarks>
        /// <param name="token">Un token de cancelación que se puede usar para solicitar la finalización del ciclo de actualización. </param>
        private void IniciarHiloActualizacion(CancellationToken token)
        {
            Task.Run(async () =>
            {
                try
                {
                    //Carga inicial 
                    await CargarUsuario();

                    // Si encontramos al usuario, cargamos sus listas
                    if (_usuario != null)
                    {
                        await CargarListasDetalladas(_usuario.Id);

                    }

                    //Bucle de actualización cada 5 segundos
                    while (!token.IsCancellationRequested)
                    {
                        for (int i = 5; i > 0; i--)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TxtMensajeTimer = "VisorUser_Timer_Refrescando";
                                TxtVariableTimer = $" {i}s";
                            });
                            await Task.Delay(1000, token);
                        }

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            TxtMensajeTimer = "VisorUser_Timer_Consultando";
                            TxtVariableTimer = "";
                        });

                        // Refrescamos usuario y listas
                        await CargarUsuario();
                        if (_usuario != null)
                        {
                            // Actualizamos la UI visualmente
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TxtMensajeTimer = "VisorUser_Timer_Actualizado";
                                TxtVariableTimer = "";
                            });
                        }
                    }
                }
                catch (TaskCanceledException) { }
            });
        }
        /// <summary>
        /// Carga asincrónicamente los datos de usuario del identificador de usuario seleccionado actualmente y actualiza el relacionado
        /// propiedades.
        /// </summary>
        /// <remarks>Este método recupera la información del usuario de la fuente de datos basada en el usuario actual
        /// identificador y actualiza el usuario vinculado y las propiedades calculadas relacionadas en el hilo de la interfaz. Destinado a
        /// uso interno dentro del modelo de vista para asegurar la consistencia de la interfaz de usuario después de cambios en los datos del usuario. </remarks>
        /// <returns>Devuelve una tarea que representa la operación de carga asíncrona. </returns>
        private async Task CargarUsuario()
        {
            var listaUnId = new List<string> { _idUsuarioCargado };

            var resultados = await MongoClientSingleton.Instance.Cliente.ObtenerUsuariosPorListaIds(listaUnId);

            var usuarioEncontrado = resultados.FirstOrDefault();

            if (usuarioEncontrado != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Usuario = usuarioEncontrado;
                    // Forzamos actualización de las propiedades calculadas 
                    this.RaisePropertyChanged(nameof(FechaNacimientoFormateada));
                    this.RaisePropertyChanged(nameof(CantidadCanciones));
                });
            }
        }
        /// <summary>
        /// Carga de forma asíncrona las listas detalladas de canciones y listas de reproducción creadas por el usuario especificado y actualiza el
        /// propiedades correspondientes en el hilo de la interfaz.
        /// </summary>
        /// <remarks>Este método recupera las canciones y listas de reproducción del usuario desde la fuente de datos y las actualizaciones
        /// las propiedades vinculadas a la interfaz de usuario. Las actualizaciones se envían al hilo de la interfaz para garantizar la seguridad del hilo al modificarlo
        /// elementos de la interfaz de usuario. </remarks>
        /// <param name="idUser">El identificador único del usuario cuyas canciones cargadas y listas de reproducción creadas se deben cargar. No puede ser
        /// nulo. </param>
        /// <returns>Devuelve una tarea que representa la operación de carga asíncrona. </returns>
        private async Task CargarListasDetalladas(string idUser)
        {
            // Buscamos las lista que nos interesan
            var canciones = await MongoClientSingleton.Instance.Cliente.ObtenerCancionesPorAutor(idUser);
            var playlists = await MongoClientSingleton.Instance.Cliente.ObtenerPlaylistsPorCreador(idUser);

            // Las mandamos al hilo principal para que las actualize
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CancionesSubidas = canciones;
                PlaylistsCreadas = playlists;
            });
        }
    }
}