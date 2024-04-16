using System.IO;
using System.Text;

namespace AndroidAotLogAnalyzer;

public class LogFileReader
{
    public AotLogDataOverview Process(Stream stream)
    {
        const string notFoundMarker = "Mono    : AOT NOT FOUND: ";
        const string foundMarker = "Mono    : AOT: FOUND method ";

        var foundTracker = new AotLogTracker(true);
        var notFoundTracker = new AotLogTracker(true);
        var total = 0;

        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (reader.ReadLine() is { } line)
        {
            var index1 = line.IndexOf(notFoundMarker, StringComparison.Ordinal);
            if (index1 >= 0) {
                var rest = line.Substring(index1 + notFoundMarker.Length);
                total++;
                notFoundTracker.HandleLine(rest);
                continue;
            }
            
            index1 = line.IndexOf(foundMarker, StringComparison.Ordinal);
            if (index1 >= 0) {
                var rest = line.Substring(index1 + foundMarker.Length);
                total++;
                foundTracker.HandleLine(rest);
                continue;
            }
        }

        return new AotLogDataOverview(foundTracker.GetResult(), notFoundTracker.GetResult(), total);
    }

    private class AotLogTracker(bool aotFound)
    {
        private int _total;
        private int _wrappers;
        private int _count;
        private readonly Dictionary<string, AotLogType> _typesByNames = new();
        
        // ReSharper disable once MemberCanBePrivate.Local
        public bool AotFound { get; } = aotFound;

        public void HandleLine(string rest)
        {
            _total++;
            const string wrapperPrefix = "(wrapper ";
            if (rest.StartsWith(wrapperPrefix)) {
                _wrappers++;
                return;
            }

            _count++;
            var fullMethodName = rest.TrimEnd('.');
            var parts = fullMethodName.Split(':');
            if (parts.Length != 2)
                return;
            var typeName = parts[0];
            var methodName = parts[1];
            if (Remap(typeName, methodName, out var newTypeName, out var newMethodName)) {
                typeName = newTypeName;
                methodName = newMethodName;
            }
            if (!_typesByNames.TryGetValue(typeName, out var aotLogType)) {
                aotLogType = new AotLogType(typeName);
                _typesByNames.Add(typeName, aotLogType);
            }
            aotLogType.AddMethod(methodName);
        }

        public AotLogData GetResult()
        {
            foreach (var types in _typesByNames.Values)
                types.SortMethods();
            var sortedTypes = _typesByNames
                .OrderBy(c => c.Key)
                .Select(c => c.Value)
                .ToArray();
            return new AotLogData(AotFound, sortedTypes, new AotLogStat(_total, _count, _wrappers));
        }
        
        private readonly char[] _typeSeparators = new char[] { '/', '<' };

        private bool Remap(string typeName, string methodName, out string newTypeName, out string newMethodName)
        {
            newTypeName = typeName;
            newMethodName = methodName;
        
            var index = typeName.IndexOfAny(_typeSeparators);
            if (index > 0) {
                var c = typeName[index];
                if (c == '/') {
                    newTypeName = typeName.Substring(0, index);
                    newMethodName = typeName.Substring(index) + " " + methodName;
                    return true;
                }
            }

            if (StartsWith(typeName, "System.Collections.Concurrent.ConcurrentDictionary`2/Tables", out var rest1)) {
                newTypeName = "System.Collections.Concurrent.ConcurrentDictionary`2" + rest1;
                newMethodName = "/Tables " + methodName;
                return true;
            }
        
            if (StartsWith(typeName, "System.Collections.Concurrent.ConcurrentDictionary`2/Node", out var rest2)) {
                newTypeName = "System.Collections.Concurrent.ConcurrentDictionary`2" + rest2;
                newMethodName = "/Node " + methodName;
                return true;
            }
        
            return false;
        }

        private bool StartsWith(string source, string search, out string rest)
        {
            rest = "";
            if (string.IsNullOrEmpty(source))
                return false;

            if (!source.StartsWith(search, StringComparison.Ordinal))
                return false;

            rest = source.Substring(search.Length);
            return true;
        }
    }
}
