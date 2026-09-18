using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Prezentownik.WebApi.Data;

public class UuidV7ValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
}

public class UuidV7ValueGeneratorFactory : ValueGeneratorFactory
{
    public override ValueGenerator Create(IProperty property, ITypeBase typeBase) => new UuidV7ValueGenerator();
}
