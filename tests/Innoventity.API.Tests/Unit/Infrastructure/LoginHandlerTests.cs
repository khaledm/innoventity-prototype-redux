using Innoventity.API.Domain.Entities;

namespace Innoventity.API.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for login lockout domain behavior (R8.4, T081).
/// Validates Actor domain API: increment, lockout, reset — no direct field writes.
/// </summary>
public class LoginHandlerTests
{
    private static Actor MakeActiveActor() => new Actor(Guid.NewGuid())
    {
        Email = "lockout.test@example.com",
        FirstName = "Test",
        LastName = "User",
        ActorType = ActorType.IdeaGenerator,
        AccountStatus = AccountStatus.Active,
        PasswordHash = "$2a$12$somehash",
        PasswordSalt = "somesalt"
    };

    [Fact]
    public void Actor_Default_HasZeroFailedAttemptsAndNoLockout_R8_4()
    {
        var actor = MakeActiveActor();
        var now = DateTimeOffset.UtcNow;

        Assert.Equal(0, actor.GetFailedLoginAttempts());
        Assert.False(actor.IsAccountLocked(now));
    }

    [Fact]
    public void Actor_GetMaxAllowedFailedCount_Returns5_R8_4()
    {
        var actor = MakeActiveActor();

        Assert.Equal(5, actor.GetMaxAllowedFailedCount());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Actor_WrongPassword_IncrementsFailedLoginAttempts_BelowLockThreshold_R8_4(int attemptNumber)
    {
        var actor = MakeActiveActor();
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < attemptNumber; i++)
            actor.IncrementFailedLoginAttemptCount(now);

        Assert.Equal(attemptNumber, actor.GetFailedLoginAttempts());
        Assert.False(actor.IsAccountLocked(now)); // not locked until 5th failure
    }

    [Fact]
    public void Actor_FifthWrongPassword_SetsLockoutUntil15MinutesInFuture_R8_4()
    {
        var actor = MakeActiveActor();
        var now = DateTimeOffset.UtcNow;

        // 4 prior failures
        for (int i = 0; i < 4; i++)
            actor.IncrementFailedLoginAttemptCount(now);

        var before = DateTimeOffset.UtcNow;
        actor.IncrementFailedLoginAttemptCount(before); // 5th failure triggers lockout
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(5, actor.GetFailedLoginAttempts());
        Assert.True(actor.IsAccountLocked(before));
        Assert.NotNull(actor.LockoutUntil);
        Assert.InRange(
            actor.LockoutUntil!.Value,
            before.AddMinutes(15).AddSeconds(-1),
            after.AddMinutes(15).AddSeconds(1));
    }

    [Fact]
    public void Actor_SuccessfulLogin_ResetsFailedAttemptsAndLockout_R8_4()
    {
        var actor = MakeActiveActor();
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < 3; i++)
            actor.IncrementFailedLoginAttemptCount(now);

        actor.RegisterSuccessfulLogin(now);

        Assert.Equal(0, actor.GetFailedLoginAttempts());
        Assert.False(actor.IsAccountLocked(now));
    }

    [Fact]
    public void Actor_IsAccountLocked_ReturnsTrue_WhenLockoutIsActive_R8_4()
    {
        var actor = MakeActiveActor();
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < 5; i++)
            actor.IncrementFailedLoginAttemptCount(now);

        Assert.True(actor.IsAccountLocked(now));
    }

    [Fact]
    public void Actor_IsAccountLocked_ReturnsFalse_WhenLockoutHasExpired_R8_4()
    {
        var actor = MakeActiveActor();
        // Simulate lockout being set 20 minutes in the past
        var pastNow = DateTimeOffset.UtcNow.AddMinutes(-20);

        for (int i = 0; i < 5; i++)
            actor.IncrementFailedLoginAttemptCount(pastNow);

        // LockoutUntil = pastNow + 15 min = 5 minutes ago → expired
        var currentNow = DateTimeOffset.UtcNow;
        Assert.False(actor.IsAccountLocked(currentNow));
    }

    [Fact]
    public void Actor_IsAccountLocked_ReturnsFalse_AtExactLockoutExpiry_R8_4()
    {
        // Verifies the boundary condition: lockout is strictly >, not >=
        // i.e. at the exact moment of expiry the account is unlocked
        var actor = MakeActiveActor();
        var lockoutSetAt = DateTimeOffset.UtcNow;

        for (int i = 0; i < 5; i++)
            actor.IncrementFailedLoginAttemptCount(lockoutSetAt);

        // LockoutUntil = lockoutSetAt + 15 min (exact expiry)
        var lockoutExpiry = lockoutSetAt.AddMinutes(15);

        // At expiry the account must not be considered locked (boundary is exclusive)
        Assert.False(actor.IsAccountLocked(lockoutExpiry));
    }

    [Fact]
    public void Actor_SortableName_FormatsAsLastNameCommaFirstName()
    {
        var actor = MakeActiveActor(); // FirstName = "Test", LastName = "User"

        Assert.Equal("User, Test", actor.SortableName);
    }
}
