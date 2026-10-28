namespace Tulpar;

public sealed record RocketConfiguration(
    double DryMassKg,
    double PropellantMassKg,
    double ThrustNewtons,
    double BurnDurationSeconds,
    double DragCoefficient);

public sealed class FlightSimulation
{
    private const double Gravity = 9.80665;
    private const double SeaLevelAirDensity = 1.225;
    private const double AtmosphericScaleHeight = 8500;
    private const double ReferenceAreaSquareMeters = 0.02;

    private readonly RocketConfiguration _configuration;
    private double _propellantRemainingKg;

    public FlightSimulation(RocketConfiguration configuration)
    {
        _configuration = configuration;
        Reset();
    }

    public double TimeSeconds { get; private set; }
    public double AltitudeMeters { get; private set; }
    public double VelocityMetersPerSecond { get; private set; }
    public double AccelerationMetersPerSecondSquared { get; private set; }
    public double PropellantRemainingKg => _propellantRemainingKg;
    public double CurrentMassKg => _configuration.DryMassKg + _propellantRemainingKg;
    public double MaximumAltitudeMeters { get; private set; }
    public bool IsLanded { get; private set; }
    public bool IsEngineRunning => !IsLanded
        && TimeSeconds < _configuration.BurnDurationSeconds
        && _propellantRemainingKg > 0;

    public void Reset()
    {
        TimeSeconds = 0;
        AltitudeMeters = 0;
        VelocityMetersPerSecond = 0;
        AccelerationMetersPerSecondSquared = 0;
        MaximumAltitudeMeters = 0;
        _propellantRemainingKg = _configuration.PropellantMassKg;
        IsLanded = false;
    }

    public void Advance(double deltaTimeSeconds)
    {
        if (IsLanded || deltaTimeSeconds <= 0)
        {
            return;
        }

        var engineRunning = IsEngineRunning;
        var thrust = engineRunning ? _configuration.ThrustNewtons : 0;
        var airDensity = SeaLevelAirDensity
            * Math.Exp(-Math.Max(0, AltitudeMeters) / AtmosphericScaleHeight);
        var drag = 0.5
            * airDensity
            * _configuration.DragCoefficient
            * ReferenceAreaSquareMeters
            * VelocityMetersPerSecond
            * Math.Abs(VelocityMetersPerSecond);

        AccelerationMetersPerSecondSquared = (thrust - drag) / CurrentMassKg - Gravity;
        VelocityMetersPerSecond += AccelerationMetersPerSecondSquared * deltaTimeSeconds;
        AltitudeMeters += VelocityMetersPerSecond * deltaTimeSeconds;
        TimeSeconds += deltaTimeSeconds;

        if (engineRunning)
        {
            var propellantFlowKgPerSecond =
                _configuration.PropellantMassKg / _configuration.BurnDurationSeconds;
            _propellantRemainingKg = Math.Max(
                0,
                _propellantRemainingKg - propellantFlowKgPerSecond * deltaTimeSeconds);
        }

        if (AltitudeMeters <= 0)
        {
            AltitudeMeters = 0;
            VelocityMetersPerSecond = 0;
            IsLanded = true;
        }

        MaximumAltitudeMeters = Math.Max(MaximumAltitudeMeters, AltitudeMeters);
    }
}
