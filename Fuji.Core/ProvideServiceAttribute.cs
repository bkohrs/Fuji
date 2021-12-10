namespace Fuji;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProvideServiceAttribute : Attribute
{
    public ProvideServiceAttribute(Type serviceProviderType)
    {
        ServiceProviderType = serviceProviderType;
    }

    public Type ServiceProviderType { get; }
}