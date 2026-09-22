import { useState, type FormEvent } from "react";
import { useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ticketsApi } from "../api/tickets";
import { useAuth } from "../auth/AuthContext";
import { PrioridadeBadge, StatusBadge } from "../components/Badges";

export function TicketDetailPage() {
  const { id } = useParams<{ id: string }>();
  if (!id) throw new Error("Rota sem id de ticket.");

  const { sessao } = useAuth();
  const queryClient = useQueryClient();
  const [comentario, setComentario] = useState("");

  const {
    data: ticket,
    isLoading,
    error,
  } = useQuery({ queryKey: ["tickets", id], queryFn: () => ticketsApi.obter(id) });

  const invalidar = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["tickets", id] }),
      queryClient.invalidateQueries({ queryKey: ["tickets"] }),
    ]);

  const assumir = useMutation({
    mutationFn: () => ticketsApi.assumir(id),
    onSuccess: invalidar,
  });

  const comentar = useMutation({
    mutationFn: (texto: string) => ticketsApi.comentar(id, texto),
    onSuccess: async () => {
      setComentario("");
      await invalidar();
    },
  });

  function onSubmitComentario(e: FormEvent) {
    e.preventDefault();
    if (comentario.trim()) comentar.mutate(comentario);
  }

  if (isLoading) return <p>Carregando...</p>;
  if (error) return <p className="erro">{error.message}</p>;
  if (!ticket) return null;

  const podeAssumir = sessao?.papeis.includes("Atendente")
    && (ticket.status === "Aberto" || ticket.status === "Reaberto");

  return (
    <div>
      <div className="card">
        <div className="topbar" style={{ marginBottom: "0.5rem" }}>
          <h2 style={{ margin: 0 }}>{ticket.titulo}</h2>
          <div style={{ display: "flex", gap: "0.5rem" }}>
            <PrioridadeBadge prioridade={ticket.prioridade} />
            <StatusBadge status={ticket.status} />
          </div>
        </div>
        <p>{ticket.descricao}</p>
        <p className="meta">
          Prazo de primeira resposta: {new Date(ticket.prazoPrimeiraResposta).toLocaleString()} · Prazo de
          resolução: {new Date(ticket.prazoResolucao).toLocaleString()}
        </p>

        {podeAssumir && (
          <button className="botao" onClick={() => assumir.mutate()} disabled={assumir.isPending}>
            {assumir.isPending ? "Assumindo..." : "Assumir ticket"}
          </button>
        )}
        {assumir.isError && <p className="erro">{assumir.error.message}</p>}
      </div>

      <h3>Comentários</h3>
      <div className="card">
        {ticket.comentarios.length === 0 && <p className="meta">Nenhum comentário ainda.</p>}
        {ticket.comentarios.map((c, i) => (
          <div key={i} className="comentario">
            <div className="meta">{new Date(c.criadoEm).toLocaleString()}</div>
            <div>{c.texto}</div>
          </div>
        ))}
      </div>

      {ticket.status !== "Fechado" && (
        <form onSubmit={onSubmitComentario} style={{ marginTop: "1rem" }}>
          <div className="campo">
            <label htmlFor="comentario">Novo comentário</label>
            <textarea
              id="comentario"
              rows={3}
              value={comentario}
              onChange={(e) => setComentario(e.target.value)}
              required
            />
          </div>
          {comentar.isError && <p className="erro">{comentar.error.message}</p>}
          <button className="botao" type="submit" disabled={comentar.isPending}>
            {comentar.isPending ? "Enviando..." : "Comentar"}
          </button>
        </form>
      )}
    </div>
  );
}
