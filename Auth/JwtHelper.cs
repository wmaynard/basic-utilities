using Jose;
using Maynard.Time;
using System.Security.Cryptography;
using Maynard.Configuration;
using Maynard.ErrorHandling;
using Maynard.Extensions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Maynard.Json;

namespace Maynard.Auth;

public static class JwtHelper
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
    private const string KEY_FIRST_NAME = "fn";
    private const string KEY_LAST_NAME = "ln";
    
    internal static JwtConfiguration Config { get; } = new();

    private static string GenerateJwt(Dictionary<string, object> claims)
    {
        RSAParameters rsaParams;
        using (StringReader reader = new(Config.PrivateKey))
        {
            PemReader pemReader = new(reader);
            object pemObject = pemReader.ReadObject()
                ?? throw new InternalException("Unable to read RSA private key", ErrorCode.NotConfigured);

            RsaPrivateCrtKeyParameters privateRsaParams = pemObject switch
            {
                AsymmetricCipherKeyPair keyPair => (RsaPrivateCrtKeyParameters)keyPair.Private,
                RsaPrivateCrtKeyParameters rsaParamsOnly => rsaParamsOnly,
                _ => throw new InternalException($"Unsupported PEM object type: {pemObject.GetType().Name}", ErrorCode.NotConfigured)
            };

            rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
        }

        using (RSACryptoServiceProvider rsa = new())
        {
            rsa.ImportParameters(rsaParams);
            return JWT.Encode(claims, rsa, ALGORITHM);
        }
    }
    
    /// <summary>
    /// Creates a JWT from given TokenInfo.  Additionally, assigns the JWT to <see cref="TokenInfo.RawJwt"/>. 
    /// </summary>
    /// <param name="token">A representation of a user you want to generate a token for.</param>
    /// <returns>A string representation of a JSON Web Token.  JWTs are publicly inspectable.</returns>
    internal static string GenerateJwt(TokenInfo token, int permissionFlags = 0, bool isAdmin = false, int delayInSecondsBeforeValid = 0)
    {
        if (string.IsNullOrWhiteSpace(token?.AccountId))
            throw new InternalException("Missing required field.", ErrorCode.InvalidValue, new
            {
                MissingField = nameof(TokenInfo.AccountId)
            });
        
        Dictionary<string, object> payload = new()
        {
            // Standard claims
            { KEY_SUBSCRIBER_ID, token.AccountId },
            { KEY_JWT_ID, Guid.NewGuid().ToString() },
            { KEY_AUDIENCE, Config.ServerAudience },
            { KEY_ISSUER, Config.ServerIssuer },
            { KEY_ISSUED_AT, Timestamp.Now },
            { KEY_EXPIRATION, Timestamp.InTheFuture(seconds: Config.LifetimeInSeconds) },
        };

        if (delayInSecondsBeforeValid > 0)
            payload[KEY_VALID_FROM] = Timestamp.InTheFuture(seconds: delayInSecondsBeforeValid);
        
        // Custom claims use validation before inclusion in the JWT
        #region PII
        if (!string.IsNullOrWhiteSpace(token.Username))
            payload[KEY_USERNAME] = token.Username.Mask();
        if (!string.IsNullOrWhiteSpace(token.Email))
            payload[KEY_EMAIL] = token.Email.Mask();
        if (!string.IsNullOrWhiteSpace(token.FirstName))
            payload[KEY_FIRST_NAME] = token.FirstName.Mask();
        if (!string.IsNullOrWhiteSpace(token.LastName))
            payload[KEY_LAST_NAME] = token.LastName.Mask();
        #endregion PII
        if (permissionFlags > 0)
            payload[KEY_PERMISSIONS] = permissionFlags;
        if (isAdmin)
            payload[KEY_IS_ADMIN] = true;
        
        return GenerateJwt(payload);
    }
    
    /// <summary>
    /// Creates a JWT from given parameters.  Certain parameters should be set during application startup.  See
    /// <see cref="AuthConfigurationBuilder"/> for more information.  Only accountId is required. PII, such as username or email,
    /// will be encrypted with AES to protect the data in case of leaked tokens.  See <see cref="StringExtension.Mask"/> for more information.
    /// </summary>
    /// <param name="accountId">The specific account ID for a user.  If using MongoDB, this should be a 24-digit hex string (ObjectID)</param>
    /// <param name="username">Optional.  The username of the account.</param>
    /// <param name="email">Optional.  The username of the account.</param>
    /// <param name="permissionFlags">Optional.  If you want to lock specific features of your application on a per-token basis, use a
    /// [Flags] enum and pass in the integer value of it.</param>
    /// <param name="isAdmin">Optional.  If this token represents an administrator for your system, this flag will mark the token as an admin.</param>
    /// <param name="delayInSecondsBeforeValid">Optional, Advanced.  If you want to generate a token for future use that isn't immediately valid,
    /// pass in a delay.</param>
    /// <returns>A string representation of a JSON Web Token.  JWTs are publicly inspectable.</returns>
    /// <exception cref="InternalException"></exception>
    public static string GenerateJwt(string accountId, string username = null, string email = null, int permissionFlags = 0, bool isAdmin = false, int delayInSecondsBeforeValid = 0)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new InternalException("Missing required field.", ErrorCode.InvalidValue, new
            {
                MissingField = nameof(accountId)
            });
        Dictionary<string, object> payload = new()
        {
            // Standard claims
            { KEY_SUBSCRIBER_ID, accountId },
            { KEY_JWT_ID, Guid.NewGuid().ToString() },
            { KEY_AUDIENCE, Config.ServerAudience },
            { KEY_ISSUER, Config.ServerIssuer },
            { KEY_ISSUED_AT, Timestamp.Now },
            { KEY_EXPIRATION, Timestamp.InTheFuture(seconds: Config.LifetimeInSeconds) },
        };

        if (delayInSecondsBeforeValid > 0)
            payload[KEY_VALID_FROM] = Timestamp.InTheFuture(seconds: delayInSecondsBeforeValid);
        
        // Custom claims use validation before inclusion in the JWT
        if (!string.IsNullOrWhiteSpace(username))
            payload[KEY_USERNAME] = username.Mask();
        if (!string.IsNullOrWhiteSpace(email))
            payload[KEY_EMAIL] = email.Mask();
        if (permissionFlags > 0)
            payload[KEY_PERMISSIONS] = permissionFlags;
        if (isAdmin)
            payload[KEY_IS_ADMIN] = true;

        return GenerateJwt(payload);
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
                Username = json.Optional<string>(KEY_USERNAME)?.Unmask(),
                Email = json.Optional<string>(KEY_EMAIL)?.Unmask(),
                FirstName = json.Optional<string>(KEY_FIRST_NAME)?.Unmask(),
                LastName = json.Optional<string>(KEY_LAST_NAME)?.Unmask(),
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

