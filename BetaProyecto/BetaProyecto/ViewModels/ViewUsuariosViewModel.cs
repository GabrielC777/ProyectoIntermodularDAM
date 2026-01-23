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

        // --- TEMPORIZADOR DE ACTUALIZACIÓN (DIVIDIDO) ---

        // 1. Clave del mensaje (Ej: "VisorUser_Timer_Refrescando")
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

        // Propiedades formateas (lo configuramos que acepte null)
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

            // Configurar Comando Volver
            BtnVolver = ReactiveCommand.Create(() =>
            {
                _cancelToken.Cancel(); // IMPORTANTE: Matar el hilo al salir
                accionVolver();
            });
            BtnSeguir = ReactiveCommand.CreateFromTask(AlterarSeguimiento);

            // Arrancar el Hilo de PSP
            _cancelToken = new CancellationTokenSource();
            IniciarHiloActualizacion(_cancelToken.Token);
            ActualizarBtnSeguir();
        }
        private async Task AlterarSeguimiento()
        {
            string miId = GlobalData.Instance.userIdGD;

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
        private void ActualizarBtnSeguir()
        {
            List<string> lista = GlobalData.Instance.seguidoresGD;

            if (lista != null && lista.Contains(_idUsuarioCargado))
            {
                EsSeguido = true;
            }
            else
            {
                EsSeguido = false;
            }
        }
        //Aqui iniciamos el hilo de paso de que buscamos el Usuario completo mediante el idUsuario que nos han pasado
        private void IniciarHiloActualizacion(CancellationToken token)
        {
            Task.Run(async () =>
            {
                try
                {
                    // 1. CARGA INICIAL
                    await CargarUsuario();

                    // Si encontramos al usuario, cargamos sus listas
                    if (_usuario != null)
                    {
                        await CargarListasDetalladas(_usuario.Id);

                    }

                    // 2. BUCLE DE ACTUALIZACIÓN (PSP)
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