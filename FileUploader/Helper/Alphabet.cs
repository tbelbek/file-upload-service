namespace FileUploader.Helper
{
    public class AlphabetTest
    {
        /// <summary>
        /// Base-16: hexadecimal.
        /// </summary>
        public const string Base16 = "0123456789ABCDEF";

        /// <summary>
        /// Base-52: similar to base-62, but excluding vowels.
        /// </summary>
        public const string Base52 = "0123456789BCDFGHJKLMNPQRSTVWXYZbcdfghjklmnpqrstvwxyz";

        /// <summary>
        /// Base-58: similar to base-62, but excluding characters that may look similar.
        /// </summary>
        public const string Base58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        /// <summary>
        /// Base-62: 0-9, A-Z, a-z.
        /// </summary>
        public const string Base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    }
}