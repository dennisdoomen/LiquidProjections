using System.Collections.Generic;
using Nancy.Bootstrapper;

namespace LiquidProjections.Owin.Nancy.Linker.Sources
{
    public class Registration : IRegistrations
  {
    public IEnumerable<TypeRegistration> TypeRegistrations { get; } = new TypeRegistration[] {
      new TypeRegistration(typeof(IResourceLinker), typeof(ResourceLinker)),
      new TypeRegistration(typeof(IUriFilter), typeof(IdentityUriFilter))
    };

    public IEnumerable<CollectionTypeRegistration> CollectionTypeRegistrations { get; } = null;
    public IEnumerable<InstanceRegistration> InstanceRegistrations { get; } = null;
  }
}
