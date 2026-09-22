import { createContext, use, useMemo, useState, type ReactNode } from "react";
import { api } from "../api/client";
import { decodeJwt, papeisDoToken, type JwtClaims } from "./jwt";
import { getToken, setToken, logout as limparToken } from "./token";

export interface Sessao {
  claims: JwtClaims;
  papeis: string[];
  ehAtendenteOuSupervisor: boolean;
}

interface AuthContextValue {
  sessao: Sessao | null;
  entrar: (tenantId: string, email: string, senha: string) => Promise<void>;
  sair: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function sessaoDoToken(token: string): Sessao | null {
  const claims = decodeJwt(token);
  if (!claims) return null;

  const papeis = papeisDoToken(claims);
  return { claims, papeis, ehAtendenteOuSupervisor: papeis.includes("Atendente") || papeis.includes("Supervisor") };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [sessao, setSessao] = useState<Sessao | null>(() => {
    const token = getToken();
    return token ? sessaoDoToken(token) : null;
  });

  const value = useMemo<AuthContextValue>(
    () => ({
      sessao,
      async entrar(tenantId, email, senha) {
        const { data, error } = await api.POST("/api/auth/login", {
          body: { tenantId, email, senha },
        });
        if (error || !data?.token) throw new Error("E-mail, senha ou empresa inválidos.");

        setToken(data.token);
        setSessao(sessaoDoToken(data.token));
      },
      sair() {
        limparToken();
        setSessao(null);
      },
    }),
    [sessao],
  );

  return <AuthContext value={value}>{children}</AuthContext>;
}

export function useAuth(): AuthContextValue {
  const context = use(AuthContext);
  if (!context) throw new Error("useAuth precisa estar dentro de <AuthProvider>.");
  return context;
}
