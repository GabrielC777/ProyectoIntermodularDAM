
using System.Threading.Tasks;

namespace BetaProyecto.Services
{
    public interface IDialogoService
    
    {   // Solo la firma del método. Nada de código aquí.
        void MostrarAlerta(string mensaje);
        Task<bool> Preguntar(string titulo, string mensaje, string textoSi, string textoNo);
    }
}
