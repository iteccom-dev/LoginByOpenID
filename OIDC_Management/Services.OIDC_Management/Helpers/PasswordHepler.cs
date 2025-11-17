using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
namespace Services.OIDC_Management.Helpers

{
    public static class PasswordHelper
    {
        private const int Iterations = 100_000; // số vòng lặp PBKDF2
        private const int HashByteSize = 32;    // 256-bit

        /// <summary>
        /// Tạo hash từ password + salt
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            using (var rfc2898 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), Iterations, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(rfc2898.GetBytes(HashByteSize));
            }
        }

        /// <summary>
        /// Kiểm tra password với hash + salt
        /// </summary>
        /// <param name="password">Password nhập từ user</param>
        /// <param name="salt">Salt lưu trong DB</param>
        /// <param name="storedHash">Hash lưu trong DB</param>
        /// <returns>true nếu đúng, false nếu sai</returns>
        public static bool VerifyPassword(string password, string salt, string storedHash)
        {
            var computedHash = HashPassword(password, salt);
            return computedHash == storedHash;
        }

        /// <summary>
        /// Tạo salt mới ngẫu nhiên
        /// </summary>
        public static string GenerateSalt(int size = 16)
        {
            var bytes = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }
    }
}
