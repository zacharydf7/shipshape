namespace Shipshape;

internal static class AcceptMode
{
    public static bool IsOn() => IsOn(Environment.GetEnvironmentVariable);

    // A build server never accepts: a stale script there must fail. CI and TF_BUILD are what most build servers
    // (GitHub Actions, Bitbucket, GitLab, Azure DevOps) set.
    public static bool IsOn(Func<string, string?> variable) =>
        variable(MigrationScript.AcceptVariable) is "1" or "true"
        && !(variable("CI") is { Length: > 0 } ci && ci != "false" && ci != "0")
        && variable("TF_BUILD") is not { Length: > 0 };
}
