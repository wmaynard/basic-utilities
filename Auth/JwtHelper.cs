using Jose;
using Maynard.Time;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Jose;
using Maynard.Configuration;
using Maynard.ErrorHandling;
using Maynard.Json.Utilities;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using MongoDB.Bson.Serialization.Attributes;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Maynard.Json;

namespace Maynard.Auth;

internal static class JwtHelper
{
    // TODO: change to ES256; faster performance and smaller tokens.
    // To generate keys:
    // openssl ecparam -genkey -name prime256v1 -noout -out private-ec.pem
    // openssl ec -in private-ec.pem -pubout -out public-ec.pem
    private const JwsAlgorithm ALGORITHM = JwsAlgorithm.RS256;
    private const string KEY_SUBSCRIBER_ID = "sub";
    private const string KEY_USERNAME = "sn";
    private const string KEY_EMAIL = "@";
    private const string KEY_PERMISSIONS = "p";
    private const string KEY_IS_ADMIN = "su";
    private const string KEY_JWT_ID = "jti";
    private const string KEY_AUDIENCE = "aud";
    private const string KEY_ISSUER = "iss";
    private const string KEY_VALID_FROM = "nbf";
    private const string KEY_EXPIRATION = "exp";
    private const string KEY_ISSUED_AT = "iat";
    
    internal static JwtConfiguration Config { get; } = new();

    internal static string GenerateJwt(TokenInfo token)
    {
        token.RawJwt = GenerateJwt(token.AccountId, token.Username, token.Email, token.PermissionSet, token.IsAdmin);
        return token.RawJwt;
    }
    internal static string GenerateJwt(string accountId, string username, string email, int permissionFlags, bool isAdmin, int delayInSecondsBeforeValid = 0)
    {
        Dictionary<string, object> payload = new()
        {
            // Standard claims
            { KEY_SUBSCRIBER_ID, accountId },
            { KEY_JWT_ID, Guid.NewGuid().ToString() },
            { KEY_AUDIENCE, Config.ServerAudience },
            { KEY_ISSUER, Config.ServerIssuer },
            { KEY_ISSUED_AT, Timestamp.Now },
            { KEY_EXPIRATION, Timestamp.InTheFuture(seconds: Config.LifetimeInSeconds) },
            
            // Custom claims
            { KEY_USERNAME, username },
            { KEY_EMAIL, email },
            { KEY_PERMISSIONS, Convert.ToInt32(permissionFlags) },
            { KEY_IS_ADMIN, isAdmin }
        };

        if (delayInSecondsBeforeValid > 0)
            payload[KEY_VALID_FROM] = Timestamp.InTheFuture(seconds: delayInSecondsBeforeValid);

        RSAParameters rsaParams;
        using (StringReader reader = new(Config.PrivateKey))
        {
            PemReader pemReader = new(reader);
            AsymmetricCipherKeyPair pair = (AsymmetricCipherKeyPair)pemReader.ReadObject()
                ?? throw new InternalException("Unable to read RSA private key", ErrorCode.NotConfigured);
            RsaPrivateCrtKeyParameters privateRsaParams = (RsaPrivateCrtKeyParameters)pair.Private;
            rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
        }

        using (RSACryptoServiceProvider rsa = new())
        {
            rsa.ImportParameters(rsaParams);
            return JWT.Encode(payload, rsa, ALGORITHM);
        }
    }
    
    internal static TokenInfo ValidateJwt(string token)
    {
        TokenInfo output;
        RSAParameters rsaParams;

        using (StringReader rdr = new (Config.PublicKey))
        {
            PemReader pemReader = new (rdr);
            RsaKeyParameters publicKeyParams = (RsaKeyParameters)pemReader.ReadObject();
            if (publicKeyParams == null)
                throw new Exception("Could not read RSA public key");
            rsaParams = DotNetUtilities.ToRSAParameters(publicKeyParams);
        }
        using (RSACryptoServiceProvider provider = new ())
        {
            provider.ImportParameters(rsaParams);

            // This will throw if the signature is invalid
            FlexJson json = JWT.Decode(token, provider, ALGORITHM);

            output = new()
            {
                // Standard claims
                AccountId = json.Require<string>(KEY_SUBSCRIBER_ID),
                JwtId = json.Require<string>(KEY_JWT_ID),
                Audience = json.Require<string>(KEY_AUDIENCE),
                Issuer = json.Require<string>(KEY_ISSUER),
                IssuedAt = json.Require<long>(KEY_ISSUED_AT),
                ValidFrom = json.Optional<long>(KEY_VALID_FROM),
                Expiration = json.Require<long>(KEY_EXPIRATION),

                // Store the raw JWT for future calls on the user's behalf
                RawJwt = token,
                
                // Custom claims
                Username = json.Require<string>(KEY_USERNAME),
                Email = json.Require<string>(KEY_EMAIL),
                PermissionSet = json.Optional<int>(KEY_PERMISSIONS),
                IsAdmin = json.Optional<bool>(KEY_IS_ADMIN)
            };
        }

        if (output.IsExpired)
            throw new InternalException("Authorization failed; token is expired.", ErrorCode.ExpiredToken, new
            {
                Token = output,
                ExpiredFor = $"{Timestamp.Now - output.Expiration} seconds",
                
            });
        if (output.Audience != Config.ServerAudience)
            throw new InternalException("Authorization failed; token is not valid for this resource.", ErrorCode.InvalidToken, new
            {
                Token = output,
                Audience = output.Audience,
                ExpectedAudience = Config.ServerAudience
            });
        
        Config.OnAuthenticated?.Invoke(null, output);
        

        return output;
    }

    internal static FlexJson ReadJwt(string token) => JWT.Decode(token);

    internal static void UseTemporaryKeyPair()
    {
        using RSA rsa = RSA.Create(2048);
        Config.PrivateKey = rsa.ExportRSAPrivateKeyPem();
        Config.PublicKey = rsa.ExportRSAPublicKeyPem();
    }
}

