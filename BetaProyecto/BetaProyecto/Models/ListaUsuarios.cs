using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.Models
{
    public class ListaUsuarios
    {
        public string Titulo { get; set; }
        public ObservableCollection<Usuarios> Lista { get; set; }

        public ListaUsuarios(string titulo, ObservableCollection<Usuarios> lista)
        {
            Titulo = titulo;
            Lista = lista;
        }
    }
}
