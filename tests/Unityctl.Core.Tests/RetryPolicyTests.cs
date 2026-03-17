using System.Text.Json.Nodes;
using Unityctl.Core.Retry;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests;

public class RetryPolicyTests
{
    [Theory]
    [InlineData(StatusCode.Compiling, true)]
    [InlineData(StatusCode.Reloading, true)]
    [InlineData(StatusCode.Busy, true)]
    [InlineData(StatusCode.NotFound, false)]
    [InlineData(StatusCode.ProjectLocked, false)]
    [InlineData(StatusCode.BuildFailed, false)]
    [InlineData(StatusCode.Ready, false)]
    public void IsTransient_Classification(StatusCode code, bool expected)
    {
        Assert.Equal(expected, RetryPolicy.IsTransient(code));
    }

    [Fact]
    public void GetDelayMs_ExponentialBackoff()
    {
        var policy = new RetryPolicy { BaseDelayMs = 1000 };
        Assert.Equal(1000, policy.GetDelayMs(0));
        Assert.Equal(2000, policy.GetDelayMs(1));
        Assert.Equal(4000, policy.GetDelayMs(2));
    }

    [Fact]
    public void GetDelayMs_CappedAtMax()
    {
        var policy = new RetryPolicy { BaseDelayMs = 1000, MaxDelayMs = 5000 };
        Assert.Equal(5000, policy.GetDelayMs(10));
    }

    [Fact]
    public async Task ExecuteWithRetry_SuccessOnFirst()
    {
        var policy = new RetryPolicy();
        var response = await policy.ExecuteWithRetryAsync(() =>
            Task.FromResult(CommandResponse.Ok("done")));
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ExecuteWithRetry_FatalFailsImmediately()
    {
        var policy = new RetryPolicy();
        int attempts = 0;
        var response = await policy.ExecuteWithRetryAsync(() =>
        {
            attempts++;
            return Task.FromResult(CommandResponse.Fail(StatusCode.NotFound, "nope"));
        });
        Assert.Equal(1, attempts);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteWithRetry_TransientRetries()
    {
        var policy = new RetryPolicy { MaxRetries = 2, BaseDelayMs = 10 };
        int attempts = 0;
        var response = await policy.ExecuteWithRetryAsync(() =>
        {
            attempts++;
            if (attempts < 3)
                return Task.FromResult(CommandResponse.Fail(StatusCode.Compiling, "wait"));
            return Task.FromResult(CommandResponse.Ok("done"));
        });
        Assert.Equal(3, attempts);
        Assert.True(response.Success);
    }
}
