import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { ticketsApi } from "../api/tickets";
import { useAuth } from "../auth/AuthContext";
import { PrioridadeBadge, StatusBadge } from "../components/Badges";

export function TicketsPage() {
  const { sessao } = useAuth();
  const { data: tickets, isLoading, error } = useQuery({
    queryKey: ["tickets"],
    queryFn: ticketsApi.listar,
  });

  return (
    <div>
      <div className="topbar">
        <h2 style={{ margin: 0 }}>{sessao?.ehAtendenteOuSupervisor ? "Fila de tickets" : "Meus tickets"}</h2>
        {!sessao?.ehAtendenteOuSupervisor && (
          <Link className="botao" to="/tickets/novo">
            Abrir ticket
          </Link>
        )}
      </div>

      {isLoading && <p>Carregando...</p>}
      {error && <p className="erro">{error.message}</p>}

      {tickets && tickets.length === 0 && <p>Nenhum ticket por aqui ainda.</p>}

      <div className="lista-tickets">
        {tickets?.map((t) => (
          <Link key={t.id} to={`/tickets/${t.id}`} className="card ticket-item">
            <div>
              <div className="titulo">{t.titulo}</div>
              <div className="meta">Aberto em {new Date(t.criadoEm).toLocaleString()}</div>
            </div>
            <div style={{ display: "flex", gap: "0.5rem" }}>
              <PrioridadeBadge prioridade={t.prioridade} />
              <StatusBadge status={t.status} />
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
