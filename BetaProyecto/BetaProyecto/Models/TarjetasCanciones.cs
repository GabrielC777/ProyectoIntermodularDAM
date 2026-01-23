using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.Models
{
    public class TarjetasCanciones
    {
        public string TituloSeccion { get; set; }
        public ObservableCollection<Canciones> ListaCanciones { get; set; }
        
        public TarjetasCanciones(string titulo,ObservableCollection<Canciones> canciones)
        {
            TituloSeccion = titulo;
            ListaCanciones = canciones;
        }
    }
}
