using BetaProyecto.Helpers;
using BetaProyecto.Models;
using BetaProyecto.Singleton;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BetaProyecto.Services
{
    public class MongoAtlas
    {

        /////////////////////////////////
        // Variables de la base de datos
        ////////////////////////////////
        // Las declaramos aquí fuera para que no se "mueran" al terminar la función
        private MongoClient _client;

        // Esta propiedad pública permitirá que los ViewModels puedan pedir datos a la BD
        public IMongoDatabase Database { get; private set; }
        public MongoAtlas()
        {

        }
        // Método para conectar a la base de datos en la nube
        // Esto permite que la conexion siga viva mientras estamos en la aplicación
        public async Task<bool> Conectar()
        {
            if (Database != null)
            {
                // Ya existe una conexion
                return true;
            }
            else
            {
                const string connectionUri = "mongodb+srv://admin:12345@appaudiolibcluster.6orokhr.mongodb.net/?appName=AppAudioLibCluster";
                try
                {
                    // 1. Usamos la configuración automática
                    var settings = MongoClientSettings.FromConnectionString(connectionUri);

                    settings.ServerApi = new ServerApi(ServerApiVersion.V1);

                    // Creamos el cliente y nos conectamos al server
                    _client = new MongoClient(settings);
                    Database = _client.GetDatabase("AppAudioLibDB");

                    // Enviamos un ping para confirmar una conexión exitosa
                    var result = await Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
                    System.Diagnostics.Debug.WriteLine("Conexión exitosa a MongoDB");
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("****DEBUG****" + ex);
                    return false;
                }
            }

        }
        public async Task<Usuarios> LoginUsuario(string username, string password)
        {
            try
            {
                // Conectamos con la colección que queremos. 
                var coleccionUsuarios= Database.GetCollection<Usuarios>("usuarios");
                
                string passwordHash = Encriptador.HashPassword(password);

                // Buscamos un usuario que tenga ESE username Y ESA contraseña
                var usuario = await coleccionUsuarios
                                        .Find(u => u.Username == username && u.Password == passwordHash)
                                        .FirstOrDefaultAsync();
                // 3. Devolvemos el resultado (será 'null' si no lo encuentra)
                return usuario;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en Login: " + ex.Message);
                return null;
            }
        }


        #region Metodos Select / Obtener Datos 
        /////////////////////////////////////////
        ////METODOS SELECT O OBTENER DE DATOS////
        /////////////////////////////////////////
        #region Para obtener canciones
        public async Task<List<Canciones>> ObtenerCancionesFavoritos()
        {
            var listafavoritos = GlobalData.Instance.favoritosGD;
            //Comprobarmos si en la lista de favoritos hay algo
            if (listafavoritos.Equals(null) || listafavoritos.Count == 0 )
            {
                System.Diagnostics.Debug.WriteLine("No se encontraron datos");
                return new List<Canciones>(); 
            }

            if(!await Conectar()){
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<Canciones>();

            }

            try
            {
                //Apuntamos a la tablas de queremos de la base de datos
                var coleccionCanciones = Database.GetCollection<Canciones>("canciones");
                
                //Creamo un filtro que dice "Buscame todas las canciones que tengan el ID en esta lista"
                var filtro = Builders<Canciones>.Filter.In(c => c.Id, listafavoritos);

                var listaObtenida = await coleccionCanciones.Find(filtro).ToListAsync();
                
                // RELLENAMOS EL NOMBRE DEL ARTISTA/S
                await RellenarNombresDeArtistas(listaObtenida);
                
                //Devolvemos las canciones encontradas
                return listaObtenida; 

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error recuperando canciones favoritas: " + ex.Message);
                return new List<Canciones>();
            }

        }//ObtenerCancionesFavoritos
        public async Task<List<Canciones>> ObtenerCacionesNovedades()
        {
            if (!await Conectar())
            {
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<Canciones>();

            }

            try
            {
                // Apuntamos a la colección. 
                var coleccionCanciones = Database.GetCollection<Canciones>("canciones");

                // Traemos todas las canciones
                var listaCanciones = await coleccionCanciones.Find(_ => true)
                                                                .SortByDescending(c => c.Id)
                                                                .Limit(10)
                                                                .ToListAsync();
                // RELLENAMOS EL NOMBRE DEL ARTISTA/S
                await RellenarNombresDeArtistas(listaCanciones);

                return listaCanciones;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo canciones: " + ex.Message);
                return new List<Canciones>();
            }
        }

        public async Task<List<Canciones>> ObtenerCancionesPorGenero(string genero)
        {
            if (!await Conectar())
            {
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<Canciones>();

            }

            try
            {
                // Apuntamos a la colección. 
                var coleccionCanciones = Database.GetCollection<Canciones>("canciones");

                // Hacemos el Find buscado por género
                var listaCanciones = await coleccionCanciones.Find(c => c.Datos.Generos.Contains(genero))
                                                     .Limit(15)
                                                     .ToListAsync();

                // Rellenamos autores igual que antes
                // RELLENAMOS EL NOMBRE DEL ARTISTA/S
                await RellenarNombresDeArtistas(listaCanciones);

                return listaCanciones;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo canciones: " + ex.Message);
                return new List<Canciones>();
            }
        }
        //Obtener todas las canciones
        public async Task<List<Canciones>> ObtenerCanciones()
        {
            if (!await Conectar())
            {
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<Canciones>();

            }

            try
            {
                // Apuntamos a la colección. 
                // ¡IMPORTANTE! Verifica en Mongo si se llama "canciones" o "Canciones"
                var coleccionCanciones = Database.GetCollection<Canciones>("canciones");
                var coleccionUsuarios = Database.GetCollection<Usuarios>("usuarios");

                // Traemos todas las canciones
                var listaCanciones = await coleccionCanciones.Find(_ => true).ToListAsync();

                // Creamos una lista temporal para guardar los nombres de los autores

                // RELLENAMOS EL NOMBRE DEL ARTISTA/S
                await RellenarNombresDeArtistas(listaCanciones);

                return listaCanciones;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo canciones: " + ex.Message);
                return new List<Canciones>();
            }
        }       
        public async Task<List<Canciones>> ObtenerCancionesPorListaIds(List<string> ids)
        {
            if (!await Conectar())
            {
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<Canciones>();
            }

            var coleccionCanciones = Database.GetCollection<Canciones>("canciones");

            // Filtro "IN": Dame todas las canciones cuyo ID esté en esta lista
            var filtro = Builders<Canciones>.Filter.In(c => c.Id, ids);

            var listacanciones = await coleccionCanciones.Find(filtro).ToListAsync();

            await RellenarNombresDeArtistas(listacanciones);

            return listacanciones;
        }

        public async Task<List<Canciones>> ObtenerCancionesPorBusqueda(string textoBusqueda)
        {
            if (!await Conectar())
            {
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<Canciones>();
            }
            try
            {
                // Apuntamos a la colección. 
                var coleccionCanciones = Database.GetCollection<Canciones>("canciones");

                // Hacemos el Find buscado por nombre (IGNORA LAS MAYUSCULAS Y MINUSCULAS poniendo la "i" activamos esto)
                var filtro = Builders<Canciones>.Filter.Regex(c => c.Titulo, new BsonRegularExpression(textoBusqueda, "i"));

                var listaCanciones = await coleccionCanciones.Find(filtro).ToListAsync();
                // RELLENAMOS EL NOMBRE DEL ARTISTA/S
                await RellenarNombresDeArtistas(listaCanciones);

                return listaCanciones;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo canciones: " + ex.Message);
                return new List<Canciones>();
            }
        }
        public async Task<List<Canciones>> ObtenerCancionesPorAutor(string idAutor)
        {
            if (!await Conectar())
            {
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<Canciones>();
            }

            try
            {
                var coleccion = Database.GetCollection<Canciones>("canciones");

                // 'AnyEq' sirve para buscar un valor dentro de un array en Mongo
                var filtro = Builders<Canciones>.Filter.AnyEq(c => c.AutoresIds, idAutor);

                var lista = await coleccion.Find(filtro).ToListAsync();

                await RellenarNombresDeArtistas(lista);

                return lista;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo canciones por autor: " + ex.Message);
                return new List<Canciones>();
            }
        }

        #endregion

        #region Para obtener usuarios
        public async Task<List<Usuarios>> ObtenerTodosLosUsuarios()
        {
            if (!await Conectar())
            {
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<Usuarios>();
            }
            try
            {
                var coleccionUsuarios = Database.GetCollection<Usuarios>("usuarios");

                // Traemos la lista (limitada a 20 o 50 para no sobrecargar si hay millones)
                var listaUsuarios = await coleccionUsuarios.Find(_ => true).Limit(50).ToListAsync();

                return listaUsuarios;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al obtener usuarios: " + ex.Message);
                return new List<Usuarios>();
            }

        }
        public async Task<List<Usuarios>> ObtenerUsuariosPorBusqueda(string textoBusqueda, List<string> idsExcluidos)
        {
            if (!await Conectar())
            {
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<Usuarios>();
            }
            try
            {
                var coleccionUsuarios = Database.GetCollection<Usuarios>("usuarios");

                // 1. Filtro básico: Buscar por nombre (ignorando mayúsculas/minúsculas)
                var filtroBusqueda = Builders<Usuarios>.Filter.Regex(c => c.Username, new BsonRegularExpression(textoBusqueda, "i"));

                // 2. Filtro de exclusión: Si nos pasan IDs, le decimos a Mongo "Busca X, PERO que el ID NO ESTÉ en esta lista"
                // 'Nin' significa "Not In" (No está en...)
                var filtroExclusion = Builders<Usuarios>.Filter.Nin(u => u.Id, idsExcluidos);

                //Declaramos un filtro
                FilterDefinition<Usuarios> filtroFinal;

                // Combinamos ambos filtros con un AND
                filtroFinal = Builders<Usuarios>.Filter.And(filtroBusqueda, filtroExclusion);

                // Ejecutamos la consulta con el filtro combinado
                var listaUsuarios = await coleccionUsuarios.Find(filtroFinal).ToListAsync();

                return listaUsuarios;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo canciones: " + ex.Message);
                return new List<Usuarios>();
            }
        }
        public async Task<List<Usuarios>> ObtenerUsuariosPorListaIds(List<string> listaIds)
        {
            // 1. Si la lista está vacía, nos ahorramos el viaje a la base de datos
            if (listaIds == null || listaIds.Count == 0)
            {
                return new List<Usuarios>();
            }

            // 2. Aseguramos conexión
            if (!await Conectar())
            {
                return new List<Usuarios>();
            }

            try
            {
                var coleccionUsuarios = Database.GetCollection<Usuarios>("usuarios");

                // 3. EL TRUCO DE RENDIMIENTO: Filter.In
                // Busca cualquier usuario cuyo Id esté DENTRO de la lista 'listaIds'
                var filtro = Builders<Usuarios>.Filter.In(u => u.Id, listaIds);

                // 4. Devolvemos la lista completa de objetos Usuario
                return await coleccionUsuarios.Find(filtro).ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al obtener lista de usuarios por IDs: " + ex.Message);
                return new List<Usuarios>();
            }
        }
        #endregion
 
        #region Para obtener listaspersonalizas
        // 1. Obtener todas las listas (o las de un usuario concreto si quisieras filtrar)
        public async Task<List<ListaPersonalizada>> ObtenerListasReproduccion()
        {
            if (!await Conectar()) { 
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<ListaPersonalizada>(); 
            }
            try
            {
                var coleccionListas = Database.GetCollection<ListaPersonalizada>("listapersonalizada");
                var listas = await coleccionListas.Find(_ => true).ToListAsync();

                // TRUCO: Rellenamos las canciones reales para cada lista
                foreach (var lista in listas)
                {
                    if (lista.IdsCanciones.Count > 0)
                    {
                        lista.CancionesCompletas = await ObtenerCancionesPorListaIds(lista.IdsCanciones);
                    }
                }

                return listas;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo playlists: " + ex.Message);
                return new List<ListaPersonalizada>();
            }
        }
        public async Task<List<ListaPersonalizada>> ObtenerPlaylistsPorCreador(string idUsuario)
        {
            if (!await Conectar())
            {
                System.Diagnostics.Debug.WriteLine("Error de conexión");
                return new List<ListaPersonalizada>();
            }

            try
            {
                var coleccion = Database.GetCollection<ListaPersonalizada>("listapersonalizada");

                // 'AnyEq' sirve para buscar un valor dentro de un campo normal en este caso IdUsuario en Mongo
                var filtro = Builders<ListaPersonalizada>.Filter.Eq(l => l.IdUsuario, idUsuario);

                var listas = await coleccion.Find(filtro).ToListAsync();

                foreach (var lista in listas)
                {
                    if (lista.IdsCanciones.Count > 0)
                    {
                        lista.CancionesCompletas = await ObtenerCancionesPorListaIds(lista.IdsCanciones);
                    }
                }

                return listas;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo playlists por creador: " + ex.Message);
                return new List<ListaPersonalizada>();
            }
        }
        #endregion


        //Otros metodos select
        public async Task<List<Reportes>> ObtenerReportes()
        {
            if (!await Conectar())
            {
                return new List<Reportes>();
            }

            try
            {
                var coleccion = Database.GetCollection<Reportes>("reportes");
                // Los traemos ordenados por fecha (los más nuevos primero)
                var lista = await coleccion.Find(_ => true)
                                           .SortByDescending(r => r.FechaCreacion)
                                           .ToListAsync();

                // RELLENAMOS LOS NOMBRES REALES (Usuario y Canción)
                var colUsuarios = Database.GetCollection<Usuarios>("usuarios");
                var colCanciones = Database.GetCollection<Canciones>("canciones");

                foreach (var reporte in lista)
                {
                    
                    var usuario = await colUsuarios.Find(u => u.Id == reporte.Referencias.UsuarioReportanteId)
                                                   .Project(u => new { u.Username })
                                                   .FirstOrDefaultAsync();
                  
                    reporte.NombreReportante = usuario != null ? usuario.Username : "Usuario Eliminado";
                    

                    var cancion = await colCanciones.Find(c => c.Id == reporte.Referencias.CancionReportadaId)
                                                    .Project(c => new { c.Titulo })
                                                    .FirstOrDefaultAsync();
                   
                    reporte.TituloCancionReportada = cancion != null ? cancion.Titulo : "Canción Eliminada";
                    
                }
            
                return lista;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo reportes: " + ex.Message);
                return new List<Reportes>();
            }
        }

        public async Task<List<string>> ObtenerNombresGeneros()
        {
            if (!await Conectar())
            {
                return new List<string>();
            }
            try
            {
                var coleccion = Database.GetCollection<Generos>("generos");

                // 2. Buscamos todos los datos de la coleccionUsuarios generos
                var listaGeneros = await coleccion.Find(_ => true)
                                                  .SortBy(g => g.Nombre)
                                                  .ToListAsync();

                // Todo OK: Devolvemos los nombres
                return listaGeneros.Select(g => g.Nombre).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo géneros: " + ex.Message);
                return new List<string>(); // Devolvemos lista vacía si falla
            }
        }
        public async Task<List<Generos>> ObtenerGenerosCompletos()
        {
            if (!await Conectar())
            {
                return new List<Generos>();
            }

            try
            {
                var coleccion = Database.GetCollection<Generos>("generos");
                return await coleccion.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error obteniendo géneros: " + ex.Message);
                return new List<Generos>(); // Devolvemos lista vacía si falla
            }
        }
        #endregion

        #region Metodos Inserts
        /////////////////////////////////////////
        ////////////METODOS INSERTS//////////////
        /////////////////////////////////////////
        public async Task<bool> AgregarAFavorito(string idUsuario, string idCancion)
        {
            if (!await Conectar()) 
            {
                return false;
            }
            try
            {
                var coleccionUsuarios = Database.GetCollection<Usuarios>("usuarios");
                //Configuramos el filtro para encontrar el usuario a actualizar (Usuario)
                var filtro = Builders<Usuarios>.Filter.Eq(u => u.Id, idUsuario);
                //Y ahora buscamos la lista de favoritos que queremos actualizar y le decimos el ID de la canción que tiene que añadir (Update configurado)
                var update = Builders<Usuarios>.Update.AddToSet("listas.favoritos", ObjectId.Parse(idCancion));
                //Ejecutamos la actualización
                var documentosActualizados = await coleccionUsuarios.UpdateOneAsync(filtro, update);

                var seActualizo = documentosActualizados.ModifiedCount > 0;

                return seActualizo;
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine("Error Añadir a Fav: " + ex); 
                return false; 
            }
        }
        public async Task<bool> SeguirUsuario(string idUsuario, string idUsuarioASeguir)
        {
            if (!await Conectar())
            {
                return false;
            }

            try
            {
                var coleccionUsuarios = Database.GetCollection<Usuarios>("usuarios");

                var filtro = Builders<Usuarios>.Filter.Eq(u => u.Id, idUsuario);
                
                var update = Builders<Usuarios>.Update.AddToSet("listas.seguidores", ObjectId.Parse(idUsuarioASeguir));

                var documentosActualizados = await coleccionUsuarios.UpdateOneAsync(filtro, update);

                var seActualizo = documentosActualizados.ModifiedCount > 0;

                return seActualizo;
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine("Error al seguir: " + ex); 
                return false; 
            }
        }
        public async Task<bool> PublicarCancion(Canciones nuevaCancion)
        {
            if (!await Conectar())
            {
                return false;
            }

            try
            {
                var coleccionCanciones = Database.GetCollection<Canciones>("canciones");
                await coleccionCanciones.InsertOneAsync(nuevaCancion);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al publicar canción: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> CrearListaReproduccion(ListaPersonalizada nuevaLista)
        {
            if (!await Conectar())
            {
                return false;
            }

            try
            {
                var coleccion = Database.GetCollection<ListaPersonalizada>("listapersonalizada");
                await coleccion.InsertOneAsync(nuevaLista);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error creando lista: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> EnviarReporte(Reportes nuevoReporte)
        {
            if (!await Conectar())
            {
                return false;
            }

            try
            {
                var coleccion = Database.GetCollection<Reportes>("reportes");
                await coleccion.InsertOneAsync(nuevoReporte);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al enviar reporte: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> CrearGenero(string nuevoGenero)
        {
            if (!await Conectar())
            {
                return false;
            }
            try
            {
                var coleccion = Database.GetCollection<Generos>("generos");

                // Comprobamos si hay algun género con el mismo nombre (ignorando mayúsculas/minúsculas)
                var filtro = Builders<Generos>.Filter.Regex(g => g.Nombre, new MongoDB.Bson.BsonRegularExpression($"^{nuevoGenero}$", "i"));

                var existe = await coleccion.Find(filtro).AnyAsync();
                
                if (existe)
                {
                    return false; 
                } 

                // Si no existe, lo creamos
                var nuevo = new Generos { Nombre = nuevoGenero };
                await coleccion.InsertOneAsync(nuevo);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al insetar un nuevo género: " + ex.Message);
                return false;
            }
        }
        // Añadir esto en la región "Metodos Inserts" de MongoAtlas.cs
        public async Task<bool> CrearUsuario(Usuarios nuevoUsuario)
        {
            if (!await Conectar())
            {
                return false;
            }
            try
            {
                var coleccion = Database.GetCollection<Usuarios>("usuarios");

                // Opcional: Verificar si el email o username ya existen antes de insertar
                var existe = await coleccion.Find(u => u.Email == nuevoUsuario.Email).AnyAsync();
                if (existe) return false;

                await coleccion.InsertOneAsync(nuevoUsuario);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error registrando usuario: " + ex.Message);
                return false;
            }
        }
        #endregion

        #region Metodos Deletes
        /////////////////////////////////////////
        /////////////METODOS DELETE//////////////
        /////////////////////////////////////////
        public async Task<bool> EliminarDeFavorito(string idUsuario, string idCancion)
        {
            if (!await Conectar())
            {
                return false;
            }
            try
            {
                var coleccionUsuarios = Database.GetCollection<Usuarios>("usuarios");
                //Configuramos el filtro para encontrar el usuario a actualizar (Usuario)
                var filtro = Builders<Usuarios>.Filter.Eq(u => u.Id, idUsuario);
                //Y ahora buscamos la lista de favoritos que queremos actualizar y le decimos el ID de la canción que tiene que añadir (Update configurado)
                var update = Builders<Usuarios>.Update.Pull("listas.favoritos", ObjectId.Parse(idCancion));
                //Ejecutamos la actualización
                var documentosActualizados = await coleccionUsuarios.UpdateOneAsync(filtro, update);

                var seActualizo = documentosActualizados.ModifiedCount > 0;

                return seActualizo;
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine("Error Eliminar de Fav: " + ex); 
                return false; 
            }
        }
        public async Task<bool> DejarDeSeguirUsuario(string idUsuario, string idUsuarioADejar)
        {
            if (!await Conectar())
            {
                return false;
            }
            try
            {
                var coleccionUsuarios = Database.GetCollection<Usuarios>("usuarios");

                var filtro = Builders<Usuarios>.Filter.Eq(u => u.Id, idUsuario);

                var update = Builders<Usuarios>.Update.Pull("listas.seguidores", ObjectId.Parse(idUsuarioADejar));

                var documentosActualizados = await coleccionUsuarios.UpdateOneAsync(filtro, update);

                var seActualizo = documentosActualizados.ModifiedCount > 0;

                return seActualizo;
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine("Error al dejar de seguir: " + ex); 
                return false; 
            }
        }
        public async Task<bool> EliminarCancionPorId(string idCancion)
        {
            if(!await Conectar())
            {
                return false;
            }
            // 1. LIMPIAR DE LISTAS DE REPRODUCCIÓN
            var colListas = Database.GetCollection<ListaPersonalizada>("listapersonalizada");

            // El driver ve [BsonRepresentation] y convierte 'idCancion' a ObjectId solo
            var updateListas = Builders<ListaPersonalizada>.Update.Pull(l => l.IdsCanciones, idCancion);
            var filtroListas = Builders<ListaPersonalizada>.Filter.AnyEq(l => l.IdsCanciones, idCancion);

            await colListas.UpdateManyAsync(filtroListas, updateListas);


            // 2. LIMPIAR DE FAVORITOS DE USUARIOS
            var colUsuarios = Database.GetCollection<Usuarios>("usuarios");

            // Aquí igual: Accedemos a la propiedad tipada y el driver hace la magia
            var updateUsuarios = Builders<Usuarios>.Update.Pull(u => u.Listas.Favoritos, idCancion);
            var filtroUsuarios = Builders<Usuarios>.Filter.AnyEq(u => u.Listas.Favoritos, idCancion);

            await colUsuarios.UpdateManyAsync(filtroUsuarios, updateUsuarios);
            var coleccionCanciones= Database.GetCollection<Canciones>("canciones");

            var filtro = Builders<Canciones>.Filter.Eq(c => c.Id, idCancion);

            var documentosEliminados = await coleccionCanciones.DeleteOneAsync(filtro);

            var seElimino = documentosEliminados.DeletedCount > 0;

            return seElimino;
        }

        public async Task<bool> EliminarPlaylistPorId(string idPlaylist)
        {
            if (!await Conectar())
            {
                return false;
            }
            var coleccionLista= Database.GetCollection<ListaPersonalizada>("listapersonalizada");

            var filtro = Builders<ListaPersonalizada>.Filter.Eq(l => l.Id, idPlaylist);
            
            var documentosEliminados = await coleccionLista.DeleteOneAsync(filtro);

            var seElimino = documentosEliminados.DeletedCount > 0;

            return seElimino;
        }
        public async Task<bool> EliminarGenero(Generos generoAEliminar)
        {
            if (!await Conectar())
            {
                return false;
            }
            try

            {
                var colCanciones = Database.GetCollection<Canciones>("canciones");
                var colGeneros = Database.GetCollection<Generos>("generos");

                // 1. VERIFICAR USO 
                // Buscamos si existe alguna canción que contenga este nombre de género
                var filtroEnUso = Builders<Canciones>.Filter.AnyEq(c => c.Datos.Generos, generoAEliminar.Nombre);

                var estaEnUso = await colCanciones.Find(filtroEnUso).AnyAsync();

                if (estaEnUso)
                {
                    System.Diagnostics.Debug.WriteLine($"[PROTECCIÓN] '{generoAEliminar.Nombre}' está en uso.");
                    return false;
                }

                var result = await Database.GetCollection<Generos>("generos").DeleteOneAsync(g => g.Id == generoAEliminar.Id);

                var EsEliminado = result.DeletedCount > 0;

                return EsEliminado; 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al insetar un nuevo género: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> EliminarUsuario(string idUsuario)
        {
            if (!await Conectar())
            {
                return false;
            }
            try {
                var colUsuarios = Database.GetCollection<Usuarios>("usuarios");
                var colCanciones = Database.GetCollection<Canciones>("canciones");
                var colListas = Database.GetCollection<ListaPersonalizada>("listapersonalizada");

                // 1. LIMPIAR DE CANCIONES (Como Autor)
                // 'AutoresIds' tiene el atributo, así que pasamos string directo
                var updateCanciones = Builders<Canciones>.Update.Pull(c => c.AutoresIds, idUsuario);
                var filtroCanciones = Builders<Canciones>.Filter.AnyEq(c => c.AutoresIds, idUsuario);

                await colCanciones.UpdateManyAsync(filtroCanciones, updateCanciones);


                // 2. LIMPIAR SEGUIDORES DE OTROS USUARIOS
                // 'Seguidores' tiene el atributo, pasamos string directo
                var updateSeguidores = Builders<Usuarios>.Update.Pull(u => u.Listas.Seguidores, idUsuario);
                var filtroSeguidores = Builders<Usuarios>.Filter.AnyEq(u => u.Listas.Seguidores, idUsuario);

                await colUsuarios.UpdateManyAsync(filtroSeguidores, updateSeguidores);


                // 3. BORRAR SUS PLAYLISTS
                await colListas.DeleteManyAsync(l => l.IdUsuario == idUsuario);

                // 1. Borrar el usuario
                var resUser = await Database.GetCollection<Usuarios>("usuarios").DeleteOneAsync(u => u.Id == idUsuario);

                // 2. (Opcional) Borrar sus playlists para no dejar basura
                await Database.GetCollection<ListaPersonalizada>("listapersonalizada").DeleteManyAsync(l => l.IdUsuario == idUsuario);

                return resUser.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al insetar un nuevo género: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> EliminarReporte(string idReporte)
        {
            if (!await Conectar())
            {
                return false;
            }
            try
            {
                var coleccion = Database.GetCollection<Reportes>("reportes");
                var resultado = await coleccion.DeleteOneAsync(r => r.Id == idReporte);
                return resultado.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al eliminar reporte: " + ex.Message);
                return false;
            }
        }
        #endregion

        #region Metodos Updates
        /////////////////////////////////////////
        ////////////METODOS UPDATES//////////////
        /////////////////////////////////////////
        public async Task<bool> ActualizarPerfilUsuario(string idUsuario, string nuevoNombre, string nuevoEmail, string nuevoPais, DateTime nuevaFecha, bool esPrivada)
        {
            //Preparamos el contructor y una lista con para guardar los cambios
            var builder = Builders<Usuarios>.Update;
            var listaCambios = new List<UpdateDefinition<Usuarios>>();

            // Comparamos con lo que tenemos en memoria (GlobalData) para ver si cambió

            // Si el algo ha cambiado, lo añadimos a la lista de cambios 
            if (nuevoNombre != GlobalData.Instance.usernameGD)
                listaCambios.Add(builder.Set(u => u.Username, nuevoNombre));

            if (nuevoEmail != GlobalData.Instance.emailGD)
                listaCambios.Add(builder.Set(u => u.Email, nuevoEmail));

            if (nuevoPais != GlobalData.Instance.paisGD)
                listaCambios.Add(builder.Set(u => u.Perfil.Pais, nuevoPais));

            if (nuevaFecha.Date != GlobalData.Instance.fechaNacimientoGD.Date)
                listaCambios.Add(builder.Set(u => u.Perfil.FechaNacimiento, nuevaFecha));

            if (esPrivada != GlobalData.Instance.es_PrivadaGD)
                listaCambios.Add(builder.Set(u => u.Perfil.EsPrivada, esPrivada));

            // Si no hay cambios no llamamos al método
            if (listaCambios.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No se detectaron cambios. No se envió nada a Mongo.");
                return false;
            }

            // Ejecutamos actualización
            if (!await Conectar())
            {
                return false;
            }

            var filtro = Builders<Usuarios>.Filter.Eq(u => u.Id, idUsuario);

            // Combinamos todas las pequeñas actualizaciones en una sola
            var updateFinal = builder.Combine(listaCambios);

            var resultado = await Database.GetCollection<Usuarios>("usuarios").UpdateOneAsync(filtro, updateFinal);

            var EsActualizado = resultado.MatchedCount > 0;

            return EsActualizado;
        }// Método ESPECÍFICO para el Panel de Administración (Actualiza TODO)
        public async Task<bool> ActualizarUsuario(string id, string nombre, string email, string password, string rol, string pais, string imagenUrl, DateTime fecha, bool esPrivada)
        {
            if (!await Conectar())
            {
                return false;
            }
            try
            {
                var builder = Builders<Usuarios>.Update;
                var listaCambios = new List<UpdateDefinition<Usuarios>>();

                listaCambios.Add(builder.Set(u => u.Username, nombre));
                listaCambios.Add(builder.Set(u => u.Email, email));
                listaCambios.Add(builder.Set(u => u.Password, password));
                listaCambios.Add(builder.Set(u => u.Rol, rol));
                listaCambios.Add(builder.Set(u => u.Perfil.Pais, pais));
                listaCambios.Add(builder.Set(u => u.Perfil.ImagenUrl, imagenUrl));
                listaCambios.Add(builder.Set(u => u.Perfil.FechaNacimiento, fecha));
                listaCambios.Add(builder.Set(u => u.Perfil.EsPrivada, esPrivada));

                var filtro = Builders<Usuarios>.Filter.Eq(u => u.Id, id);

                var updateFinal = builder.Combine(listaCambios);

                var resultado = await Database.GetCollection<Usuarios>("usuarios").UpdateOneAsync(filtro, updateFinal);

                var EsActualizado = resultado.MatchedCount > 0;

                return EsActualizado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error actualizando usuario desde admin: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> ActualizarPlaylist(string nuevoNombre, string nuevaDesc, List<string> nuevasCanciones, string nuevaPortada, ListaPersonalizada original)
        {
            try
            {
                //Preparamos el contructor y una lista con para guardar los cambios
                var builder = Builders<ListaPersonalizada>.Update;
                var listaCambios = new List<UpdateDefinition<ListaPersonalizada>>();

                // 2. COMPARACIONES INTELIGENTES
                // Comparamos lo que nos llega nuevo vs lo que tenía el objeto original

                if (nuevoNombre != original.Nombre)
                {
                    listaCambios.Add(builder.Set(p => p.Nombre, nuevoNombre));
                }

                if (nuevaDesc != original.Descripcion)
                {
                    listaCambios.Add(builder.Set(p => p.Descripcion, nuevaDesc));
                }

                if (nuevaPortada != original.UrlPortada)
                {
                    listaCambios.Add(builder.Set(p => p.UrlPortada, nuevaPortada));
                }

                // SequenceEqual comprueba si tienen los mismos elementos en el mismo orden.
                if (nuevasCanciones != null && !nuevasCanciones.SequenceEqual(original.IdsCanciones ?? new List<string>()))
                {
                    listaCambios.Add(builder.Set(p => p.IdsCanciones, nuevasCanciones));
                }

                // Si no hay cambios no llamamos al método
                if (listaCambios.Count == 0)
                {
                    return true;
                }

                // Ejecutamos actualización
                if (!await Conectar())
                {
                    return false; 
                }

                var filtro = Builders<ListaPersonalizada>.Filter.Eq(p => p.Id, original.Id);
                var updateFinal = builder.Combine(listaCambios);

                var resultado = await Database.GetCollection<ListaPersonalizada>("listapersonalizada").UpdateOneAsync(filtro, updateFinal);

                return resultado.MatchedCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ERROR MONGO] Al actualizar playlist: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> ActualizarCancion(string nuevoTitulo, string nuevaPortada, List<string> nuevosAutores, List<string> nuevosGeneros, Canciones original)
        {
            try
            {
                var builder = Builders<Canciones>.Update;
                var listaCambios = new List<UpdateDefinition<Canciones>>();

                // 1. COMPARACIONES

                // -- Título --
                if (nuevoTitulo != original.Titulo)
                {
                    listaCambios.Add(builder.Set(c => c.Titulo, nuevoTitulo));
                }

                // -- Portada --
                if (nuevaPortada != original.ImagenPortadaUrl)
                {
                    listaCambios.Add(builder.Set(c => c.ImagenPortadaUrl, nuevaPortada));
                }

                // -- Autores (Colaboradores) --
                // Usamos SequenceEqual para ver si la lista de IDs es idéntica
                if (nuevosAutores != null && !nuevosAutores.SequenceEqual(original.AutoresIds))
                {
                    listaCambios.Add(builder.Set(c => c.AutoresIds, nuevosAutores));
                }

                // -- Géneros (Dentro del objeto Datos) --
                var generosOriginales = original.Datos?.Generos ?? new List<string>();

                if (nuevosGeneros != null && !nuevosGeneros.SequenceEqual(generosOriginales))
                {
                    // Actualizamos la propiedad anidada "Datos.Generos"
                    listaCambios.Add(builder.Set(c => c.Datos.Generos, nuevosGeneros));
                    System.Diagnostics.Debug.WriteLine($"[MONGO] Cambio en Géneros ({nuevosGeneros.Count})");
                }

                // 2. SI NO HAY CAMBIOS, SALIMOS
                if (listaCambios.Count == 0) return true;

                // 3. EJECUTAR ACTUALIZACIÓN
                if (!await Conectar()) return false;

                var filtro = Builders<Canciones>.Filter.Eq(c => c.Id, original.Id);
                var updateFinal = builder.Combine(listaCambios);

                var resultado = await Database.GetCollection<Canciones>("canciones")
                                              .UpdateOneAsync(filtro, updateFinal);

                var EsActualizado = resultado.MatchedCount > 0;

                return EsActualizado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ERROR MONGO] ActualizarCancion: " + ex.Message);
                return false;
            }
        }
        public async Task<bool> ActualizarEstadoReporte(string nuevoEstado, string nuevaResolucion, Reportes original)
        {
            if (!await Conectar())
            {
                return true;
            }
            try
            {
                var builder = Builders<Reportes>.Update;
                var listaCambios = new List<UpdateDefinition<Reportes>>();

                // -- Estado --
                if (nuevoEstado != original.Estado)
                {
                    listaCambios.Add(builder.Set(r => r.Estado, nuevoEstado));
                }

                // -- Resolución (Notas del admin) --
              
                if ((nuevaResolucion ?? "") != (original.Resolucion ?? ""))
                {
                    listaCambios.Add(builder.Set(r => r.Resolucion, nuevaResolucion));
                }

                // 3. SI NO HAY CAMBIOS, NO HACEMOS NADA
                if (listaCambios.Count == 0) 
                {
                    return true;
                }

                // 4. EJECUTAR ACTUALIZACIÓN
                var coleccion = Database.GetCollection<Reportes>("reportes");
                var filtro = Builders<Reportes>.Filter.Eq(r => r.Id, original.Id);

                var updateFinal = builder.Combine(listaCambios);

                var resultado = await coleccion.UpdateOneAsync(filtro, updateFinal);
                
                var EsActualizado = resultado.MatchedCount > 0;

                return EsActualizado;
            }
            catch
            { 
                return false; 
            }
        }
        public async Task IncrementarMetricaCancion(string idCancion, string campo, int cantidad)
        {
            if (!await Conectar())
            { 
                return; // Fallo de conexión
            } 

            try
            {
                var coleccion = Database.GetCollection<Canciones>("canciones");
                var filtro = Builders<Canciones>.Filter.Eq(c => c.Id, idCancion);
                // Usamos Inc (Increment) que es atómico y eficiente
                var update = Builders<Canciones>.Update.Inc(campo, cantidad);

                var resultado = await coleccion.UpdateOneAsync(filtro, update);
               
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando métrica {campo}: {ex.Message}");
            }
        }
        // Método para sumar 1 al contador de canciones subida
        // En MongoAtlas.cs

        public async Task IncrementarContadorCancionesUsuario(string idUsuario, int cantidad)
        {
            if (!await Conectar())
            {
                return; // Fallo de conexión
            }

            try
            {
                var coleccion = Database.GetCollection<Usuarios>("usuarios");
                var filtro = Builders<Usuarios>.Filter.Eq(u => u.Id, idUsuario);

                // Si es 1, suma. Si es -1, resta.
                var update = Builders<Usuarios>.Update.Inc("estadisticas.n_canciones_subidas", cantidad);

                await coleccion.UpdateOneAsync(filtro, update);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error actualizando contador de usuario: " + ex.Message);
            }
        }
        // En MongoAtlas.cs
        public async Task<bool> ActualizarGenero(string id, string nuevoNombre)
        {
            if (!await Conectar())
            {
                return false; 
            }
            try
            {
                var coleccion = Database.GetCollection<Generos>("generos");

                // VALIDACIÓN: ¿Existe OTRO género con ese nombre?
                // Buscamos: (Nombre IGUAL al nuevo) Y (Id DIFERENTE al mío)
                var filtroDuplicado = Builders<Generos>.Filter.And(
                    Builders<Generos>.Filter.Regex(g => g.Nombre, new MongoDB.Bson.BsonRegularExpression($"^{nuevoNombre}$", "i")),
                    Builders<Generos>.Filter.Ne(g => g.Id, id) // Excluye mi id 
                );

                var existeOtro = await coleccion.Find(filtroDuplicado).AnyAsync();
                if (existeOtro) 
                { 
                    return false; // Nombre ocupado por otro.
                } 

                // Actualizamos
                var filtro = Builders<Generos>.Filter.Eq(g => g.Id, id);
                var update = Builders<Generos>.Update.Set(g => g.Nombre, nuevoNombre);


                var resultado = await coleccion.UpdateOneAsync(filtro, update);

                var EsActualizado = resultado.MatchedCount > 0;

                return EsActualizado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error actualizando género: " + ex.Message);
                return false;
            }
        }
        #endregion

        #region Metodos Helpers
        /////////////////////////////////////////
        ////////////METODOS HELPERS//////////////
        /////////////////////////////////////////
        private async Task RellenarNombresDeArtistas(List<Canciones> listaCanciones)
        {
            // Necesitamos acceso a la colección de usuarios para buscar los nombres
            var coleccionUsuarios = Database.GetCollection<Usuarios>("usuarios");

            foreach (var cancion in listaCanciones)
            {
                // Solo entramos si hay autores en la lista de IDs
                if (cancion.AutoresIds != null && cancion.AutoresIds.Count > 0)
                {
                    List<string> autoresUsername = new List<string>();

                    foreach (var idAutor in cancion.AutoresIds)
                    {
                        // 1. EL FILTRO: Buscamos por ID
                        var filtro = Builders<Usuarios>.Filter.Eq(u => u.Id, idAutor);

                        // 2. LA PROYECCIÓN (Aquí está el truco) 🔦
                        // En lugar de traer todo, hacemos: u => new Usuarios { ... }
                        // Esto crea un objeto Usuarios "vacío" y rellena SOLO el Username.
                        // El resto de campos (Email, Password...) serán null.
                        var usuario = await coleccionUsuarios.Find(filtro)
                                                    .Project(u => new Usuarios { Username = u.Username })
                                                    .FirstOrDefaultAsync();

                        if (usuario != null)
                        {
                            autoresUsername.Add(usuario.Username);
                        }
                    }

                    // Unimos los nombres (Ej: "Fito, Estopa")
                    cancion.NombreArtista = string.Join(", ", autoresUsername);
                }
                else
                {
                    cancion.NombreArtista = "Artista Desconocido";
                }
            }
        }
        #endregion

    }
}
