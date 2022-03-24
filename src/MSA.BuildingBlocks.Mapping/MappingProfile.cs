using System.Reflection;

namespace AutoMapper.Extensions.MappingProfile;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        ApplyMappingsFromForAssembly(Assembly.GetEntryAssembly()!);
        ApplyMappingsToForAssembly(Assembly.GetEntryAssembly()!);
    }

    private void ApplyMappingsFromForAssembly(Assembly assembly)
    {
        IEnumerable<Type> ownTypes = assembly.DefinedTypes
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)));

        IEnumerable<Type> referencedTypes = assembly.GetReferencedAssemblies()
            .Select(Assembly.Load)
            .SelectMany(x => x.DefinedTypes)
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)));

        foreach (var type in ownTypes.Concat(referencedTypes))
        {
            var instance = Activator.CreateInstance(type);

            var methodInfo = type.GetMethod("MapFrom")
                ?? type.GetInterface("IMapFrom`1")?.GetMethod("MapFrom");

            methodInfo?.Invoke(instance, new object[] { this });
        }
    }

    private void ApplyMappingsToForAssembly(Assembly assembly)
    {
        IEnumerable<Type> ownTypes = assembly.DefinedTypes
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapTo<>)));

        IEnumerable<Type> referencedTypes = assembly.GetReferencedAssemblies()
            .Select(Assembly.Load)
            .SelectMany(x => x.DefinedTypes)
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapTo<>)));

        foreach (Type type in ownTypes.Concat(referencedTypes))
        {
            var instance = Activator.CreateInstance(type);

            var methodInfo = type.GetMethod("MapTo")
                ?? type.GetInterface("IMapTo`1")?.GetMethod("MapTo");

            methodInfo?.Invoke(instance, new object[] { this });
        }
    }
}