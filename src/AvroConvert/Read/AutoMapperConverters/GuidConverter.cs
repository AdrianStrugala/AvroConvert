namespace AvroConvert.Read.AutoMapperConverters
{
    using System;
    using AutoMapper;
    using Models;

    public class GuidConverter : ITypeConverter<Fixed, Guid>
    {
        public Guid Convert(Fixed source, Guid destination, ResolutionContext context)
        {
            return new Guid(source.Value);
        }
    }
}