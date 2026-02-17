using System;
using System.Collections.Generic;
using BetaProyecto.Models;

namespace BetaProyecto.Singleton
{
    public class GlobalData
    {
        private static GlobalData _instance;
        public static GlobalData Instance => _instance ??= new GlobalData();

        // Variables globales
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

        // Método para cargar datos del usuario en las variables globales
        /// <summary>
        /// Sincroniza y mapea la información completa de un objeto <see cref="Usuarios"/> hacia las propiedades globales de la sesión actual.
        /// </summary>
        /// <remarks>
        /// Este método actúa como un adaptador que distribuye los datos del usuario autenticado en diferentes categorías:
        /// <list type="bullet">
        /// <item><b>Datos de Identidad:</b> Mapea ID, nombre, correo y rol directamente desde la raíz del objeto.</item>
        /// <item><b>Perfil y Preferencias:</b> Extrae información geográfica, imagen de perfil y estado de privacidad.</item>
        /// <item><b>Actividad y Social:</b> Inicializa contadores de estadísticas y asegura que las listas de seguidores y favoritos no sean nulas.</item>
        /// <item><b>Configuración de Entorno:</b> Carga los diccionarios de tema, idioma y fuente, aplicando valores por defecto si no existen preferencias guardadas.</item>
        /// </list>
        /// Se utiliza principalmente durante el inicio de sesión o tras una actualización exitosa del perfil del usuario para mantener la consistencia en toda la aplicación.
        /// </remarks>
        /// <param name="user">El objeto <see cref="Usuarios"/> recuperado de la base de datos que contiene la información maestra.</param>
        public void SetUserData(Usuarios user)
        {
            if (user != null)
            {
                // Datos que están en la raíz
                this.UserIdGD = user.Id;
                this.UsernameGD = user.Username;
                this.EmailGD = user.Email;
                this.PasswordGD = user.Password;
                this.RolGD = user.Rol;
                this.Fecha_registroGD = user.FechaRegistro;

                // Datos dentro de "Perfil" 
                if (user.Perfil != null)
                {
                    this.UrlFotoPerfilGD = user.Perfil.ImagenUrl;
                    this.FechaNacimientoGD = user.Perfil.FechaNacimiento;
                    this.Es_PrivadaGD = user.Perfil.EsPrivada;
                    this.PaisGD = user.Perfil.Pais;
                }

                // Datos dentro de "Estadisticas"
                if (user.Estadisticas != null)
                {
                    this.Num_canciones_subidasGD = user.Estadisticas.NumCancionesSubidas;
                }
                else
                {
                    this.Num_canciones_subidasGD = 0;
                }

                // Datos dentro de "Listas"
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


        // Para limpiar los datos
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
        // Para generar un objeto Usuarios completo a partir de las variables globales
        /// <summary>
        /// Reconstruye y devuelve un objeto de tipo <see cref="Usuarios"/> integrando todas las propiedades almacenadas en la sesión global.
        /// </summary>
        /// <remarks>
        /// Este método realiza una operación de ensamblado para convertir las propiedades planas de <see cref="GlobalData"/> en una estructura jerárquica compleja. 
        /// Es fundamental para operaciones de persistencia, permitiendo que otros servicios (como el cliente de base de datos) reciban una entidad completa 
        /// con sus objetos anidados de <see cref="PerfilUsuario"/>, <see cref="EstadisticasUsuario"/>, <see cref="ListasUsuario"/> y <see cref="ConfiguracionUser"/>.
        /// </remarks>
        /// <returns>
        /// Una nueva instancia de <see cref="Usuarios"/> que refleja el estado actual de la sesión del usuario, 
        /// incluyendo sus preferencias de configuración y listas sociales.
        /// </returns>
        public Usuarios GetUsuarioObject()
        {
            // Reconstruimos el objeto completo
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
        // Constructor
        private GlobalData() { }
    }
}