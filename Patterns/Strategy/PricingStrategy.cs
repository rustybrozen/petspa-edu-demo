namespace PetSpaApi.Services.PricingStrategy;

public interface IPricingStrategy
{
    string PolicyCode { get; }
    string PolicyName { get; }
    double Calculate(double basePrice);
}

public class WeekdayPricingStrategy : IPricingStrategy
{
    public string PolicyCode
    {
        get { return "WEEKDAY"; }
    }

    public string PolicyName
    {
        get { return "Ngày thường"; }
    }

    public double Calculate(double basePrice)
    {
        return basePrice;
    }
}

public class WeekendPricingStrategy : IPricingStrategy
{
    private readonly double _rate;

    public WeekendPricingStrategy(double surchargeRate = 10)
    {
        _rate = surchargeRate;
    }

    public string PolicyCode
    {
        get { return "WEEKEND"; }
    }

    public string PolicyName
    {
        get { return "Cuối tuần"; }
    }

    public double Calculate(double basePrice)
    {
        return Math.Round(basePrice * (1 + _rate / 100.0));
    }
}

public class PricingContext
{
    private IPricingStrategy _strategy;

    public PricingContext(IPricingStrategy strategy)
    {
        _strategy = strategy;
    }

    public void SetStrategy(IPricingStrategy strategy)
    {
        _strategy = strategy;
    }

    public double ExecuteStrategy(double basePrice)
    {
        return _strategy.Calculate(basePrice);
    }

    public string GetPolicyName()
    {
        return _strategy.PolicyName;
    }

    public string GetPolicyCode()
    {
        return _strategy.PolicyCode;
    }
}
