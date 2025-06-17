using System.Security.Cryptography;

namespace TrackMyScore.Services
{
    public static class PasswordHasher
    {
        private const int SaltSize    = 16;
        private const int KeySize     = 32;
        private const int Iterations  = 100_000;

        public static string Hash(string password)
        {
            var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[SaltSize];
            rng.GetBytes(salt);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(KeySize);

            // store as iterations.salt.key
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string storedHash)
        {
            // split the parts at every dot
            var parts = storedHash.Split('.', 3);
            if (parts.Length != 3) return false;

            int    iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] keyStored = Convert.FromBase64String(parts[2]);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] keyNew = pbkdf2.GetBytes(keyStored.Length);

            return CryptographicOperations.FixedTimeEquals(keyNew, keyStored);
        }
    }
}
