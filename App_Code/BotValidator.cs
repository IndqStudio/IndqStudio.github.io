using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Sinuvera
{
    public class InvisibleBotValidator
    {

        #region "Constants and Statics"

        // Statics
        private static SortedList<DateTime, string> _pastAddresses = new SortedList<DateTime, string>();

        private static SortedList<string, DateTime> _requestKeys = new SortedList<string, DateTime>();
        //Contants
        private const int ResponseMinimumDelaySeconds = 4;
        private const int CutoffWindowSeconds = 14400;
        private const int CutoffMaximumInstances = 6;
        private const string SignatureHeaderName = "X-DataSign";
        private const string KeyFieldName = "Bot_KeyField";
        #endregion

        #region "Methods"

        public static string Load()
        {
            DateTime utcNow = DateTime.UtcNow;
            string requestKey = Guid.NewGuid().ToString("N");
            lock (_requestKeys)
            {
                _requestKeys.Add(requestKey, utcNow.AddSeconds(ResponseMinimumDelaySeconds));
            }
            return requestKey;
        }

        public static string getHashSha256(string text)
        {
            StringBuilder op = new StringBuilder();
            byte[] hash = new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(text));
            foreach (byte x in hash)
            {
                op.Append(x.ToString("x2"));
            }
            return op.ToString();
        }

        public static string EvaluateIsValid()
        {
            try
            {
                DateTime utcNow = DateTime.UtcNow;
                // Report invalid if response arrives too soon
                //
                lock (_requestKeys)
                {
                    // Get the request Key from the POSTed value
                    string requestKey = HttpContext.Current.Request.Form[KeyFieldName];

                    //Remove expired keys older than 6 hours
                    List<string> ExpiredEntries = _requestKeys.Where(x => x.Value < utcNow.AddHours(-6)).Select(y => y.Key).ToList();
                    foreach (string ky in ExpiredEntries)
                    {
                        _requestKeys.Remove(ky);
                    }

                    // Fail if request key is not found or if the response came too soon
                    if (string.IsNullOrEmpty(requestKey) || (!_requestKeys.ContainsKey(requestKey)))
                    {
                        return "Request Key not found";
                    }
                    else if (utcNow < _requestKeys[requestKey])
                    {
                        return "Response came too soon";
                    }
                }

                // Report invalid if response is wrong
                //
                string body = "";
                using (System.IO.StreamReader reader = new System.IO.StreamReader(HttpContext.Current.Request.InputStream))
                {
                    try
                    {
                        reader.BaseStream.Position = 0;
                        body = reader.ReadToEnd();
                    }
                    finally
                    {
                        reader.BaseStream.Position = 0;
                    }
                }
                string requiredResponse = getHashSha256(body);
                if (requiredResponse != HttpContext.Current.Request.Headers[SignatureHeaderName])
                {
                    return "Signature Mismatch";
                }

                // Report invalid if too many responses from same IP address
                lock (_pastAddresses)
                {
                    // Add user address to address cache, taking care not to duplicate keys
                    string userAddress = HttpContext.Current.Request.UserHostAddress;
                    DateTime utcAdd = utcNow.AddSeconds(CutoffWindowSeconds);
                    while (_pastAddresses.ContainsKey(utcAdd))
                    {
                        utcAdd = utcAdd.AddTicks(1);
                    }
                    _pastAddresses.Add(utcAdd, userAddress);

                    //Remove expired addresses
                    List<DateTime> ExpiredEntries = _pastAddresses.Keys.Where(x => x < utcNow).ToList();
                    foreach (DateTime dt in ExpiredEntries)
                    {
                        _pastAddresses.Remove(dt);
                    }

                    // Determine number of instances of user address in cache
                    int instances = _pastAddresses.Values.Where(x => x == userAddress).Count();
                    // Fail if too many
                    if (CutoffMaximumInstances < instances)
                    {
                        return "IP Address is too active";
                    }
                }

                // All checks OK, report valid
                //
                return "";
            }
            catch (NullReferenceException)
            {
                return "Bad Session";
            }
        }

        #endregion
    }
}