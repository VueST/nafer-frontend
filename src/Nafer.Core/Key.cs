namespace Nafer.Core;

public class Key<T>
{
    public string Name { get; }
    private readonly Func<T> _defaultFactory;

    public Key(string name, T defaultValue)
    {
        Name = name;
        _defaultFactory = () => defaultValue;
    }

    public T Default => _defaultFactory();
}
