using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.Models
{
    public class TarjetasListas
    {
        public string TituloSeccion { get; set; }
        public ObservableCollection<ListaPersonalizada> Listas { get; set; }

        public TarjetasListas(string titulo, ObservableCollection<ListaPersonalizada> listas)
        {
            TituloSeccion = titulo;
            Listas = listas;
        }
    }
}
