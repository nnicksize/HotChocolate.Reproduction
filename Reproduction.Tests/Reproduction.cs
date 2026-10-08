using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace Reproduction.Tests;

public class AuthorizedResolverTests
{
    [Fact]
    public async Task Reproduce()
    {
        var result = await Execute(
            """
            mutation {
              mutation(
                input: { name: "test" }
              ) {
                id
              }
            }
            """
        );

        Assert.Empty(result.Errors);
    }

    private static async Task<OperationResult> Execute(string operation)
    {
        var requestBuilder = new ServiceCollection()
            .AddGraphQL()
            // The next line breaks the entire setup for a mutation method that's:
            // on a non-static partial class, is a non-static method, and has an annotation.
            // Remove to make the test work.
            .AddMutationConventions(applyToAllMutations: true)
            .AddTestsTypes()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddAuthorizationHandler<AllowAllAuthorizationHandler>();

        var executor = await requestBuilder.BuildRequestExecutorAsync();
        var result = await executor.ExecuteAsync(operation);

        return result.ExpectOperationResult();
    }
}

[MutationType]
public partial class MutationType
{
    [Authorize]
    public MutationPayload Mutation(MutationInput input, CancellationToken cancellationToken)
    {
        return new MutationPayload(-1);
    }
}

public record MutationInput(string Name);

[Node]
public record MutationPayload([property: ID] int Id)
{
    [NodeResolver]
    public static MutationPayload Get(int id) => new MutationPayload(id);
}

public class AllowAllAuthorizationHandler : IAuthorizationHandler
{
    public ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        CancellationToken cancellationToken = default
    ) => new(AuthorizeResult.Allowed);

    public ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken cancellationToken = default
    ) => new(AuthorizeResult.Allowed);
}
