namespace uchat_server.Services;

public interface IHashService
{
    string Hash(string value);
    bool Verify(string value, string hash);
}
