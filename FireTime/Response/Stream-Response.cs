using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using FireTime.Private;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace FireTime.Response
{
    /// <summary>
    /// Monitor at any nested path usin this class
    /// </summary>
    public partial class StreamResponse
    {
        #region Declaration

        private string PPth; // Global pre path config
        private long WorkerID; // Prevent multiple instance of same worker thread
        private Thread Worker = null; // Base worker thread
        private JToken RootData = null; // Base local cache object to store and sync with server data
        private bool IsReListen = false; // To be dispatched along events
        private bool HasCancelled = false; // Custom Cancellation Implementation
        private readonly Uri FireURI = null; // Root Uri object used in connection
        private readonly ReConnector AutoConnect; // Custom implementation of reconnection

        /// <summary>
        /// <para>Get the copy of local cache which represents the synchronized state of data with the server</para>
        /// <para>It contains the data from the branch which you have selected to Listen</para>
        /// <para>You may get a null or empty object if there is no data (or if the data was deleted) on the server at the selected path</para>
        /// </summary>
        public JToken RootDataBranch { get => RootData.DeepClone(); }

        /// <summary>
        /// Utilise this property to subscribe to the streaming events of Firebase
        /// </summary>
        public FireStreamEvent Changes { get; internal set; }

        #endregion

        #region Base Methods

        internal StreamResponse(Uri _WorkUri)
        {
            FireURI = _WorkUri;
            Changes = new FireStreamEvent();
            AutoConnect = new ReConnector(this);
        }

        internal void StartDetection()
        {
            Thread.Sleep(500);

            Worker = new Thread(async () =>
            {
                var MClient = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10
                }, true);

                HttpResponseMessage StreamResp;
                var StreamReq = new HttpRequestMessage(HttpMethod.Get, FireURI);
                MClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

                try
                {
                    StreamResp = (await MClient.
                    SendAsync(StreamReq, HttpCompletionOption.ResponseHeadersRead)).
                    EnsureSuccessStatusCode();
                }
                catch (Exception Ep)
                {
                    if (IsReListen) StartDetection();
                    else Changes.WarnError(Ep);
                    return;
                }

                Changes.NotifyMonitoring(IsReListen);
                AutoConnect.ReStartDetection();
                IsReListen = true;

                using (StreamResp) // Memory management with 'using' blocks
                using (Stream SCnt = await StreamResp.Content.ReadAsStreamAsync())
                using (StreamReader SRLive = new StreamReader(SCnt))
                {
                    WorkerID++;
                    string SEvent = string.Empty;
                    string WIDCurrent = WorkerID.ToString();

                    try
                    {
                        while (true) // Infinitely loop for data
                        {
                            string ReadData = await SRLive.ReadLineAsync(); // Blocks here till a new data is received from Firebase server
                            if (HasCancelled || !WIDCurrent.Equals(WorkerID.ToString())) break; // Silently exit the loop when any condition matches
                            
                            if (!string.IsNullOrWhiteSpace(ReadData))
                            {
                                AutoConnect.ReStartDetection();

                                if (ReadData.StartsWith("event:")) // Check if server sent the event name
                                {
                                    SEvent = ReadData.Substring(7).ToUpper(); // Store the event name
                                    if (!string.IsNullOrWhiteSpace(SEvent) && !SEvent.StartsWith("P")) ProcessWildEvents(SEvent);
                                    continue; // Proceed this loop and check for data related to the event name
                                }

                                if (ReadData.StartsWith("data:")) // Check if server sent the data based upon the previous event signal
                                {
                                    if (!string.IsNullOrWhiteSpace(SEvent)) // Check if event name is associated with the data
                                        ProcessDataEvent(SEvent, ReadData.Substring(6)); // Handle the server data and event on another method
                                }

                                SEvent = string.Empty; // Make the event name empty
                            }
                        }
                    }
                    catch { if (!HasCancelled) StartDetection(); return; }
                }
            });

            Worker.Start();
        }

        private void ProcessWildEvents(string EvtName)
        {
            switch (EvtName)
            {
                case "KEEP-ALIVE": /* Simply Ignore This */ break;
                case "CANCEL": Changes.WarnError(new FireError("Read operation at specified location has been revoked.")); break;
                case "AUTH_REVOKED": Changes.WarnError(new FireError("Authentication token was revoked for read operation.")); break;
            }
        }

        private void ProcessDataEvent(string EvtName, string EData)
        {
            try
            {
                switch (EvtName)
                {
                    case "PUT":
                    case "PATCH":

                        // PUT or PATCH event is received
                        var SData = JsonConvert.DeserializeObject<ServerData>(EData); // Convert the server sent string to a valid object
                        SData.Path = SData.Path.Substring(1); // Remove the first / character from the path data
                        ProcessServerDATA(SData.Path, SData.Data); // Pass the Path and Object data to next method
                        break;
                }
            }
            catch (Exception E) { Changes.WarnError(E); }
        }

        private void ProcessServerDATA(string Path, object PData)
        {
            if (Path == string.Empty)
            {
                if (RootData != null && PData != null &&
                    IsArrayPUSH(Path, out JArray AProc, out bool IsPreC)) HandleJArray(Path, PData, AProc, IsPreC); // Hadle whenever json array data-type is received
                else if (PData != null) Perform_MBO(ref RootData, string.Empty, PData); // Something is either added or updated
                else if (RootData != null) // As main branch was not null but since new data received is null so simply invoke delete commands
                {
                    var Prev = RootData;
                    RootData = JToken.FromObject(0); // Remove data from the main branch by making it void but not null
                    Changes.NotifyRemoved(string.Empty, Prev); // Fire removed event with previous data
                }

                Changes.HasPrevented = false;
            }
            else
            {
                if (RootData == null)
                    RootData = JToken.FromObject(0);

                if (PData == null) // Universal Remove event handler
                {
                    var TPth = TreatPath(Path); // Treat the operable path to JToken escaped format
                    var DelTOK = RootData.SelectToken(string.Join(".", TPth)); // Select the JToken based on the the treated path

                    if (DelTOK == null)
                    {
                        JToken SelToDel;
                        string DPath;

                        if ((DPath = GetDynamicDELPath(Path, out int AIndex)) != null)
                        {
                            if ((SelToDel = RootData.SelectToken(DPath)) != null)
                            {
                                if (SelToDel is JArray JADel)
                                {
                                    if (AIndex > -1 && AIndex < JADel.Count)
                                    {
                                        var Prev = JADel[AIndex];
                                        JADel[AIndex] = null;
                                        JADel.TrimNull();
                                        Changes.NotifyRemoved(Path, Prev);
                                    }
                                }
                                else
                                {
                                    var Prev = SelToDel;
                                    SelToDel.Parent.Remove();
                                    TryClearEmptyKeys(RootData, TPth);
                                    Changes.NotifyRemoved(Path, Prev);
                                }
                            }
                        }
                    }
                    else
                    {
                        var Prev = DelTOK;
                        DelTOK.Parent.Remove(); // Remove the node from the local cache
                        TryClearEmptyKeys(RootData, TPth); // Try to clear the path if it become empty after deletion
                        Changes.NotifyRemoved(Path, Prev); // Fire the OnRemoved event
                    }
                }
                else TriggerHandlers(Path, PData);
            }
        }

        private void Perform_MBO(ref JToken Subject, string Path, object UPData) // Perform Main Branch Operation
        {
            bool IsSub0, IsAddEvent, CanEnter;
            try { IsSub0 = Subject == null || Subject.ToObject<int>() == 0; } catch { IsSub0 = false; }

            CanEnter = IsSub0 || !Subject.HasValues;
            IsAddEvent = CanEnter && IsSub0;

            if (UPData is JObject JObj) // Complex data type received
            {
                if (CanEnter && Subject is JObject)
                {
                    foreach (var DItem in JObj.Properties()) // Loop through each property persent in the data
                    {
                        var Prev = Subject[DItem.Name];
                        Subject[DItem.Name] = JToken.FromObject(DItem.Value); // Write the new data to root object at specified path
                        if (Subject[DItem.Name] == null) Changes.NotifyAdded(Path, new JObject(DItem)); // Fire OnAdded event
                        else Changes.NotifyUpdated(Path, Prev, new JObject(DItem)); // Fire OnUpdated event
                    }
                }
                else // New data is received directly to the main branch
                {
                    var Prev = Subject;
                    var NData = JToken.FromObject(UPData); // Initialize the main object with new data
                    Subject = NData; // Write data to the main branch
                    if (IsAddEvent) Changes.NotifyAdded(Path, NData); // Fire OnAdded event
                    else Changes.NotifyUpdated(Path, Prev, NData); // Fire OnUpdated event
                }
            }
            else // Same docs as above but with simple object data
            {
                var Prev = Subject;
                var DataParsable = JToken.FromObject(UPData);
                Subject = DataParsable; // Write data to the main branch
                if (IsAddEvent) Changes.NotifyAdded(Path, DataParsable);
                else Changes.NotifyUpdated(Path, Prev, DataParsable);
            }
        }

        #endregion

        #region Action Handlers

        private void HandleJArray(string Path, object SData, JArray Base, bool IsPreC, string PrePath = "")
        {
            int DIndex;
            string Key = "-";
            bool IsAIndexed = false;
            if (string.IsNullOrWhiteSpace(PrePath)) PrePath = Path;

            if (IsPreC)
            {
                Key = Path.Substring(Path.LastIndexOf('/') + 1);
                IsAIndexed = int.TryParse(Key, out DIndex);
                if (!IsAIndexed) DIndex = -1;
            }
            else DIndex = -1;

            if (IsPreC)
            {
                var ToRender = JToken.FromObject(SData);

                if (IsAIndexed) // [Key as Int] => Object (Add inside data)
                {
                    if (Base.Count > DIndex)
                    {
                        var Prev = Base[DIndex];
                        Base[DIndex] = ToRender;
                        if (Base[DIndex].Type == JTokenType.Null) Changes.NotifyAdded(PrePath, ToRender);
                        else Changes.NotifyUpdated(PrePath, Prev, ToRender);
                    }
                    else
                    {
                        Base.Fill(DIndex);
                        Base[DIndex] = ToRender;
                        Changes.NotifyAdded(PrePath, ToRender);
                    }
                }
                else // [Key as String] => Object (Rebuild path data)
                {
                    var AObj = Base.GetOFromA();
                    AObj.Add(Key, ToRender);
                    if (Base == RootData) RootData = AObj;
                    else Base.ChangeTO(ref RootData, AObj);
                    Changes.NotifyAdded(PrePath, ToRender);
                }
            }
            else // No subkey in path proceed accordingly
            {
                if (SData is JObject SDJObj) // PATCH event
                {
                    var Prop = SDJObj.Properties();

                    if (Prop.IsJArrayType())
                    {
                        foreach (var PVal in Prop)
                        {
                            int IVal = int.Parse(PVal.Name);
                            var ToRender = PVal.Value;

                            if (IVal < Base.Count())
                            {
                                var Prev = Base[IVal];
                                Base[IVal] = ToRender;
                                if (Base[IVal].Type == JTokenType.Null) Changes.NotifyAdded(PrePath, ToRender);
                                else Changes.NotifyUpdated(PrePath, Prev, ToRender);
                            }
                            else
                            {
                                Base.Fill(IVal);
                                Base[IVal] = ToRender;
                                Changes.NotifyAdded(PrePath, ToRender);
                            }
                        }
                    }
                    else // Rebuild
                    {
                        var AObj = Base.GetOFromA();
                        foreach (var PVal in Prop)
                            AObj.Add(PVal.Name, PVal.Value);
                        Base.ChangeTO(ref RootData, AObj);
                        Changes.NotifyAdded(PrePath, SDJObj);
                    }
                }
                else // Put event
                {
                    var Prev = Base;
                    var ToDispatch = JToken.FromObject(SData);
                    Base.ChangeTO(ref RootData, ToDispatch);
                    Changes.NotifyUpdated(PrePath, Prev, ToDispatch);
                }
            }
        }

        private void HandleJObject(string Path, JObject SvrData, JToken RData = null, string PrePath = "")
        {
            // Initialize basic variables
            JToken SData = SvrData;
            var TreatPth = TreatPath(Path);
            bool HasAssigned = RData == null;
            var TmpA = SvrData.TryGetAFromO();
            if (TmpA != null) SData = TmpA;
            if (HasAssigned) RData = RootData;
            string[] PArray = GetPathArray(Path);
            if (string.IsNullOrWhiteSpace(PrePath)) PrePath = Path;
            var Selected = RData.SelectToken(string.Join(".", TreatPth));

            if (Selected == null) // Direct PUT or PATCH attempt was made
            {
                if (DynamicInitialize(ref RData, PArray, SData)) // Initialize the lacking path
                {
                    var ReSel = RData.SelectToken(string.Join(".", TreatPth)); // Re-Select the new path
                    ReSel.ChangeTO(ref RootData, SData); // Replace void(no) data with the new data
                    Changes.NotifyAdded(PrePath, SData); // Fire OnAdded event
                }
            }
            else // Generous event found
            {
                if (Selected is JObject SelJOBG && SData is JObject SDObj) // Requested resource is a JS-Object
                {
                    foreach (var DProp in SDObj.Properties()) // Iliterate over the properties of server received data
                    {
                        if (SelJOBG.ContainsKey(DProp.Name)) // Key is already persent on the requested resource
                        {
                            var Prev = SelJOBG[DProp.Name];
                            SelJOBG[DProp.Name] = DProp.Value; // Update the cache [local object]
                            Changes.NotifyUpdated(PrePath, Prev, new JObject(DProp)); // Fire the OnUpdated event
                        }
                        else // Key is not persent on the requested resource
                        {
                            SelJOBG.Add(DProp.Name, DProp.Value); // Add the new entry to the cache [local object]
                            Changes.NotifyAdded(PrePath, new JObject(DProp)); // Fire the OnAdded event
                        }
                    }
                }
                else // Requested resource is a simple object (int), (string), (array) etc.....
                {
                    var Prev = Selected;
                    Selected.ChangeTO(ref RootData, SData); // Simply update the cache [local object]
                    Changes.NotifyUpdated(PrePath, Prev, SData); // Fire the OnUpdated event
                }
            }

            if (HasAssigned) RootData = RData;
        }

        private void HandleOBJ(string Path, object SData, JToken RData = null, string PrePath = "")
        {
            // Initialize basic variables
            bool Assigned = RData == null;
            var TreatPth = TreatPath(Path);
            if (Assigned) RData = RootData;
            string[] PArray = GetPathArray(Path);
            if (string.IsNullOrWhiteSpace(PrePath)) PrePath = Path;
            var Selected = RData.SelectToken(string.Join(".", TreatPth));

            if (Selected == null) // Direct PUT or PATCH attempt was made
            {
                if (DynamicInitialize(ref RData, PArray, SData)) // Initialize the lacking path
                {
                    var ReSel = RData.SelectToken(string.Join(".", TreatPth)); // Re-Select the new path
                    var ADDData = JToken.FromObject(SData); // Store new data to a new variable
                    ReSel.ChangeTO(ref RootData, ADDData); // Replace void(no) data with the new data
                    Changes.NotifyAdded(PrePath, ADDData); // Fire OnAdded event
                }
            }
            else // Generous event found
            {
                var Prev = Selected;
                var UPData = JToken.FromObject(SData); // Store new data to a new variable
                Selected.ChangeTO(ref RootData, UPData); // Replace previous data with the new one
                Changes.NotifyUpdated(PrePath, Prev, UPData); // Fire the OnUpdated event
            }

            if (Assigned) RootData = RData;
        }

        #endregion

        #region Helper Utilities

        /// <summary>
        /// Call this method to detach the listner and release all associated resources.
        /// </summary>
        public void Detach()
        {
            Changes.HasStopped = true;
            AutoConnect.Destroy();
            HasCancelled = true;
        }

        private string[] TreatPath(string BPth)
        {
            if (BPth.Contains("/")) // Check if path is of multi-level
            {
                var PathArray = BPth.Split('/'); // Split and store the path levels
                var PthTreated = new List<string>(); // Create new List to store treated paths

                foreach (var SubPth in PathArray) // Loop through each path
                {
                    string TempSub = SubPth; // Store the path in new temp variable as modifying the iliterating value is not allowed
                    var SIndexes = TempSub.GetIndexes("'").ToArray(); // Get the indexes of all occurence of '
                    for (int I = 0; I < SIndexes.Length; I++) // Using this loop append the escaping \ character before every occurence of the ' character
                        TempSub = TempSub.Insert(SIndexes[I] + I, "\\");
                    PthTreated.Add($"['{TempSub}']"); // Build the final treated value and store it in the list
                }

                return PthTreated.ToArray(); // Return the treated path as array
            }
            else
            {
                // Perform same operation as described above but with only single path variable
                var SIndexes = BPth.GetIndexes("'").ToArray();
                for (int I = 0; I < SIndexes.Length; I++)
                    BPth = BPth.Insert(SIndexes[I] + I, "\\");
                return new string[] { $"['{BPth}']" };
            }
        }

        private string[] GetPathArray(string PlainPath)
            => PlainPath.Contains("/") ? PlainPath.Split('/') : new string[] { PlainPath };

        private string GetDynamicDELPath(string InitialPath, out int ArrayI)
        {
            ArrayI = -1;
            JToken WorkDATA = RootData;
            List<string> PData = new List<string>();
            var DPthArray = GetPathArray(InitialPath);

            for (int I = 0; I < DPthArray.Length; I++)
            {
                if (WorkDATA == null) return null;
                else
                {
                    if (WorkDATA is JArray WAry)
                    {
                        if (int.TryParse(DPthArray[I], out int AIndex))
                        {
                            if (I == DPthArray.Length - 1) ArrayI = AIndex;
                            else PData.Add($"[{AIndex}]");
                            WorkDATA = WAry[AIndex];
                        }
                        else return null;
                    }
                    else if (WorkDATA is JObject WObj)
                    {
                        var TSPth = $"['{DPthArray[I]}']";
                        WorkDATA = WObj.SelectToken(TSPth);
                        PData.Add(TSPth);
                    }
                    else return null;
                }
            }

            return string.Join(".", PData);
        }

        private void TriggerHandlers(string DPath, object SvrData, JToken RefToken = null)
        {
            if (IsArrayPUSH(DPath, out JArray AProc, out bool IsPreC, RefToken)) HandleJArray(DPath, SvrData, AProc, IsPreC, PPth); // Hadle whenever json array data-type is received
            else if (SvrData is JObject SOBJ) HandleJObject(DPath, SOBJ, RefToken, PPth); // Handle triggers on API event or when array is received
            else HandleOBJ(DPath, SvrData, RefToken, PPth); // Handle trigger when manually data is changed using Firebase UI console or when simple object is received
        }

        private bool DynamicInitialize(ref JToken ToInit, string[] PathData, object PData)
        {
            int PSize = PathData.Length; // Get the size of path
            string Pth = TreatPath(PathData[0])[0], PrevPath = Pth; // Get and treat the first element of the path

            if (PSize > 1) // Perform operation when size of path data is greater than 1
            {
                for (int I = 0; I < PSize; I++)
                {
                    var InitSel = ToInit.SelectToken(Pth, false);

                    if (InitSel == null) // Select and check if the data on selected path is null
                    {
                        string TPart = "\":0" + new string('}', PSize - I),
                            SPart = string.Join("\":{\"", PathData.Skip(I).ToArray()),
                            Joined = "{\"" + SPart + TPart; // Build a json body to be filled in the main lacking path

                        if (I - 1 < 0) // Check if path is reached to it's root level
                        {
                            // Fill the json object to the root of the lacking path
                            if (ToInit is JObject JObj)
                                JObj.Add(PathData[0], JToken.Parse(Joined)[PathData[0]]);
                            //else if (ToInit is JArray IntJAry)
                            //    IntJAry.Add(JToken.Parse($"{{\"{PathData.Last()}\":0}}"));
                            else ToInit = JToken.Parse(Joined);
                            break;
                        }


                        // Fill the json object to the root of the lacking path
                        var SelUp = ToInit.SelectToken(PrevPath, false);

                        if (SelUp is JObject SelJObj)
                            SelJObj.Add(PathData[I], JToken.FromObject(0));
                        //else if (SelUp is JArray SelJAry)
                        //    SelJAry.Add(JToken.Parse($"{{\"{PathData.Last()}\":0}}"));
                        else SelUp[PathData[I - 1]] = JToken.Parse(Joined);
                        break;
                    }
                    else if (InitSel is JArray LayerAR)
                    {
                        int DeltaSize = PSize - I;
                        var PDat = PathData.Skip(I + 1).ToArray();
                        if (string.IsNullOrEmpty(PPth)) PPth = string.Join("/", PathData);

                        for (int J = 0; J < DeltaSize; J++)
                        {
                            if (int.TryParse(PDat[J], out int JIndex))
                            {
                                int WI = J + 1;
                                var IVTok = LayerAR[JIndex];
                                var WPth = string.Join("/", PDat.Skip(WI));
                                if (JIndex >= LayerAR.Count) LayerAR.Fill(JIndex);
                                TriggerHandlers(WPth, PData, IVTok);
                                break;
                            }
                        }

                        PPth = string.Empty;
                        return false;
                    }

                    PrevPath = Pth; // Set the current path to be utilized for the next loop operation
                    if (I + 1 < PSize) Pth += "." + TreatPath(PathData[I + 1])[0]; // Treat the next path and store it in Pth variable
                }
            }
            else // Path data contains only one element
            {
                // Check and fill the json object to the root of the lacking path accordingly
                if (ToInit is JObject JObj)
                {
                    if (!JObj.ContainsKey(PathData[0]))
                        JObj.Add(PathData[0], JToken.FromObject(0));
                }
                else ToInit = JToken.Parse($"{{\"{PathData[0]}\":0}}");
            }

            return true;
        }

        private bool IsArrayPUSH(string Path, out JArray TProc, out bool IsPreC, JToken BData = null)
        {
            IsPreC = false;
            JToken PreChild = null;
            var TPath = TreatPath(Path);
            if (BData == null) BData = RootData;
            if (BData == null) { TProc = null; return false; }
            var Child = BData.SelectToken(string.Join(".", TPath));
            if (TPath.Length > 1) PreChild = BData.SelectToken(string.Join(".", TPath.Take(TPath.Length - 1)));
            else if (TPath.Length == 1) PreChild = BData;

            if (PreChild != null && PreChild is JArray PChld)
            {
                IsPreC = true;
                TProc = PChld;
                return true;
            }
            else if (Child != null && Child is JArray Chld)
            {
                TProc = Chld;
                return true;
            }
            else
            {
                TProc = null;
                return false;
            }
        }

        private void TryClearEmptyKeys(JToken ToClear, string[] PToProceed /* This path data should be treated */)
        {
            var Len = PToProceed.Length; // Get and store the length of path data to proceed

            for (int I = 0; I < Len; I++)
            {
                PToProceed = PToProceed.Take(Len - I).ToArray(); // Keep removing the last element from the path array
                var CPTH = string.Join(".", PToProceed); // Build the path to the token based on the above operation
                var Sel = ToClear.SelectToken(CPTH, false); // Select the token based on the path generated above

                if (Sel != null) // Proceed if selected token is not null
                {
                    if (!Sel.HasValues) Sel.Parent.Remove(); // Remove the node from passed JToken if it doesn't contains any data
                    else break; // Break the loop and exit the method immediately if data is found as moving upwards will not have any effect on the further loop iliteration
                }
            }
        }

        #endregion
    }
}
