namespace Nafer.WinUI.Infrastructure.Navigation;

/// <summary>
/// Maps a ViewModel type to its corresponding Page type.
/// Registered as IEnumerable&lt;PageTypeMapping&gt; in DI.
/// </summary>
public record PageTypeMapping(Type ViewModelType, Type PageType);
