using System;
using System.Security.Cryptography;

namespace EcoRecycle.Services
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128-bit
        private const int KeySize = 32;  // 256-bit
        private const int Iterations = 100000;

        public static string HashPassword(string password)
        {
            using (var algorithm = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithmName.SHA256))
            {
                var key = algorithm.GetBytes(KeySize);
                var salt = algorithm.Salt;

                var bytes = new byte[SaltSize + KeySize];
                Array.Copy(salt, 0, bytes, 0, SaltSize);
                Array.Copy(key, 0, bytes, SaltSize, KeySize);

                return Convert.ToBase64String(bytes);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                var bytes = Convert.FromBase64String(hashedPassword);
                var salt = new byte[SaltSize];
                var key = new byte[KeySize];

                Array.Copy(bytes, 0, salt, 0, SaltSize);
                Array.Copy(bytes, SaltSize, key, 0, KeySize);

                using (var algorithm = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    var keyToCheck = algorithm.GetBytes(KeySize);

                    var difference = (uint)key.Length ^ (uint)keyToCheck.Length;
                    for (var i = 0; i < key.Length && i < keyToCheck.Length; i++)
                    {
                        difference |= (uint)(key[i] ^ keyToCheck[i]);
                    }
                    return difference == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
