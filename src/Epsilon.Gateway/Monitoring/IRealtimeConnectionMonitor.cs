namespace Epsilon.Gateway;

public interface IRealtimeConnectionMonitor
{
    void RecordAcceptedConnection();
    void RecordClosedConnection();
    RealtimeConnectionSnapshot GetSnapshot();
}
