/** Claims que a Api emite no JWT (ver Helpdesk.Api/Auth/JwtTokenService.cs). */
export interface JwtClaims {
  sub: string;
  email: string;
  tenant_id: string;
  role?: string | string[];
  exp: number;
}

/**
 * Decodifica o payload do JWT só para ler claims no cliente (papel, nome, tenant) e
 * ajustar a UI. Isto NÃO valida a assinatura — quem garante que o token é legítimo é
 * sempre a Api, em cada requisição; o front confia porque foi ele mesmo que acabou de
 * receber o token do /api/auth/login.
 */
export function decodeJwt(token: string): JwtClaims | null {
  try {
    const payload = token.split(".")[1];
    return JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/")));
  } catch {
    return null;
  }
}

export function papeisDoToken(claims: JwtClaims): string[] {
  if (!claims.role) return [];
  return Array.isArray(claims.role) ? claims.role : [claims.role];
}
