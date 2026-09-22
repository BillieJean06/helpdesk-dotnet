import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "./AuthContext";

/** Papel exigido, se houver; sem sessão válida, redireciona sempre para /login. */
export function RequireAuth({ children, papel }: { children: ReactNode; papel?: string }) {
  const { sessao } = useAuth();

  if (!sessao) return <Navigate to="/login" replace />;
  if (papel && !sessao.papeis.includes(papel)) return <Navigate to="/" replace />;

  return children;
}
