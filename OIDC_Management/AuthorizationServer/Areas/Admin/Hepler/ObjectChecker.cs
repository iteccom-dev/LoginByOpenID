using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace OIDCDemo.AuthorizationServer.Areas.Admin.Controllers
{
    // Class helper tĩnh
    public static class ObjectChecker
    {
        public static bool IsValid(object? value)
        {

            bool isDataNullOrEmpty = IsNullOrEmpty(value);
            bool isDataValid = IsSuspicious(value);

            if (isDataNullOrEmpty ==true && isDataValid == true)
                return true;
                return false;
        }





        /// <summary>
        /// Kiểm tra xem giá trị truyền vào có null hoặc rỗng không
        /// Hỗ trợ string, collection, nullable và object
        /// </summary>
        public static bool IsNullOrEmpty(object? value)
        {
            if (value == null)
                return true;

            // Nếu là string
            if (value is string str)
                return string.IsNullOrWhiteSpace(str);

            // Nếu là collection
            if (value is IEnumerable enumerable)
                return !enumerable.Cast<object>().Any();

            // Nếu là nullable type (int?, DateTime?, ...)
            var type = value.GetType();
            if (Nullable.GetUnderlyingType(type) != null)
                return value.Equals(Activator.CreateInstance(type));

            // Mặc định coi object không null là có giá trị
            return false;
        }
        private static readonly string[] DangerousPatterns = {
        @"\b(SELECT|INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|TRUNCATE|EXEC|UNION|BACKUP|RESTORE|GRANT|REVOKE|DENY|KILL|SHUTDOWN)\b",
        @"(--|#|/\*|\*/|;)",
        @"\bUNION\b\s+.*\bSELECT\b",
        @"\b(?:OR|AND)\s+1\s*=\s*1\b",
        @"'\s*(?:OR|AND)\s*'\w+'?\s*=\s*'\w+'?",
        @"\b(?:xp_|sp_)\w+",
        @"\bWAITFOR\s+DELAY\b|\bSLEEP\s*\(",
        @";\s*\b(?:SELECT|INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|TRUNCATE|EXEC)\b"
    };

        private static readonly RegexOptions Options =
            RegexOptions.IgnoreCase | RegexOptions.Compiled;

        private static readonly HashSet<object> Visited =
            new HashSet<object>(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Hàm kiểm tra đầu vào có mã SQL nguy hiểm không. Nếu có trả về true, nếu không trả về false
        /// </summary>
        /// <param name="model">Object đầu vào để kiểm tra</param>
        /// <param name="maxDepth"></param>
        /// <returns>Nếu có mã SQL trả về true, nếu không trả về false</returns>
        public static bool IsSuspicious(object? model, int maxDepth = 3)
        {
            if (model == null) return false;
            Visited.Clear();
            return Scan(model, maxDepth);
        }

        // Hàm quét
        private static bool Scan(object obj, int depth)
        {

            // Kiểm tra nếu hết cái để quét thì dừng, nếu quét quá nhiều lần sẽ dừng, nếu đã quét rồi sẽ dừng.
            if (obj == null || depth < 0 || !Visited.Add(obj)) return false;

            var type = obj.GetType();

            // Lấy tất cả các property là object và public
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!prop.CanRead) continue;

                object? value;
                try { value = prop.GetValue(obj); }
                catch { continue; }

                // 1. Nếu là chuỗi → kiểm tra
                if (value is string s && !string.IsNullOrWhiteSpace(s))
                {
                    var clean = Clean(s);
                    foreach (var pattern in DangerousPatterns)
                    {
                        if (Regex.IsMatch(clean, pattern, Options))
                            return true;
                    }
                }
                // 2. Nếu là danh sách → quét từng phần tử
                else if (value is IEnumerable list && value is not string)
                {
                    foreach (var item in list)
                        if (item != null && Scan(item, depth - 1))
                            return true;
                }
                // 3. Nếu là object phức tạp → đi sâu
                else if (value != null && !IsSimpleType(prop.PropertyType))
                {
                    if (Scan(value, depth - 1))
                        return true;
                }
            }
            return false;
        }

        // Dọn dẹp chuỗi 
        private static string Clean(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            var halfWidth = new StringBuilder();
            foreach (var c in s)
            {
                if (c >= 0xFF01 && c <= 0xFF5E)
                    halfWidth.Append((char)(c - 0xFEE0));
                else if (c == 0x3000)
                    halfWidth.Append(' ');
                else
                    halfWidth.Append(c);
            }

            var normalized = halfWidth.ToString();

            return Regex.Replace(
                Regex.Replace(normalized, @"\p{C}+", " "),
                @"\s+", " ").Trim();
        }

        // Kiểm tra kiểu dữ liệu là kiểu thông thường hay là object. nếu là thường thì trả về true object sẽ trả về false
        private static bool IsSimpleType(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t.IsPrimitive || t.IsEnum || t == typeof(string) ||
                   t == typeof(decimal) || t == typeof(DateTime) ||
                   t == typeof(DateTimeOffset) || t == typeof(Guid) || t == typeof(TimeSpan);
        }
    }
}
