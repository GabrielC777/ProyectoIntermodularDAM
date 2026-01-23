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
        public string userIdGD { get; set; }
        public string usernameGD { get; set; }
        public string emailGD { get; set; }
        public string passwordGD { get; set; }
        public string rolGD { get; set; }
        public string urlFotoPerfilGD { get; set; }
        public DateTime fechaNacimientoGD { get; set; }
        public bool es_PrivadaGD { get; set; }
        public string paisGD { get; set; }
        public int num_canciones_subidasGD { get; set; }
        public List<string> seguidoresGD { get; set; }
        public List<string> favoritosGD { get; set; }
        public DateTime fecha_registroGD { get; set; }

        // --- EL MÉTODO PARA LLENAR LOS DATOS ---
        public void SetUserData(Usuarios user)
        {
            if (user != null)
            {
                // 1. Datos que están en la raíz (Directos)
                this.userIdGD = user.Id;
                this.usernameGD = user.Username;
                this.emailGD = user.Email;
                this.passwordGD = user.Password;
                this.rolGD = user.Rol;
                this.fecha_registroGD = user.FechaRegistro;

                // 2. Datos dentro de "Perfil" (Hay que entrar en la cajita)
                if (user.Perfil != null)
                {
                    this.urlFotoPerfilGD = user.Perfil.ImagenUrl;
                    this.fechaNacimientoGD = user.Perfil.FechaNacimiento;
                    this.es_PrivadaGD = user.Perfil.EsPrivada;
                    this.paisGD = user.Perfil.Pais;
                }

                // 3. Datos dentro de "Estadisticas"
                if (user.Estadisticas != null)
                {
                    this.num_canciones_subidasGD = user.Estadisticas.NumCancionesSubidas;
                }
                else
                {
                    this.num_canciones_subidasGD = 0;
                }

                // 4. Datos dentro de "Listas"
                if (user.Listas != null)
                {
                    this.seguidoresGD = user.Listas.Seguidores ?? new List<string>(); ;
                    this.favoritosGD = user.Listas.Favoritos ?? new List<string>(); ;
                }
                else
                {
                    // Inicializamos listas vacías para evitar errores luego
                    this.seguidoresGD = new List<string>();
                    this.favoritosGD = new List<string>();
                }
            }
        }

        // --- IMPORTANTE: MÉTODO PARA CERRAR SESIÓN ---
        // (Añade esto, lo necesitarás para el botón "Cerrar Sesión")
        public void ClearUserData()
        {
            this.userIdGD = string.Empty;
            this.usernameGD = string.Empty;
            this.emailGD = string.Empty;
            this.passwordGD = string.Empty;
            this.rolGD = string.Empty;
            this.urlFotoPerfilGD = string.Empty;
            this.fechaNacimientoGD = DateTime.MinValue;
            this.es_PrivadaGD = false;
            this.paisGD = string.Empty;
            this.num_canciones_subidasGD = 0;
            this.seguidoresGD = new List<string>();
            this.favoritosGD = new List<string>();
            // Reiniciar instancia si fuera necesario o simplemente limpiar propiedades
        }

        // Dentro de BetaProyecto.Singleton.GlobalData

        public Usuarios GetUsuarioObject()
        {
            // 2. Reconstruimos el objeto completo
            var usuarioCompleto = new Usuarios
            {
                Id = this.userIdGD,
                Username = this.usernameGD,
                Email = this.emailGD,
                Password = this.passwordGD,
                Rol = this.rolGD,
                FechaRegistro = this.fecha_registroGD,

                // Reconstruimos el Perfil
                Perfil = new PerfilUsuario
                {
                    ImagenUrl = this.urlFotoPerfilGD,
                    FechaNacimiento = this.fechaNacimientoGD,
                    EsPrivada = this.es_PrivadaGD,
                    Pais = this.paisGD
                },

                // Reconstruimos Estadísticas
                Estadisticas = new EstadisticasUsuario
                {
                    NumCancionesSubidas = this.num_canciones_subidasGD
                },

                // Reconstruimos Listas
                Listas = new ListasUsuario
                {
                    Seguidores = this.seguidoresGD ?? new List<string>(),
                    Favoritos = this.favoritosGD ?? new List<string>()
                }
            };

            return usuarioCompleto;
        }
        private GlobalData() { }
    }
}