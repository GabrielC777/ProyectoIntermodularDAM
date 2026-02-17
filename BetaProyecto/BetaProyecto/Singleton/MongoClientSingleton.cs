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
        //Singleton
        private static MongoClientSingleton _instance;
        public static MongoClientSingleton Instance => _instance ??= new MongoClientSingleton();
        
        // Objeto guardado 
        public MongoAtlas Cliente { get; private set; }

        // Constructor
        private MongoClientSingleton(){
            
            Cliente = new MongoAtlas();
        }
    }
}
