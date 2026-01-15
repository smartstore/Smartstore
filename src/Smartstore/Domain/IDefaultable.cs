namespace Smartstore.Domain;

/// <summary>
/// Defines a contract for objects that support resetting their state to a predefined default configuration.
/// </summary>
/// <remarks>Implementations of this interface provide a way to determine whether the object is currently in its
/// default state and to reset the object to that state. This can be useful for reusable components or settings objects
/// that need to be restored to a known baseline.</remarks>
public interface IDefaultable
{
    /// <summary>
    /// Gets a value indicating whether the current state is the initial/default state.
    /// </summary>
    bool IsDefaultState { get; }

    /// <summary>
    /// Resets all settings or values to their default state.
    /// </summary>
    /// <exception cref="NotImplementedException">The method is not implemented.</exception>
    void ResetToDefault() 
        => throw new NotImplementedException();
}
