import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, Chip } from '@/components/shared/UI';
import { useDetections } from '@/hooks/useApi';
import type { Detection, PlantPassport } from '@/types';
import { formatDistanceToNow } from 'date-fns';
import { ro, ru, enUS } from 'date-fns/locale';
import styles from './Detection.module.css';

function useDateLocale() {
  const { i18n } = useTranslation();
  if (i18n.language === 'ro') return ro;
  if (i18n.language === 'ru') return ru;
  return enUS;
}

// ── Detection list ────────────────────────────────────────

export function DetectionList() {
  const { t } = useTranslation();
  const { data: detections, loading } = useDetections(10);

  if (loading) return <Card><p style={{ color: 'var(--txt-3)', fontSize: 13 }}>{t('detection.loading')}</p></Card>;

  return (
    <Card>
      <CardHeader title={t('detection.title')} right={<Chip label={t('detection.today')} />} />
      <div className={styles.list}>
        {(detections ?? []).map((d) => (
          <DetectionItem key={d.id} detection={d} />
        ))}
      </div>
    </Card>
  );
}

function DetectionItem({ detection: d }: { detection: Detection }) {
  const { t } = useTranslation();
  const dateLocale = useDateLocale();
  const variantMap = { critical: 'r', warning: 'a', healthy: 'g' } as const;
  const v = variantMap[d.severity];

  return (
    <div className={[styles.item, styles[`item-${v}`]].join(' ')}>
      <div className={[styles.itemIcon, styles[`icon-${v}`]].join(' ')}>
        {v === 'r' && (
          <svg viewBox="0 0 16 16" fill="none"><circle cx="8" cy="8" r="6" fill="#ef4444" /><path d="M8 5v4M8 10.5v.5" stroke="white" strokeWidth="1.5" strokeLinecap="round" /></svg>
        )}
        {v === 'a' && (
          <svg viewBox="0 0 16 16" fill="none"><path d="M8 2L2 13h12L8 2z" fill="#f59e0b" /><path d="M8 7v3M8 11.5v.5" stroke="white" strokeWidth="1.2" strokeLinecap="round" /></svg>
        )}
        {v === 'g' && (
          <svg viewBox="0 0 16 16" fill="none"><circle cx="8" cy="8" r="6" fill="#52b788" /><path d="M5 8l2 2 4-4" stroke="white" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>
        )}
      </div>
      <div className={styles.itemBody}>
        <div className={styles.itemName}>
          {d.topPrediction.diseaseClass === 'healthy'
            ? t('detection.severity.healthy')
            : d.topPrediction.label}
        </div>
        <div className={styles.itemMeta}>
          {t('detection.itemMeta', {
            row: d.row,
            plant: d.plantId,
            depth: d.boundingBox.depthMeters.toFixed(2),
          })} · {formatDistanceToNow(new Date(d.timestamp), { addSuffix: true, locale: dateLocale })}
        </div>
      </div>
      {d.severity !== 'healthy' && (
        <div
          className={styles.itemConf}
          style={{ color: v === 'r' ? '#b91c1c' : '#92400e' }}
        >
          {(d.topPrediction.confidence * 100).toFixed(0)}%
        </div>
      )}
    </div>
  );
}

// ── Plant passport timeline ───────────────────────────────

export function PassportTimeline({ passport }: { passport: PlantPassport }) {
  const { t } = useTranslation();
  const dateLocale = useDateLocale();

  // Lightbox: the URL of the frame currently enlarged, or null when closed.
  const [lightbox, setLightbox] = useState<string | null>(null);

  useEffect(() => {
    if (!lightbox) return;
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') setLightbox(null); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [lightbox]);

  const typeColor: Record<string, string> = {
    disease: 'r',
    symptom: 'a',
    healthy: 'g',
    created: 'b',
    treatment: 'g',
    resolved: 'g',
  };

  return (
    <Card>
      <CardHeader
        title={t('detection.passport.title', {
          id: passport.plantIndex.toString().padStart(3, '0'),
          row: passport.row,
        })}
        right={
          <Chip
            label={passport.currentStatus === 'critical' ? t('detection.passport.diseaseActive') : t('detection.passport.healthy')}
            variant={passport.currentStatus === 'critical' ? 'red' : 'green'}
          />
        }
      />

      {/* Severity sparkline */}
      <div className={styles.sparkWrap}>
        {passport.severityHistory.map((pt, i) => (
          <div
            key={i}
            className={styles.sparkBar}
            style={{
              height: `${pt.value}%`,
              background:
                pt.value > 70 ? '#dc2626'
                : pt.value > 40 ? '#ef4444'
                : pt.value > 20 ? '#fca5a5'
                : 'var(--forest-5)',
            }}
          />
        ))}
      </div>
      <div className={styles.sparkLabels}>
        <span>{t('detection.passport.sparkStart')}</span>
        <span>{t('detection.passport.sparkEnd')}</span>
      </div>

      {/* Events */}
      <div className={styles.timeline}>
        {passport.events.map((ev, idx) => {
          const c = typeColor[ev.type] ?? 'g';
          const isLast = idx === passport.events.length - 1;
          return (
            <div key={ev.id} className={styles.tlItem}>
              <div className={styles.tlLeft}>
                <div className={[styles.tlDot, styles[`dot-${c}`]].join(' ')}>
                  <svg viewBox="0 0 10 10"><circle cx="5" cy="5" r="3" fill={
                    c === 'r' ? '#ef4444' : c === 'a' ? '#f59e0b' : c === 'b' ? '#3b82f6' : '#52b788'
                  } /></svg>
                </div>
                {!isLast && <div className={styles.tlLine} />}
              </div>
              <div className={styles.tlBody}>
                <div className={styles.tlTitle}>
                  {ev.titleKey ? t(ev.titleKey) : ev.title}
                </div>
                <div className={styles.tlDesc}>
                  {ev.descKey ? t(ev.descKey, ev.descParams) : ev.description}
                </div>
                {ev.imageUrl && (
                  <button
                    type="button"
                    className={styles.tlThumb}
                    onClick={() => setLightbox(ev.imageUrl!)}
                    title={t('detection.passport.viewPhoto')}
                  >
                    <img src={ev.imageUrl} alt={ev.titleKey ? t(ev.titleKey) : ev.title} loading="lazy" />
                  </button>
                )}
                <div className={styles.tlTime}>
                  {formatDistanceToNow(new Date(ev.timestamp), { addSuffix: true, locale: dateLocale })}
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {lightbox && (
        <div className={styles.lightbox} onClick={() => setLightbox(null)} role="dialog" aria-modal="true">
          <img src={lightbox} alt={t('detection.passport.viewPhoto')} onClick={(e) => e.stopPropagation()} />
        </div>
      )}
    </Card>
  );
}
