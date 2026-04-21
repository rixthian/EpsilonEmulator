namespace Epsilon.Auth;

public interface ITicketGenerator
{
    string Generate(int length);
}

