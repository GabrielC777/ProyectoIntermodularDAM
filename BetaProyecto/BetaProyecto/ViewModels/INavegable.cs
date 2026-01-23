using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.ViewModels
{
    public interface INavegable
    {
        Action VolverAtras { get; set; }
    }
}
