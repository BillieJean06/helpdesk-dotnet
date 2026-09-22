import createClient from "openapi-fetch";
import type { paths } from "./schema";
import { getToken, logout } from "../auth/token";

const baseUrl = import.meta.env.VITE_API_URL ?? "http://localhost:5099";

export const api = createClient<paths>({ baseUrl });

// Injeta o Bearer token em toda chamada; em 401, o token não serve mais (expirou ou é
// inválido), então desloga em vez de deixar a UI presa numa sessão morta.
api.use({
  onRequest({ request }) {
    const token = getToken();
    if (token) request.headers.set("Authorization", `Bearer ${token}`);
    return request;
  },
  onResponse({ response }) {
    if (response.status === 401) logout();
    return response;
  },
});
