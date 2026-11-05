// Unity's destroyed or missing objects compare equal to null only through UnityEngine.Object's == operator.
// The C# ?. and ?? operators skip that check, so they must only see a value passed through OrNull().
internal static class CoreGuardianUnityObjectExtensions
{
    /// <summary>Returns the object, or a real null when Unity considers it destroyed or missing.</summary>
    public static T OrNull<T>(this T value)
        where T : UnityEngine.Object => value != null ? value : null;
}
