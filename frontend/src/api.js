const configuredApiUrl = process.env.REACT_APP_API_URL?.trim();
const apiPort = process.env.REACT_APP_API_PORT?.trim() || "5050";

export const API_BASE_URL =
  configuredApiUrl ||
  `${window.location.protocol}//${window.location.hostname}:${apiPort}`;

export function apiUrl(path) {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${API_BASE_URL}${normalizedPath}`;
}
