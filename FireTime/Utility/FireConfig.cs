namespace FireTime
{
    /// <summary>
    /// Use this class to set custom protocols for connecting to the Firebase Realtime Database!
    /// </summary>
    public class FireConfig
    {
        /// <summary>
        /// <para>Initial and the root URL of your Firebase database. The url should be in the following formats</para>
        /// <para>Either : https://{your-dbname}.{location-prefix}.firebasedatabase.app</para>
        /// <para>Or : https://{your-dbname}.firebaseio.com</para>
        /// </summary>
        public string FirebaseURL { get; set; }

        /// <summary>
        /// Pass in the authentication token(auth) if your databese is protected by security rules
        /// </summary>
        public string AuthToken { get; set; }

        /// <summary>
        /// Pass in the bearer token for accesing database resources if it requires special OAuth or Custom login method
        /// </summary>
        public string BearerToken { get; set; }
    }
}
