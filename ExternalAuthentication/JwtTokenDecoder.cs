using System;
using System.IdentityModel.Tokens.Jwt;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.DTO.JsonTypes;

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
                throw new JwtDecodeException("BadAccessTokenFormat");
            }
            try
            {
                var decodedJwtToken = _tokenHandler.ReadJwtToken(accessToken);
                return new DecodedAccessToken(decodedJwtToken);
            }
            catch (Exception)
            {
                throw new JwtDecodeException("NotReadableAccessToken");
            }
        }
    }
}
