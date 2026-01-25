using System;
using System.Collections.Generic;
using BetaProyecto.Models; // Asegúrate de que esto apunta a donde está tu clase Usuario corregida

namespace BetaProyecto.Singleton
{
    public class GlobalData
    {
        private static GlobalData _instance;
        public static GlobalData Instance => _instance ??= new GlobalData();

        // --- VARIABLES GLOBALES ---
        public string UserIdGD { get; set; }
        public string UsernameGD { get; set; }
        public string EmailGD { get; set; }
        public string PasswordGD { get; set; }
        public string RolGD { get; set; }
        public string UrlFotoPerfilGD { get; set; }
        public DateTime FechaNacimientoGD { get; set; }
        public bool Es_PrivadaGD { get; set; }
        public string PaisGD { get; set; }
        public int Num_canciones_subidasGD { get; set; }
        public List<string> SeguidoresGD { get; set; }
        public List<string> FavoritosGD { get; set; }
        public string DiccionarioTemaGD { get; set; }
        public string DiccionarioIdiomaGD { get; set; }
        public string DiccionarioFuenteGD { get; set; }
        public DateTime Fecha_registroGD { get; set; }

        // --- EL MÉTODO PARA LLENAR LOS DATOS ---
        public void SetUserData(Usuarios user)
        {
            if (user != null)
            {
                // 1. Datos que están en la raíz (Directos)
                this.UserIdGD = user.Id;
                this.UsernameGD = user.Username;
                this.EmailGD = user.Email;
                this.PasswordGD = user.Password;
                this.RolGD = user.Rol;
                this.Fecha_registroGD = user.FechaRegistro;

                // 2. Datos dentro de "Perfil" (Hay que entrar en la cajita)
                if (user.Perfil != null)
                {
                    this.UrlFotoPerfilGD = user.Perfil.ImagenUrl;
                    this.FechaNacimientoGD = user.Perfil.FechaNacimiento;
                    this.Es_PrivadaGD = user.Perfil.EsPrivada;
                    this.PaisGD = user.Perfil.Pais;
                }

                // 3. Datos dentro de "Estadisticas"
                if (user.Estadisticas != null)
                {
                    this.Num_canciones_subidasGD = user.Estadisticas.NumCancionesSubidas;
                }
                else
                {
                    this.Num_canciones_subidasGD = 0;
                }

                // 4. Datos dentro de "Listas"
                if (user.Listas != null)
                {
                    this.SeguidoresGD = user.Listas.Seguidores ?? new List<string>(); ;
                    this.FavoritosGD = user.Listas.Favoritos ?? new List<string>(); ;
                }
                else
                {
                    // Inicializamos listas vacías para evitar errores luego
                    this.SeguidoresGD = new List<string>();
                    this.FavoritosGD = new List<string>();
                }
                if (user.Configuracion != null)
                {
                    this.DiccionarioTemaGD = user.Configuracion.DiccionarioTema;
                    this.DiccionarioIdiomaGD = user.Configuracion.DiccionarioIdioma;
                    this.DiccionarioFuenteGD = user.Configuracion.DiccionarioFuente;
                }
                else
                {
                    // Valores por defecto para evitar errores
                    this.DiccionarioTemaGD = "ModoClaro";
                    this.DiccionarioIdiomaGD = "Spanish";
                    this.DiccionarioFuenteGD = "Lexend";
                }
            }
        }

        // --- IMPORTANTE: MÉTODO PARA CERRAR SESIÓN ---
        // (Añade esto, lo necesitarás para el botón "Cerrar Sesión")
        public void ClearUserData()
        {
            this.UserIdGD = string.Empty;
            this.UsernameGD = string.Empty;
            this.EmailGD = string.Empty;
            this.PasswordGD = string.Empty;
            this.RolGD = string.Empty;
            this.UrlFotoPerfilGD = string.Empty;
            this.FechaNacimientoGD = DateTime.MinValue;
            this.Es_PrivadaGD = false;
            this.PaisGD = string.Empty;
            this.Num_canciones_subidasGD = 0;
            this.SeguidoresGD = new List<string>();
            this.FavoritosGD = new List<string>();
            this.DiccionarioTemaGD = string.Empty;
            this.DiccionarioIdiomaGD = string.Empty;
            this.DiccionarioFuenteGD = string.Empty;
        }

        // Dentro de BetaProyecto.Singleton.GlobalData

        public Usuarios GetUsuarioObject()
        {
            // 2. Reconstruimos el objeto completo
            var usuarioCompleto = new Usuarios
            {
                Id = this.UserIdGD,
                Username = this.UsernameGD,
                Email = this.EmailGD,
                Password = this.PasswordGD,
                Rol = this.RolGD,
                FechaRegistro = this.Fecha_registroGD,

                // Reconstruimos el Perfil
                Perfil = new PerfilUsuario
                {
                    ImagenUrl = this.UrlFotoPerfilGD,
                    FechaNacimiento = this.FechaNacimientoGD,
                    EsPrivada = this.Es_PrivadaGD,
                    Pais = this.PaisGD
                },

                // Reconstruimos Estadísticas
                Estadisticas = new EstadisticasUsuario
                {
                    NumCancionesSubidas = this.Num_canciones_subidasGD
                },

                // Reconstruimos Listas
                Listas = new ListasUsuario
                {
                    Seguidores = this.SeguidoresGD ?? new List<string>(),
                    Favoritos = this.FavoritosGD ?? new List<string>()
                },
                Configuracion = new ConfiguracionUser{
                    DiccionarioTema = this.DiccionarioTemaGD ?? "ModoClaro",
                    DiccionarioIdioma = this.DiccionarioIdiomaGD ?? "Spanish",
                    DiccionarioFuente = this.DiccionarioFuenteGD ?? "Lexend"
                }
            };

            return usuarioCompleto;
        }
        private GlobalData() { }
    }
}