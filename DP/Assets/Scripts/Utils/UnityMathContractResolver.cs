using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ConvoyManager.Utils
{
    public class UnityMathContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (member.DeclaringType != null && member.DeclaringType.Namespace != null &&
                member.DeclaringType.Namespace.StartsWith("Unity.Mathematics"))
            {
                if (member.MemberType == MemberTypes.Property)
                    property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }
}
