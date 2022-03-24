using AutoMapper;
using AutoMapper.Extensions.MappingProfile;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1;

[ApiController]
[Route("[controller]")]
public class AddressController : ControllerBase
{
    private readonly IMapper _mapper;

    public AddressController(IMapper mapper)
    {
        _mapper = mapper;
    }

    [HttpGet]
    public AddressDto Get()
    {
        AddressModel model = new("Ukraine", "Lviv");
        var dto = _mapper.Map<AddressModel, AddressDto>(model);
        return dto;
    }
    
    [HttpPost]
    public AddressModel Post(AddressDto dto)
    {
        var model = _mapper.Map<AddressModel>(dto);
        return model;
    }
}

public record AddressModel(string Country, string City);

public class AddressDto : IMapFrom<AddressModel>, IMapTo<AddressModel>
{
    public string Country { get; set; }
    public string City { get; set; }
    public string Address { get; set; }

    public void MapFrom(Profile profile) => profile
        .CreateMap<AddressModel, AddressDto>()
        .ForMember(dest => dest.Address, opt => opt.MapFrom(src => $"{src.Country}, {src.City} city"));
}