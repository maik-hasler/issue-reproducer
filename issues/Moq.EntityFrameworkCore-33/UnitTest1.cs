using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AutoFixture.Xunit2;
using Moq.EntityFrameworkCore;

namespace Moq.EntityFrameworkCore_33;

public class User
{
    public Guid Id { get; set; }

    public string Firstname { get; set; } = string.Empty;

    public string Lastname { get; set; } = string.Empty;

    public bool SomeActionHasBeenPerformed { get; set; }
}

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
}

public record ExampleRequest(string Firstname) : IRequest;

public class ExampleRequestHandler : IRequestHandler<ExampleRequest>
{
    private readonly ApplicationDbContext _dbContext;

    public ExampleRequestHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(ExampleRequest request, CancellationToken cancellationToken)
    {
        var users = _dbContext.Users;

        var user = await users.FirstOrDefaultAsync(u => u.Firstname == request.Firstname, cancellationToken);

        user.SomeActionHasBeenPerformed = true;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class UnitTest1
{
    [Theory, AutoData]
    public async Task Try_To_Reproduce(
        string firstname,
        [CollectionSize(1)] List<User> users,
        [Frozen] Mock<ApplicationDbContext> mockDbContext,
        ExampleRequestHandler sut)
    {
        mockDbContext.Setup(x => x.Set<User>())
            .ReturnsDbSet(users);

        mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        var request = new ExampleRequest(firstname);

        await sut.Handle(request, default);
    }
}