
using System.Threading.Tasks;

namespace BetaProyecto.Services
{
    public interface IDialogoService

    {   // Solo definimos la interfaz, sin detalles de implementación ni referencias 
        void MostrarAlerta(string mensaje);
        Task<bool> Preguntar(string titulo, string mensaje, string textoSi, string textoNo);
    }
}
