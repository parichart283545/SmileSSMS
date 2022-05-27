using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JWT.Builder;

namespace SmileSCommunicateRESTfulService.Helper
{
    public class GlobalFunction
    {
        public static string GetAPIUserFromJWTKey(string key)
        {
            var result = "";
            var jwtKey = "";

            //Get jwt key
            jwtKey = Properties.Settings.Default.IsUAT ? Properties.Settings.Default.JWTKey_UAT : Properties.Settings.Default.JWTKey_Production;

            //read jwt
            try
            {
                var payload = new JwtBuilder()
                    .WithSecret(jwtKey)
                    .MustVerifySignature()
                    .Decode<IDictionary<string,object>>(key);

                result = payload["projectid"].ToString();
            }
            //Token Expire
            catch(JWT.TokenExpiredException)
            {
                //token has expire
                result = "token has expire";
            }

            //Invalid Signature
            catch(JWT.SignatureVerificationException)
            {
                //token has invalid signature
                result = "token has invalid signature";
            }
            catch(Exception e)
            {
                result = e.ToString();
            }

            return result;
        }
    }
}