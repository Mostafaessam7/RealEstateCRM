using RealEstateCRM.Application.Tasks.DTOs;
using RealEstateCRM.Application.Tasks.Validators;
using Xunit;

namespace RealEstateCRM.Tests.Tasks;

public class TaskItemValidatorsTests
{
    private readonly CreateTaskItemRequestValidator _validator = new();

    [Fact]
    public void Fails_WhenTitleIsEmpty()
    {
        var result = _validator.Validate(new CreateTaskItemRequest { Title = "", AssignedToUserId = Guid.NewGuid() });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Fails_WhenAssignedToUserIdMissing()
    {
        var result = _validator.Validate(new CreateTaskItemRequest { Title = "Task", AssignedToUserId = Guid.Empty });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Succeeds_ForValidRequest()
    {
        var result = _validator.Validate(new CreateTaskItemRequest { Title = "Task", AssignedToUserId = Guid.NewGuid() });

        Assert.True(result.IsValid);
    }
}
