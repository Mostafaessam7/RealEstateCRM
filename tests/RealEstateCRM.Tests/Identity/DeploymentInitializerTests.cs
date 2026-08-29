using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RealEstateCRM.Api.Configuration;

namespace RealEstateCRM.Tests.Identity;

/// <summary>
/// Guards the gate that decides whether deployment tasks run.
///
/// This matters because the failure is silent in both directions: if the flag stops being
/// recognised, a deployment "succeeds" without ever seeding; if the startup path stops being
/// Development-only, every production instance goes back to writing to the database as it boots -
/// the exact behaviour this change removed.
/// </summary>
public class DeploymentInitializerTests
{
    [Theory]
    [InlineData("--init")]
    [InlineData("--INIT")]
    public void The_flag_is_recognised_regardless_of_case(string flag)
    {
        Assert.True(DeploymentInitializer.IsRequested([flag]));
    }

    [Fact]
    public void The_flag_is_recognised_alongside_other_arguments()
    {
        Assert.True(DeploymentInitializer.IsRequested(["--environment", "Production", "--init"]));
    }

    [Fact]
    public void Anything_else_does_not_request_deployment_tasks()
    {
        Assert.False(DeploymentInitializer.IsRequested([]));
        Assert.False(DeploymentInitializer.IsRequested(["--environment", "Production"]));

        // Near-misses must not count. A partial match here would mean an ordinary start silently
        // turning into a database write.
        Assert.False(DeploymentInitializer.IsRequested(["--initialize"]));
        Assert.False(DeploymentInitializer.IsRequested(["init"]));
        Assert.False(DeploymentInitializer.IsRequested(["-init"]));
    }

    [Fact]
    public void No_arguments_at_all_is_handled()
    {
        Assert.False(DeploymentInitializer.IsRequested(null!));
    }

    [Fact]
    public void Startup_seeding_happens_in_development_only()
    {
        Assert.True(DeploymentInitializer.ShouldRunOnStartup(Environment("Development")));

        // The whole point of the change: no other environment writes to the database on boot.
        Assert.False(DeploymentInitializer.ShouldRunOnStartup(Environment("Production")));
        Assert.False(DeploymentInitializer.ShouldRunOnStartup(Environment("Staging")));
        Assert.False(DeploymentInitializer.ShouldRunOnStartup(Environment("Testing")));
    }

    private static IHostEnvironment Environment(string environmentName) =>
        new StubHostEnvironment { EnvironmentName = environmentName };

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = "RealEstateCRM.Api";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
