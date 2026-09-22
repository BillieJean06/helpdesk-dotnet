// localStorage é simples e suficiente para este projeto de estudo, mas fica exposto a
// XSS (qualquer script injetado na página lê o token). Um app de produção real preferiria
// um cookie HttpOnly, que o JavaScript não consegue ler — troca deliberada aqui, documentada.
const CHAVE_TOKEN = "helpdesk:token";

export function getToken(): string | null {
  return localStorage.getItem(CHAVE_TOKEN);
}

export function setToken(token: string): void {
  localStorage.setItem(CHAVE_TOKEN, token);
}

export function logout(): void {
  localStorage.removeItem(CHAVE_TOKEN);
  if (location.pathname !== "/login") location.assign("/login");
}
