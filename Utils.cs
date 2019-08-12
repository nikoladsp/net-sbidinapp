using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace net_sbidinapp
{
    class Utils
    {
        public static string CodeVerifier(uint len = 64)
        {
            if (31 > len || 96 < len)
                throw new ArgumentOutOfRangeException("len must be in range [31, 96].");

            Random rnd = new Random();
            //int[] challengeBuffer = new int[len];
            //for (int i = 0; i < challengeBuffer.Length; ++i)
            //    challengeBuffer[i] = r.Next();


            byte[] challengeBytes = new byte[len];
            rnd.NextBytes(challengeBytes);
            //Buffer.BlockCopy(challengeBuffer, 0, challengeBytes, 0, challengeBytes.Length);

            var verifier = Encode(challengeBytes);
            if (43 > verifier.Length)
                throw new ArgumentOutOfRangeException("Verifier too short. len must be > 30.");
            else if (128 < verifier.Length)
                throw new ArgumentOutOfRangeException("Verifier too long. len must be < 97.");

            return verifier;


            //verifier = base64.urlsafe_b64encode(os.urandom(n_bytes)).rstrip(b'=')
            //# https://tools.ietf.org/html/rfc7636#section-4.1
            //# minimum length of 43 characters and a maximum length of 128 characters.
            //if len(verifier) < 43:
            //    raise ValueError("Verifier too short. n_bytes must be > 30.")
            //elif len(verifier) > 128:
            //    raise ValueError("Verifier too long. n_bytes must be < 97.")
            //else:
            //    return verifier
        }

        public static string CodeChallenge(string verifier)
        {
            var digest = ComputeSha256Bytes(verifier);
            return Encode(digest);
            //digest = hashlib.sha256(verifier).digest()
            //return base64.urlsafe_b64encode(digest).rstrip(b'=')
        }

        public static string ComputeSha256Hash(string rawData)
        {
            byte[] bytes = ComputeSha256Bytes(rawData);

            // Convert byte array to a string   
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; ++i)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
        public static byte[] ComputeSha256Bytes(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                return bytes;
            }
        }
        public static string Encode(byte[] input)
        {
            var output = Convert.ToBase64String(input);

            output = output.Split('=')[0]; // Remove any trailing '='s
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding

            return output;
        }
        public static byte[] Decode(string input)
        {
            var output = input;

            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding

            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0:
                    break; // No pad chars in this case
                case 2:
                    output += "==";
                    break; // Two pad chars
                case 3:
                    output += "=";
                    break; // One pad char
                default:
                    throw new ArgumentOutOfRangeException(nameof(input), "Illegal base64url string!");
            }

            var converted = Convert.FromBase64String(output); // Standard base64 decoder

            return converted;
        }

        public static string getFinalURL(string url, string cookieHeader)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AllowAutoRedirect = false;
                request.Headers.Add("Cookie", cookieHeader);

                HttpWebResponse res = (HttpWebResponse)request.GetResponse();

                HttpStatusCode status = res.StatusCode;

                if (HttpStatusCode.Moved == status || HttpStatusCode.Redirect == status)
                {
                    string redirectUrl = string.Join("", res.Headers.GetValues("Location"));

                    return getFinalURL(redirectUrl, cookieHeader);
                }
            }
            catch (WebException e)
            {

            }

            return url;
        }
    }
}
