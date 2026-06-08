import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, Chip } from '@/components/shared/UI';
import { useCameraFrame, useDetections } from '@/hooks/useApi';
import { api, API_BASE, tokenStore } from '@/services/api';
import styles from './CameraPanel.module.css';

export function CameraPanel() {
  const { t } = useTranslation();
  const { data: frame } = useCameraFrame();
  const { data: detections } = useDetections(1);

  const [streamUrl, setStreamUrl] = useState<string | null>(null);
  const [streamError, setStreamError] = useState(false);
  const [liveGps, setLiveGps] = useState<{ lat: number; lon: number } | null>(null);
  const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  // ── MJPEG stream: fetch a single-use token, build the URL, refresh before expiry ──
  useEffect(() => {
    let cancelled = false;

    const refreshStream = () => {
      api.getCameraStreamToken()
        .then(({ token, expiresInSeconds }) => {
          if (cancelled) return;
          setStreamUrl(api.cameraStreamUrl(token));
          setStreamError(false);
          // refresh a few seconds before the token expires
          refreshTimer.current = setTimeout(refreshStream, Math.max(5, expiresInSeconds - 5) * 1000);
        })
        .catch(() => {
          if (cancelled) return;
          setStreamError(true);
          refreshTimer.current = setTimeout(refreshStream, 5_000);
        });
    };

    refreshStream();
    return () => {
      cancelled = true;
      if (refreshTimer.current) clearTimeout(refreshTimer.current);
    };
  }, []);

  // ── Live GPS proxied from the Jetson (best-effort; ignored when offline) ──
  useEffect(() => {
    let active = true;
    const fetchGps = async () => {
      try {
        const token = tokenStore.getAccess();
        const res = await fetch(`${API_BASE}/api/camera/gps`, {
          headers: token ? { Authorization: `Bearer ${token}` } : {},
        });
        if (!res.ok) return;
        const data = await res.json();
        if (active && data?.fix && typeof data.lat === 'number' && typeof data.lon === 'number') {
          setLiveGps({ lat: data.lat, lon: data.lon });
        }
      } catch {
        /* device offline — keep last value */
      }
    };
    fetchGps();
    const id = setInterval(fetchGps, 3_000);
    return () => { active = false; clearInterval(id); };
  }, []);

  const latest = detections?.[0] ?? null;
  const hasDisease = latest?.severity === 'critical' || latest?.severity === 'warning';
  const pos = frame?.position;
  const diseaseCount = detections?.filter((d) => d.severity !== 'healthy').length ?? 0;

  const lat = liveGps?.lat ?? pos?.gps?.lat ?? null;
  const lon = liveGps?.lon ?? pos?.gps?.lon ?? null;

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

        <div className={styles.camBox}>
          <div className={styles.camFoliage} />

          {streamError && (
            <div style={{
              position: 'absolute', inset: 0, display: 'flex',
              alignItems: 'center', justifyContent: 'center',
              color: '#f87171', fontSize: 14, zIndex: 2,
            }}>
              {t('camera.offline')}
            </div>
          )}

          {streamUrl && !streamError && (
            <img
              src={streamUrl}
              className={styles.camStream}
              alt={t('camera.title')}
              onError={() => setStreamError(true)}
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
              <span className={styles.hudChip}>ZED 2 · {t('camera.stereo')}</span>
            </div>
            <div className={styles.hudBottom}>
              <span className={styles.hudGps}>
                {lat != null ? `${lat.toFixed(4)}°N ` : '— '}
                {lon != null ? `${lon.toFixed(4)}°E · ` : '— · '}
                {t('camera.hudRow', { row: pos?.row ?? '—' })} · {pos?.positionMeters?.toFixed(1) ?? '—'}m
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

        {/* Position strip */}
        <div className={styles.posGrid}>
          <div className={styles.posItem}>
            <div className={styles.posVal}>{lat != null ? `${lat.toFixed(4)}°N` : '—'}</div>
            <div className={styles.posLbl}>{t('camera.latitude')}</div>
          </div>
          <div className={styles.posItem}>
            <div className={styles.posVal}>{lon != null ? `${lon.toFixed(4)}°E` : '—'}</div>
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
          {!latest && <p style={{ color: 'var(--txt-3)', fontSize: 13 }}>{t('camera.noModelOutput')}</p>}
        </div>

        {latest && (
          <div className={styles.detailTable}>
            <div className={styles.dtRow}>
              <span className={styles.dtLabel}>{t('camera.boundingBoxes')}</span>
              <span className={styles.dtVal}>{t('camera.detected', { count: 1 })}</span>
            </div>
            <div className={styles.dtRow}>
              <span className={styles.dtLabel}>{t('camera.depth')}</span>
              <span className={styles.dtVal}>{t('camera.depthVal', { val: latest.boundingBox.depthMeters.toFixed(2) })}</span>
            </div>
            <div className={styles.dtRow}>
              <span className={styles.dtLabel}>{t('camera.leafArea')}</span>
              <span className={styles.dtVal}>{t('camera.leafAreaVal', { val: latest.boundingBox.affectedAreaPercent })}</span>
            </div>
            <div className={styles.dtRow}>
              <span className={styles.dtLabel}>{t('camera.confidenceGate')}</span>
              <span className={styles.dtVal} style={{ color: 'var(--forest)' }}>
                {latest.confidenceGatePassed ? t('camera.pass') : t('camera.fail')}
              </span>
            </div>
            <div className={styles.dtRow}>
              <span className={styles.dtLabel}>{t('camera.inferenceTime')}</span>
              <span className={styles.dtVal}>{t('camera.inferenceVal', { val: latest.inferenceMs })}</span>
            </div>
          </div>
        )}
      </Card>
    </div>
  );
}
