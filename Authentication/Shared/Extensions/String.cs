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

        public static string SHA256(this string randomString)
        {
            if (string.IsNullOrWhiteSpace(randomString)) return "";
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }


        /// <summary>
        /// Check is email is valid
        /// </summary>
        /// <param name="email">email address</param>
        /// <returns>true if email is valid</returns>
        public static bool IsValidEmailAddress(this string email)
        {
            try
            {
                new MailAddress(email);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Get name from an email
        /// </summary>
        /// <param name="email">email address</param>
        /// <returns>name or null</returns>
        public static string GetNameFromEmail(this string email)
        {
            try
            {
                return new MailAddress(email).User;
            }
            catch (Exception)
            {
                return null;
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
