namespace Azure.Functions.Testing.Cli.Telemetry;

public interface ITelemetry
{
    bool Enabled { get; }

    void TrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements);

    void Flush();
}
