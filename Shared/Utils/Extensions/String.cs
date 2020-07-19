using System;
using System.Net.Mail;
using System.Text;

namespace Extensions
{
    /// <summary>
    /// String extension methods
    /// </summary>
    public static class String
    {
        /// <summary>
        /// Generate md5 hash
        /// </summary>
        /// <param name="str">input string</param>
        /// <returns>md5 hash</returns>
        public static string MD5(this string str)
        {
            using (var provider = System.Security.Cryptography.MD5.Create())
            {
                StringBuilder builder = new StringBuilder();

                foreach (byte b in provider.ComputeHash(Encoding.UTF8.GetBytes(str)))
                {
                    builder.Append(b.ToString("x2").ToLower());
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Check is email is valid
        /// </summary>
        /// <param name="address">email address</param>
        /// <returns>true if email is valid</returns>
        public static bool IsValidEmailAddress(this string address)
        {
            try
            {
                MailAddress mail = new MailAddress(address);
                return mail.Address == address;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Get base64 of utf string
        /// </summary>
        /// <param name="str">input string</param>
        /// <returns>Base64 string</returns>
        public static string ToBase64(this string str)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Compare string ignore case
        /// </summary>
        /// <param name="str">input string</param>
        /// <param name="other">other string</param>
        /// <returns>true if string is equal</returns>
        public static bool EqualsIgnoreCase(this string str, string other)
        {
            return str.Equals(other, StringComparison.OrdinalIgnoreCase);
        }
    }
}
