using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetaProyecto.Models
{
    public class Canciones
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("titulo")]
        public string Titulo { get; set; }

        [BsonElement("autores_ids")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> AutoresIds { get; set; }
        
        // [BsonIgnore] le dice a mongo que ignore la propiedad
        [BsonIgnore]
        public string NombreArtista { get; set; } = "Artista Desconocido";
        
        //La necesitamos para los botones flyout de la tarjetas 
        [BsonIgnore]
        public List<string> ListaArtistasIndividuales
        {
            get
            {
                if (string.IsNullOrEmpty(NombreArtista)) return new List<string>();

                // Cortamos por la coma y quitamos espacios
                return NombreArtista.Split(',').Select(a => a.Trim()).ToList();
            }
        }

        [BsonElement("imagen_portada_url")]
        public string ImagenPortadaUrl { get; set; }

        [BsonElement("url_cancion")]
        public string UrlCancion { get; set; }

        // OBJETOS ANIDADOS
        [BsonElement("datos")]
        public DatosCancion Datos { get; set; } = new DatosCancion();

        [BsonElement("metricas")]
        public MetricasCancion Metricas { get; set; } = new MetricasCancion();
    }

    public class DatosCancion
    {
        [BsonElement("duracion_segundos")]
        public int DuracionSegundos { get; set; }

        [BsonElement("generos")]
        public List<string> Generos { get; set; } = new List<string>();

        [BsonIgnore]
        public string GenerosTexto => string.Join(", ", Generos);

        [BsonElement("fecha_lanzamiento")]
        public DateTime FechaLanzamiento { get; set; }
    }

    public class MetricasCancion
    {
        [BsonElement("total_reproducciones")]
        public long TotalReproducciones { get; set; } = 0;

        [BsonElement("total_megustas")]
        public long TotalMegustas { get; set; } = 0;

        [BsonElement("puntuacion_tendencia")]
        public double PuntuacionTendencia { get; set; } = 0.0;
    }

}