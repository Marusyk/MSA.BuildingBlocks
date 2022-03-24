namespace AutoMapper.Extensions.MappingProfile;

public interface IMapTo<T>
{
    void MapTo(Profile profile) => profile.CreateMap(GetType(), typeof(T));
}