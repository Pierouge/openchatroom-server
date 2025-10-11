public static class SessionChecker
{  
    // Serves both as a Session checker, and as a user fetching function
    public static User? fetchUserBySession(ISession session, AppDbContext _context)
    {
        if (session == null) return null;
        _ = bool.TryParse(session.GetString("logged_in"), out bool isLoggedIn);
        string? sessionUsername = session.GetString("username");

        if (sessionUsername == null || !isLoggedIn) return null;

        User? user = _context.Users.Where(u => u.Username == sessionUsername).FirstOrDefault();

        return user;
    }
}