using FastMember;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SolTechnology.Avro.Infrastructure.Extensions
{
    internal static class MemberExtensions
    {
        public static IEnumerable<T> GetMemberAttributes<T>(this Member member) where T : Attribute
        {
            return GetPrivateField<MemberInfo>(member, "member").GetCustomAttributes<T>();
        }
    }
}
