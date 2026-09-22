import type { Prioridade } from "../api/tickets";
import type { components } from "../api/schema";

type StatusTicket = components["schemas"]["StatusTicket"];

const CORES_STATUS: Record<StatusTicket, string> = {
  Aberto: "#8a8a92",
  EmAtendimento: "#2f5fd6",
  AguardandoCliente: "#c68a1c",
  Resolvido: "#1f9d55",
  Fechado: "#4a4a52",
  Reaberto: "#c02626",
};

const CORES_PRIORIDADE: Record<Prioridade, string> = {
  Baixa: "#4a4a52",
  Media: "#2f5fd6",
  Alta: "#c68a1c",
  Critica: "#c02626",
};

export function StatusBadge({ status }: { status: StatusTicket }) {
  return (
    <span className="badge" style={{ background: CORES_STATUS[status] + "22", color: CORES_STATUS[status] }}>
      {status}
    </span>
  );
}

export function PrioridadeBadge({ prioridade }: { prioridade: Prioridade }) {
  return (
    <span
      className="badge"
      style={{ background: CORES_PRIORIDADE[prioridade] + "22", color: CORES_PRIORIDADE[prioridade] }}
    >
      {prioridade}
    </span>
  );
}
