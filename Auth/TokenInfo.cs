using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using Maynard.Time;

namespace Maynard.Auth;

using System;
using System.Text.Json.Serialization;
using Maynard.Json;
using MongoDB.Bson.Serialization.Attributes;

[BsonIgnoreExtraElements]
public class TokenInfo : Model
{
    //
    
    
    [FlexIgnore]
    public string RawJwt { get; set; }

    [FlexKeys(json: "accountId", bson: "aid", Ignore.WhenNull)]
    public string AccountId { get; set; }
    
    [FlexKeys(json: "permissions", bson: "perm")]
    public int PermissionSet { get; set; }

    [FlexKeys(json: "discriminator", bson: "d", Ignore.WhenDefault)]
    public int Discriminator { get; set; }

    [FlexKeys(json: "email", bson: "@", Ignore.WhenNull)]
    public string Email { get; set; }

    [FlexKeys(json: "expiration", bson: "exp", Ignore.WhenDefault)]
    public long Expiration { get; set; }

    [FlexKeys(json: "ip", bson: "ip", Ignore.WhenNull)]
    public string IpAddress { get; set; }

    [FlexKeys(json: "country", bson: "cc", Ignore.WhenNull)]
    public string CountryCode { get; set; }

    [FlexKeys(json: "issuedAt", bson: "iat", Ignore.WhenDefault)]
    public long IssuedAt { get; set; }

    [FlexKeys(json: "issuer", bson: "iss", Ignore.WhenNull)]
    public string Issuer { get; set; }

    [FlexKeys(json: "isAdmin", bson: "su", Ignore.WhenDefault)]
    public bool IsAdmin { get; set; }

    [FlexKeys(json: "secondsRemaining", ignore: Ignore.InBson | Ignore.WhenDefault)]
    public long SecondsRemaining => Math.Max(0, Expiration - Timestamp.Now);

    [FlexKeys(json: "username", ignore: Ignore.InBson | Ignore.WhenNull)]
    public string Username { get; set; }

    [FlexIgnore]
    public bool IsExpired => Expiration <= Timestamp.Now;
    
    [FlexKeys(json: "jwtId", bson: "jti", Ignore.WhenNull)]
    public string JwtId { get; set; }
    
    [FlexKeys(json: "audience", bson: "aud", Ignore.WhenNull)]
    public string Audience { get; set; }
    
    [FlexKeys(json: "validFrom", bson: "nbf", Ignore.WhenDefault)]
    public long ValidFrom { get; set; }

    public string ToJwt() => RawJwt ?? JwtHelper.GenerateJwt(this);
    public static TokenInfo FromJwt(string jwt) => JwtHelper.ValidateJwt(jwt);
}