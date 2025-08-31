using GameLog.Application.Common;

namespace GameLog.Tests.Unit.Application;

public class PagingTests
{
    [Fact]
    public void Defaults_apply_when_nothing_is_supplied()
    {
        var (page, size) = Paging.Clamp(null, null);
        Assert.Equal(1, page);
        Assert.Equal(Paging.DefaultPageSize, size);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(7, 7)]
    public void Page_is_at_least_one(int input, int expected) => Assert.Equal(expected, Paging.Clamp(input, null).Page);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10_000, Paging.MaxPageSize)]
    [InlineData(25, 25)]
    public void Page_size_is_capped_to_protect_the_database(int input, int expected) =>
        Assert.Equal(expected, Paging.Clamp(1, input).PageSize);
}
