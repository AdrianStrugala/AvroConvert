using System.Text;

namespace SolTechnology.Avro.Features.GenerateModel.NetModel
{
    public interface INetType
    {
        string Name { get; set; }
        void Write(StringBuilder sb);
    }
}