import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function Layout({ children }: { children: ReactNode }) {
  const { sessao, sair } = useAuth();

  return (
    <>
      <header className="topbar">
        <h1>
          <Link to="/">Helpdesk</Link>
        </h1>
        {sessao && (
          <div className="usuario">
            <span>
              {sessao.claims.email} · {sessao.papeis.join(", ")}
            </span>
            <button className="botao-secundario" onClick={sair}>
              Sair
            </button>
          </div>
        )}
      </header>
      <main>{children}</main>
    </>
  );
}
