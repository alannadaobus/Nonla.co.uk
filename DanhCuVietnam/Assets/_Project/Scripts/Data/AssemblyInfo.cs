using System.Runtime.CompilerServices;

// The editor asset factory and tests configure definitions through internal setters;
// runtime code only ever reads them.
[assembly: InternalsVisibleTo("DanhCu.Editor")]
[assembly: InternalsVisibleTo("DanhCu.Tests.EditMode")]
