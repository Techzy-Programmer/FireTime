using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FireTime.Response
{
    /// <summary>
    /// Response class for accessing the Rest [JSON] Response into raw or various other formats
    /// </summary>
    public class FireResponse
    {

        /// <summary>
        /// <para>Implement this check to be sure if your request was successful or not</para>
        /// <para>Returns true if request failed or false if sucess response was received from the server</para>
        /// </summary>
        public bool HasError { get; internal set; }

        /// <summary>
        /// Get basic response as JSON string received from the server
        /// </summary>
        public string RawJSON { get; internal set; }

        /// <summary>
        /// Use this property to retrieve the Error message string. This property will return null if there was no error while making the request
        /// </summary>
        public string GetErrorMSG { get; internal set; }

        /// <summary>
        /// <para>Check this property to know if the response received from server has key-value pair and is convertible to other types such as JObject or a Custom typed object</para>
        /// <para>You will get a null value on JObject, or Custom typed object if this property returns false. Hence it is recommended to use this property before accessing "GetAsJObject", or "GetAs&lt;T&gt;()" method.</para>
        /// </summary>
        public bool IsConvertible { get; internal set; }

        /// <summary>
        /// <para>Get a dynamic JObject out of the json response received from Firebase database server</para>
        /// <para>Please implement IsConvertible check first if it returns true then you can easily access this object otherwise you may get a null value</para>
        /// </summary>
        public JObject GetAsJObject { get; internal set; }

        /// <summary>
        /// Get the Http status response code.
        /// </summary>
        public int GetHttpStatusCode { get; internal set; }

        /// <summary>
        /// <para>Get a Custom typed object out of the json response received from Firebase database server</para>
        /// <para>NOTE : Some property value of your Class object may be null if it is not mapped properly.</para>
        /// <para>Please implement IsConvertible check first if it returns true then you can easily access this object otherwise you may get a null value</para>
        /// </summary>
        /// <typeparam name="T">Custom type class with fields mapped to server JSON structure</typeparam>
        /// <returns>A custom object or null if the method fails</returns>
        public T GetAs<T>() where T : class
        {
            if (!IsConvertible || HasError) return null;
            try { return JsonConvert.DeserializeObject<T>(RawJSON); }
            catch { return null; }
        }

        internal FireResponse() { }
        internal FireResponse(string Data, int HStatusCode, bool IsError = true)
        {
            HasError = IsError;
            GetHttpStatusCode = HStatusCode;

            if (!IsError)
            {
                try
                {
                    GetAsJObject = JObject.Parse(Data);
                    IsConvertible = true;
                    GetErrorMSG = null;
                    HasError = false;
                    RawJSON = Data;
                }
                catch
                {
                    IsConvertible = false;
                    GetAsJObject = null;
                }
            }
            else
            {
                RawJSON = null;
                GetErrorMSG = Data;
                GetAsJObject = null;
                IsConvertible = false;
            }
        }
    }
}