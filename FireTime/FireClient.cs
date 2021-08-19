using System.Net.Http;
using FireTime.Private;
using FireTime.Response;
using System.Threading.Tasks;

namespace FireTime
{
    /// <summary>
    /// Create instance of this class to start using the power of Firebase inside C#.Net
    /// </summary>
    public class FireClient
    {
        internal ReqManager Requester { get; set; }

        /// <summary>
        /// <para>Base class to access the Firebase services</para>
        /// <para>An error will be thrown if wrongly formatted URL is provided in the FireConfig object</para>
        /// </summary>
        /// <param name="FConfig">Settings and rules for connecting with Firebase services</param>
        public FireClient(FireConfig FConfig)
        {
            if (!IsURLFormatted(FConfig.FirebaseURL))
                throw new FireError("Provided Firebase Realtime Database URL in FireConfig object is invalid! " +
                    "it must start with https:// and should end with a valid firebase domain");
            Requester = new ReqManager(FConfig);
        }

        #region Main Public Methods

        /// <summary>
        /// <para>Call this method to attach a listner to the firebase realtime database instance.</para>
        /// <para>Exception will be thrown if initial request to the server resource fails</para>
        /// <para>NOTE : This will increase the simultaneous connection limit from your firebase quota by 1.</para>
        /// </summary>
        /// <param name="MonitorPath">The relative path at which you would like to listen for Realtime Database changes</param>
        /// <returns>The stream response which you can use to subscribe to any specific event</returns>
        public StreamResponse AttachListner(string MonitorPath) => Requester.Monitor(MonitorPath);

        /// <summary>
        /// Reads the remote json file using the Http GET request from Firebase Realtime Database
        /// </summary>
        /// <param name="ReadPath">The relative path from where you would like to read data</param>
        /// <returns>A FireResponse object containing all information regarding the current request</returns>
        public async Task<FireResponse> ReadAsync(string ReadPath) => await GetFireResponse(await Requester.GetAsync(ReadPath));

        /// <summary>
        /// Write data to Firebase Realtime Database using the Http PUT request
        /// </summary>
        /// <param name="WritePath">The relative path as on the server where you would like to write data</param>
        /// <param name="WriteData">The data to write on server it can be either a valid JSON string, a dynamic or anonymous object, a Dictionary object or a custom typed object</param>
        /// <returns>A FireResponse object containing all information regarding the current request</returns>
        public async Task<FireResponse> WriteAsync(string WritePath, object WriteData)
            => await GetFireResponse(await Requester.PutAsync(WritePath, WriteData));

        /// <summary>
        /// Update data to Firebase Realtime Database using the Http PATCH request
        /// </summary>
        /// <param name="UpdatePath">The relative path as on the server where you would like to update data</param>
        /// <param name="UpdateData">The data used to update (replace values) on server it can be either a valid JSON string, a dynamic or anonymous object, a Dictionary object or a custom typed object</param>
        /// <returns>A FireResponse object containing all information regarding the current request</returns>
        public async Task<FireResponse> UpdateAsync(string UpdatePath, object UpdateData)
            => await GetFireResponse(await Requester.PatchAsync(UpdatePath, UpdateData));

        /// <summary>
        /// Remove data will all child nodes from the Firebase Realtime Database using the Http DELETE request
        /// </summary>
        /// <param name="RemovePath">The relative path as on the server where you would like to perform completed deletion</param>
        /// <returns>A FireResponse object containing all information regarding the current request</returns>
        public async Task<FireResponse> RemoveAsync(string RemovePath)
            => await GetFireResponse(await Requester.DeleteAsync(RemovePath), true);

        #endregion

        #region Utility And Helper Methods

        private bool IsURLFormatted(string UrI)
        {
            if (!string.IsNullOrWhiteSpace(UrI) && UrI.StartsWith("https://"))
            {
                string CheckUri = UrI.EndsWith("/") ? UrI.Substring(0, UrI.Length - 1) : UrI;
                if (CheckUri.EndsWith(".firebasedatabase.app") || CheckUri.EndsWith(".firebaseio.com")) return true;
            }

            return false;
        }

        private async Task<FireResponse> GetFireResponse(HttpResponseMessage HRespM, bool IsRemoved = false)
        {
            FireResponse RToDispatch;
            int HStatusCode = (int)HRespM.StatusCode;
            string STRData = await HRespM.Content.ReadAsStringAsync();

            if (HRespM.IsSuccessStatusCode)
            {
                if (STRData == "null" && !IsRemoved) RToDispatch = new FireResponse
                        ("There is no data available for the path you have specified.", HStatusCode);
                else RToDispatch = new FireResponse(STRData, HStatusCode, false);
            }
            else
            {
                try
                {
                    var JErOBJ = Newtonsoft.Json.Linq.JObject.Parse(STRData);
                    if (JErOBJ.ContainsKey("error"))
                        RToDispatch = new FireResponse(JErOBJ["error"].ToString(), HStatusCode);
                    else RToDispatch = new FireResponse("Unspecified error was encountered, Have a look at the " +
                        $"followings for further information :{Environment.NewLine}{STRData}", HStatusCode);
                }
                catch
                {
                    RToDispatch = new FireResponse("An additional error was encountered while handling the API errors, " +
                        $"Please check the following data for further information{Environment.NewLine}{STRData}", HStatusCode);
                }
            }

            return RToDispatch;
        }

        #endregion
    }
}
