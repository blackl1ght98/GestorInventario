using GestorInventario.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Shared.DTOS.Auth;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Services
{
    public class HashService: IHashService
    {
        public HashResult Hash(string password)
        {
            //Generacion del salt

            var salt = new byte[16];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            return Hash(password, salt);
        }

       
        public HashResult Hash(string password, byte[] salt)
        {
          

            var claveDerivada = KeyDerivation.Pbkdf2(password: password,
                salt: salt, prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            var hash = Convert.ToBase64String(claveDerivada);

            return new HashResult()
            {
                Hash = hash,
                Salt = salt
            };

        }

    }
}
