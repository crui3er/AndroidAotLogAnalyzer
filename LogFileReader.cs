using System.IO;
using System.Text;

namespace AndroidAotLogAnalyzer;

public class LogFileReader
{
    public AotLogData Process(Stream stream, bool aotFound)
    {
        const string notFoundMarker = "Mono    : AOT NOT FOUND: ";
        const string foundMarker = "Mono    : AOT: FOUND method ";
        var searchMarker = aotFound ? foundMarker : notFoundMarker;

        var total = 0;
        var wrappers = 0;
        var count = 0;
        var typesByNames = new Dictionary<string, AotLogType>();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (reader.ReadLine() is { } line)
        {
            var index1 = line.IndexOf(searchMarker, StringComparison.Ordinal);
            if (index1 <= 0)
                continue;
            total++;
            var rest = line.Substring(index1 + searchMarker.Length);
            const string wrapperPrefix = "(wrapper ";
            if (rest.StartsWith(wrapperPrefix)) {
                wrappers++;
                continue;
            }

            count++;
            var fullMethodName = rest.TrimEnd('.');
            var parts = fullMethodName.Split(':');
            if (parts.Length != 2)
                continue;
            var typeName = parts[0];
            var methodName = parts[1];
            if (Remap(typeName, methodName, out var newTypeName, out var newMethodName)) {
                typeName = newTypeName;
                methodName = newMethodName;
            }
            if (!typesByNames.TryGetValue(typeName, out var aotLogType)) {
                aotLogType = new AotLogType(typeName);
                typesByNames.Add(typeName, aotLogType);
            }
            aotLogType.AddMethod(methodName);
        }

        foreach (var types in typesByNames.Values)
            types.SortMethods();

        var sortedTypes = typesByNames
            .OrderBy(c => c.Key)
            .Select(c => c.Value)
            .ToArray();
        return new AotLogData(aotFound, sortedTypes, new AotLogStat(total, count, wrappers));
    }
    
    private readonly char[] _typeSeparators = new char[] { '/', '<' };

    private bool Remap(string typeName, string methodName, out string newTypeName, out string newMethodName)
    {
        newTypeName = typeName;
        newMethodName = methodName;
        
        var index = typeName.IndexOfAny(_typeSeparators);
        if (index > 0)
        {
            var c = typeName[index];
            if (c == '/')
            {
                newTypeName = typeName.Substring(0, index);
                newMethodName = typeName.Substring(index) + " " + methodName;
                return true;
            }
        }
        

        if (StartsWith(typeName, "System.Collections.Concurrent.ConcurrentDictionary`2/Tables", out var rest1))
        {
            newTypeName = "System.Collections.Concurrent.ConcurrentDictionary`2" + rest1;
            newMethodName = "/Tables " + methodName;
            return true;
        }
        
        if (StartsWith(typeName, "System.Collections.Concurrent.ConcurrentDictionary`2/Node", out var rest2))
        {
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
