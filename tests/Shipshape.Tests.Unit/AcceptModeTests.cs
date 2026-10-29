namespace Shipshape.Tests.Unit;

public class AcceptModeTests
{
    [Theory]
    [InlineData("1", null, null, true)]
    [InlineData("true", "false", null, true)]
    [InlineData("1", "true", null, false)]
    [InlineData("1", null, "True", false)]
    [InlineData(null, null, null, false)]
    [InlineData("yes", null, null, false)]
    public void IsOn_ShouldAcceptOnlyOffBuildServers_WhenGivenVariables(string? accept, string? ci, string? tfBuild, bool expected)
    {
        var variables = new Dictionary<string, string?> { ["SHIPSHAPE_ACCEPT"] = accept, ["CI"] = ci, ["TF_BUILD"] = tfBuild };

        var isOn = AcceptMode.IsOn(name => variables.GetValueOrDefault(name));

        Assert.Equal(expected, isOn);
    }
}
