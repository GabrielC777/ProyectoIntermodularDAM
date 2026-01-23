using BetaProyecto.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.Singleton
{
    public class MongoClientSingleton
    {
        // 1. EL SINGLETON (La estructura básica)
        private static MongoClientSingleton _instance;
        public static MongoClientSingleton Instance => _instance ??= new MongoClientSingleton();
        
        // 2. EL OBJETO GUARDADO (Tu clase lógica)
        // Aquí es donde "guardas el objeto" para acceder a él luego.
        public MongoAtlas Cliente { get; private set; }

        // 3. Constructor
        private MongoClientSingleton(){
            
            Cliente = new MongoAtlas();
        }
    }
}
