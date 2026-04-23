namespace Epsilon.Gateway;

public interface IProtocolHealthMonitor
{
    ProtocolHealthSnapshot GetSnapshot();
    void Refresh();
}
