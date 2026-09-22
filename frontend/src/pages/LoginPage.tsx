import { useState, type FormEvent } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

// Tenant e credenciais do seed de desenvolvimento (README). Só pré-preenchido para
// facilitar testar localmente; não existe fluxo de escolha de empresa ainda.
const TENANT_DEMO = "11111111-1111-1111-1111-111111111111";

export function LoginPage() {
  const { sessao, entrar } = useAuth();
  const navigate = useNavigate();

  const [tenantId, setTenantId] = useState(TENANT_DEMO);
  const [email, setEmail] = useState("cliente@helpdesk.local");
  const [senha, setSenha] = useState("Demo123$");
  const [erro, setErro] = useState<string | null>(null);
  const [carregando, setCarregando] = useState(false);

  if (sessao) return <Navigate to="/" replace />;

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      await entrar(tenantId, email, senha);
      navigate("/");
    } catch (err) {
      setErro(err instanceof Error ? err.message : "Falha ao entrar.");
    } finally {
      setCarregando(false);
    }
  }

  return (
    <div className="card" style={{ maxWidth: 380, margin: "2rem auto" }}>
      <h2>Entrar</h2>
      <form onSubmit={onSubmit}>
        <div className="campo">
          <label htmlFor="tenantId">Empresa (tenantId)</label>
          <input id="tenantId" value={tenantId} onChange={(e) => setTenantId(e.target.value)} required />
        </div>
        <div className="campo">
          <label htmlFor="email">E-mail</label>
          <input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </div>
        <div className="campo">
          <label htmlFor="senha">Senha</label>
          <input
            id="senha"
            type="password"
            value={senha}
            onChange={(e) => setSenha(e.target.value)}
            required
          />
        </div>
        {erro && <p className="erro">{erro}</p>}
        <button className="botao" type="submit" disabled={carregando}>
          {carregando ? "Entrando..." : "Entrar"}
        </button>
      </form>
      <p className="credenciais-demo">
        Usuários de exemplo (seed de dev): <code>cliente@helpdesk.local</code>,{" "}
        <code>atendente@helpdesk.local</code> ou <code>supervisor@helpdesk.local</code>, senha{" "}
        <code>Demo123$</code>.
      </p>
    </div>
  );
}
