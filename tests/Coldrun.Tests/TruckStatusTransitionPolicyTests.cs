using Coldrun.Modules.Trucks.Policies;

namespace Coldrun.Tests;

public class TruckStatusTransitionPolicyTests
{
    private readonly TruckStatusTransitionPolicy _policy = new();

    // Out Of Service transitions (5 cells)
    [Theory]
    [InlineData("Out Of Service", "Out Of Service", true)]
    [InlineData("Out Of Service", "Loading", true)]
    [InlineData("Out Of Service", "To Job", true)]
    [InlineData("Out Of Service", "At Job", true)]
    [InlineData("Out Of Service", "Returning", true)]
    public void OutOfService_Transitions(string current, string next, bool expected)
    {
        Assert.Equal(expected, _policy.CanTransition(current, next));
    }

    // Loading transitions (5 cells)
    [Theory]
    [InlineData("Loading", "Out Of Service", true)]
    [InlineData("Loading", "Loading", true)]
    [InlineData("Loading", "To Job", true)]
    [InlineData("Loading", "At Job", false)]
    [InlineData("Loading", "Returning", false)]
    public void Loading_Transitions(string current, string next, bool expected)
    {
        Assert.Equal(expected, _policy.CanTransition(current, next));
    }

    // To Job transitions (5 cells)
    [Theory]
    [InlineData("To Job", "Out Of Service", true)]
    [InlineData("To Job", "Loading", false)]
    [InlineData("To Job", "To Job", true)]
    [InlineData("To Job", "At Job", true)]
    [InlineData("To Job", "Returning", false)]
    public void ToJob_Transitions(string current, string next, bool expected)
    {
        Assert.Equal(expected, _policy.CanTransition(current, next));
    }

    // At Job transitions (5 cells)
    [Theory]
    [InlineData("At Job", "Out Of Service", true)]
    [InlineData("At Job", "Loading", false)]
    [InlineData("At Job", "To Job", false)]
    [InlineData("At Job", "At Job", true)]
    [InlineData("At Job", "Returning", true)]
    public void AtJob_Transitions(string current, string next, bool expected)
    {
        Assert.Equal(expected, _policy.CanTransition(current, next));
    }

    // Returning transitions (5 cells)
    [Theory]
    [InlineData("Returning", "Out Of Service", true)]
    [InlineData("Returning", "Loading", true)]
    [InlineData("Returning", "To Job", false)]
    [InlineData("Returning", "At Job", false)]
    [InlineData("Returning", "Returning", true)]
    public void Returning_Transitions(string current, string next, bool expected)
    {
        Assert.Equal(expected, _policy.CanTransition(current, next));
    }

    // Full cycle test: Loading → To Job → At Job → Returning → Loading
    [Fact]
    public void FullCycle_TransitionsShouldAllBeValid()
    {
        Assert.True(_policy.CanTransition("Loading", "To Job"));
        Assert.True(_policy.CanTransition("To Job", "At Job"));
        Assert.True(_policy.CanTransition("At Job", "Returning"));
        Assert.True(_policy.CanTransition("Returning", "Loading"));
    }

    // Invalid cycle direction tests
    [Theory]
    [InlineData("To Job", "Loading")]
    [InlineData("At Job", "To Job")]
    [InlineData("Returning", "At Job")]
    [InlineData("Loading", "Returning")]
    public void InvalidCycleDirection_TransitionsShouldBeInvalid(string current, string next)
    {
        Assert.False(_policy.CanTransition(current, next));
    }

    // GetAllowedTransitions tests
    [Fact]
    public void GetAllowedTransitions_OutOfService_ShouldReturnAllStatuses()
    {
        var allowed = _policy.GetAllowedTransitions("Out Of Service");
        Assert.Equal(5, allowed.Length);
        Assert.Contains("Out Of Service", allowed);
        Assert.Contains("Loading", allowed);
        Assert.Contains("To Job", allowed);
        Assert.Contains("At Job", allowed);
        Assert.Contains("Returning", allowed);
    }

    [Fact]
    public void GetAllowedTransitions_Loading_ShouldReturnLoadingOutOfServiceAndToJob()
    {
        var allowed = _policy.GetAllowedTransitions("Loading");
        Assert.Equal(3, allowed.Length);
        Assert.Contains("Loading", allowed);
        Assert.Contains("Out Of Service", allowed);
        Assert.Contains("To Job", allowed);
    }

    [Fact]
    public void GetAllowedTransitions_InvalidStatus_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _policy.GetAllowedTransitions("Invalid"));
    }

    [Fact]
    public void CanTransition_InvalidCurrentStatus_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _policy.CanTransition("Invalid", "Loading"));
    }

    [Fact]
    public void CanTransition_InvalidNewStatus_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _policy.CanTransition("Loading", "Invalid"));
    }
}
