using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace net_sbidinapp
{
    class Program
    {
        private static string cookieHeader = string.Empty;

        static HttpWebResponse Step1(string verifier)
        {
            // STEP 1: Call authorize using method "sbid-inapp"

            string nid = "197602208253";
            string challenge = Utils.CodeChallenge(verifier);
            Console.Out.WriteLine("PKCE: verifier='{0}', challenge='{1}'", verifier, challenge);

            Random random = new Random(); //TODO use RNGCryptoServiceProvider instead
            const string alphabet = "ABCDEF0123456789";
            string state = new string(Enumerable.Repeat(alphabet, 8).Select(s => s[random.Next(s.Length)]).ToArray());

            Uri uri = new Uri(string.Format("https://preprod.signicat.com/oidc/authorize?response_type=code&scope=openid+profile+signicat.national_id+phone&client_id=demo-inapp&redirect_uri=https://example.com/redirect&acr_values=urn:signicat:oidc:method:sbid-inapp&state={0}&login_hint=subject-{1}&code_challenge_method=S256&code_challenge={2}", state, nid, challenge));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.Accept = "application/json";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            cookieHeader = string.Join(", ", response.Headers.GetValues("Set-Cookie"));

            return response;
        }

        static dynamic Step2(HttpWebResponse response1)
        {
            // STEP 2: Poll collectUrl until progressStatus=COMPLETE

            dynamic result = null;

            using (var streamReader = new StreamReader(response1.GetResponseStream()))
            {
                dynamic jsonData = JsonConvert.DeserializeObject(streamReader.ReadToEnd());

                if (null != jsonData["error"])
                    throw new Exception("Error occured");

                string orderRef = jsonData["orderRef"].ToString();
                string autoStartToken = jsonData["autoStartToken"].ToString();
                string collectUrl = jsonData["collectUrl"].ToString();

                Uri uri = new Uri(collectUrl + "?orderRef=" + orderRef);

                HttpWebResponse res = null;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "GET";
                request.Accept = "application/json";
                request.Headers.Add("Cookie", cookieHeader);

                string progressStatus = null;
                Console.Out.WriteLine("\nPolling...");

                while ("COMPLETE" != progressStatus) // Check if COMPLETE, if not sleep 5s and check again.
                {
                    Thread.Sleep(5000);

                    res = (HttpWebResponse)request.GetResponse();

                    using (var reader = new StreamReader(res.GetResponseStream()))
                    {
                        result = JsonConvert.DeserializeObject(reader.ReadToEnd());

                        progressStatus = result["progressStatus"];
                        Console.Out.WriteLine("  -- Status: {0}", progressStatus);
                    }
                }

                Console.Out.WriteLine(string.Format("collectUrl Response: {0}", result));
            }

            return result;
        }

        static string Step3(dynamic jsonData)
        {
            // STEP 3: Call completeUrl - the last redirect will contain CODE and STATE.

            string completeUrl = jsonData["completeUrl"].ToString();

            string url = Utils.getFinalURL(completeUrl, cookieHeader);

            NameValueCollection args = HttpUtility.ParseQueryString(new Uri(url).Query);
            string code = args.Get("code");
            string state = args.Get("state");

            Console.Out.WriteLine(string.Format("\nFinal redirect URL: {0}", url));
            Console.Out.WriteLine(string.Format("  -- CODE: '{0}'", code));
            Console.Out.WriteLine(string.Format("  -- STATE: '{0}'", state));

            return code;
        }

        static string Step4(string code, string verifier)
        {
            // STEP 4: Call /token end-point as normal (using CODE we got in STEP 3)

            Dictionary<string, string> payload = new Dictionary<string, string>();
            payload.Add("client_id", "demo-inapp");
            payload.Add("redirect_uri", "https://example.com/redirect");
            payload.Add("grant_type", "authorization_code");
            payload.Add("code_verifier", verifier);
            payload.Add("code", code);

            string postData = string.Empty;// HttpUtility.UrlEncode(payload);

            string last = payload.Keys.Last();
            foreach (string key in payload.Keys)
            {
                postData += HttpUtility.UrlEncode(key) + "="
                      + HttpUtility.UrlEncode(payload[key]);

                if (!key.Equals(last))
                    postData += "&";
            }

            byte[] data = Encoding.ASCII.GetBytes(postData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://preprod.signicat.com/oidc/token");
            request.Method = "POST";
            request.Headers.Add("Authorization", "Basic ZGVtby1pbmFwcDptcVotXzc1LWYyd05zaVFUT05iN09uNGFBWjd6YzIxOG1yUlZrMW91ZmE4");
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (StreamWriter stOut = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII))
            {
                stOut.Write(postData);
            }

            HttpWebResponse res = (HttpWebResponse)request.GetResponse();

            using (var reader = new StreamReader(res.GetResponseStream()))
            {
                dynamic jsonData = JsonConvert.DeserializeObject(reader.ReadToEnd());
                return jsonData["access_token"];
            }
        }

        static dynamic Step5(string accessToken)
        {
            // STEP 5 (optional): Call /userinfo with access token.

            dynamic userInfo = null;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://preprod.signicat.com/oidc/userinfo");
            request.Method = "GET";
            request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));

            HttpWebResponse res = (HttpWebResponse)request.GetResponse();

            using (var reader = new StreamReader(res.GetResponseStream()))
            {
                userInfo = JsonConvert.DeserializeObject(reader.ReadToEnd());

                Console.Out.WriteLine(string.Format("UserInfo Response:{0}", userInfo));
            }

            return userInfo;
        }

        static void Main(string[] args)
        {
            // STEP 0: Prepare PKCE (https://tools.ietf.org/html/rfc7636)
            string verifier = Utils.CodeVerifier();

            HttpWebResponse res1 = Step1(verifier);
            dynamic res2 = Step2(res1);
            string code = Step3(res2);
            string accessToken = Step4(code, verifier);
            Step5(accessToken);

            int x = 99;
        }
    }
}
