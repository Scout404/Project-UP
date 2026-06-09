const configuredApiUrl = process.env.REACT_APP_API_URL?.trim();
const apiPort = process.env.REACT_APP_API_PORT?.trim() || "5050";
const devServerApiUrl = `${window.location.protocol}//${window.location.hostname}:${apiPort}`;
const reactDevPorts = new Set(["3000", "3007"]);
const inferredApiUrl = reactDevPorts.has(window.location.port) ? devServerApiUrl : "";

export const API_BASE_URL =
  configuredApiUrl ||
  inferredApiUrl;

export function apiUrl(path) {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return API_BASE_URL ? `${API_BASE_URL}${normalizedPath}` : normalizedPath;
}



