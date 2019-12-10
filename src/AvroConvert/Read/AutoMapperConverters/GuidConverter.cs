using System;
using AutoMapper;
using SolTechnology.Avro.Models;

namespace SolTechnology.Avro.Read.AutoMapperConverters
{
    public class GuidConverter : ITypeConverter<Fixed, Guid>
    {
        public Guid Convert(Fixed source, Guid destination, ResolutionContext context)
        {
            return new Guid(source.Value);
        }
    }
}