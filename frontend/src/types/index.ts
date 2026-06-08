export interface AuthRequest {
  email: string;
  password: string;
}

export interface AuthResult {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

// ── Camera & Position ────────────────────────────────────
// Only fields the backend can derive from real detection data are populated.
// GPS / height / speed / fps come from the live device and are null when absent.

export interface GpsCoordinates {
  lat: number;
  lon: number;
}

export interface StandPosition {
  gps: GpsCoordinates | null;
  row: number | null;
  totalRows: number | null;
  positionMeters: number | null;
  heightMeters: number | null;
  speedMs: number | null;
}

export interface CameraFrame {
  frameId: number | null;
  timestamp: string | null;
  depthMeters: number | null;
  position: StandPosition;
}

/** Short-lived, single-use token for the MJPEG camera stream. */
export interface StreamToken {
  token: string;
  expiresInSeconds: number;
}

// ── Detection ────────────────────────────────────────────

export type DiseaseClass =
  | 'late_blight'
  | 'early_blight'
  | 'fusarium_wilt'
  | 'powdery_mildew'
  | 'bacterial_spot'
  | 'leaf_mold'
  | 'septoria_leaf_spot'
  | 'spider_mites'
  | 'healthy';

export type DetectionSeverity = 'critical' | 'warning' | 'healthy';

export interface ClassPrediction {
  diseaseClass: DiseaseClass;
  confidence: number; // 0–1
  label: string;
}

export interface BoundingBox {
  x: number;
  y: number;
  width: number;
  height: number;
  depthMeters: number;
  affectedAreaPercent: number;
}

export interface Detection {
  id: string;
  frameId: number;
  timestamp: string;
  severity: DetectionSeverity;
  topPrediction: ClassPrediction;
  allPredictions: ClassPrediction[];
  boundingBox: BoundingBox;
  inferenceMs: number;
  confidenceGatePassed: boolean; // >= 60%
  row: number;
  plantId: string;
  positionMeters: number;
}

// ── Detection write DTOs ─────────────────────────────────

export interface DetectionWriteBase {
  frameId: number;
  timestamp: string;
  severity: DetectionSeverity;
  predictions: ClassPrediction[];
  boundingBox: BoundingBox;
  inferenceMs: number;
  confidenceGatePassed: boolean;
  row: number;
  plantId: string;
  positionMeters: number;
}

export type CreateDetectionRequest = DetectionWriteBase;

export interface UpdateDetectionRequest extends DetectionWriteBase {
  id: string;
}

// ── Plants ───────────────────────────────────────────────

/** One plant with its latest disease status, from GET /api/plants. */
export interface PlantSummary {
  plantId: string;
  row: number | null;
  latestSeverity: DetectionSeverity | null;
  latestLabel: string | null;
  latestDiseaseClass: DiseaseClass | null;
  lastSeenAt: string | null;
  detectionCount: number;
}

// ── Plant Passport ───────────────────────────────────────

export type PassportEventType = 'created' | 'healthy' | 'symptom' | 'disease' | 'treatment' | 'resolved';

export interface PassportEvent {
  id: string;
  type: PassportEventType;
  timestamp: string;
  title: string;
  description: string;
  detectionId?: string;
  confidence?: number;
  severity?: DetectionSeverity;
  titleKey?: string;
  descKey?: string;
  descParams?: Record<string, unknown>;
}

export interface PlantPassport {
  id: string;
  plantIndex: number;
  row: number;
  createdAt: string;
  currentStatus: DetectionSeverity;
  events: PassportEvent[];
  severityHistory: { date: string; value: number }[]; // 0–100
}

// ── Treatment ────────────────────────────────────────────

export type TreatmentType = 'biological' | 'chemical';

export interface Treatment {
  id: string;
  diseaseClass: DiseaseClass;
  name: string;
  type: TreatmentType;
  rank: number; // 1 = bio-first
  description: string;
  descriptionKey?: string;
  dosage: string;
  repeatAfterDays: number;
  phiDays: number; // pre-harvest interval
  costLevel: 'low' | 'medium' | 'high';
  tags: string[];
}

/** A record that a treatment was applied to a plant, from GET /api/treatments/applied. */
export interface AppliedTreatment {
  id: string;
  treatmentId: string;
  treatmentName: string;
  type: TreatmentType;
  diseaseClass: DiseaseClass;
  dosage: string;
  phiDays: number;
  plantId: string;
  row: number;
  appliedAt: string;
  notes: string | null;
}

export interface RecordAppliedTreatmentRequest {
  treatmentId: string;
  plantId: string;
  appliedAt: string;
  notes?: string | null;
}

// ── System ───────────────────────────────────────────────

export type DeviceStatus = 'online' | 'offline' | 'error';

export interface SystemStatus {
  deviceStatus: DeviceStatus;
  cameraConnected: boolean;
  gpsActive: boolean;
  modelLoaded: boolean;
  modelName: string | null;
  syncedAt: string | null;
  pendingAlerts: number;
}

// ── Dashboard summary ────────────────────────────────────

export interface DashboardStats {
  detectionsToday: number;
  detectionsDelta: number;
  avgConfidence: number;
  rowsScanned: number;
  totalRows: number;
  plantsTracked: number;
}

// ── Admin: ingestion API keys ────────────────────────────

export interface ApiKey {
  id: string;
  ownerUserId: string;
  name: string;
  tokenLast4: string;
  scope: string;
  createdAt: string;
  lastUsedAt: string | null;
  revokedAt: string | null;
  isActive: boolean;
}

/** Returned only at create time — the one time the plaintext key is visible. */
export interface ApiKeyCreated {
  id: string;
  ownerUserId: string;
  name: string;
  plaintextKey: string;
  tokenLast4: string;
  scope: string;
  createdAt: string;
}

export interface CreateApiKeyRequest {
  ownerUserId: string;
  name: string;
}

export interface User {
  id: string;
  email: string;
  roles: string[];
}
