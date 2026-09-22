using Helpdesk.Domain.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Helpdesk.Infrastructure.Persistence.Configurations;

internal sealed class ComentarioConfiguration : IEntityTypeConfiguration<Comentario>
{
    public void Configure(EntityTypeBuilder<Comentario> builder)
    {
        builder.ToTable("comentarios");

        builder.HasKey(c => c.Id);
        // Sem isso o EF trata um Comentario novo (com Id já preenchido) como existente e faz UPDATE.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.AutorId).IsRequired();
        builder.Property(c => c.Texto).HasMaxLength(Ticket.ComentarioMaxLength).IsRequired();
        builder.Property(c => c.CriadoEm).IsRequired();
    }
}
