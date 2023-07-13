namespace HEAppE.DataStagingAPI
{
    /// <summary>
    /// Extension Class
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Request size limit for endpoint method
        /// </summary>
        /// <typeparam name="TBuilder"></typeparam>
        /// <param name="builder">Builder</param>
        /// <param name="size">Size in Bytes</param>
        /// <returns></returns>
        internal static TBuilder RequestSizeLimit<TBuilder>(this TBuilder builder, long size) where TBuilder : IEndpointConventionBuilder
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(new SizeAttribute(size));
            });
            return builder;
        }

        /// <summary>
        /// Remove specific character from beginning or end of string
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="character">Character</param>
        /// <returns></returns>
        internal static string RemoveCharacterFromBeginAndEnd(string text, char character)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            else
            {
                string tempValue = text;
                if (tempValue.ElementAt(0) == character)
                {
                    tempValue = tempValue.Substring(1);
                }
                int lastCharPosition = tempValue.Length - 1;
                if (tempValue.ElementAt(lastCharPosition) == character)
                {
                    tempValue = tempValue.Substring(0, lastCharPosition);
                }
                return tempValue;
            }
        }
    }
}
