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
        // --- MÉTODO 2: ENCRIPTAR (Para proteger la música al descargarla) ---
        // Recibe los bytes "limpios" de la canción y devuelve una "papilla" ilegible.
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
