using System.Runtime.CompilerServices;

// Tests reach rule internals directly instead of by reflection strings, so renames fail at compile time.
[assembly: InternalsVisibleTo("Fortress.Game.EditModeTests")]
[assembly: InternalsVisibleTo("Fortress.Game.PlayModeTests")]
