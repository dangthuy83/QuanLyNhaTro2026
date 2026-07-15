using Microsoft.AspNetCore.Identity;

var password = Environment.GetEnvironmentVariable("QLNT_ADMIN_PASSWORD_INPUT");
if (password == null)
{
    Console.Write("New admin password: ");
    password = ReadSecret();
    Console.WriteLine();
}
if (password.Length < 12)
{
    Console.Error.WriteLine("Password must contain at least 12 characters.");
    return 1;
}

Console.WriteLine(new PasswordHasher<string>().HashPassword("admin", password));
return 0;

static string ReadSecret()
{
    var characters = new List<char>();
    while (Console.ReadKey(intercept: true) is var key && key.Key != ConsoleKey.Enter)
    {
        if (key.Key == ConsoleKey.Backspace)
        {
            if (characters.Count > 0) characters.RemoveAt(characters.Count - 1);
            continue;
        }
        if (!char.IsControl(key.KeyChar)) characters.Add(key.KeyChar);
    }
    return new string(characters.ToArray());
}
