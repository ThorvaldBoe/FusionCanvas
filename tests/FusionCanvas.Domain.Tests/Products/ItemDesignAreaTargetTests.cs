using FusionCanvas.Domain.Products;

namespace FusionCanvas.Domain.Tests.Products;

public sealed class ItemDesignAreaTargetTests
{
    [Fact]
    public void Target_RequiresNonEmptyIds()
    {
        Assert.Throws<ArgumentException>(() => new ItemDesignAreaTarget(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new ItemDesignAreaTarget(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Target_RetainsReferences()
    {
        var itemId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var target = new ItemDesignAreaTarget(itemId, areaId);
        Assert.Equal(itemId, target.ItemId);
        Assert.Equal(areaId, target.DesignAreaId);
    }

    [Fact]
    public void Target_IsValueTypeEqualityByReferences()
    {
        var itemId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        Assert.Equal(new ItemDesignAreaTarget(itemId, areaId), new ItemDesignAreaTarget(itemId, areaId));
    }
}
