import { useEffect, useState, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, Chip } from '@/components/shared/UI';
import { useCameraFrame, useDetections, useEnvironment } from '@/hooks/useApi';
import { tokenStore } from '@/services/api';
import styles from './CameraPanel.module.css';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '';

/** Cere un stream token de la backend (valid 60s), returneaza URL-ul complet al stream-ului. */
async function fetchStreamUrl(): Promise<string> {
  const accessToken = tokenStore.getAccess();
  const res = await fetch(`${API_BASE}/api/camera/token`, {
    headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
  });
  if (!res.ok) throw new Error(`Token error: ${res.status}`);
  const { token } = await res.json();
  return `${API_BASE}/api/camera/stream?token=${token}`;
}

export function CameraPanel() {
  const { t } = useTranslation();
  const { data: frame } = useCameraFrame();
  const { data: detections } = useDetections(1);
  const { data: env } = useEnvironment();

  const [streamUrl, setStreamUrl] = useState<string | null>(null);
  const [streamError, setStreamError] = useState(false);
  const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  // ── GPS live de pe Jetson ──────────────────────────────
  const [liveGps, setLiveGps] = useState<{ lat: number; lon: number } | null>(null);

  useEffect(() => {
    if (!API_BASE) return;

    const fetchGps = async () => {
      try {
        const accessToken = tokenStore.getAccess();
        const res = await fetch(`${API_BASE}/api/camera/gps`, {
          headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
        });
        if (res.ok) {
          const data = await res.json();
          if (data.fix && data.lat && data.lon) {
            setLiveGps({ lat: data.lat, lon: data.lon });
          }
        }
      } catch { /* Jetson offline */ }
    };

    fetchGps();
    const interval = setInterval(fetchGps, 3000);
    return () => clearInterval(interval);
  }, []);

  // ── Stream token ───────────────────────────────────────
  const refreshStream = () => {
    fetchStreamUrl()
      .then((url) => {
        setStreamUrl(url);
        setStreamError(false);
        refreshTimer.current = setTimeout(refreshStream, 55_000);
      })
      .catch(() => {
        setStreamError(true);
        refreshTimer.current = setTimeout(refreshStream, 5_000);
      });
  };

  useEffect(() => {
    refreshStream();
    return () => {
      if (refreshTimer.current) clearTimeout(refreshTimer.current);
    };
  }, []);

  const latest = detections?.[0] ?? null;
  const hasDisease = latest?.severity === 'critical' || latest?.severity === 'warning';
  const pos = frame?.position;
  const diseaseCount = detections?.filter(d => d.severity !== 'healthy').length ?? 1;

  // Coordonate finale: GPS live de pe Jetson, fallback la mock
  const displayLat = liveGps?.lat ?? pos?.gps.lat;
  const displayLon = liveGps?.lon ?? pos?.gps.lon;

  return (
    <div className={styles.wrapper}>
      {/* Live feed */}
      <Card>
        <CardHeader
          title={t('camera.title')}
          subtitle={t('camera.rowSubtitle', {
            row: pos?.row ?? '—',
            pos: pos?.positionMeters?.toFixed(1) ?? '—',
            height: pos?.heightMeters?.toFixed(1) ?? '—',
          })}
          right={<Chip label={hasDisease ? t('camera.diseaseFound') : t('camera.allClear')} variant={hasDisease ? 'red' : 'green'} />}
        />

        {/* Camera preview */}
        <div className={styles.camBox}>
          {streamError && (
            <div style={{
              position: 'absolute', inset: 0, display: 'flex',
              alignItems: 'center', justifyContent: 'center',
              color: '#f87171', fontSize: 14, zIndex: 2,
            }}>
              Camera offline
            </div>
          )}

          {streamUrl && !streamError && (
            <img
              src={streamUrl}
              className={styles.camStream}
              alt="Live camera"
              onError={() => {
                setStreamError(true);
                setTimeout(refreshStream, 3_000);
              }}
            />
          )}

          {/* Bounding box overlay */}
          {hasDisease && latest && (
            <div
              className={styles.bbox}
              style={{
                left: `${latest.boundingBox.x * 100}%`,
                top: `${latest.boundingBox.y * 100}%`,
                width: `${latest.boundingBox.width * 100}%`,
                height: `${latest.boundingBox.height * 100}%`,
              }}
            >
              <span className={styles.bboxLabel}>
                {latest.topPrediction.label.split('(')[0].trim()}{' '}
                {(latest.topPrediction.confidence * 100).toFixed(1)}%
              </span>
            </div>
          )}

          {/* HUD overlay */}
          <div className={styles.hud}>
            <div className={styles.hudTop}>
              <span className={styles.hudChip}>
                ZED 2 · {frame?.resolution ?? '1080p'} · {frame?.fps ?? 30}fps · stereo
              </span>
              {frame?.isRecording && (
                <span className={styles.recChip}>
                  <span className={styles.recDot} />REC
                </span>
              )}
            </div>
            <div className={styles.hudBottom}>
              <span className={styles.hudGps}>
                {displayLat?.toFixed(4) ?? '—'}°N {displayLon?.toFixed(4) ?? '—'}°E · Row {pos?.row} · {pos?.positionMeters?.toFixed(1)}m
              </span>
              {hasDisease && (
                <span className={styles.hudFound}>
                  <span className={styles.hudFoundDot} />
                  {t('camera.disease', { count: diseaseCount })}
                </span>
              )}
            </div>
          </div>
        </div>

        {/* GPS / position strip */}
        <div className={styles.posGrid}>
          <div className={styles.posItem}>
            <div className={styles.posVal}>{displayLat?.toFixed(6) ?? '—'}°N</div>
            <div className={styles.posLbl}>{t('camera.latitude')}</div>
          </div>
          <div className={styles.posItem}>
            <div className={styles.posVal}>{displayLon?.toFixed(6) ?? '—'}°E</div>
            <div className={styles.posLbl}>{t('camera.longitude')}</div>
          </div>
          <div className={styles.posItem}>
            <div className={styles.posVal}>{pos?.positionMeters?.toFixed(1) ?? '—'} m</div>
            <div className={styles.posLbl}>{t('camera.standPos')}</div>
          </div>
        </div>
      </Card>

      {/* Model output */}
      <Card>
        <CardHeader title={t('camera.modelTitle')} right={<Chip label={t('camera.modelOutput')} />} />

        <div className={styles.predictions}>
          {(latest?.allPredictions ?? []).map((p) => (
            <div key={p.diseaseClass} className={styles.predRow}>
              <span className={styles.predName}>
                {p.diseaseClass === 'healthy' ? t('detection.severity.healthy') : p.label}
              </span>
              <div className={styles.predBarWrap}>
                <div className={styles.predBar}>
                  <div
                    className={styles.predFill}
                    style={{
                      width: `${p.confidence * 100}%`,
                      background: p.diseaseClass === 'healthy' ? 'var(--forest-3)' : p.confidence > 0.6 ? '#ef4444' : '#f59e0b',
                    }}
                  />
                </div>
                <span
                  className={styles.predPct}
                  style={{ color: p.confidence > 0.6 && p.diseaseClass !== 'healthy' ? '#b91c1c' : 'var(--txt-2)' }}
                >
                  {(p.confidence * 100).toFixed(0)}%
                </span>
              </div>
            </div>
          ))}
        </div>

        <div className={styles.detailTable}>
          <div className={styles.dtRow}>
            <span className={styles.dtLabel}>{t('camera.boundingBoxes')}</span>
            <span className={styles.dtVal}>{t('camera.detected', { count: latest ? 1 : 0 })}</span>
          </div>
          <div className={styles.dtRow}>
            <span className={styles.dtLabel}>{t('camera.depth')}</span>
            <span className={styles.dtVal}>{t('camera.depthVal', { val: latest?.boundingBox.depthMeters.toFixed(2) ?? '—' })}</span>
          </div>
          <div className={styles.dtRow}>
            <span className={styles.dtLabel}>{t('camera.leafArea')}</span>
            <span className={styles.dtVal}>{t('camera.leafAreaVal', { val: latest?.boundingBox.affectedAreaPercent ?? 0 })}</span>
          </div>
          <div className={styles.dtRow}>
            <span className={styles.dtLabel}>{t('camera.confidenceGate')}</span>
            <span className={styles.dtVal} style={{ color: 'var(--forest)' }}>
              {latest?.confidenceGatePassed ? t('camera.pass') : t('camera.fail')}
            </span>
          </div>
          <div className={styles.dtRow}>
            <span className={styles.dtLabel}>{t('camera.inferenceTime')}</span>
            <span className={styles.dtVal}>{t('camera.inferenceVal', { val: latest?.inferenceMs ?? '—' })}</span>
          </div>
        </div>

        {env && (
          <div className={styles.envRow}>
            <div className={styles.envItem}>
              <div className={styles.envVal}>{env.temperatureC.toFixed(1)}°C</div>
              <div className={styles.envLbl}>{t('camera.temperature')}</div>
              <div className={styles.envBar}>
                <div className={styles.envFill} style={{ width: `${(env.temperatureC / 40) * 100}%` }} />
              </div>
            </div>
            <div className={styles.envItem}>
              <div className={styles.envVal}>{env.humidityPercent.toFixed(0)}%</div>
              <div className={styles.envLbl}>{t('camera.humidity')}</div>
              <div className={styles.envBar}>
                <div className={styles.envFill} style={{ width: `${env.humidityPercent}%` }} />
              </div>
            </div>
          </div>
        )}
      </Card>
    </div>
  );
}