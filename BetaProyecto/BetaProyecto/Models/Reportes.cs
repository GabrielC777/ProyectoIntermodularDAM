using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.Models
{
    public class Reportes
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("tipo_problema")]
        public string TipoProblema { get; set; }

        [BsonElement("descripcion")]
        public string Descripcion { get; set; }

        [BsonElement("estado")]
        public string Estado { get; set; } = "Pendiente";

        [BsonElement("referencias")]
        public ReferenciasReporte Referencias { get; set; }

        [BsonElement("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [BsonElement("resolucion")]
        public string Resolucion { get; set; } = "";
        //Campos calculados en la aplicación, no se almacenan en la BD
        [BsonIgnore]
        public string NombreReportante { get; set; } = "Usuario Desconocido";

        [BsonIgnore]
        public string TituloCancionReportada { get; set; } = "Canción Desconocida";

        // Propiedad para cambiar el color de la tarjeta según estado
        [BsonIgnore]
        public string ColorEstado => Estado switch
        {
            "Pendiente" => "#FF5252",      // Rojo
            "Investigando" => "#FFB300",   // Naranja
            "Finalizado" => "#4CAF50",     // Verde
            _ => "Gray"
        };
    }

    public class ReferenciasReporte
    {
        [BsonElement("usuario_reportante_id")]
        public string UsuarioReportanteId { get; set; }

        [BsonElement("cancion_reportada_id")]
        public string CancionReportadaId { get; set; }
    }
}
