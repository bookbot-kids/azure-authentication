using System;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace Extensions
{
    /// <summary>
    /// String extension methods
    /// </summary>
    public static class String
    {
        static Regex phoneRegex = new Regex(@"^\+\d{1,15}$");

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


        /// <summary>
        /// Fix the encode issue because email parameter that contains "+" will be encoded by space
        /// e.g. client sends "a+1@gmail.com" => Azure function read: "a 1@gmail.com" (= req.Query["email"])
        /// We need to replace space by "+" when reading the parameter req.Query["email"]
        /// Then the result is correct "a+1@gmail.com"
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static string NormalizeEmail(this string srt)
        {
            return srt.Trim().Replace(" ", "+").ToLower();
        }

        public static string NormalizePhone(this string srt)
        {
            return srt.Replace(" ", "+").ToLower();
        }

        public static bool isValidPhone(this string str)
        {
            return phoneRegex.IsMatch(str);
        }
    }

    public static class StringHelper
    {
        public static string CombineName(string firstName, string lastName)
        {
            return string.IsNullOrWhiteSpace(firstName) ?
                      (string.IsNullOrWhiteSpace(lastName) ? "" : lastName) :
                      (string.IsNullOrWhiteSpace(lastName) ? firstName : $"{firstName} {lastName}");
        }

        public static string ExtractFirstName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            // Split by whitespace and filter out empty entries
            string[] parts = name.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);

            // If there's only one part or no parts, return the original name
            if (parts.Length <= 1)
                return name.Trim();

            // Return the first part as the first name
            return parts[0].Trim();
        }

        public static bool IsTestEmail(string domains, string email)
        {
            if (string.IsNullOrEmpty(domains) || string.IsNullOrEmpty(email))
                return false;

            return domains.Split(';')
                .Any(domain => !string.IsNullOrEmpty(domain) &&
                     email.ToLower().EndsWith(domain.Trim().ToLower()));
        }
    }
}
