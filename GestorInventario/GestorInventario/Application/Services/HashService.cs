using GestorInventario.Application.Classes;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services
{
    public class HashService
    {
        public ResultadoHash Hash(string password)
        {
            //Generacion del salt

            var salt = new byte[16];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            return Hash(password, salt);
        }

        //Aqui le decimos que es de tipo ResultadoHash y nombre Hash el cual se le pasa un
        //parametro de tipo string y otro un array de byte que es el salt
        public ResultadoHash Hash(string password, byte[] salt)
        {
            //Logica necesaria para cifrar la contraseña

            var claveDerivada = KeyDerivation.Pbkdf2(password: password,
                salt: salt, prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 32);

            var hash = Convert.ToBase64String(claveDerivada);

            return new ResultadoHash()
            {
                Hash = hash,
                Salt = salt
            };

        }

    }
}
