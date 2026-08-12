using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace RealEstateCRM.Tests.Webhooks;

internal class FakeBackgroundJobClient : IBackgroundJobClient
{
    public List<Job> CreatedJobs { get; } = new();

    public string Create(Job job, IState state)
    {
        CreatedJobs.Add(job);
        return Guid.NewGuid().ToString();
    }

    public bool ChangeState(string jobId, IState state, string? expectedState) => true;
}
