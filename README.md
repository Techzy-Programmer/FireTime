# FireTime
 * ▶️ The Firebase realtime database wrapper for **C#.NET**
 * 🌠 Works on Firebase REST API service with live event streaming
 * ✳️ Provides root JSON data as [**JToken**](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JToken.htm) object syncronized with server in realtime
----

### Downloading & Installation Of Library
You need to reference the dll file manually inside your project. <br/>
You can use either of these two methods to get started.
* Build the project in Visual Studio 2019 by yourself.
* Or Checkout the **Releases** section of this repository to download binaries.
> Project will be shipped to **Nuget PM** very soon.... 😀
----

### Task & Features
- [x] Read, Write, Update & Remove request.
- [x] Live streaming from REST API.
- [x] Dynamic support for server interactions.
- [x] Add auto reconnection when network drops(while streaming).
- [ ] Add Push support **`Waiting`**
- [ ] Add Queries & Request filtering **`Waiting`**
----

### Usage (Mini Docs)
> **NOTE :** **FireTime** depends upon [**Newtonsoft.Json**](https://github.com/JamesNK/Newtonsoft.Json) for proper functioning.<br/>
> Please [Visit Here](https://github.com/Techzy-Programmer/FireTime/tree/master/FireTime.Example) for complete examples with proper documentations.

#### Initialize FireTime 👇
```csharp
using FireTime;

var FConfigs = new FireConfig // Initialize a new class for Firebase configuarations
{
    FirebaseURL = "https://{your-db-id}.{firebase-domain}.{extension}", // [Required] Valid Firebase realtime database url
    AuthToken = "Your unique firebase auth or access_token", // [Optional] Remove this field if not required
    BearerToken = "Bearer token from custom authorizations" // [Optional] Remove this field if not required
};

var FClient = new FireClient(FConfigs); // Initialize the base client.
```

#### Monitor For Changes From REST Streaming 👇
```csharp
var Listner = FClient.AttachListner("Path/To/Monitor"); // Create a new listner for the specified path in your database
/* Subscribe to events immediately to receive data from REST streaming */

Listner.Changes.OnError += OnError; // [Subscribe for better tracking] Listen for any errors that occur during streaming
Listner.Changes.OnAdded += OnAdded; // Listen to all Addition event that take place in your database at any sub-level
Listner.Changes.OnUpdated += OnUpdated; // Listen to all Updation event that take place in your database at any sub-level
Listner.Changes.OnRemoved += OnRemoved; // Listen to all Deletion event that take place in your database at any sub-level
Listner.Changes.OnMonitoringStarted += OnMonitoringStarted; // Fires whenever monitoring starts or restarts

/* You can access the whole data as available on Firebase server locally from the path you are monitoring */
var SyncedData = Listner.RootDataBranch; // This is a JToken object
if (SyncedData != null) Console.WriteLine(SyncedData);

Listner.Detach(); // Call this to stop listening and free up resources
```
* > Please visit [FireTime.Example](https://github.com/Techzy-Programmer/FireTime/blob/e6c30acef07e69564d3c0e463e2821b23aeea142/FireTime.Example/Stream-API-Example.cs#L1) section to see full implementation of REST streaming service

#### Read JSON Data From Firebase 👇
```csharp
Console.WriteLine("Trying to read data.....");
var FResp = await FClient.ReadAsync("Path/To/Read");

if (FResp.HasError)
{
    Console.WriteLine("Request Failed!\n");
    Console.WriteLine(FResp.GetErrorMSG); // Print the errror message
}
else
{
    Console.WriteLine("Request was sucessfull\n");
    Console.WriteLine(FResp.RawJSON); // Print the read data
}
```
* > Please visit [FireTime.Example](https://github.com/Techzy-Programmer/FireTime/blob/e6c30acef07e69564d3c0e463e2821b23aeea142/FireTime.Example/Read-Write-Example.cs#L10) section to see full implementation on how to properly handle the Read Response

#### Write(Overwrite) Data To Firebase 👇
```csharp
Console.WriteLine("Trying to write data.....");
object DataToWrite = "{\"Sample Key\":\"Sample Data\"}";
var FResp = await FClient.WriteAsync("Path/To/Write", DataToWrite);

if (FResp.HasError)
{
    Console.WriteLine("Request Failed!\n");
    Console.WriteLine(FResp.GetErrorMSG); // Print the errror message
}
else
{
    Console.WriteLine("Request was sucessfull\n");
    Console.WriteLine(FResp.RawJSON); // Print the data which was written to db
}
```
* > Please visit [FireTime.Example](https://github.com/Techzy-Programmer/FireTime/blob/e6c30acef07e69564d3c0e463e2821b23aeea142/FireTime.Example/Read-Write-Example.cs#L39) section to see full implementation on how to properly handle the Write Response

#### Update Data To Firebase 👇
```csharp
Console.WriteLine("Trying to update data.....");
object DataToUpdate = "{\"Sample Key\":\"Sample Data\"}";
var FResp = await FClient.UpdateAsync("Path/To/Update", DataToUpdate);

if (FResp.HasError)
{
    Console.WriteLine("Request Failed!\n");
    Console.WriteLine(FResp.GetErrorMSG); // Print the errror message
}
else
{
    Console.WriteLine("Request was sucessfull\n");
    Console.WriteLine(FResp.RawJSON); // Print the data which was updated to db
}
```
* > Please visit [FireTime.Example](https://github.com/Techzy-Programmer/FireTime/blob/e6c30acef07e69564d3c0e463e2821b23aeea142/FireTime.Example/Update-Remove-Example.cs#L10) section to see full implementation on how to properly handle the Update Response

#### Remove Data(With Nested Nodes) From Firebase 👇
```csharp
Console.WriteLine("Trying to remove data.....");
var FResp = await FClient.RemoveAsync("Path/To/Read");

if (FResp.HasError)
{
    Console.WriteLine("Request Failed!\n");
    Console.WriteLine(FResp.GetErrorMSG); // Print the errror message
}
else
    Console.WriteLine("Request was sucessfull, Node with all nested data has been removed!\n");
```
* > Please visit [FireTime.Example](https://github.com/Techzy-Programmer/FireTime/blob/e6c30acef07e69564d3c0e463e2821b23aeea142/FireTime.Example/Update-Remove-Example.cs#L88) section to see full implementation on how to properly handle the Remove Response
----

### Contributing Guidelines

~~~~
This Repo is currently in it's infancy state so, bugs and problems may occur!
We strictly want a huge contributing Community to make FireTime robust.
Please check below to know how to contribute.
~~~~

* #### Technical Contributions
###### If you're a developer and know c# and .net languages then you can contribute. If you find any issue or bug then kindly initiale a issue request on github asap and mention the file and line number where you are having the problems with potential fixes (if any). If you want to request for new feature or for any code fixes then initiate a pull request with proper comments and label, I will verify and merge it.

* #### Financial Contributions
###### If you are not from a technical background and found this library useful then you can donate us to contribute financially.<br/> I accept Bitcoin(BTC), Litecoin(LTC), Ethereum(ETH) and Dogecoin(DOGE)<br/> Any support to the following crypto addresses is appreciated:
> Bitcoin(BTC) => **bc1qpn87859y0cmrka5yarg0y7u2y96wtkzjczzm9d** <br/>
> Litecoin(LTC) => **LTwUNH5LJuu5c1QtSm78a93PWFPW72u5kC** <br/>
> Ethereum(ETH) => **0xC6336b712Bec53beC85aA9611C23570c5072AD5D** <br/>
> Dogecoin(Doge) => **DT6T8juTzguG17uNQMJNcBdPki5CF75iQT** <br/>
----

### License Term
##### FireTime library is governed under the opensource [MIT License](https://en.wikipedia.org/wiki/MIT_License).<br/>We here by grant a free usage, modification, and commercial usage of this project without limitation.<br/>However, copyright credits must be given to the original author.
