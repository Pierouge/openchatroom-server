using System.Collections.Concurrent;

public interface ILoginStorage
{
  public bool addEntry(string key, Dictionary<string, string> entry);
  public bool removeEntry(string key);
  public Task cleanDictionaryAsync(TimeSpan delay);
  public Dictionary<string, string>? getEntry(string key);
}
public class LoginStorage : ILoginStorage
{
  private readonly ConcurrentDictionary<string, Dictionary<string, string>> loginDict = [];

  public bool addEntry(string key, Dictionary<string, string> entry)
  {
    Dictionary<string, string> dictEntry = entry;

    // Add the time to the entry
    DateTime time = DateTime.UtcNow;
    long ticks = time.Ticks;
    string encoded = Convert.ToBase64String(BitConverter.GetBytes(ticks));

    dictEntry.Add("time", encoded);

    return loginDict.TryAdd(key, dictEntry);
  }

  public bool removeEntry(string key)
  {
    return loginDict.TryRemove(key, out _);
  }

  public async Task cleanDictionaryAsync(TimeSpan delay)
  {
    DateTime now = DateTime.UtcNow;
    foreach (KeyValuePair<string, Dictionary<string, string>> entry in loginDict)
    {
      string entryKey = entry.Key;
      Dictionary<string, string> entryValue = entry.Value;

      string timeString = entryValue["time"];
      byte[] timeBytes = Convert.FromBase64String(timeString);
      long decodedTicks = BitConverter.ToInt64(timeBytes, 0);
      DateTime time = new(decodedTicks, DateTimeKind.Utc);

      if (time > now || (now - time) > delay) loginDict.TryRemove(entryKey, out _);
    }

  }

  public Dictionary<string, string>? getEntry(string key)
  {
    loginDict.TryGetValue(key, out Dictionary<string, string>? value);
    return value;
  }
}
