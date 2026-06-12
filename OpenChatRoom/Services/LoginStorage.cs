using System.Collections.Concurrent;

public interface ILoginStorage
{
  public void addEntry(string key, LoginStorage.Data entry);
  public bool removeEntry(string key);
  public Task cleanDictionaryAsync(TimeSpan delay);
  public LoginStorage.Data? getEntry(string key);
}
public class LoginStorage : ILoginStorage
{
  private readonly ConcurrentDictionary<string, TimedData> loginDict = [];

  public void addEntry(string key, Data entry)
  {
    // Add the time to the entry
    DateTime time = DateTime.UtcNow;
    long ticks = time.Ticks;
    string encoded = Convert.ToBase64String(BitConverter.GetBytes(ticks));

    loginDict[key] = new TimedData(entry, encoded);
  }

  public bool removeEntry(string key)
  {
    return loginDict.TryRemove(key, out _);
  }

  public async Task cleanDictionaryAsync(TimeSpan delay)
  {
    DateTime now = DateTime.UtcNow;
    foreach (KeyValuePair<string, TimedData> entry in loginDict)
    {
      string entryKey = entry.Key;
      TimedData entryValue = entry.Value;

      string timeString = entryValue.Time;
      byte[] timeBytes = Convert.FromBase64String(timeString);
      long decodedTicks = BitConverter.ToInt64(timeBytes, 0);
      DateTime time = new(decodedTicks, DateTimeKind.Utc);

      if (time > now || (now - time) > delay) loginDict.TryRemove(entryKey, out _);
    }

  }

  public Data? getEntry(string key)
  {
    loginDict.TryGetValue(key, out TimedData? value);
    return value?.Data;
  }

  public record Data(
    string ServerSecretEphemeral,
    string ClientPublicEphemeral,
    string Salt,
    string Verifier
  );

  private record TimedData(
    Data Data,
    string Time
  );

}
