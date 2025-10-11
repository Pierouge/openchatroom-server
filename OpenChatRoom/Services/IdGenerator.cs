public static class IdGenerator
{
    public static string generateId()
    {
        return Guid.NewGuid().ToString("N");
    }
}