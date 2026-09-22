using Helpdesk.Domain.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Helpdesk.Infrastructure.Persistence.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(t => t.Id);
        // O Id nasce no domínio (Guid.NewGuid); o banco nunca o gera.
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.Titulo).HasMaxLength(Ticket.TituloMaxLength).IsRequired();
        builder.Property(t => t.Descricao).HasMaxLength(Ticket.DescricaoMaxLength).IsRequired();

        // Enums como texto: legível no banco e imune a reordenação do enum.
        builder.Property(t => t.Prioridade).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(t => t.SolicitanteId).IsRequired();

        // SLA contratado na abertura: colunas sla_primeira_resposta e sla_resolucao na própria tabela.
        // PrazosSla é imutável (só get, construtor com validação): o EF precisa ser avisado de que são colunas.
        builder.OwnsOne(t => t.Sla, sla =>
        {
            sla.Property(s => s.PrimeiraResposta).IsRequired();
            sla.Property(s => s.Resolucao).IsRequired();
        });
        builder.Navigation(t => t.Sla).IsRequired();

        // Comentários pertencem ao agregado: só entram por Ticket.Comentar (campo _comentarios).
        builder.HasMany(t => t.Comentarios)
            .WithOne()
            .HasForeignKey("TicketId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Comentarios).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Concorrência otimista via xmin do Postgres: dois atendentes não assumem o mesmo ticket.
        builder.Property<uint>("xmin").IsRowVersion();

        // Fila de atendimento: tickets de um tenant por status.
        builder.HasIndex(t => new { t.TenantId, t.Status });
    }
}
