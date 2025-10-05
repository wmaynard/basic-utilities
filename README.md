# Maynard.Utilities

A collection of utilities and helper classes to speed up development.

## Project History

Three years of my career were spent building dozens of microservices for R Studios.  I built a monolithic library that all of our backend projects used.  The tools included in that library enabled me to have a cross-discipline kickoff meeting with designers, PMs, and developers for a new microservice and have a prototype deployed and live within thirty minutes.

After R Studios closed its doors, I was granted permission to open source the library, but it had all the extra cruft built around our specific tech stack.  I'm currently stitching together the most common tools I built and am working to make them more portable and reusable for other projects.

This NuGet package is unstable in the sense that namespaces and classes may see breaking changes while I clean up the code, reintroduce other classes, and change method signatures.  You're of course welcome to use it, but I make no guarantees of stability while it's in beta.

## Highlights

Among other tools included in this library, the most useful of them are listed below.

### Logging Utilities

Pretty-printed logs are a must for any developer.  Nearly every engineer has some form of custom printing they've built at some point, and the `Log` class here is the wrapper we used at R Studios.  With a configurable Timestamp column, custom owners, logging levels, and messages, we pretty-printed our logs in a tabular format during debugging.  An additional configurable option allows you to print both additional log data and any exceptions that go along with the log.

#### Sample Output

```
 Timestamp              ┃ Owner  ┃ Level ┃ Message
━━━━━━━━━━━━━━━━━━━━━━━━╇━━━━━━━━╇━━━━━━━╇━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
 [PDT] 08:37:36.340     │ John   │ WARN  │  A private or public key was not found for JWT authentication.  A new private / public key pair will be generated.  This is not recommended for
                        ┊        ┊       │  production use.
                        ┊        ┊       └──┬── Log Data
                        ┊        ┊          │ {
                        ┊        ┊          │   "help": "Use the AuthConfigurationBuilder to set the private / public keys (PEM format).",
                        ┊        ┊          │   "detail": "Generated keys do not persist and are unique to each instance of the application.  Auth will work, but if deployed in a cluster with multiple instances, you will see auth errors."
                        ┊        ┊       ┌──┘ }
 [PDT] 08:37:36.438     │ Will   │ GOOD  │ Logging configured successfully!
 [PDT] 08:37:36.625     │ Will   │ GOOD  │ Connected to MongoDB.
 [PDT] 08:37:36.629     │ Will   │ GOOD  │ Mongo configured successfully!
```

#### Usage

```csharp
// Log.{Level}(Enum, string, object, Exception)
// Log.{Level}(string, object, Exception)
Log.Error("This is a sample error message.", new Exception("Hello, World!"));
Log.Info(Owner.Will, "Hi-diddly-ho neighborino!");
Log.Verbose(Owner.Bob, "Here's a log with data", new 
{
    SomeField = "Some value",
    AnotherField = 1234567890
});
```

For release builds, it's recommended to instead reroute the logs and send them to a proper logging system rather than console printing.  See Getting Started for how to do this.

### Timestamps & Intervals

We relied heavily on helper classes to reduce boilerplate dealing with Unix timestamps.  Above all else, I wanted our code to be self-documenting when it came to dealing with these, as Unix timestamps are meaningless for a human-readable standpoint.

No more dealing with `DateTimeOffset` manually!

#### Usage

```csharp
long now = Timestamp.Now;
long inFiveMinutes = Timestamp.FiveMinutesFromNow;
long twoDaysAgo = Timestamp.TwoDaysAgo;
long customFuture = Timestamp.InTheFuture(hours: 10, minutes: 5, seconds: 42);

long interval = Interval.FiveMinutes;
long intervalMs = IntervalMs.OneWeek;
```

### JSON Web Tokens (JWTs)

The majority of our authentication was done with JWTs.  We used them for our end-users as well as our internal users for administrative functions.  This library seeks to make authentication with JWTs as easy as possible.

#### Usage

```csharp
// Sample permissions, not part of the library
[Flags]
enum Permissions
{
    CreateRecords = 0b_0001,
    ReadRecords = 0b_0010,
    UpdateRecords = 0b_0100,
    DeleteRecords = 0b_1000
}

string jwt = new TokenInfo
{
    AccountId = "deadbeefdeadbeefdeadbeef",
    // All fields below are optional
    Email = "will@willmaynard.com",
    Username = "willmaynard",
    PermissionSet = (int)(Permissions.ReadRecords | Permissions.CreateRecords | Permissions.DeleteRecords),
    IsAdmin = true
}.ToJwt();
```

Tokens will work by default with no additional setup, which is useful for prototyping.  However, a significant caveat is that the auth should be configured before a project is deployed to production.

Without configuring RSA keys, each token is only valid for the current running instance.  If you deploy a project in a cluster with more than one instance, they will not be able to authenticate tokens originating from any other instances.  Similarly, tokens created between runs will not be valid.  See Getting Started for how to do this.

#### Note: More authentication helpers are coming soon in the form of Filters and Attributes for Controllers.

### Stock Filters

Coming soon!

## Getting Started

In your application's startup, use the builder patterns to configure these tools:

```csharp
app.ConfigureMaynardTools(tools => tools
    // JWT parameters
    .ConfigureAuth(auth => auth
        .SetAudience("https://localhost:3013/")
        .SetIssuer("Will Maynard")
        .SetTokenValidity(secondsToLive: Interval.ThreeDays)
        .SetKeys(Secret.PrivateKey, Secret.PublicKey)
    )
    // Logging parameters
    .ConfigureLogging(logs => logs
        // Set your custom Owners enum here, and default owner for the project
        .AssignOwners(typeof(Owners), defaultOwner: Owners.Will)
        // Hide all logs below this severity
        .SetMinimumSeverity(Severity.Warn)
        // Customize your timestamps for pretty printing
        .SetTimestampDisplay(TimestampDisplaySettings.TimeLocal)
        // Determine which log severities to print detailed log data for
        .PrintExtras(Severity.Warn | Severity.Error | Severity.Alert)
        #if RELEASE
        .Reroute(...) // Custom EventHandler<LogData>; when provided, this silences console log output
        #endif
    )
);
```

More configuration options will be coming soon!

## What's Next For This Project?

Since the project was split off from a monolithic library, there are a lot of documentaiton / quality of life updates on the way.

The most important update coming soon is a collection of filters and an attribute we used on our Controllers for automatic authorization and uncaught exception handling.  For more information on filters, see the [Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-9.0).