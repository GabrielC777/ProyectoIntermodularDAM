using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BetaProyecto.Helpers
{
    public static class Encriptador
    {
        // --- CLAVES PARA EL CIFRADO SIMÉTRICO(AES) ---
        // En criptografía simétrica, la MISMA clave sirve para cerrar y abrir.
        // _claveAes: La "llave" principal (16 bytes = 128 bits).
        // _ivAes: Vector de Inicialización (añade aleatoriedad al primer bloque para más seguridad).
        private static readonly byte[] _claveAes = Encoding.UTF8.GetBytes("1234567890123456");
        private static readonly byte[] _ivAes = Encoding.UTF8.GetBytes("mi_vector_inicio");

        /// <summary>
        /// Aplica un algoritmo de hashing criptográfico SHA-256 a una cadena de texto para proteger información sensible.
        /// </summary>
        /// <remarks>
        /// Este proceso de seguridad transforma la contraseña mediante los siguientes pasos:
        /// <list type="number">
        /// <item><b>Codificación:</b> Convierte la cadena original en una secuencia de bytes utilizando el estándar UTF-8.</item>
        /// <item><b>Cifrado:</b> Utiliza una instancia de <see cref="SHA256"/> para calcular un resumen único (hash) de 256 bits.</item>
        /// <item><b>Representación:</b> Transforma los bytes resultantes en una cadena hexadecimal de longitud fija (64 caracteres) mediante un <see cref="StringBuilder"/>.</item>
        /// <item><b>Gestión de Memoria:</b> Emplea la sentencia <c>using</c> para garantizar la liberación inmediata de los recursos criptográficos en la memoria RAM.</item>
        /// </list>
        /// Nota: El hashing es una operación unidireccional; no es posible revertir el resultado para obtener la contraseña original.
        /// </remarks>
        /// <param name="password">La contraseña en texto plano que se desea anonimizar.</param>
        /// <returns>Una cadena de texto en formato hexadecimal que representa el hash único de la contraseña.</returns>
        public static string HashPassword(string password)
        {
            // Creamos un objeto Sha257
            // Usamos 'using' para que se limpie de la memoria RAM automáticamente al terminar
            using (SHA256 sha256 = SHA256.Create())
            {
                // Convertimos de string a array de byte 
                byte[] bytesPassword = Encoding.UTF8.GetBytes(password);

                // Calculamos el hash 
                byte[] bytesDelHash = sha256.ComputeHash(bytesPassword);

                // Preparamos el StringBuilder para el foreach
                StringBuilder builder = new StringBuilder();

                // Y convertimos los bytes del hash a hexadecimal
                foreach (byte b in bytesDelHash)
                {
                    builder.Append(b.ToString("x2"));
                }
                // Y devolvermos el stringbuilder como string para compararlo
                return builder.ToString();
            }
        }
        // Encriptar
        // Recibe los bytes "limpios" de la canción y devuelve una "papilla" ilegible.
        /// <summary>
        /// Realiza un cifrado simétrico AES sobre una secuencia de bytes para proteger el contenido de archivos multimedia.
        /// </summary>
        /// <remarks>
        /// Este proceso de encriptación transforma los datos originales en un formato ilegible mediante los siguientes pasos técnicos:
        /// <list type="number">
        /// <item><b>Inicialización:</b> Se instancia el algoritmo <see cref="Aes"/> y se configuran las propiedades <c>Key</c> (clave secreta) e <c>IV</c> (vector de inicialización).</item>
        /// <item><b>Canalización (Streaming):</b> Se utiliza un <see cref="CryptoStream"/> como intermediario para procesar los datos a través de un transformador de cifrado.</item>
        /// <item><b>Escritura Segura:</b> Los bytes originales se escriben en el flujo de memoria, donde se aplican las operaciones matemáticas del estándar AES.</item>
        /// <item><b>Finalización:</b> Se ejecuta <c>FlushFinalBlock</c> para procesar los bytes restantes y garantizar la integridad del bloque cifrado.</item>
        /// </list>
        /// El resultado es un array de bytes que solo puede ser recuperado mediante el método de desencriptación correspondiente utilizando la misma clave e IV.
        /// </remarks>
        /// <param name="byteSinEncrip">El array de bytes original (en texto plano o formato multimedia crudo) que se desea proteger.</param>
        /// <returns>Un array de bytes cifrados mediante el estándar AES, listos para ser almacenados de forma segura en el almacenamiento persistente.</returns>
        public static byte[] EncriptarBytes(byte[] byteSinEncrip)
        {
            // Creamos el algoritmo AES (Estándar de cifrado simétrico)
            using (Aes aes = Aes.Create())
            {
                aes.Key = _claveAes; // Asignamos nuestra clave secreta
                aes.IV = _ivAes;     // Y el vector de inicio

                // Preparamos un flujo en memoria para guardar el resultado
                using (var memoryStream = new MemoryStream())
                {
                    // CryptoStream es un "túnel" que cifra todo lo que pasa por él.
                    // CryptoStreamMode.Write: Lo usamos para ESCRIBIR datos y que salgan cifrados.
                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        // Escribimos los datos de la canción en el túnel
                        cryptoStream.Write(byteSinEncrip, 0, byteSinEncrip.Length);

                        // "FlushFinalBlock" asegura que el último trocito de datos se escriba bien
                        cryptoStream.FlushFinalBlock();

                        // Devolvemos el array de bytes cifrados (ilegibles)
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        // Desencriptar
        // Lee el archivo cifrado del disco y crea uno temporal limpio que VLC pueda entender.
        /// <summary>
        /// Lee un archivo cifrado del almacenamiento local, lo descifra utilizando el algoritmo AES y guarda el resultado en una ubicación temporal.
        /// </summary>
        /// <remarks>
        /// Este método es el pilar de la recuperación de datos protegidos y opera bajo los siguientes pasos:
        /// <list type="number">
        /// <item><b>Carga de Datos:</b> Recupera los bytes cifrados del disco mediante <see cref="File.ReadAllBytesAsync"/>.</item>
        /// <item><b>Configuración Criptográfica:</b> Reinstancia el motor <see cref="Aes"/> asegurando el uso de la misma clave y vector de inicialización (IV) empleados durante la encriptación.</item>
        /// <item><b>Procesamiento de Flujo:</b> Utiliza un <see cref="CryptoStream"/> en modo lectura (<see cref="CryptoStreamMode.Read"/>) que actúa como un filtro de transformación, convirtiendo los bytes cifrados en datos originales.</item>
        /// <item><b>Persistencia Temporal:</b> Vuelca el flujo descifrado en un nuevo archivo físico (normalmente un .mp3 temporal) para que sea accesible por los servicios de reproducción.</item>
        /// </list>
        /// Al utilizar flujos asíncronos, se garantiza que la interfaz de usuario no se bloquee durante el procesamiento de archivos de gran tamaño.
        /// </remarks>
        /// <param name="rutaEntrada">La ruta del archivo cifrado (habitualmente con extensión .enc) que se desea procesar.</param>
        /// <param name="rutaSalida">La ruta de destino donde se escribirá el archivo resultante ya descifrado.</param>
        /// <returns>Una tarea asíncrona que representa el proceso de lectura, descifrado y escritura.</returns>
        public static async Task DesencriptarArchivo(string rutaEntrada, string rutaSalida)
        {
            // Volvemos a crear AES con la MISMA CLAVE (imprescindible en simétrico)
            using (Aes aes = Aes.Create())
            {
                aes.Key = _claveAes;
                aes.IV = _ivAes;

                // 1. Leemos el archivo cifrado del disco (son bytes "basura" para un humano)
                byte[] bytesCifrados = await File.ReadAllBytesAsync(rutaEntrada);

                // 2. Preparamos los flujos (Streams) para procesar los datos
                using (var memoryStreamEntrada = new MemoryStream(bytesCifrados))
                {
                    using (var memoryStreamSalida = new MemoryStream())
                    {
                        // 3. Creamos el túnel de descifrado.
                        // CryptoStreamMode.Read: Al LEER del túnel, los datos salen limpios.
                        using (var cryptoStream = new CryptoStream(memoryStreamEntrada, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            // Copiamos los datos a través del túnel hacia la salida
                            await cryptoStream.CopyToAsync(memoryStreamSalida);
                        }

                        // 4. Guardamos el resultado "limpio" en el archivo de salida (temp)
                        await File.WriteAllBytesAsync(rutaSalida, memoryStreamSalida.ToArray());
                    }
                }
            }
        }
    }
}
