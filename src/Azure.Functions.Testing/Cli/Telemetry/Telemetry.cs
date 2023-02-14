// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Functions.Testing.Cli.Common;
using Colors.Net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.PlatformAbstractions;

namespace Azure.Functions.Testing.Cli.Telemetry;

// Most of the Telemetry implementation is inspired / acquired from https://github.com/dotnet/cli/tree/master/src/dotnet/Telemetry
public class Telemetry : ITelemetry
{
    internal static string? CurrentSessionId;
    private readonly int _senderCount;
    private TelemetryClient? _client;
    private Dictionary<string, string> _commonProperties = new();
    private Dictionary<string, double> _commonMeasurements = new();
    private Task? _trackEventTask;

    public bool Enabled { get; }

    public Telemetry(
        string? sessionId,
        bool blockThreadInitialization = false,
        int senderCount = 3)
    {
        if (!StaticSettings.IsTelemetryEnabled)
        {
            Enabled = false;
            return;
        }

        Enabled = true;

        // Store the session ID in a static field so that it can be reused
        CurrentSessionId = sessionId ?? Guid.NewGuid().ToString();
        _senderCount = senderCount;
        if (blockThreadInitialization)
        {
            InitializeTelemetry();
        }
        else
        {
            // initialize in task to offload to parallel thread
            _trackEventTask = Task.Run(InitializeTelemetry);
        }
    }

    public void TrackEvent(string eventName, IDictionary<string, string> properties,
        IDictionary<string, double> measurements)
    {
        if (!Enabled || _trackEventTask == null)
        {
            return;
        }

        //continue the task in different threads
        _trackEventTask = _trackEventTask.ContinueWith(
            _ => TrackEventTask(eventName, properties, measurements)
        );
    }

    public void Flush()
    {
        if (!Enabled || _trackEventTask == null)
        {
            return;
        }

        _trackEventTask.Wait();
    }

    public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
    {
        if (!Enabled)
        {
            return;
        }
        TrackEventTask(eventName, properties, measurements);
    }

    private void InitializeTelemetry()
    {
        try
        {
            var persistenceChannel = new PersistenceChannel.PersistenceChannel(string.Empty, sendersCount: _senderCount);
            persistenceChannel.SendingInterval = TimeSpan.FromMilliseconds(1);
                
#pragma warning disable CS0618
            TelemetryConfiguration.Active.TelemetryChannel = persistenceChannel;
            _client = new TelemetryClient();
#pragma warning restore CS0618

            _client.InstrumentationKey = Constants.TelemetryInstrumentationKey;
            _client.Context.Session.Id = CurrentSessionId;
            _client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;

            // We don't want to log this.
            // Setting it to null doesn't work. So might as well log the session id.
            _client.Context.Cloud.RoleInstance = $"private-{CurrentSessionId}";

            _commonProperties = new TelemetryCommonProperties().GetTelemetryCommonProperties();
            _commonMeasurements = new Dictionary<string, double>();
        }
        catch (Exception e)
        {
            _client = null;
            // we don't want to fail the tool if telemetry fails.
            if (StaticSettings.IsDebug)
            {
                ColoredConsole.Error.WriteLine(e.ToString());
            }
        }
    }

    private void TrackEventTask(
        string eventName,
        IDictionary<string, string> properties,
        IDictionary<string, double> measurements)
    {
        if (_client == null)
        {
            return;
        }

        try
        {
            Dictionary<string, string> eventProperties = GetEventProperties(properties);
            Dictionary<string, double> eventMeasurements = GetEventMeasures(measurements);

            eventProperties.Add("event id", Guid.NewGuid().ToString());

            _client.TrackEvent(PrependProducerNamespace(eventName), eventProperties, eventMeasurements);
        }
        catch (Exception e)
        {
            if (StaticSettings.IsDebug)
            {
                ColoredConsole.Error.WriteLine(e.ToString());
            }
        }
    }

    private static string PrependProducerNamespace(string eventName)
    {
        return "core-tools: func " + eventName;
    }

    private Dictionary<string, double> GetEventMeasures(IDictionary<string, double>? measurements)
    {
        Dictionary<string, double> eventMeasurements = new Dictionary<string, double>(_commonMeasurements);
        if (measurements != null)
        {
            foreach (KeyValuePair<string, double> measurement in measurements)
            {
                eventMeasurements[measurement.Key] = measurement.Value;
            }
        }
        return eventMeasurements;
    }

    private Dictionary<string, string> GetEventProperties(IDictionary<string, string>? properties)
    {
        if (properties != null)
        {
            var eventProperties = new Dictionary<string, string>(_commonProperties);
            foreach (KeyValuePair<string, string> property in properties)
            {
                eventProperties[property.Key] = property.Value;
            }
            return eventProperties;
        }
        else
        {
            return _commonProperties;
        }
    }
}
