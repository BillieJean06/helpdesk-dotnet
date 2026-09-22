import { api } from "./client";
import type { components } from "./schema";

export type TicketResumo = components["schemas"]["TicketResumoDto"];
export type TicketDetalhe = components["schemas"]["TicketDetalheDto"];
export type Prioridade = components["schemas"]["Prioridade"];

async function comFalha<T>(promessa: Promise<{ data?: T; error?: unknown }>, mensagem: string): Promise<T> {
  const { data, error } = await promessa;
  if (error || data === undefined) {
    const detalhe = (error as { detail?: string } | undefined)?.detail;
    throw new Error(detalhe ?? mensagem);
  }
  return data;
}

export const ticketsApi = {
  listar: () => comFalha(api.GET("/api/tickets"), "Não foi possível carregar os tickets."),

  obter: (id: string) =>
    comFalha(api.GET("/api/tickets/{id}", { params: { path: { id } } }), "Ticket não encontrado."),

  abrir: (titulo: string, descricao: string, prioridade: Prioridade) =>
    comFalha(
      api.POST("/api/tickets", { body: { titulo, descricao, prioridade } }),
      "Não foi possível abrir o ticket.",
    ),

  assumir: async (id: string) => {
    const { error } = await api.POST("/api/tickets/{id}/assumir", { params: { path: { id } } });
    if (error) throw new Error((error as { detail?: string }).detail ?? "Não foi possível assumir o ticket.");
  },

  comentar: async (id: string, texto: string) => {
    const { error } = await api.POST("/api/tickets/{id}/comentarios", {
      params: { path: { id } },
      body: { texto },
    });
    if (error) throw new Error((error as { detail?: string }).detail ?? "Não foi possível comentar.");
  },
};
