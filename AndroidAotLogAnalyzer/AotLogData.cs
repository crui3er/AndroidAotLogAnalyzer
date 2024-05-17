namespace AndroidAotLogAnalyzer;

public record AotLogDataOverview(AotLogData Found, AotLogData NotFound, int Total);

public record AotLogData(bool AotFound, AotLogType[] Types, AotLogStat Stat)
{
    public static AotLogData Empty(bool aotFound) => new AotLogData(aotFound, [], new AotLogStat(0, 0, 0));
    
    public bool AotNotFound => !AotFound;
    public string Title => (AotFound ? "AOT FOUND:" : "AOT NOT FOUND:")
                           + " " + Types.Length + " types,"
                           + $" methods {Stat.Count} (+ {Stat.Wrappers} wrappers)";
}

public record AotLogStat(int Total, int Count, int Wrappers);

public class AotLogType(string name)
{
    private readonly List<string> _methods = new();
    
    // ReSharper disable once MemberCanBePrivate.Global
    public string Name { get; } = name;

    public string DisplayName => $"{Name} ({Methods.Count})";

    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public IReadOnlyCollection<string> Methods => _methods;

    public void AddMethod(string methodName)
        => _methods.Add(methodName);

    public void SortMethods()
    {
        if (_methods.Any(c => c.StartsWith("/"))) {
            var copy = _methods.OrderBy(c => c.StartsWith("/")).ThenBy(c => c).ToArray();
            _methods.Clear();
            _methods.AddRange(copy);
        }
        else {
            _methods.Sort();
        }
    }
}