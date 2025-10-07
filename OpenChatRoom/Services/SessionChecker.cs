using Microsoft.EntityFrameworkCore;
public static class SessionChecker
{  
    public static bool checkSession(ISession session)
    {
        if (session == null) return false;
        _ = bool.TryParse(session.GetString("logged_in"), out bool isLoggedIn);
        return isLoggedIn;
    }
}