using Coldrun.Modules.Trucks.Models;
using Coldrun.Modules.Trucks.Policies;

namespace Coldrun.Tests;

public class TruckTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateTruck()
    {
        // Arrange & Act
        var truck = Truck.Create("TRK001", "Test Truck", "Out Of Service", "Test description");

        // Assert
        Assert.NotEqual(Guid.Empty, truck.Id);
        Assert.Equal("TRK001", truck.Code);
        Assert.Equal("Test Truck", truck.Name);
        Assert.Equal("Out Of Service", truck.Status);
        Assert.Equal("Test description", truck.Description);
    }

    [Fact]
    public void Create_WithNullCode_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Truck.Create(null!, "Name", "Out Of Service"));
    }

    [Fact]
    public void Create_WithEmptyCode_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Truck.Create("", "Name", "Out Of Service"));
    }

    [Fact]
    public void Create_WithNonAlphanumericCode_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Truck.Create("TRK-001", "Name", "Out Of Service"));
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Truck.Create("TRK001", null!, "Out Of Service"));
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Truck.Create("TRK001", "", "Out Of Service"));
    }

    [Fact]
    public void Create_WithNullStatus_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Truck.Create("TRK001", "Name", null!));
    }

    [Fact]
    public void Create_WithEmptyStatus_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Truck.Create("TRK001", "Name", ""));
    }

    [Fact]
    public void Create_WithInvalidStatus_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Truck.Create("TRK001", "Name", "Invalid Status"));
    }

    [Fact]
    public void Create_WithOptionalDescription_ShouldCreateTruck()
    {
        var truck = Truck.Create("TRK001", "Test Truck", "Loading");
        Assert.Null(truck.Description);
    }

    [Theory]
    [InlineData("Out Of Service")]
    [InlineData("Loading")]
    [InlineData("To Job")]
    [InlineData("At Job")]
    [InlineData("Returning")]
    public void Create_WithValidStatus_ShouldCreateTruck(string status)
    {
        var truck = Truck.Create("TRK001", "Test Truck", status);
        Assert.Equal(status, truck.Status);
    }

    [Fact]
    public void Update_WithValidTransition_ShouldUpdateTruck()
    {
        // Arrange
        var truck = Truck.Create("TRK001", "Test Truck", "Out Of Service");
        var policy = new TruckStatusTransitionPolicy();

        // Act
        truck.Update("TRK001", "Updated Truck", "Loading", policy);

        // Assert
        Assert.Equal("TRK001", truck.Code);
        Assert.Equal("Updated Truck", truck.Name);
        Assert.Equal("Loading", truck.Status);
    }

    [Fact]
    public void Update_WithInvalidTransition_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var truck = Truck.Create("TRK001", "Test Truck", "Loading");
        var policy = new TruckStatusTransitionPolicy();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            truck.Update("TRK001", "Test Truck", "At Job", policy));
    }

    [Fact]
    public void Update_WithSameStatus_ShouldNotCheckTransition()
    {
        // Arrange
        var truck = Truck.Create("TRK001", "Test Truck", "Loading");
        var policy = new TruckStatusTransitionPolicy();

        // Act - same status, no transition check needed
        truck.Update("TRK001", "Updated Name", "Loading", policy);

        // Assert
        Assert.Equal("Updated Name", truck.Name);
        Assert.Equal("Loading", truck.Status);
    }
}
