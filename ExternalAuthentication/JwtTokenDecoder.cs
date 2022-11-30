using System;
using System.IdentityModel.Tokens.Jwt;
using HEAppE.ExternalAuthentication.Exceptions;
using HEAppE.ExternalAuthentication.JsonTypes;

namespace HEAppE.ExternalAuthentication
{
    public static class JwtTokenDecoder
    {
        private static JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();

        /// <summary>
        /// Decode raw OpenId access token as JWT token.
        /// </summary>
        /// <param name="accessToken">OpenId access token.</param>
        /// <returns>Decoded JWT access token token.</returns>
        /// <exception cref="JwtDecodeException">Thrown when unable to decoded JWT token.</exception>
        public static DecodedAccessToken Decode(string accessToken)
        {
            if (!_tokenHandler.CanReadToken(accessToken))
            {
                throw new JwtDecodeException("Provided access_token is not well formed JWT.");
            }
            try
            {
                var decodedJwtToken = _tokenHandler.ReadJwtToken(accessToken);
                return new DecodedAccessToken(decodedJwtToken);
            }
            catch (Exception innerException)
            {
                throw new JwtDecodeException("Unable to read provided access_token as JWT token.", innerException);
            }
        }
    }
}
