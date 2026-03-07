using System.Text;

namespace Darb.Api.Helpers
{
    public static class SecurityHelper
    {
        public static string ConvertToBase64(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

 
        public static string DecodeFromBase64(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data)) return string.Empty;
            var base64Bytes = Convert.FromBase64String(base64Data);
            return Encoding.UTF8.GetString(base64Bytes);
        }
    }
}