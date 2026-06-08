/// <reference types="vite/client" />

import type {
  Detection,
  DashboardStats,
  StandPosition,
  CameraFrame,
  StreamToken,
  PlantSummary,
  PlantPassport,
  Treatment,
  AppliedTreatment,
  RecordAppliedTreatmentRequest,
  SystemStatus,
  CreateDetectionRequest,
  UpdateDetectionRequest,
  ApiKey,
  ApiKeyCreated,
  CreateApiKeyRequest,
} from "@/types";
import { authApi } from "@/services/auth";

// Empty base = same-origin (nginx proxies /api to the backend in production).
// Set VITE_API_BASE_URL for split-origin dev. There is no mock mode — every call
// hits the real API.
export const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "";

// ── Token storage ─────────────────────────────────────────

const ACCESS_KEY  = "agricure_access_token";
const REFRESH_KEY = "agricure_refresh_token";

export const tokenStore = {
  getAccess:  () => localStorage.getItem(ACCESS_KEY),
  getRefresh: () => localStorage.getItem(REFRESH_KEY),
  set: (access: string, refresh: string) => {
    localStorage.setItem(ACCESS_KEY, access);
    localStorage.setItem(REFRESH_KEY, refresh);
  },
  clear: () => {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    window.dispatchEvent(new Event("agricure:logout"));
  },
};

export function isAccessTokenExpired(): boolean {
  const token = tokenStore.getAccess();
  if (!token) return true;
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    // 10s buffer to avoid edge cases near expiry
    return Date.now() >= payload.exp * 1000 - 10_000;
  } catch {
    return true;
  }
}

export class AuthError extends Error {
  constructor() {
    super("Unauthorized");
    this.name = "AuthError";
  }
}

// ── Generic fetch helper ──────────────────────────────────

interface FetchOptions {
  method?: string;
  body?: unknown;
}

async function apiFetch<T>(path: string, options?: FetchOptions, retried = false): Promise<T> {
  const token = tokenStore.getAccess();
  const method = options?.method ?? "GET";
  const headers: Record<string, string> = {};
  if (token) headers["Authorization"] = `Bearer ${token}`;
  if (options?.body !== undefined) headers["Content-Type"] = "application/json";

  const res = await fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body: options?.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (res.status === 401 && !retried) {
    const refresh = tokenStore.getRefresh();
    if (refresh) {
      try {
        const result = await authApi.refresh(refresh);
        tokenStore.set(result.accessToken, result.refreshToken);
        return apiFetch<T>(path, options, true);
      } catch {
        // refresh failed — fall through to clear + throw
      }
    }
    tokenStore.clear();
    throw new AuthError();
  }

  if (!res.ok) throw new Error(`API error ${res.status}: ${path}`);
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

// ── API functions ─────────────────────────────────────────

export const api = {
  /** GET /api/system/status */
  getSystemStatus: () => apiFetch<SystemStatus>("/api/system/status"),

  /** GET /api/dashboard/stats */
  getDashboardStats: () => apiFetch<DashboardStats>("/api/dashboard/stats"),

  /** GET /api/camera/frame — latest frame metadata */
  getCameraFrame: () => apiFetch<CameraFrame>("/api/camera/frame"),

  /** GET /api/camera/token — short-lived stream token */
  getCameraStreamToken: () => apiFetch<StreamToken>("/api/camera/token"),

  /** Full URL for the MJPEG stream, authenticated by a token from getCameraStreamToken(). */
  cameraStreamUrl: (token: string) => `${API_BASE}/api/camera/stream?token=${encodeURIComponent(token)}`,

  /** GET /api/stand/position — current stand row + position (real fields only) */
  getStandPosition: () => apiFetch<StandPosition>("/api/stand/position"),

  /** GET /api/detections?limit=20 — recent detections */
  getDetections: (limit = 20) =>
    apiFetch<Detection[]>(`/api/detections?limit=${limit}`),

  /** GET /api/detections/:id */
  getDetection: (id: string) => apiFetch<Detection>(`/api/detections/${id}`),

  /** POST /api/detections */
  createDetection: (data: CreateDetectionRequest) =>
    apiFetch<{ id: string }>("/api/detections", { method: "POST", body: data }),

  /** PUT /api/detections/:id */
  updateDetection: (id: string, data: UpdateDetectionRequest) =>
    apiFetch<void>(`/api/detections/${id}`, { method: "PUT", body: data }),

  /** DELETE /api/detections/:id */
  deleteDetection: (id: string) =>
    apiFetch<void>(`/api/detections/${id}`, { method: "DELETE" }),

  /** GET /api/plants — all visible plants with latest disease status */
  getPlants: () => apiFetch<PlantSummary[]>("/api/plants"),

  /** GET /api/passports/:plantId */
  getPassport: (plantId: string) =>
    apiFetch<PlantPassport>(`/api/passports/${encodeURIComponent(plantId)}`),

  /** GET /api/treatments?diseaseClass=late_blight — recommendation catalog */
  getTreatments: (diseaseClass: string) =>
    apiFetch<Treatment[]>(`/api/treatments?diseaseClass=${encodeURIComponent(diseaseClass)}`),

  /** GET /api/treatments/applied — applied-treatment history */
  getAppliedTreatments: (plantId?: string, limit = 50) =>
    apiFetch<AppliedTreatment[]>(
      `/api/treatments/applied?limit=${limit}${plantId ? `&plantId=${encodeURIComponent(plantId)}` : ""}`),

  /** POST /api/treatments/applied — record a treatment application */
  recordAppliedTreatment: (data: RecordAppliedTreatmentRequest) =>
    apiFetch<{ id: string }>("/api/treatments/applied", { method: "POST", body: data }),

  // ── Admin: ingestion API keys ──────────────────────────

  /** GET /api/admin/api-keys */
  getApiKeys: (includeRevoked = false) =>
    apiFetch<ApiKey[]>(`/api/admin/api-keys?includeRevoked=${includeRevoked}`),

  /** POST /api/admin/api-keys — returns the plaintext key once */
  createApiKey: (data: CreateApiKeyRequest) =>
    apiFetch<ApiKeyCreated>("/api/admin/api-keys", { method: "POST", body: data }),

  /** DELETE /api/admin/api-keys/:id — revoke (soft delete) */
  revokeApiKey: (id: string) =>
    apiFetch<void>(`/api/admin/api-keys/${id}`, { method: "DELETE" }),
};
