using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BetaProyecto.Models
{
    public class Usuarios
    {
        // 1. EL ID ES OBLIGATORIO (Mapea el _id de Mongo a string)
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("email")] 
        public string Email { get; set; }

        [BsonElement("password")]
        public string Password { get; set; }

        [BsonElement("rol")]
        public string Rol { get; set; }

        // 2. OBJETOS ANIDADOS (Aquí está el truco)
        // En lugar de poner las propiedades sueltas, creamos propiedades de tipo clase

        [BsonElement("perfil")]
        public PerfilUsuario Perfil { get; set; } = new PerfilUsuario();

        [BsonElement("estadisticas")]
        public EstadisticasUsuario Estadisticas { get; set; } = new EstadisticasUsuario();

        [BsonElement("listas")]
        public ListasUsuario Listas { get; set; } = new ListasUsuario();

        [BsonElement("configuracion")]
        public ConfiguracionUser Configuracion { get; set; } = new ConfiguracionUser();

        [BsonElement("fecha_registro")]
        public DateTime FechaRegistro { get; set; }
    }

    // SUBCLASE 1: PERFIL
    public class PerfilUsuario
    {
        [BsonElement("imagen_url")]
        public string ImagenUrl { get; set; }

        [BsonElement("fecha_nacimiento")]
        public DateTime FechaNacimiento { get; set; } // DateTime se lleva mejor con Mongo que DateOnly

        [BsonElement("es_privada")]
        public bool EsPrivada { get; set; }
        [BsonElement("pais")]
        public string Pais { get; set; }
    }

    // SUBCLASE 2: ESTADISTICAS
    public class EstadisticasUsuario
    {
        [BsonElement("n_canciones_subidas")]
        public int NumCancionesSubidas { get; set; }
    }

    // SUBCLASE 3: LISTAS
    public class ListasUsuario
    {
        // Convertimos los ObjectId de la lista a strings automáticamente
        [BsonElement("seguidores")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Seguidores { get; set; } = new List<string>();

        [BsonElement("favoritos")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Favoritos { get; set; } = new List<string>();
    }
    // SUBCLASE 4: CONFIGURACION
    public class ConfiguracionUser
    {
        [BsonElement("tema")]
        public string DiccionarioTema { get; set; } = "ModoClaro";

        [BsonElement("idioma")]
        public string DiccionarioIdioma { get; set; } = "Español";

        [BsonElement("fuente")]
        public string DiccionarioFuente { get; set; } = "Lexend";
    }
}