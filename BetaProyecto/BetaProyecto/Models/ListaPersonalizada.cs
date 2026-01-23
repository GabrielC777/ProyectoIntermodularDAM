using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.Models
{
    public class ListaPersonalizada
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("nombre")]
        public string Nombre { get; set; }

        [BsonElement("descripcion")]
        public string Descripcion { get; set; }

        [BsonElement("urlportada")]
        public string UrlPortada { get; set; }

        // Aquí guardamos solo los IDs de las canciones
        [BsonElement("listacanciones")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> IdsCanciones { get; set; } = new List<string>();

        [BsonElement("id_usuario")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdUsuario { get; set; }

        // Esta propiedad NO se guarda en BD, la rellenamos nosotros después
        [BsonIgnore]
        public List<Canciones> CancionesCompletas { get; set; } = new List<Canciones>();
    }
}
