namespace AutoMapper.Extensions.MappingProfile
{
    public interface IMapFrom<T>
    {
        void MapFrom(Profile profile) => profile.CreateMap(typeof(T), GetType());
    }
}