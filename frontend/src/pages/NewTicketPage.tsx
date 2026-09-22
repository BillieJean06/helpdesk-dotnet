import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ticketsApi, type Prioridade } from "../api/tickets";

const PRIORIDADES: Prioridade[] = ["Baixa", "Media", "Alta", "Critica"];

export function NewTicketPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [titulo, setTitulo] = useState("");
  const [descricao, setDescricao] = useState("");
  const [prioridade, setPrioridade] = useState<Prioridade>("Media");

  const abrir = useMutation({
    mutationFn: () => ticketsApi.abrir(titulo, descricao, prioridade),
    onSuccess: async (id) => {
      await queryClient.invalidateQueries({ queryKey: ["tickets"] });
      navigate(`/tickets/${id}`);
    },
  });

  function onSubmit(e: FormEvent) {
    e.preventDefault();
    abrir.mutate();
  }

  return (
    <div className="card" style={{ maxWidth: 480 }}>
      <h2>Abrir ticket</h2>
      <form onSubmit={onSubmit}>
        <div className="campo">
          <label htmlFor="titulo">Título</label>
          <input id="titulo" value={titulo} onChange={(e) => setTitulo(e.target.value)} required />
        </div>
        <div className="campo">
          <label htmlFor="descricao">Descrição</label>
          <textarea
            id="descricao"
            rows={4}
            value={descricao}
            onChange={(e) => setDescricao(e.target.value)}
            required
          />
        </div>
        <div className="campo">
          <label htmlFor="prioridade">Prioridade</label>
          <select
            id="prioridade"
            value={prioridade}
            onChange={(e) => setPrioridade(e.target.value as Prioridade)}
          >
            {PRIORIDADES.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </div>
        {abrir.isError && <p className="erro">{abrir.error.message}</p>}
        <button className="botao" type="submit" disabled={abrir.isPending}>
          {abrir.isPending ? "Abrindo..." : "Abrir ticket"}
        </button>
      </form>
    </div>
  );
}
